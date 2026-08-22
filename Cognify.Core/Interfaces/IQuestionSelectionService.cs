using Cognify.Core.Entities;

namespace Cognify.Core.Interfaces
{
    public interface IQuestionSelectionService
    {
        Task<Question?> GetFirstQuestionAsync(ExamAttempt attempt);
        Task<Question?> GetNextQuestionAsync(ExamAttempt attempt, AttemptAnswer lastAnswer);
    }
}
