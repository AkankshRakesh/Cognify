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
    public class ExamController : Controller
    {
        private readonly AppDbContext _context;

        public ExamController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
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

            if (!model.EnableAdaptive && (model.SelectedQuestionIds == null || !model.SelectedQuestionIds.Any()))
            {
                ModelState.AddModelError(string.Empty, "Please select at least one question for the exam, or enable the Adaptive Engine.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateExamCreationDataAsync(model);
                return View(model);
            }

            var scoringMode = model.ResolveScoringMode();

            var exam = new Exam
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                CreatedByUserId = user.UserId,
                DurationMinutes = model.DurationMinutes,
                StartTime = model.StartTime.ToUniversalTime(),
                EndTime = model.EndTime.ToUniversalTime(),
                NegativeMarking = model.NegativeMarking,
                ShuffleQuestions = model.ShuffleQuestions,
                ScoringMode = scoringMode,
                TotalMarks = 0
            };

            int totalMarks = 0;
            if (model.EnableAdaptive)
            {
                var selectedConceptIds = new HashSet<int>();
                if (model.ConceptPools != null)
                {
                    foreach (var pool in model.ConceptPools)
                    {
                        if (pool.EasyCount > 0 || pool.MediumCount > 0 || pool.HardCount > 0)
                        {
                            selectedConceptIds.Add(pool.ConceptTagId);
                            exam.QuestionPools.Add(new ExamQuestionPool
                            {
                                ConceptTagId = pool.ConceptTagId,
                                EasyCount = pool.EasyCount,
                                MediumCount = pool.MediumCount,
                                HardCount = pool.HardCount
                            });
                        }
                    }
                }

                var questionsToLink = new List<Question>();
                if (model.SelectedQuestionIds != null && model.SelectedQuestionIds.Any())
                {
                    questionsToLink = await _context.Questions
                        .Where(q => model.SelectedQuestionIds.Contains(q.QuestionId))
                        .ToListAsync();
                }
                else if (selectedConceptIds.Any())
                {
                    questionsToLink = await _context.Questions
                        .Where(q => selectedConceptIds.Contains(q.ConceptTagId))
                        .ToListAsync();
                }
                else
                {
                    questionsToLink = await _context.Questions.Take(10).ToListAsync();
                }

                int seq = 1;
                foreach (var q in questionsToLink)
                {
                    exam.ExamQuestions.Add(new ExamQuestion
                    {
                        QuestionId = q.QuestionId,
                        SequenceOrder = seq++
                    });
                    totalMarks += q.Marks;
                }
            }
            else
            {
                var selectedIds = model.SelectedQuestionIds ?? new List<int>();
                var questions = await _context.Questions
                    .Where(q => selectedIds.Contains(q.QuestionId))
                    .ToListAsync();

                int seq = 1;
                foreach (var q in questions)
                {
                    exam.ExamQuestions.Add(new ExamQuestion
                    {
                        QuestionId = q.QuestionId,
                        SequenceOrder = seq++
                    });
                    totalMarks += q.Marks;
                }
            }

            exam.TotalMarks = totalMarks > 0 ? totalMarks : 30;

            await _context.Exams.AddAsync(exam);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Exam \"{exam.Title}\" was successfully created and scheduled!";
            return RedirectToAction("Index", "Dashboard");
        }

        private async Task PopulateExamCreationDataAsync(ExamCreateVM model)
        {
            var conceptTags = await _context.ConceptTags.ToListAsync();
            var questions = await _context.Questions.Include(q => q.ConceptTag).ToListAsync();

            model.ConceptPools = conceptTags.Select(ct => new ConceptPoolVM
            {
                ConceptTagId = ct.ConceptTagId,
                ConceptName = ct.Name,
                Subject = ct.Subject,
                AvailableEasy = questions.Count(q => q.ConceptTagId == ct.ConceptTagId && q.DifficultyLevel == DifficultyLevel.Easy),
                AvailableMedium = questions.Count(q => q.ConceptTagId == ct.ConceptTagId && q.DifficultyLevel == DifficultyLevel.Medium),
                AvailableHard = questions.Count(q => q.ConceptTagId == ct.ConceptTagId && q.DifficultyLevel == DifficultyLevel.Hard),
                EasyCount = 2,
                MediumCount = 2,
                HardCount = 1
            }).ToList();

            var selectedIds = model.SelectedQuestionIds?.ToHashSet() ?? new HashSet<int>();

            model.AvailableQuestions = questions.Select(q => new QuestionSelectVM
            {
                QuestionId = q.QuestionId,
                Text = q.Text,
                Subject = q.Subject,
                ConceptTagId = q.ConceptTagId,
                ConceptName = q.ConceptTag?.Name ?? "General",
                DifficultyLevel = q.DifficultyLevel,
                Marks = q.Marks,
                IsSelected = selectedIds.Contains(q.QuestionId)
            }).ToList();
        }
    }
}
