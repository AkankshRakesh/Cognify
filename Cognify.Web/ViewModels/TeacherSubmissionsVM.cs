namespace Cognify.Web.ViewModels
{
    public class TeacherSubmissionsVM
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string ShareUrl { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationMinutes { get; set; }
        public int QuestionCount { get; set; }
        public int TotalMarks { get; set; }
        public List<TeacherSubmissionVM> Submissions { get; set; } = new();
        public double AverageScore => Submissions.Where(s => s.IsCompleted).Select(s => s.Percent).DefaultIfEmpty().Average();
        public double HighestScore => Submissions.Where(s => s.IsCompleted).Select(s => s.Percent).DefaultIfEmpty().Max();
    }

    public class TeacherSubmissionVM
    {
        public int AttemptId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartedOn { get; set; }
        public DateTime? SubmittedOn { get; set; }
        public double Score { get; set; }
        public int TotalMarks { get; set; }
        public bool IsAdaptive { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int SkippedCount { get; set; }
        public double? CalibrationScore { get; set; }
        public List<TeacherSubmissionAnswerVM> Answers { get; set; } = new();
        public bool IsCompleted => Status == "Submitted" || Status == "Auto-submitted";
        public double Percent => TotalMarks == 0 ? 0 : Math.Round(Score * 100 / TotalMarks, 1);
        public string DurationText
        {
            get
            {
                var end = SubmittedOn ?? DateTime.UtcNow;
                var minutes = Math.Max(0, (int)Math.Round((end - StartedOn).TotalMinutes));
                return $"{minutes} min";
            }
        }
    }

    public class TeacherSubmissionAnswerVM
    {
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? SelectedOption { get; set; }
        public string CorrectOption { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public double MarksAwarded { get; set; }
        public string Difficulty { get; set; } = string.Empty;
    }
}
