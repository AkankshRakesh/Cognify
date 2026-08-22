namespace Cognify.Core.Entities
{
    // Aggregated per-concept accuracy for a single attempt - powers the Weak Concept Map
    public class ConceptPerformance
    {
        public int ConceptPerformanceId { get; set; }
        public int AttemptId { get; set; }
        public ExamAttempt? Attempt { get; set; }
        public int ConceptTagId { get; set; }
        public ConceptTag? ConceptTag { get; set; }

        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public double AccuracyPercent => TotalQuestions == 0
            ? 0
            : (double)CorrectCount / TotalQuestions * 100;
    }
}
