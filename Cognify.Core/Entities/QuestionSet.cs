namespace Cognify.Core.Entities
{
    public class QuestionSet
    {
        public int QuestionSetId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Subject { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public User? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public bool IsBuiltIn { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();

        public int QuestionCount => Questions.Count;
        public int TotalMarks => Questions.Sum(q => q.Marks);
    }
}

