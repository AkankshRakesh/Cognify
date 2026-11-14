using System.ComponentModel.DataAnnotations;
using Cognify.Core.Enums;

namespace Cognify.Core.Entities
{
    public class ExamAttempt
    {
        [Key]
        public int AttemptId { get; set; }
        public int? ExamId { get; set; }
        public Exam? Exam { get; set; }
        public string? ExamTitleSnapshot { get; set; }
        public int? ExamTotalMarksSnapshot { get; set; }
        public bool? IsAdaptiveSnapshot { get; set; }
        public bool? IsConfidenceWeightedSnapshot { get; set; }
        public bool? NegativeMarkingSnapshot { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? SubmitTime { get; set; }
        public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;

        public double TotalScore { get; set; }

        // Calibration metrics - only populated if ConfidenceWeighted mode was on
        public double? OverconfidenceIndex { get; set; }
        public double? UnderconfidenceIndex { get; set; }

        public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
        public ICollection<ConceptPerformance> ConceptPerformances { get; set; } = new List<ConceptPerformance>();
    }
}
