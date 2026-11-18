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
        private readonly ThreeParameterIrtService _irtService;
        private readonly IUrlTokenProtector _urlTokens;

        public ExamAttemptController(
            AppDbContext context,
            AttemptRepository attemptRepository,
            ExamRepository examRepository,
            ScoringServiceFactory scoringFactory,
            IConceptAnalyzer conceptAnalyzer,
            ThreeParameterIrtService irtService,
            IUrlTokenProtector urlTokens)
        {
            _context = context;
            _attemptRepository = attemptRepository;
            _examRepository = examRepository;
            _scoringFactory = scoringFactory;
            _conceptAnalyzer = conceptAnalyzer;
            _irtService = irtService;
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

            return View("Start", new ExamStartVM
            {
                ExamToken = token,
                ExamTitle = exam.Title,
                Description = exam.Description,
                DurationMinutes = exam.DurationMinutes,
                QuestionCount = exam.UsesIrtAdaptive ? exam.AdaptiveQuestionCount : exam.ExamQuestions.Count
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmStart(string token)
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

            var attempt = await _attemptRepository.StartAsync(exam, userId);
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

            if (!attempt.ExamId.HasValue)
                return RedirectToAction("Index", "Dashboard");

            var exam = await _examRepository.GetByIdAsync(attempt.ExamId.Value);
            if (exam == null)
                return RedirectToAction("Index", "Dashboard");

            var deadline = new[]
            {
                exam.EndTime,
                attempt.StartTime.AddMinutes(exam.DurationMinutes)
            }.Min();
            if (DateTime.UtcNow >= deadline)
                return await FinishAttemptAsync(attempt, exam);

            if (exam.UsesIrtAdaptive && attempt.Answers.Count >= exam.AdaptiveQuestionCount)
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

            var totalQuestionCount = exam.UsesIrtAdaptive
                ? exam.AdaptiveQuestionCount
                : exam.ExamQuestions.Count;

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
                NegativeMarking = exam.NegativeMarking,
                IsAdaptive = exam.UsesIrtAdaptive,
                CurrentQuestionNumber = attempt.Answers.Count + 1,
                TotalQuestionCount = totalQuestionCount,
                IsLastQuestion = attempt.Answers.Count + 1 >= totalQuestionCount,
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

            if (!attempt.ExamId.HasValue)
                return RedirectToAction("Index", "Dashboard");

            var exam = await _examRepository.GetByIdAsync(attempt.ExamId.Value);
            var question = await _context.Questions.FindAsync(questionId);
            if (exam == null || question == null)
                return RedirectToAction("Index", "Dashboard");

            if (attempt.Answers.Any(answer => answer.QuestionId == questionId))
                return BadRequest("This question has already been answered.");

            if (!model.Skip && !IsAvailableOption(question, model.SelectedOption))
                return BadRequest("Select one of the available answer options.");

            var deadline = new[]
            {
                exam.EndTime,
                attempt.StartTime.AddMinutes(exam.DurationMinutes)
            }.Min();
            if (DateTime.UtcNow >= deadline)
                return await FinishAttemptAsync(attempt, exam);

            if (exam.UsesIrtAdaptive && attempt.Answers.Count >= exam.AdaptiveQuestionCount)
                return await FinishAttemptAsync(attempt, exam);

            var selectionService = _scoringFactory.GetSelectionService(exam);
            var expectedQuestion = attempt.Answers.Count == 0
                ? await selectionService.GetFirstQuestionAsync(attempt)
                : await selectionService.GetNextQuestionAsync(
                    attempt,
                    attempt.Answers.OrderBy(answer => answer.SequencePosition).Last());
            if (expectedQuestion?.QuestionId != questionId)
                return BadRequest("This question is not the next question in your exam.");

            var isCorrect = !string.IsNullOrEmpty(model.SelectedOption) &&
                            !model.Skip &&
                            string.Equals(model.SelectedOption.Trim(), question.CorrectOption.Trim(), StringComparison.OrdinalIgnoreCase);

            var answer = new AttemptAnswer
            {
                AttemptId = attempt.AttemptId,
                QuestionId = question.QuestionId,
                SelectedOption = model.Skip ? null : model.SelectedOption,
                ConfidenceLevel = exam.IsConfidenceWeighted && !model.Skip
                    ? (model.ConfidenceLevel ?? ConfidenceLevel.Medium)
                    : null,
                IsCorrect = isCorrect,
                SequencePosition = attempt.Answers.Count + 1,
                DifficultyAtServeTime = question.DifficultyLevel
            };

            var scoringService = _scoringFactory.GetScoringService(exam);
            answer.MarksAwarded = scoringService.CalculateMarks(question, answer, exam.NegativeMarking);

            await _attemptRepository.SaveAnswerAsync(answer);

            // Update question stats
            if (!model.Skip)
            {
                question.TimesAttempted++;
                if (isCorrect) question.TimesCorrect++;
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("Attempt", new { token = _urlTokens.ProtectAttempt(attempt.AttemptId) });
        }

        private async Task<IActionResult> FinishAttemptAsync(ExamAttempt attempt, Exam exam)
        {
            attempt.Status = AttemptStatus.Submitted;
            attempt.SubmitTime = DateTime.UtcNow;

            // Calculate total score
            attempt.TotalScore = exam.UsesIrtAdaptive
                ? Math.Clamp(Math.Round(_irtService.ToScaledScore(
                    _irtService.EstimateAbility(attempt.Answers).Theta), 2), 0, exam.TotalMarks)
                : Math.Clamp(
                    Math.Round(attempt.Answers.Sum(a => a.MarksAwarded), 2),
                    0,
                    exam.TotalMarks);

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

        private static bool IsAvailableOption(Question question, string? selectedOption)
        {
            if (string.IsNullOrWhiteSpace(selectedOption))
                return false;

            return selectedOption.Trim().ToUpperInvariant() switch
            {
                "A" => !string.IsNullOrWhiteSpace(question.OptionA),
                "B" => !string.IsNullOrWhiteSpace(question.OptionB),
                "C" => !string.IsNullOrWhiteSpace(question.OptionC),
                "D" => !string.IsNullOrWhiteSpace(question.OptionD),
                _ => false
            };
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
