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
    public class QuestionBankController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUrlTokenProtector _urlTokens;

        public QuestionBankController(AppDbContext context, IUrlTokenProtector urlTokens)
        {
            _context = context;
            _urlTokens = urlTokens;
        }

        // GET: /QuestionBank
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await DbInitializer.InitializeAsync(_context);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var sets = await _context.QuestionSets
                .Where(s => userRole == UserRole.Admin.ToString() || s.CreatedByUserId == userId)
                .Include(s => s.Questions)
                .OrderByDescending(s => s.CreatedOn)
                .ToListAsync();

            var vm = new QuestionBankIndexVM
            {
                QuestionSets = sets.Select(s => new QuestionSetCatalogVM
                {
                    QuestionSetId = s.QuestionSetId,
                    Title = s.Title,
                    Description = s.Description,
                    Subject = s.Subject,
                    QuestionCount = s.Questions.Count,
                    TotalMarks = s.Questions.Sum(q => q.Marks),
                    CreatedOn = s.CreatedOn
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditSet(string token)
        {
            if (!_urlTokens.TryUnprotectQuestionSet(token, out var id)) return BadRequest("Invalid question bank link.");
            var set = await GetManageableSetAsync(id);
            if (set == null) return RedirectToAction(nameof(Index));

            await PopulateCreateSetViewAsync();
            ViewBag.QuestionSetToken = _urlTokens.ProtectQuestionSet(set.QuestionSetId);
            return View(new QuestionSetCreateVM
            {
                Title = set.Title,
                Description = set.Description,
                Subject = set.Subject,
                ConceptTagId = set.Questions.Select(q => (int?)q.ConceptTagId).FirstOrDefault(),
                Questions = set.Questions.OrderBy(q => q.QuestionId).Select(q => new QuestionInputVM
                {
                    QuestionId = q.QuestionId,
                    Text = q.Text,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectOption = q.CorrectOption,
                    DifficultyLevel = q.DifficultyLevel,
                    Marks = q.Marks
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSet(string token, QuestionSetCreateVM model)
        {
            if (!_urlTokens.TryUnprotectQuestionSet(token, out var id)) return BadRequest("Invalid question bank link.");
            var set = await GetManageableSetAsync(id);
            if (set == null) return RedirectToAction(nameof(Index));

            if (!ModelState.IsValid || !model.Questions.Any(q => !string.IsNullOrWhiteSpace(q.Text)))
            {
                await PopulateCreateSetViewAsync();
                ViewBag.QuestionSetToken = _urlTokens.ProtectQuestionSet(id);
                ModelState.AddModelError(string.Empty, "Add at least one complete question to this set.");
                return View(model);
            }

            var submittedQuestions = model.Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.Text))
                .ToList();
            var existingIds = submittedQuestions
                .Where(q => q.QuestionId > 0)
                .Select(q => q.QuestionId)
                .ToHashSet();

            foreach (var existing in set.Questions.Where(q => !existingIds.Contains(q.QuestionId)).ToList())
            {
                var usedInExam = await _context.ExamQuestions.AnyAsync(eq => eq.QuestionId == existing.QuestionId);
                if (usedInExam)
                    existing.QuestionSetId = null;
                else
                    _context.Questions.Remove(existing);
            }

            var newQuestions = submittedQuestions.Where(q => q.QuestionId <= 0).ToList();
            var concept = newQuestions.Any() ? await ResolveConceptAsync(model) : null;
            if (newQuestions.Any() && concept == null)
            {
                await PopulateCreateSetViewAsync();
                ViewBag.QuestionSetToken = _urlTokens.ProtectQuestionSet(id);
                ModelState.AddModelError(nameof(model.ConceptTagId), "Select or create a concept for new questions.");
                return View(model);
            }

            set.Title = model.Title.Trim();
            set.Description = model.Description?.Trim();
            set.Subject = string.IsNullOrWhiteSpace(model.Subject) ? "General" : model.Subject.Trim();

            foreach (var input in submittedQuestions.Where(q => q.QuestionId > 0))
            {
                var question = set.Questions.FirstOrDefault(q => q.QuestionId == input.QuestionId);
                if (question == null) continue;
                UpdateQuestion(question, input);
            }

            foreach (var input in newQuestions)
            {
                var question = new Question
                {
                    Subject = set.Subject,
                    ConceptTagId = concept!.ConceptTagId,
                    QuestionSetId = set.QuestionSetId
                };
                UpdateQuestion(question, input);
                set.Questions.Add(question);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Question set \"{set.Title}\" was updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CreateSet()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != UserRole.Teacher.ToString() && userRole != UserRole.Admin.ToString())
                return RedirectToAction("Index", "Dashboard");

            await PopulateCreateSetViewAsync();
            return View(new QuestionSetCreateVM());
        }

        // POST: /QuestionBank/CreateSet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSet(QuestionSetCreateVM model, string? returnUrl = null)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != UserRole.Teacher.ToString() && userRole != UserRole.Admin.ToString())
            {
                TempData["ErrorMessage"] = "Only teachers can add question sets.";
                return RedirectToAction("Index", "Dashboard");
            }

            var concept = await ResolveConceptAsync(model);
            if (concept == null)
            {
                TempData["ErrorMessage"] = "Please select or provide a valid concept name.";
                await PopulateCreateSetViewAsync();
                return View(model);
            }

            var validQuestions = model.Questions.Where(q => !string.IsNullOrWhiteSpace(q.Text)).ToList();
            if (!validQuestions.Any())
            {
                TempData["ErrorMessage"] = "No valid questions were provided in the set.";
                await PopulateCreateSetViewAsync();
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                await PopulateCreateSetViewAsync();
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var questionSet = new QuestionSet
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                Subject = string.IsNullOrWhiteSpace(model.Subject) ? concept.Subject : model.Subject.Trim(),
                CreatedByUserId = userId
            };
            _context.QuestionSets.Add(questionSet);

            foreach (var q in validQuestions)
            {
                var question = new Question
                {
                    Text = q.Text.Trim(),
                    OptionA = q.OptionA.Trim(),
                    OptionB = q.OptionB.Trim(),
                    OptionC = q.OptionC.Trim(),
                    OptionD = q.OptionD.Trim(),
                    CorrectOption = string.IsNullOrWhiteSpace(q.CorrectOption) ? "A" : q.CorrectOption.Trim().ToUpper(),
                    Subject = concept.Subject,
                    ConceptTagId = concept.ConceptTagId,
                    DifficultyLevel = q.DifficultyLevel,
                    Marks = q.Marks > 0 ? q.Marks : 4,
                    TimesAttempted = 0,
                    TimesCorrect = 0
                };
                questionSet.Questions.Add(question);
                await _context.Questions.AddAsync(question);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Question set \"{questionSet.Title}\" was created with {validQuestions.Count} question(s).";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        // GET: /QuestionBank/GetConcepts
        [HttpGet]
        public async Task<IActionResult> GetConcepts()
        {
            var concepts = await _context.ConceptTags
                .Select(c => new { c.ConceptTagId, c.Name, c.Subject })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Json(concepts);
        }

        private async Task<ConceptTag?> ResolveConceptAsync(QuestionSetCreateVM model)
        {
            if (model.ConceptTagId.HasValue && model.ConceptTagId.Value > 0)
            {
                return await _context.ConceptTags.FindAsync(model.ConceptTagId.Value);
            }

            if (!string.IsNullOrWhiteSpace(model.NewConceptName))
            {
                var name = model.NewConceptName.Trim();
                var existing = await _context.ConceptTags
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

                if (existing != null) return existing;

                var sub = string.IsNullOrWhiteSpace(model.Subject) ? "General" : model.Subject.Trim();
                var newTag = new ConceptTag
                {
                    Name = name,
                    Subject = sub
                };
                await _context.ConceptTags.AddAsync(newTag);
                await _context.SaveChangesAsync();
                return newTag;
            }

            return await _context.ConceptTags.FirstOrDefaultAsync();
        }

        private async Task<QuestionSet?> GetManageableSetAsync(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role);
            return await _context.QuestionSets
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.QuestionSetId == id &&
                    (role == UserRole.Admin.ToString() || s.CreatedByUserId == userId));
        }

        private static void UpdateQuestion(Question question, QuestionInputVM input)
        {
            question.Text = input.Text.Trim();
            question.OptionA = input.OptionA.Trim();
            question.OptionB = input.OptionB.Trim();
            question.OptionC = input.OptionC.Trim();
            question.OptionD = input.OptionD.Trim();
            question.CorrectOption = string.IsNullOrWhiteSpace(input.CorrectOption) ? "A" : input.CorrectOption.Trim().ToUpper();
            question.DifficultyLevel = input.DifficultyLevel;
            question.Marks = input.Marks > 0 ? input.Marks : 1;
        }

        private async Task PopulateCreateSetViewAsync()
        {
            ViewBag.Concepts = await _context.ConceptTags
                .OrderBy(c => c.Name)
                .Select(c => new { c.ConceptTagId, c.Name, c.Subject })
                .ToListAsync();
        }
    }
}
