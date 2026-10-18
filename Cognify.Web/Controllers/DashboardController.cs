using System.Security.Claims;
using Cognify.Core.Entities;
using Cognify.Core.Enums;
using Cognify.Data;
using Cognify.Services;
using Cognify.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "Account");

            // Ensure system catalog exists and new accounts have initial data assigned
            await DbInitializer.EnsureUserInitializedAsync(_context, userId);

            // Fetch user info from database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            var fullName = user?.Name ?? User.Identity?.Name ?? "Student";

            // Fetch user's completed attempts from database with related entities
            var attempts = await _context.ExamAttempts
                .Include(a => a.Exam)
                .Include(a => a.ConceptPerformances)
                    .ThenInclude(cp => cp.ConceptTag)
                .Where(a => a.UserId == userId && (a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.AutoSubmitted))
                .OrderByDescending(a => a.SubmitTime ?? a.StartTime)
                .ToListAsync();

            var examsTaken = attempts.Count;

            var averageScore = attempts.Any()
                ? Math.Round(attempts.Average(a => a.Exam != null && a.Exam.TotalMarks > 0 ? (a.TotalScore / a.Exam.TotalMarks) * 100.0 : 0.0), 0)
                : 0.0;

            // Calibration score based on over/under confidence index from database
            var calibAttempts = attempts.Where(a => a.OverconfidenceIndex.HasValue && a.UnderconfidenceIndex.HasValue).ToList();
            var calibrationScore = calibAttempts.Any()
                ? Math.Round(calibAttempts.Average(a => Math.Max(0, 100.0 - ((a.OverconfidenceIndex!.Value + a.UnderconfidenceIndex!.Value) / 2.0))), 0)
                : 0.0;

            // Calculate streak from distinct attempt dates in database
            var distinctDates = attempts
                .Select(a => (a.SubmitTime ?? a.StartTime).Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            int streakDays = 0;
            var today = DateTime.UtcNow.Date;
            if (distinctDates.Any())
            {
                var latestDate = distinctDates[0];
                if (latestDate == today || latestDate == today.AddDays(-1))
                {
                    streakDays = 1;
                    var currentDate = latestDate;
                    for (int i = 1; i < distinctDates.Count; i++)
                    {
                        if (distinctDates[i] == currentDate.AddDays(-1))
                        {
                            streakDays++;
                            currentDate = distinctDates[i];
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // Experience Points (XP) & Level calculated dynamically from DB performance
            int totalXp = 50; // Starting baseline XP
            foreach (var att in attempts)
            {
                double marksPercent = att.Exam != null && att.Exam.TotalMarks > 0
                    ? (att.TotalScore / att.Exam.TotalMarks)
                    : 0.5;
                totalXp += (int)Math.Round(marksPercent * 50.0) + 20;
            }

            int level = 1 + (totalXp / 100);
            int currentXp = totalXp % 100;
            int xpToNext = 100;

            // Fetch upcoming / active exams from database (excluding exams student already finished)
            var completedExamIds = attempts.Select(a => a.ExamId).ToHashSet();
            var upcomingEntities = await _context.Exams
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                .Include(e => e.QuestionPools)
                    .ThenInclude(qp => qp.ConceptTag)
                .Where(e => e.EndTime > DateTime.UtcNow && !completedExamIds.Contains(e.ExamId))
                .OrderBy(e => e.StartTime)
                .ToListAsync();

            var upcomingExams = upcomingEntities.Select(e => new QuestVM
            {
                ExamId = e.ExamId,
                Title = e.Title,
                Subject = e.ExamQuestions.Select(eq => eq.Question?.Subject).FirstOrDefault(s => !string.IsNullOrEmpty(s))
                          ?? e.QuestionPools.Select(qp => qp.ConceptTag?.Subject).FirstOrDefault(s => !string.IsNullOrEmpty(s))
                          ?? "General",
                StartTime = e.StartTime,
                DurationMinutes = e.DurationMinutes,
                IsAdaptive = e.IsAdaptive,
                IsConfidenceWeighted = e.IsConfidenceWeighted,
                XpReward = e.TotalMarks > 0 ? e.TotalMarks * 2 : 50
            }).ToList();

            // Recent results from real database attempts
            var recentResults = attempts.Take(5).Select(a => new ResultVM
            {
                AttemptId = a.AttemptId,
                ExamId = a.ExamId,
                Title = a.Exam?.Title ?? "Exam",
                SubmittedOn = a.SubmitTime ?? a.StartTime,
                Score = (int)Math.Round(a.TotalScore),
                TotalMarks = a.Exam?.TotalMarks ?? 100
            }).ToList();

            // Real concept mastery aggregated from database ConceptPerformances
            var concepts = attempts
                .SelectMany(a => a.ConceptPerformances)
                .Where(cp => cp.ConceptTag != null)
                .GroupBy(cp => new { cp.ConceptTagId, cp.ConceptTag!.Name })
                .Select(g => new ConceptVM
                {
                    Name = g.Key.Name,
                    Mastery = g.Sum(x => x.TotalQuestions) == 0 ? 0 :
                        (int)Math.Round((double)g.Sum(x => x.CorrectCount) / g.Sum(x => x.TotalQuestions) * 100)
                })
                .OrderByDescending(c => c.Mastery)
                .ToList();

            // Badges evaluated against real user milestones
            var badges = new List<BadgeVM>
            {
                new()
                {
                    Name = "Welcome Cadet",
                    Hint = "Joined Cognify platform",
                    Glyph = "●",
                    Unlocked = true
                },
                new()
                {
                    Name = "First Exam",
                    Hint = "Complete any exam",
                    Glyph = "★",
                    Unlocked = examsTaken >= 1
                },
                new()
                {
                    Name = "Sharp Mind",
                    Hint = "Score 85%+ on any exam",
                    Glyph = "◆",
                    Unlocked = attempts.Any(a => a.Exam != null && a.Exam.TotalMarks > 0 && (a.TotalScore / a.Exam.TotalMarks) >= 0.85)
                },
                new()
                {
                    Name = "On Fire",
                    Hint = "Keep a 3-day streak",
                    Glyph = "▲",
                    Unlocked = streakDays >= 3
                },
                new()
                {
                    Name = "Calibrated",
                    Hint = "Achieve 80%+ calibration score",
                    Glyph = "◈",
                    Unlocked = calibrationScore >= 80 && attempts.Any(a => a.Exam != null && a.Exam.IsConfidenceWeighted)
                },
                new()
                {
                    Name = "Scholar",
                    Hint = "Reach Level 2 or higher",
                    Glyph = "✪",
                    Unlocked = level >= 2
                }
            };

            var vm = new DashboardVM
            {
                FullName = fullName,
                Level = level,
                Xp = currentXp,
                XpToNext = xpToNext,
                StreakDays = streakDays,
                ExamsTaken = examsTaken,
                AverageScore = averageScore,
                CalibrationScore = calibrationScore,
                UpcomingExams = upcomingExams,
                RecentResults = recentResults,
                Concepts = concepts,
                Badges = badges
            };

            return View(vm);
        }
    }
}