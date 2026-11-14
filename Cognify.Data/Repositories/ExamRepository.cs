using Microsoft.EntityFrameworkCore;
using Cognify.Core.Entities;

namespace Cognify.Data.Repositories
{
    public class ExamRepository
    {
        private readonly AppDbContext _context;
        public ExamRepository(AppDbContext context) => _context = context;

        public async Task<Exam?> GetByIdAsync(int id) =>
            await _context.Exams
                .Include(e => e.ExamQuestions).ThenInclude(eq => eq.Question)
                .Include(e => e.QuestionPools)
                .Include(e => e.QuestionSet).ThenInclude(s => s!.Questions)
                .FirstOrDefaultAsync(e => e.ExamId == id);

        public async Task<List<Exam>> GetUpcomingAsync() =>
            await _context.Exams.Where(e => e.EndTime > DateTime.UtcNow)
                .OrderBy(e => e.StartTime).ToListAsync();

        public async Task AddAsync(Exam exam)
        {
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();
        }
    }
}
