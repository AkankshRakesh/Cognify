using Microsoft.EntityFrameworkCore;
using Cognify.Core.Entities;
using Cognify.Core.Enums;

namespace Cognify.Data.Repositories
{
    public class QuestionRepository
    {
        private readonly AppDbContext _context;
        public QuestionRepository(AppDbContext context) => _context = context;

        public async Task<Question?> GetByIdAsync(int id) =>
            await _context.Questions.Include(q => q.ConceptTag)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

        public async Task<List<Question>> GetBySubjectAsync(string subject) =>
            await _context.Questions.Where(q => q.Subject == subject).ToListAsync();

        public async Task<Question?> GetRandomUnseenAsync(int conceptTagId, DifficultyLevel difficulty, IEnumerable<int> excludeIds) =>
            await _context.Questions
                .Where(q => q.ConceptTagId == conceptTagId
                            && q.DifficultyLevel == difficulty
                            && !excludeIds.Contains(q.QuestionId))
                .OrderBy(q => Guid.NewGuid())
                .FirstOrDefaultAsync();

        public async Task AddAsync(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
        }
    }
}
