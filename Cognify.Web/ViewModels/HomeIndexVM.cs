namespace Cognify.Web.ViewModels
{
    public class HomeIndexVM
    {
        public string? UserName { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? Role { get; set; } // "Teacher" | "Student" | null

        // Platform-wide stats shown on the landing page
        public int TotalExamsConducted { get; set; }
        public int TotalQuestionsInBank { get; set; }
        public int TotalStudentsRegistered { get; set; }
        public double AveragePassPercentage { get; set; }

        // Upcoming / live exams relevant to the logged-in student
        public List<UpcomingExamCardVM> UpcomingExams { get; set; } = new();

        // Recent results relevant to the logged-in student
        public List<RecentResultCardVM> RecentResults { get; set; } = new();
    }

    public class UpcomingExamCardVM
    {
        public int ExamId { get; set; }
        public string ExamToken { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsAdaptive { get; set; }
        public bool IsConfidenceWeighted { get; set; }
    }

    public class RecentResultCardVM
    {
        public int ExamId { get; set; }
        public string ExamToken { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double Score { get; set; }
        public double TotalMarks { get; set; }
        public DateTime SubmittedOn { get; set; }
    }
}
