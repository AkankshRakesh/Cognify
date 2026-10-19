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

            // Ensure catalog is seeded and accounts are initialized with initial data
            await DbInitializer.InitializeAsync(_context);
            await DbInitializer.EnsureUserInitializedAsync(_context, userId);

            // Fetch user info from database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Separate dashboard experience based on Role
            if (user.Role == UserRole.Teacher || user.Role == UserRole.Admin)
            {
                var teacherVm = await BuildTeacherDashboardAsync(user);
                return View("TeacherDashboard", teacherVm);
            }

            var studentVm = await BuildStudentDashboardAsync(user);
            return View("Index", studentVm);
        }

        private async Task<TeacherDashboardVM> BuildTeacherDashboardAsync(User user)
        {
            // Exams created by this teacher (or all exams if Admin)
            var teacherExams = await _context.Exams
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                .Include(e => e.QuestionPools)
                    .ThenInclude(qp => qp.ConceptTag)
                .Where(e => user.Role == UserRole.Admin || e.CreatedByUserId == user.UserId)
                .OrderByDescending(e => e.StartTime)
                .ToListAsync();

            var examIds = teacherExams.Select(e => e.ExamId).ToList();

            // All student submissions for these exams
            var attempts = await _context.ExamAttempts
                .Include(a => a.User)
                .Include(a => a.Exam)
                .Where(a => examIds.Contains(a.ExamId) && (a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.AutoSubmitted))
                .OrderByDescending(a => a.SubmitTime ?? a.StartTime)
                .ToListAsync();

            var totalExams = teacherExams.Count;
            var activeExams = teacherExams.Count(e => e.StartTime <= DateTime.UtcNow && e.EndTime >= DateTime.UtcNow);
            var totalSubmissions = attempts.Count;

            var avgScore = attempts.Any()
                ? Math.Round(attempts.Average(a => a.Exam != null && a.Exam.TotalMarks > 0 ? (a.TotalScore / a.Exam.TotalMarks) * 100.0 : 0.0), 1)
                : 0.0;

            var calibAttempts = attempts.Where(a => a.OverconfidenceIndex.HasValue && a.UnderconfidenceIndex.HasValue).ToList();
            var avgCalib = calibAttempts.Any()
                ? Math.Round(calibAttempts.Average(a => Math.Max(0, 100.0 - ((a.OverconfidenceIndex!.Value + a.UnderconfidenceIndex!.Value) / 2.0))), 1)
                : 0.0;

            // Map Exams List
            var examSummaries = teacherExams.Select(e =>
            {
                var examAttempts = attempts.Where(a => a.ExamId == e.ExamId).ToList();
                var examAvg = examAttempts.Any()
                    ? Math.Round(examAttempts.Average(a => e.TotalMarks > 0 ? (a.TotalScore / e.TotalMarks) * 100.0 : 0.0), 1)
                    : 0.0;

                return new TeacherExamSummaryVM
                {
                    ExamId = e.ExamId,
                    Title = e.Title,
                    Subject = e.ExamQuestions.Select(eq => eq.Question?.Subject).FirstOrDefault(s => !string.IsNullOrEmpty(s))
                              ?? e.QuestionPools.Select(qp => qp.ConceptTag?.Subject).FirstOrDefault(s => !string.IsNullOrEmpty(s))
                              ?? "General",
                    DurationMinutes = e.DurationMinutes,
                    TotalMarks = e.TotalMarks,
                    QuestionCount = e.ExamQuestions.Count,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    IsAdaptive = e.IsAdaptive,
                    IsConfidenceWeighted = e.IsConfidenceWeighted,
                    AttemptsCount = examAttempts.Count,
                    AverageScore = examAvg
                };
            }).ToList();

            // Recent Submissions
            var recentSubmissions = attempts.Take(8).Select(a => new TeacherRecentSubmissionVM
            {
                AttemptId = a.AttemptId,
                ExamId = a.ExamId,
                StudentName = a.User?.Name ?? "Student",
                ExamTitle = a.Exam?.Title ?? "Exam",
                Score = Math.Round(a.TotalScore, 1),
                TotalMarks = a.Exam?.TotalMarks ?? 100,
                SubmittedOn = a.SubmitTime ?? a.StartTime,
                CalibrationScore = a.OverconfidenceIndex.HasValue && a.UnderconfidenceIndex.HasValue
                    ? Math.Round(Math.Max(0, 100.0 - ((a.OverconfidenceIndex.Value + a.UnderconfidenceIndex.Value) / 2.0)), 1)
                    : null
            }).ToList();

            // Concept Analytics across all student attempts
            var attemptIds = attempts.Select(a => a.AttemptId).ToList();
            var conceptPerformances = await _context.ConceptPerformances
                .Include(cp => cp.ConceptTag)
                .Where(cp => attemptIds.Contains(cp.AttemptId))
                .ToListAsync();

            var conceptAnalytics = conceptPerformances
                .Where(cp => cp.ConceptTag != null)
                .GroupBy(cp => new { cp.ConceptTagId, cp.ConceptTag!.Name, cp.ConceptTag.Subject })
                .Select(g => new TeacherConceptAnalyticsVM
                {
                    ConceptName = g.Key.Name,
                    Subject = g.Key.Subject,
                    TotalQuestionsAnswered = g.Sum(x => x.TotalQuestions),
                    CorrectCount = g.Sum(x => x.CorrectCount)
                })
                .OrderBy(c => c.AccuracyPercent) // Highlight weaker concepts first for teacher attention
                .ToList();

            // Question Bank summary
            var allQuestions = await _context.Questions.Include(q => q.ConceptTag).ToListAsync();
            var questionBankSummary = allQuestions
                .Where(q => q.ConceptTag != null)
                .GroupBy(q => new { q.ConceptTagId, q.ConceptTag!.Name, q.ConceptTag.Subject })
                .Select(g => new ConceptQuestionCountVM
                {
                    ConceptName = g.Key.Name,
                    Subject = g.Key.Subject,
                    TotalQuestions = g.Count(),
                    EasyCount = g.Count(x => x.DifficultyLevel == DifficultyLevel.Easy),
                    MediumCount = g.Count(x => x.DifficultyLevel == DifficultyLevel.Medium),
                    HardCount = g.Count(x => x.DifficultyLevel == DifficultyLevel.Hard)
                })
                .OrderBy(q => q.ConceptName)
                .ToList();

            return new TeacherDashboardVM
            {
                FullName = user.Name,
                Email = user.Email,
                TotalExamsCreated = totalExams,
                ActiveExamsCount = activeExams,
                TotalSubmissions = totalSubmissions,
                AverageClassScore = avgScore,
                AverageCalibrationScore = avgCalib,
                Exams = examSummaries,
                RecentSubmissions = recentSubmissions,
                ConceptAnalytics = conceptAnalytics,
                QuestionBankSummary = questionBankSummary
            };
        }

        private async Task<DashboardVM> BuildStudentDashboardAsync(User user)
        {
            var userId = user.UserId;
            var fullName = user.Name;

            // Fetch user's completed attempts from database
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

            var calibAttempts = attempts.Where(a => a.OverconfidenceIndex.HasValue && a.UnderconfidenceIndex.HasValue).ToList();
            var calibrationScore = calibAttempts.Any()
                ? Math.Round(calibAttempts.Average(a => Math.Max(0, 100.0 - ((a.OverconfidenceIndex!.Value + a.UnderconfidenceIndex!.Value) / 2.0))), 0)
                : 0.0;

            // Streak from distinct attempt dates
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

            // Fetch upcoming / active exams from database
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

            return new DashboardVM
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
        }
    }
}