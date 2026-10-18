using System.Security.Claims;
using Cognify.Core.Entities;
using Cognify.Core.Enums;
using Cognify.Core.Interfaces;
using Cognify.Data;
using Cognify.Data.Repositories;
using Cognify.Services;
using Cognify.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Web.Controllers
{
    [Authorize]
    public class ExamAttemptController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AttemptRepository _attemptRepository;
        private readonly ExamRepository _examRepository;
        private readonly ScoringServiceFactory _scoringFactory;
        private readonly IConceptAnalyzer _conceptAnalyzer;

        public ExamAttemptController(
            AppDbContext context,
            AttemptRepository attemptRepository,
            ExamRepository examRepository,
            ScoringServiceFactory scoringFactory,
            IConceptAnalyzer conceptAnalyzer)
        {
            _context = context;
            _attemptRepository = attemptRepository;
            _examRepository = examRepository;
            _scoringFactory = scoringFactory;
            _conceptAnalyzer = conceptAnalyzer;
        }

        // GET: /ExamAttempt/Start/5
        public async Task<IActionResult> Start(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Account");

            var exam = await _examRepository.GetByIdAsync(id);
            if (exam == null)
                return RedirectToAction("Index", "Dashboard");

            // Check if student already attempted or has in-progress attempt
            var existingAttempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.ExamId == id && a.UserId == userId);

            if (existingAttempt != null)
            {
                if (existingAttempt.Status == AttemptStatus.Submitted || existingAttempt.Status == AttemptStatus.AutoSubmitted)
                {
                    return RedirectToAction("Details", "Result", new { attemptId = existingAttempt.AttemptId });
                }
                return RedirectToAction("Attempt", new { id = existingAttempt.AttemptId });
            }

            var attempt = await _attemptRepository.StartAsync(id, userId);
            return RedirectToAction("Attempt", new { id = attempt.AttemptId });
        }

        // GET: /ExamAttempt/Attempt/5
        public async Task<IActionResult> Attempt(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Account");

            var attempt = await _attemptRepository.GetByIdAsync(id);
            if (attempt == null || attempt.UserId != userId)
                return RedirectToAction("Index", "Dashboard");

            if (attempt.Status != AttemptStatus.InProgress)
                return RedirectToAction("Details", "Result", new { attemptId = attempt.AttemptId });

            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            if (exam == null)
                return RedirectToAction("Index", "Dashboard");

            var selectionService = _scoringFactory.GetSelectionService(exam);
            Question? nextQuestion = null;

            if (!attempt.Answers.Any())
            {
                nextQuestion = await selectionService.GetFirstQuestionAsync(attempt);
            }
            else
            {
                var lastAnswer = attempt.Answers.OrderBy(a => a.SequencePosition).Last();
                nextQuestion = await selectionService.GetNextQuestionAsync(attempt, lastAnswer);
            }

            // If no more questions, complete the exam
            if (nextQuestion == null)
            {
                return await FinishAttemptAsync(attempt, exam);
            }

            var vm = new ExamAttemptVM
            {
                AttemptId = attempt.AttemptId,
                ExamId = exam.ExamId,
                ExamTitle = exam.Title,
                QuestionId = nextQuestion.QuestionId,
                QuestionText = nextQuestion.Text,
                OptionA = nextQuestion.OptionA,
                OptionB = nextQuestion.OptionB,
                OptionC = nextQuestion.OptionC,
                OptionD = nextQuestion.OptionD,
                ShowConfidenceSlider = exam.IsConfidenceWeighted,
                CurrentQuestionNumber = attempt.Answers.Count + 1,
                SecondsRemaining = exam.DurationMinutes * 60
            };

            return View(vm);
        }

        // POST: /ExamAttempt/SubmitAnswer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAnswer(AnswerSubmitVM model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Account");

            var attempt = await _attemptRepository.GetByIdAsync(model.AttemptId);
            if (attempt == null || attempt.UserId != userId || attempt.Status != AttemptStatus.InProgress)
                return RedirectToAction("Index", "Dashboard");

            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            var question = await _context.Questions.FindAsync(model.QuestionId);
            if (exam == null || question == null)
                return RedirectToAction("Index", "Dashboard");

            var isCorrect = !string.IsNullOrEmpty(model.SelectedOption) &&
                            string.Equals(model.SelectedOption.Trim(), question.CorrectOption.Trim(), StringComparison.OrdinalIgnoreCase);

            var answer = new AttemptAnswer
            {
                AttemptId = attempt.AttemptId,
                QuestionId = question.QuestionId,
                SelectedOption = model.SelectedOption,
                ConfidenceLevel = exam.IsConfidenceWeighted ? (model.ConfidenceLevel ?? ConfidenceLevel.Medium) : null,
                IsCorrect = isCorrect,
                SequencePosition = attempt.Answers.Count + 1,
                DifficultyAtServeTime = question.DifficultyLevel
            };

            var scoringService = _scoringFactory.GetScoringService(exam);
            answer.MarksAwarded = scoringService.CalculateMarks(question, answer, exam.NegativeMarking);

            await _attemptRepository.SaveAnswerAsync(answer);

            // Update question stats
            question.TimesAttempted++;
            if (isCorrect) question.TimesCorrect++;
            await _context.SaveChangesAsync();

            return RedirectToAction("Attempt", new { id = attempt.AttemptId });
        }

        private async Task<IActionResult> FinishAttemptAsync(ExamAttempt attempt, Exam exam)
        {
            attempt.Status = AttemptStatus.Submitted;
            attempt.SubmitTime = DateTime.UtcNow;

            // Calculate total score
            attempt.TotalScore = Math.Max(0, attempt.Answers.Sum(a => a.MarksAwarded));

            // Calibration metrics if confidence-weighted
            if (exam.IsConfidenceWeighted)
            {
                var (over, under) = await _conceptAnalyzer.CalculateCalibrationAsync(attempt.AttemptId);
                attempt.OverconfidenceIndex = over;
                attempt.UnderconfidenceIndex = under;
            }

            // Concept breakdown
            var conceptPerformances = await _conceptAnalyzer.BuildConceptMapAsync(attempt.AttemptId);
            foreach (var cp in conceptPerformances)
            {
                contextConceptAdd(attempt.AttemptId, cp);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Result", new { attemptId = attempt.AttemptId });
        }

        private void contextConceptAdd(int attemptId, ConceptPerformance cp)
        {
            var existing = _context.ConceptPerformances
                .FirstOrDefault(p => p.AttemptId == attemptId && p.ConceptTagId == cp.ConceptTagId);
            if (existing == null)
            {
                _context.ConceptPerformances.Add(new ConceptPerformance
                {
                    AttemptId = attemptId,
                    ConceptTagId = cp.ConceptTagId,
                    TotalQuestions = cp.TotalQuestions,
                    CorrectCount = cp.CorrectCount
                });
            }
        }
    }
}
