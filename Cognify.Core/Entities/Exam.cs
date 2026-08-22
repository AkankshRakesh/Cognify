using Cognify.Core.Enums;

namespace Cognify.Core.Entities
{
    public class Exam
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CreatedByUserId { get; set; }
        public User? CreatedBy { get; set; }

        public int DurationMinutes { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalMarks { get; set; }

        public bool NegativeMarking { get; set; }
        public bool ShuffleQuestions { get; set; } = true;

        // The unique feature toggle set by the quiz maker
        public ScoringMode ScoringMode { get; set; } = ScoringMode.Standard;

        public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
        public ICollection<ExamQuestionPool> QuestionPools { get; set; } = new List<ExamQuestionPool>();
        public ICollection<ExamAttempt> Attempts { get; set; } = new List<ExamAttempt>();

        public bool IsAdaptive => ScoringMode.HasFlag(ScoringMode.Adaptive);
        public bool IsConfidenceWeighted => ScoringMode.HasFlag(ScoringMode.ConfidenceWeighted);
    }
}
