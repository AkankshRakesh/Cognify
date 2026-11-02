using System.Security.Claims;
using Cognify.Core.Entities;
using Cognify.Core.Enums;
using Cognify.Core.Interfaces;
using Cognify.Data;
using Cognify.Data.Repositories;
using Cognify.Services;
using Cognify.Web.ViewModels;
using Cognify.Web.Security;
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
        private readonly IUrlTokenProtector _urlTokens;

        public ExamAttemptController(
            AppDbContext context,
            AttemptRepository attemptRepository,
            ExamRepository examRepository,
            ScoringServiceFactory scoringFactory,
            IConceptAnalyzer conceptAnalyzer,
            IUrlTokenProtector urlTokens)
        {
            _context = context;
            _attemptRepository = attemptRepository;
            _examRepository = examRepository;
            _scoringFactory = scoringFactory;
            _conceptAnalyzer = conceptAnalyzer;
            _urlTokens = urlTokens;
        }

        // GET: /ExamAttempt/Start/5
        public async Task<IActionResult> Start(string token)
        {
            if (!_urlTokens.TryUnprotectExam(token, out var id)) return BadRequest("Invalid exam link.");
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Account");

            var exam = await _examRepository.GetByIdAsync(id);
            if (exam == null)
                return RedirectToAction("Index", "Dashboard");

            if (User.FindFirstValue(ClaimTypes.Role) != UserRole.Student.ToString())
            {
                TempData["ErrorMessage"] = "Only student accounts can attempt an exam.";
                return RedirectToAction("Index", "Dashboard");
            }

            var now = DateTime.UtcNow;
            if (now < exam.StartTime)
            {
                TempData["ErrorMessage"] = "This exam is not open yet.";
                return RedirectToAction("Index", "Dashboard");
            }
            if (now >= exam.EndTime)
            {
                TempData["ErrorMessage"] = "This exam is no longer accepting attempts.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Check if student already attempted or has in-progress attempt
            var existingAttempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.ExamId == id && a.UserId == userId);

            if (existingAttempt != null)
            {
                if (existingAttempt.Status == AttemptStatus.Submitted || existingAttempt.Status == AttemptStatus.AutoSubmitted)
                {
                    return RedirectToAction("Details", "Result", new { attemptToken = _urlTokens.ProtectAttempt(existingAttempt.AttemptId) });
                }
                return RedirectToAction("Attempt", new { token = _urlTokens.ProtectAttempt(existingAttempt.AttemptId) });
            }

            var attempt = await _attemptRepository.StartAsync(id, userId);
            return RedirectToAction("Attempt", new { token = _urlTokens.ProtectAttempt(attempt.AttemptId) });
        }

        // GET: /ExamAttempt/Attempt/5
        public async Task<IActionResult> Attempt(string token)
        {
            if (!_urlTokens.TryUnprotectAttempt(token, out var attemptId)) return BadRequest("Invalid attempt link.");
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Account");

            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null || attempt.UserId != userId)
                return RedirectToAction("Index", "Dashboard");

            if (attempt.Status != AttemptStatus.InProgress)
                return RedirectToAction("Details", "Result", new { attemptToken = _urlTokens.ProtectAttempt(attempt.AttemptId) });

            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            if (exam == null)
                return RedirectToAction("Index", "Dashboard");

            var deadline = new[]
            {
                exam.EndTime,
                attempt.StartTime.AddMinutes(exam.DurationMinutes)
            }.Min();
            if (DateTime.UtcNow >= deadline)
                return await FinishAttemptAsync(attempt, exam);

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
                AttemptToken = _urlTokens.ProtectAttempt(attempt.AttemptId),
                ExamId = exam.ExamId,
                ExamToken = _urlTokens.ProtectExam(exam.ExamId),
                ExamTitle = exam.Title,
                QuestionId = nextQuestion.QuestionId,
                QuestionToken = _urlTokens.ProtectQuestion(nextQuestion.QuestionId),
                QuestionText = nextQuestion.Text,
                OptionA = nextQuestion.OptionA,
                OptionB = nextQuestion.OptionB,
                OptionC = nextQuestion.OptionC,
                OptionD = nextQuestion.OptionD,
                ShowConfidenceSlider = exam.IsConfidenceWeighted,
                CurrentQuestionNumber = attempt.Answers.Count + 1,
                SecondsRemaining = Math.Max(0, (int)Math.Floor((deadline - DateTime.UtcNow).TotalSeconds))
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

            if (!_urlTokens.TryUnprotectAttempt(model.AttemptToken, out var attemptId) ||
                !_urlTokens.TryUnprotectQuestion(model.QuestionToken, out var questionId))
                return BadRequest("Invalid answer link.");

            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null || attempt.UserId != userId || attempt.Status != AttemptStatus.InProgress)
                return RedirectToAction("Index", "Dashboard");

            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            var question = await _context.Questions.FindAsync(questionId);
            if (exam == null || question == null)
                return RedirectToAction("Index", "Dashboard");

            var deadline = new[]
            {
                exam.EndTime,
                attempt.StartTime.AddMinutes(exam.DurationMinutes)
            }.Min();
            if (DateTime.UtcNow >= deadline)
                return await FinishAttemptAsync(attempt, exam);

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

            return RedirectToAction("Attempt", new { token = _urlTokens.ProtectAttempt(attempt.AttemptId) });
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

            return RedirectToAction("Details", "Result", new { attemptToken = _urlTokens.ProtectAttempt(attempt.AttemptId) });
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
