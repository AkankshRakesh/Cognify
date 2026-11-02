using Cognify.Core.Enums;

namespace Cognify.Web.ViewModels
{
    public class TeacherDashboardVM
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public List<TeacherQuestionSetSummaryVM> QuestionSets { get; set; } = new();

        // Key Analytical Stat Tiles
        public int TotalExamsCreated { get; set; }
        public int ActiveExamsCount { get; set; }
        public int TotalSubmissions { get; set; }
        public double AverageClassScore { get; set; } // 0-100%
        public double AverageCalibrationScore { get; set; } // 0-100%

        // Authored Exams
        public List<TeacherExamSummaryVM> Exams { get; set; } = new();

        // Class Concept Health Analytics
        public List<TeacherConceptAnalyticsVM> ConceptAnalytics { get; set; } = new();

        // Recent Student Submissions
        public List<TeacherRecentSubmissionVM> RecentSubmissions { get; set; } = new();

        // Question Bank Overview
        public List<ConceptQuestionCountVM> QuestionBankSummary { get; set; } = new();
    }

    public class TeacherQuestionSetSummaryVM
    {
        public int QuestionSetId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Subject { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public int TotalMarks { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class TeacherExamSummaryVM
    {
        public int ExamId { get; set; }
        public string ExamToken { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int TotalMarks { get; set; }
        public int QuestionCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAdaptive { get; set; }
        public bool IsConfidenceWeighted { get; set; }
        public bool IsActive => StartTime <= DateTime.UtcNow && EndTime >= DateTime.UtcNow;
        public bool IsUpcoming => StartTime > DateTime.UtcNow;
        public bool IsEnded => EndTime < DateTime.UtcNow;
        public int AttemptsCount { get; set; }
        public double AverageScore { get; set; }
        public string ShareUrl { get; set; } = string.Empty;
    }

    public class TeacherConceptAnalyticsVM
    {
        public string ConceptName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int TotalQuestionsAnswered { get; set; }
        public int CorrectCount { get; set; }
        public double AccuracyPercent => TotalQuestionsAnswered == 0 ? 0 : Math.Round((double)CorrectCount / TotalQuestionsAnswered * 100.0, 1);
        public string HealthStatus => AccuracyPercent >= 75 ? "Strong" : AccuracyPercent >= 50 ? "Moderate" : "Needs Review";
    }

    public class TeacherRecentSubmissionVM
    {
        public int AttemptId { get; set; }
        public string AttemptToken { get; set; } = string.Empty;
        public int ExamId { get; set; }
        public string ExamToken { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public double Score { get; set; }
        public int TotalMarks { get; set; }
        public double Percent => TotalMarks == 0 ? 0 : Math.Round(Score * 100.0 / TotalMarks, 1);
        public DateTime SubmittedOn { get; set; }
        public double? CalibrationScore { get; set; }
    }

    public class ConceptQuestionCountVM
    {
        public string ConceptName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int EasyCount { get; set; }
        public int MediumCount { get; set; }
        public int HardCount { get; set; }
    }
}

