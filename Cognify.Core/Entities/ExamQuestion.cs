namespace Cognify.Core.Entities
{
    // Used when the exam is NOT adaptive - a pre-selected fixed list of questions
    public class ExamQuestion
    {
        public int ExamQuestionId { get; set; }
        public int ExamId { get; set; }
        public Exam? Exam { get; set; }
        public int QuestionId { get; set; }
        public Question? Question { get; set; }
        public int SequenceOrder { get; set; }
    }
}
