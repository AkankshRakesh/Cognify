using System.Security.Claims;
using Cognify.Core.Entities;
using Cognify.Core.Enums;
using Cognify.Data;
using Cognify.Services;
using Cognify.Web.ViewModels;
using Cognify.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Web.Controllers
{
    [Authorize]
    public class ExamController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUrlTokenProtector _urlTokens;

        public ExamController(AppDbContext context, IUrlTokenProtector urlTokens)
        {
            _context = context;
            _urlTokens = urlTokens;
        }

        public async Task<IActionResult> Index()
        {
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Submissions(string token)
        {
            if (!_urlTokens.TryUnprotectExam(token, out var id))
                return BadRequest("Invalid exam link.");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || (user.Role != UserRole.Teacher && user.Role != UserRole.Admin))
                return RedirectToAction("Index", "Dashboard");

            var exam = await _context.Exams
                .Include(e => e.ExamQuestions).ThenInclude(eq => eq.Question)
                .Include(e => e.QuestionSet)
                .FirstOrDefaultAsync(e => e.ExamId == id &&
                    (user.Role == UserRole.Admin || e.CreatedByUserId == user.UserId));
            if (exam == null) return RedirectToAction("Index", "Dashboard");

            var attempts = await _context.ExamAttempts
                .Include(a => a.User)
                .Include(a => a.Answers).ThenInclude(a => a.Question)
                .Where(a => a.ExamId == exam.ExamId)
                .OrderByDescending(a => a.SubmitTime ?? a.StartTime)
                .ToListAsync();

            var questionOrder = exam.ExamQuestions
                .OrderBy(eq => eq.SequenceOrder)
                .Select((eq, index) => new { eq.QuestionId, Number = index + 1 })
                .ToDictionary(x => x.QuestionId, x => x.Number);

            var submissions = attempts.Select(attempt => new TeacherSubmissionVM
            {
                AttemptId = attempt.AttemptId,
                StudentName = attempt.User?.Name ?? "Student",
                StudentEmail = attempt.User?.Email ?? string.Empty,
                Status = attempt.Status == AttemptStatus.AutoSubmitted ? "Auto-submitted" : attempt.Status.ToString(),
                StartedOn = attempt.StartTime,
                SubmittedOn = attempt.SubmitTime,
                Score = attempt.TotalScore,
                TotalMarks = exam.TotalMarks,
                IsAdaptive = exam.UsesIrtAdaptive,
                CorrectCount = attempt.Answers.Count(a => a.IsCorrect),
                WrongCount = attempt.Answers.Count(a => !a.IsCorrect && !string.IsNullOrWhiteSpace(a.SelectedOption)),
                SkippedCount = Math.Max(0, (exam.UsesIrtAdaptive
                    ? exam.AdaptiveQuestionCount
                    : exam.ExamQuestions.Count) - attempt.Answers.Count),
                CalibrationScore = attempt.OverconfidenceIndex.HasValue && attempt.UnderconfidenceIndex.HasValue
                    ? Math.Round(Math.Max(0, 100 - ((attempt.OverconfidenceIndex.Value + attempt.UnderconfidenceIndex.Value) / 2)), 1)
                    : null,
                Answers = attempt.Answers.OrderBy(a => a.SequencePosition).Select(answer => new TeacherSubmissionAnswerVM
                {
                    QuestionNumber = questionOrder.TryGetValue(answer.QuestionId, out var number) ? number : answer.SequencePosition,
                    QuestionText = answer.Question?.Text ?? "Question",
                    SelectedOption = answer.SelectedOption,
                    CorrectOption = answer.Question?.CorrectOption ?? string.Empty,
                    IsCorrect = answer.IsCorrect,
                    MarksAwarded = answer.MarksAwarded,
                    Difficulty = answer.DifficultyAtServeTime.ToString()
                }).ToList()
            }).ToList();

            return View(new TeacherSubmissionsVM
            {
                ExamId = exam.ExamId,
                ExamTitle = exam.Title,
                Subject = exam.QuestionSet?.Subject
                    ?? exam.ExamQuestions.Select(eq => eq.Question?.Subject).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))
                    ?? "General",
                ShareUrl = Url.Action("Start", "ExamAttempt", new { token = _urlTokens.ProtectExam(exam.ExamId) }, Request.Scheme) ?? string.Empty,
                StartTime = exam.StartTime,
                EndTime = exam.EndTime,
                DurationMinutes = exam.DurationMinutes,
                QuestionCount = exam.UsesIrtAdaptive ? exam.AdaptiveQuestionCount : exam.ExamQuestions.Count,
                TotalMarks = exam.TotalMarks,
                Submissions = submissions
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExam(string token)
        {
            if (!_urlTokens.TryUnprotectExam(token, out var examId))
                return BadRequest("Invalid exam link.");

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
                return Forbid();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || (user.Role != UserRole.Teacher && user.Role != UserRole.Admin))
                return Forbid();

            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                .Include(e => e.QuestionPools)
                .Include(e => e.Attempts)
                .FirstOrDefaultAsync(e => e.ExamId == examId &&
                    (user.Role == UserRole.Admin || e.CreatedByUserId == user.UserId));

            if (exam == null)
            {
                TempData["ErrorMessage"] = "The exam could not be found or you do not have permission to delete it.";
                return RedirectToAction("Index", "Dashboard");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var attempt in exam.Attempts.ToList())
                {
                    attempt.ExamTitleSnapshot = exam.Title;
                    attempt.ExamTotalMarksSnapshot = exam.TotalMarks;
                    attempt.IsAdaptiveSnapshot = exam.UsesIrtAdaptive;
                    attempt.IsConfidenceWeightedSnapshot = exam.IsConfidenceWeighted;
                    attempt.NegativeMarkingSnapshot = exam.NegativeMarking;
                    attempt.ExamId = null;
                    attempt.Exam = null;
                }

                _context.ExamQuestionPools.RemoveRange(exam.QuestionPools);
                _context.ExamQuestions.RemoveRange(exam.ExamQuestions);
                _context.Exams.Remove(exam);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Exam \"{exam.Title}\" was deleted.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "The exam could not be deleted. No changes were made.";
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || (user.Role != UserRole.Teacher && user.Role != UserRole.Admin))
            {
                TempData["ErrorMessage"] = "Only teachers can create exams.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Ensure catalog is seeded if needed
            await DbInitializer.InitializeAsync(_context);

            var vm = new ExamCreateVM
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddDays(7),
                DurationMinutes = 45,
                ShuffleQuestions = true
            };

            await PopulateExamCreationDataAsync(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamCreateVM model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || (user.Role != UserRole.Teacher && user.Role != UserRole.Admin))
            {
                TempData["ErrorMessage"] = "Only teachers can create exams.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (model.EndTime <= model.StartTime)
            {
                ModelState.AddModelError(nameof(model.EndTime), "End Time must be after Start Time.");
            }

            var questionSet = await _context.QuestionSets
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.QuestionSetId == model.QuestionSetId &&
                    (s.IsBuiltIn || user.Role == UserRole.Admin || s.CreatedByUserId == user.UserId));

            if (questionSet == null || !questionSet.Questions.Any())
                ModelState.AddModelError(nameof(model.QuestionSetId), "Please select a question set containing at least one question.");

            if (model.Adaptive && questionSet != null)
            {
                var maximumAdaptiveQuestions = questionSet.Questions.Count / 2;
                if (questionSet.Questions.Count < 100)
                {
                    ModelState.AddModelError(nameof(model.Adaptive),
                        "Adaptive IRT exams require a question set with at least 100 questions.");
                }
                else if (model.AdaptiveQuestionCount > maximumAdaptiveQuestions)
                {
                    ModelState.AddModelError(nameof(model.AdaptiveQuestionCount),
                        $"Choose no more than half of the set ({maximumAdaptiveQuestions} questions).");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateExamCreationDataAsync(model);
                return View(model);
            }

            var exam = new Exam
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                CreatedByUserId = user.UserId,
                IsPublic = model.IsPublic,
                DurationMinutes = model.DurationMinutes,
                StartTime = model.StartTime.ToUniversalTime(),
                EndTime = model.EndTime.ToUniversalTime(),
                NegativeMarking = model.NegativeMarking,
                ShuffleQuestions = model.ShuffleQuestions,
                ScoringMode = model.Adaptive
                    ? (model.ConfidenceWeighted ? ScoringMode.Both : ScoringMode.Adaptive)
                    : (model.ConfidenceWeighted ? ScoringMode.ConfidenceWeighted : ScoringMode.Standard),
                QuestionSetId = questionSet!.QuestionSetId,
                AdaptiveQuestionCount = model.Adaptive ? model.AdaptiveQuestionCount : 0,
                TotalMarks = model.Adaptive ? 100 : questionSet.Questions.Sum(q => q.Marks)
            };

            if (!model.Adaptive)
            {
                exam.ExamQuestions = questionSet.Questions
                    .OrderBy(q => q.QuestionId)
                    .Select((q, index) => new ExamQuestion { QuestionId = q.QuestionId, SequenceOrder = index + 1 })
                    .ToList();
            }

            await _context.Exams.AddAsync(exam);
            await _context.SaveChangesAsync();

            var shareUrl = Url.Action("Start", "ExamAttempt", new { token = _urlTokens.ProtectExam(exam.ExamId) }, Request.Scheme) ?? string.Empty;
            TempData["SuccessMessage"] = $"Exam \"{exam.Title}\" was created. Share link: {shareUrl}";
            return RedirectToAction("Index", "Dashboard");
        }

        private async Task PopulateExamCreationDataAsync(ExamCreateVM model)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var currentUser = await _context.Users.FirstAsync(u => u.UserId == currentUserId);
            model.QuestionSets = await _context.QuestionSets
                .Where(s => s.IsBuiltIn || currentUser.Role == UserRole.Admin || s.CreatedByUserId == currentUserId)
                .Include(s => s.Questions)
                .OrderBy(s => s.Title)
                .Select(s => new QuestionSetOptionVM
                {
                    QuestionSetId = s.QuestionSetId,
                    Title = s.Title,
                    Subject = s.Subject,
                    QuestionCount = s.Questions.Count,
                    TotalMarks = s.Questions.Sum(q => q.Marks),
                    IsBuiltIn = s.IsBuiltIn
                })
                .ToListAsync();
        }
    }
}
