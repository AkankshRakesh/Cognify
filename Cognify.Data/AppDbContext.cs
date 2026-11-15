using Microsoft.EntityFrameworkCore;
using Cognify.Core.Entities;

namespace Cognify.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<ConceptTag> ConceptTags => Set<ConceptTag>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<QuestionSet> QuestionSets => Set<QuestionSet>();
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
        public DbSet<ExamQuestionPool> ExamQuestionPools => Set<ExamQuestionPool>();
        public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
        public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
        public DbSet<ConceptPerformance> ConceptPerformances => Set<ConceptPerformance>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.CreatedBy)
                .WithMany(u => u.ExamsCreated)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.ConceptTag)
                .WithMany(c => c.Questions)
                .HasForeignKey(q => q.ConceptTagId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.QuestionSet)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.QuestionSetId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<QuestionSet>()
                .HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.QuestionSet)
                .WithMany(s => s.Exams)
                .HasForeignKey(e => e.QuestionSetId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamQuestion>()
                .HasOne(eq => eq.Exam)
                .WithMany(e => e.ExamQuestions)
                .HasForeignKey(eq => eq.ExamId);

            modelBuilder.Entity<ExamQuestion>()
                .HasOne(eq => eq.Question)
                .WithMany()
                .HasForeignKey(eq => eq.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamQuestionPool>()
                .HasOne(p => p.Exam)
                .WithMany(e => e.QuestionPools)
                .HasForeignKey(p => p.ExamId);

            modelBuilder.Entity<ExamAttempt>()
                .HasOne(a => a.Exam)
                .WithMany(e => e.Attempts)
                .HasForeignKey(a => a.ExamId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ExamAttempt>()
                .HasOne(a => a.User)
                .WithMany(u => u.Attempts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttemptAnswer>()
                .HasOne(a => a.Attempt)
                .WithMany(at => at.Answers)
                .HasForeignKey(a => a.AttemptId);

            modelBuilder.Entity<ConceptPerformance>()
                .HasOne(cp => cp.Attempt)
                .WithMany(a => a.ConceptPerformances)
                .HasForeignKey(cp => cp.AttemptId);

            modelBuilder.Entity<ExamAttempt>()
                .HasIndex(a => new { a.ExamId, a.UserId })
                .IsUnique();
        }
    }
}
