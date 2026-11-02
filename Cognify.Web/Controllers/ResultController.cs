using System.Security.Claims;
using Cognify.Core.Enums;
using Cognify.Data;
using Cognify.Web.ViewModels;
using Cognify.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Web.Controllers
{
    [Authorize]
    public class ResultController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUrlTokenProtector _urlTokens;

        public ResultController(AppDbContext context, IUrlTokenProtector urlTokens)
        {
            _context = context;
            _urlTokens = urlTokens;
        }

        // GET: /Result/Details?examId=1 or /Result/Details?attemptId=5
        public async Task<IActionResult> Details(string? examToken, string? attemptToken)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Account");

            var query = _context.ExamAttempts
                .Include(a => a.Exam)
                .Include(a => a.Answers)
                    .ThenInclude(ans => ans.Question)
                .Include(a => a.ConceptPerformances)
                    .ThenInclude(cp => cp.ConceptTag)
                .Where(a => a.UserId == userId);

            if (!string.IsNullOrWhiteSpace(attemptToken))
            {
                if (!_urlTokens.TryUnprotectAttempt(attemptToken, out var attemptId)) return BadRequest("Invalid result link.");
                query = query.Where(a => a.AttemptId == attemptId);
            }
            else if (!string.IsNullOrWhiteSpace(examToken))
            {
                if (!_urlTokens.TryUnprotectExam(examToken, out var examId)) return BadRequest("Invalid result link.");
                query = query.Where(a => a.ExamId == examId)
                             .OrderByDescending(a => a.SubmitTime ?? a.StartTime);
            }
            else
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var attempt = await query.FirstOrDefaultAsync();
            if (attempt == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var vm = new ResultSummaryVM
            {
                AttemptId = attempt.AttemptId,
                AttemptToken = _urlTokens.ProtectAttempt(attempt.AttemptId),
                ExamTitle = attempt.Exam?.Title ?? "Exam Assessment",
                TotalScore = attempt.TotalScore,
                TotalMarks = attempt.Exam?.TotalMarks ?? 100,
                CorrectCount = attempt.Answers.Count(a => a.IsCorrect),
                WrongCount = attempt.Answers.Count(a => !a.IsCorrect && !string.IsNullOrEmpty(a.SelectedOption)),
                UnattemptedCount = attempt.Answers.Count(a => string.IsNullOrEmpty(a.SelectedOption)),
                HasCalibrationReport = attempt.OverconfidenceIndex.HasValue || attempt.UnderconfidenceIndex.HasValue,
                OverconfidenceIndex = attempt.OverconfidenceIndex ?? 0,
                UnderconfidenceIndex = attempt.UnderconfidenceIndex ?? 0,
                ConceptMap = attempt.ConceptPerformances.Select(cp => new ConceptMapEntryVM
                {
                    ConceptName = cp.ConceptTag?.Name ?? "General",
                    AccuracyPercent = Math.Round(cp.AccuracyPercent, 1)
                }).ToList()
            };

            return View(vm);
        }

        public IActionResult Index() => RedirectToAction("Index", "Dashboard");
    }
}
