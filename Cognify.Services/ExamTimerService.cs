using Cognify.Core.Entities;

namespace Cognify.Services
{
    // Server-side authority on exam timing. The client-side JS countdown is
    // for UX only - this is what actually decides whether a submission is valid.
    public class ExamTimerService
    {
        public DateTime ComputeAttemptDeadline(Exam exam, ExamAttempt attempt) =>
            attempt.StartTime.AddMinutes(exam.DurationMinutes) < exam.EndTime
                ? attempt.StartTime.AddMinutes(exam.DurationMinutes)
                : exam.EndTime;

        public bool IsExpired(Exam exam, ExamAttempt attempt) =>
            DateTime.UtcNow > ComputeAttemptDeadline(exam, attempt);

        public TimeSpan TimeRemaining(Exam exam, ExamAttempt attempt)
        {
            var remaining = ComputeAttemptDeadline(exam, attempt) - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
