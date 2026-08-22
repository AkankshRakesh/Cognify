namespace Cognify.Core.Entities
{
    // Used when the exam IS adaptive - defines how many Easy/Medium/Hard
    // questions must be available per concept for the adaptive engine to draw from.
    public class ExamQuestionPool
    {
        public int ExamQuestionPoolId { get; set; }
        public int ExamId { get; set; }
        public Exam? Exam { get; set; }
        public int ConceptTagId { get; set; }
        public ConceptTag? ConceptTag { get; set; }

        public int EasyCount { get; set; }
        public int MediumCount { get; set; }
        public int HardCount { get; set; }
    }
}
