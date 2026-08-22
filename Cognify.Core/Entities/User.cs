using Cognify.Core.Enums;

namespace Cognify.Core.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public ICollection<Exam> ExamsCreated { get; set; } = new List<Exam>();
        public ICollection<ExamAttempt> Attempts { get; set; } = new List<ExamAttempt>();
    }
}
