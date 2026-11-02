using Microsoft.AspNetCore.Mvc;
using Cognify.Web.ViewModels;
using Cognify.Web.Security;

namespace Cognify.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUrlTokenProtector _urlTokens;

        public HomeController(ILogger<HomeController> logger, IUrlTokenProtector urlTokens)
        {
            _logger = logger;
            _urlTokens = urlTokens;
        }

        // GET: /
        public IActionResult Index()
        {
            // NOTE: In the full build this pulls real data via ExamRepository /
            // AttemptRepository / QuestionRepository. Wired here with representative
            // placeholder data so the landing page is fully demoable out of the box.
            var vm = new HomeIndexVM
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                UserName = User.Identity?.IsAuthenticated == true ? User.Identity!.Name : null,
                Role = null,

                TotalExamsConducted = 128,
                TotalQuestionsInBank = 3450,
                TotalStudentsRegistered = 964,
                AveragePassPercentage = 78.4,

                UpcomingExams = new List<UpcomingExamCardVM>
                {
                    new()
                    {
                        ExamId = 1,
                        ExamToken = _urlTokens.ProtectExam(1),
                        Title = "Data Structures - Unit Test 2",
                        Subject = "Computer Science",
                        StartTime = DateTime.UtcNow.AddHours(5),
                        DurationMinutes = 45,
                        IsAdaptive = true,
                        IsConfidenceWeighted = true
                    },
                    new()
                    {
                        ExamId = 2,
                        ExamToken = _urlTokens.ProtectExam(2),
                        Title = "Thermodynamics - Quick Quiz",
                        Subject = "Mechanical Engineering",
                        StartTime = DateTime.UtcNow.AddDays(1),
                        DurationMinutes = 20,
                        IsAdaptive = false,
                        IsConfidenceWeighted = true
                    },
                    new()
                    {
                        ExamId = 3,
                        ExamToken = _urlTokens.ProtectExam(3),
                        Title = "General Aptitude - Mock Test",
                        Subject = "Aptitude",
                        StartTime = DateTime.UtcNow.AddDays(2),
                        DurationMinutes = 60,
                        IsAdaptive = true,
                        IsConfidenceWeighted = false
                    }
                },

                RecentResults = new List<RecentResultCardVM>
                {
                    new() { ExamId = 10, ExamToken = _urlTokens.ProtectExam(10), Title = "DBMS - Unit Test 1", Score = 42, TotalMarks = 50, SubmittedOn = DateTime.UtcNow.AddDays(-3) },
                    new() { ExamId = 11, ExamToken = _urlTokens.ProtectExam(11), Title = "Operating Systems - Quiz 3", Score = 18, TotalMarks = 25, SubmittedOn = DateTime.UtcNow.AddDays(-7) }
                }
            };

            return View(vm);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}
