using System.Security.Claims;
using Cognify.Core.Enums;
using Cognify.Data;
using Cognify.Web.ViewModels;
using Cognify.Web.Security;
using Cognify.Services;
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
        private readonly ThreeParameterIrtService _irtService;

        public ResultController(
            AppDbContext context,
            IUrlTokenProtector urlTokens,
            ThreeParameterIrtService irtService)
        {
            _context = context;
            _urlTokens = urlTokens;
            _irtService = irtService;
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

            var isAdaptive = attempt.Exam?.UsesIrtAdaptive ?? attempt.IsAdaptiveSnapshot ?? false;
            var negativeMarking = attempt.Exam?.NegativeMarking ?? attempt.NegativeMarkingSnapshot ?? false;
            var abilityEstimate = isAdaptive
                ? _irtService.EstimateAbility(attempt.Answers)
                : null;
            var conventionalMarks = isAdaptive
                ? attempt.Answers.Sum(answer => answer.Question?.Marks ?? 0)
                : (int?)null;
            var conventionalScore = isAdaptive
                ? Math.Max(0, attempt.Answers.Sum(answer =>
                {
                    var marks = answer.Question?.Marks ?? 0;
                    return answer.IsCorrect
                        ? marks
                        : negativeMarking ? -0.25 * marks : 0;
                }))
                : (double?)null;
            var availableMarks = attempt.Answers.Sum(answer => answer.Question?.Marks ?? 0);
            var correctMarks = attempt.Answers
                .Where(answer => answer.IsCorrect)
                .Sum(answer => answer.Question?.Marks ?? 0);
            var negativeMarks = attempt.Answers
                .Where(answer => !answer.IsCorrect && !string.IsNullOrWhiteSpace(answer.SelectedOption))
                .Sum(answer => Math.Max(0, -(answer.MarksAwarded)));
            var confidenceAdjustment = attempt.Answers
                .Where(answer => answer.IsCorrect && answer.Question != null)
                .Sum(answer => answer.MarksAwarded - answer.Question!.Marks);
            var adaptiveItems = isAdaptive && abilityEstimate != null
                ? attempt.Answers
                    .Where(answer => answer.Question != null)
                    .OrderBy(answer => answer.SequencePosition)
                    .Select(answer => _irtService.GetItemReport(answer.Question!, answer, abilityEstimate.Theta))
                    .Select(item => new IrtReportItemVM
                    {
                        ItemNumber = item.ItemNumber,
                        IsCorrect = item.IsCorrect,
                        IsSkipped = item.IsSkipped,
                        Discrimination = item.Discrimination,
                        Difficulty = item.Difficulty,
                        Guessing = item.Guessing,
                        ProbabilityAtEstimate = item.ProbabilityAtEstimate,
                        FisherInformation = item.FisherInformation
                    })
                    .ToList()
                : new List<IrtReportItemVM>();
            var abilityProgression = isAdaptive
                ? attempt.Answers
                    .OrderBy(answer => answer.SequencePosition)
                    .Select((answer, index) =>
                    {
                        var previousAnswers = attempt.Answers
                            .Where(previous => previous.SequencePosition < answer.SequencePosition);
                        var before = _irtService.EstimateAbility(previousAnswers);
                        var after = _irtService.EstimateAbility(
                            attempt.Answers.Where(current => current.SequencePosition <= answer.SequencePosition));
                        var item = answer.Question == null
                            ? null
                            : _irtService.GetItemReport(answer.Question, answer, after.Theta);

                        return new IrtAbilityProgressionVM
                        {
                            ResponseNumber = index + 1,
                            IsCorrect = answer.IsCorrect,
                            IsSkipped = string.IsNullOrWhiteSpace(answer.SelectedOption),
                            ItemDifficulty = item?.Difficulty ?? 0,
                            AbilityBefore = before.Theta,
                            AbilityAfter = after.Theta,
                            StandardErrorAfter = after.StandardError
                        };
                    })
                    .ToList()
                : new List<IrtAbilityProgressionVM>();

            var vm = new ResultSummaryVM
            {
                AttemptId = attempt.AttemptId,
                AttemptToken = _urlTokens.ProtectAttempt(attempt.AttemptId),
                ExamTitle = attempt.Exam?.Title ?? attempt.ExamTitleSnapshot ?? "Exam Assessment",
                TotalScore = attempt.TotalScore,
                TotalMarks = attempt.Exam?.TotalMarks ?? attempt.ExamTotalMarksSnapshot ?? 100,
                IsAdaptive = isAdaptive,
                AbilityEstimate = abilityEstimate?.Theta,
                AbilityStandardError = abilityEstimate?.StandardError,
                ConventionalScore = conventionalScore,
                ConventionalTotalMarks = conventionalMarks,
                CorrectCount = attempt.Answers.Count(a => a.IsCorrect),
                WrongCount = attempt.Answers.Count(a => !a.IsCorrect && !string.IsNullOrEmpty(a.SelectedOption)),
                UnattemptedCount = attempt.Answers.Count(a => string.IsNullOrEmpty(a.SelectedOption)),
                HasCalibrationReport = attempt.OverconfidenceIndex.HasValue || attempt.UnderconfidenceIndex.HasValue,
                OverconfidenceIndex = attempt.OverconfidenceIndex ?? 0,
                UnderconfidenceIndex = attempt.UnderconfidenceIndex ?? 0,
                AvailableMarks = availableMarks,
                CorrectMarks = correctMarks,
                NegativeMarks = negativeMarks,
                ConfidenceAdjustment = confidenceAdjustment,
                IrtPriorMean = ThreeParameterIrtService.PriorMean,
                IrtPriorStandardDeviation = ThreeParameterIrtService.PriorStandardDeviation,
                IrtThetaMinimum = ThreeParameterIrtService.MinimumTheta,
                IrtThetaMaximum = ThreeParameterIrtService.MaximumTheta,
                IrtThetaStep = ThreeParameterIrtService.ThetaStep,
                IrtItems = adaptiveItems,
                IrtAbilityProgression = abilityProgression
            };

            return View(vm);
        }

        public IActionResult Index() => RedirectToAction("Index", "Dashboard");
    }
}
