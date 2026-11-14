using Microsoft.EntityFrameworkCore;
using Cognify.Core.Entities;

namespace Cognify.Data.Repositories
{
    public class AttemptRepository
    {
        private readonly AppDbContext _context;
        public AttemptRepository(AppDbContext context) => _context = context;

        public async Task<ExamAttempt?> GetByIdAsync(int id) =>
            await _context.ExamAttempts
                .Include(a => a.Answers).ThenInclude(ans => ans.Question)
                .Include(a => a.ConceptPerformances)
                .FirstOrDefaultAsync(a => a.AttemptId == id);

        public async Task<ExamAttempt> StartAsync(Exam exam, int userId)
        {
            var attempt = new ExamAttempt
            {
                ExamId = exam.ExamId,
                ExamTitleSnapshot = exam.Title,
                ExamTotalMarksSnapshot = exam.TotalMarks,
                IsAdaptiveSnapshot = exam.UsesIrtAdaptive,
                IsConfidenceWeightedSnapshot = exam.IsConfidenceWeighted,
                NegativeMarkingSnapshot = exam.NegativeMarking,
                UserId = userId,
                StartTime = DateTime.UtcNow
            };
            _context.ExamAttempts.Add(attempt);
            await _context.SaveChangesAsync();
            return attempt;
        }

        public async Task SaveAnswerAsync(AttemptAnswer answer)
        {
            _context.AttemptAnswers.Add(answer);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
