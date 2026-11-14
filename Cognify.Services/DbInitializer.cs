using Microsoft.EntityFrameworkCore;
using Cognify.Core.Entities;
using Cognify.Core.Enums;
using Cognify.Data;

namespace Cognify.Services
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            await context.Database.EnsureCreatedAsync();
            await EnsureQuestionSetSchemaAsync(context);
            await EnsureAdaptiveExamSchemaAsync(context);
            await EnsureAttemptReportSchemaAsync(context);

            // 1. Seed Concept Tags
            if (!await context.ConceptTags.AnyAsync())
            {
                var conceptTags = new List<ConceptTag>
                {
                    new() { Name = "Data Structures", Subject = "Computer Science" },
                    new() { Name = "Algorithms", Subject = "Computer Science" },
                    new() { Name = "Thermodynamics", Subject = "Physics" },
                    new() { Name = "Logical Reasoning", Subject = "Aptitude" },
                    new() { Name = "Quantitative Aptitude", Subject = "Aptitude" }
                };

                await context.ConceptTags.AddRangeAsync(conceptTags);
                await context.SaveChangesAsync();
            }

            // 2. Seed Teacher User if missing
            var teacher = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Teacher);
            if (teacher == null)
            {
                teacher = new User
                {
                    Name = "Prof. Alan Turing",
                    Email = "teacher@cognify.com",
                    PasswordHash = PasswordHasher.Hash("Teacher123!"),
                    Role = UserRole.Teacher,
                    CreatedOn = DateTime.UtcNow.AddMonths(-1)
                };
                await context.Users.AddAsync(teacher);
                await context.SaveChangesAsync();
            }

            // 3. Seed Questions
            if (!await context.Questions.AnyAsync())
            {
                var dsTag = await context.ConceptTags.FirstAsync(c => c.Name == "Data Structures");
                var algoTag = await context.ConceptTags.FirstAsync(c => c.Name == "Algorithms");
                var thermoTag = await context.ConceptTags.FirstAsync(c => c.Name == "Thermodynamics");
                var logicTag = await context.ConceptTags.FirstAsync(c => c.Name == "Logical Reasoning");
                var quantTag = await context.ConceptTags.FirstAsync(c => c.Name == "Quantitative Aptitude");

                var questions = new List<Question>
                {
                    // Data Structures
                    new()
                    {
                        Text = "Which data structure operates strictly on a Last-In, First-Out (LIFO) basis?",
                        OptionA = "Queue", OptionB = "Stack", OptionC = "Array", OptionD = "Linked List",
                        CorrectOption = "B", Subject = "Computer Science", ConceptTagId = dsTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Easy, Marks = 4, TimesAttempted = 10, TimesCorrect = 9
                    },
                    new()
                    {
                        Text = "What is the worst-case time complexity of searching in a balanced AVL Binary Search Tree?",
                        OptionA = "O(1)", OptionB = "O(n)", OptionC = "O(log n)", OptionD = "O(n log n)",
                        CorrectOption = "C", Subject = "Computer Science", ConceptTagId = dsTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Medium, Marks = 4, TimesAttempted = 15, TimesCorrect = 11
                    },
                    new()
                    {
                        Text = "Which data structure is primarily utilized in the standard implementation of Breadth-First Search (BFS)?",
                        OptionA = "Stack", OptionB = "Queue", OptionC = "Priority Queue", OptionD = "Hash Table",
                        CorrectOption = "B", Subject = "Computer Science", ConceptTagId = dsTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Easy, Marks = 4, TimesAttempted = 12, TimesCorrect = 10
                    },
                    new()
                    {
                        Text = "In a min-heap with n elements, what is the time complexity to extract the root minimum element?",
                        OptionA = "O(1)", OptionB = "O(log n)", OptionC = "O(n)", OptionD = "O(n log n)",
                        CorrectOption = "B", Subject = "Computer Science", ConceptTagId = dsTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Hard, Marks = 4, TimesAttempted = 8, TimesCorrect = 5
                    },

                    // Algorithms
                    new()
                    {
                        Text = "Which algorithmic design paradigm does the Merge Sort algorithm employ?",
                        OptionA = "Greedy Approach", OptionB = "Divide and Conquer", OptionC = "Dynamic Programming", OptionD = "Backtracking",
                        CorrectOption = "B", Subject = "Computer Science", ConceptTagId = algoTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Easy, Marks = 4, TimesAttempted = 14, TimesCorrect = 13
                    },
                    new()
                    {
                        Text = "Dijkstra's shortest path algorithm cannot correctly handle graphs with which edge property?",
                        OptionA = "Directed cycles", OptionB = "Negative weight edges", OptionC = "Disconnected components", OptionD = "Weighted edges > 100",
                        CorrectOption = "B", Subject = "Computer Science", ConceptTagId = algoTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Hard, Marks = 4, TimesAttempted = 9, TimesCorrect = 6
                    },
                    new()
                    {
                        Text = "What is the average time complexity of the QuickSort algorithm with random pivoting?",
                        OptionA = "O(n^2)", OptionB = "O(n log n)", OptionC = "O(n)", OptionD = "O(log n)",
                        CorrectOption = "B", Subject = "Computer Science", ConceptTagId = algoTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Medium, Marks = 4, TimesAttempted = 11, TimesCorrect = 9
                    },

                    // Thermodynamics
                    new()
                    {
                        Text = "Which law of thermodynamics states that the entropy of an isolated system never decreases over time?",
                        OptionA = "Zeroth Law", OptionB = "First Law", OptionC = "Second Law", OptionD = "Third Law",
                        CorrectOption = "C", Subject = "Physics", ConceptTagId = thermoTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Medium, Marks = 5, TimesAttempted = 8, TimesCorrect = 6
                    },
                    new()
                    {
                        Text = "In an adiabatic thermodynamic process, which quantity across the system boundary is strictly zero?",
                        OptionA = "Work done (W)", OptionB = "Heat transfer (Q)", OptionC = "Change in temperature (dT)", OptionD = "Pressure change (dP)",
                        CorrectOption = "B", Subject = "Physics", ConceptTagId = thermoTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Medium, Marks = 5, TimesAttempted = 7, TimesCorrect = 5
                    },
                    new()
                    {
                        Text = "The ideal Carnot cycle consists of which four thermodynamic processes?",
                        OptionA = "Two isothermal and two adiabatic reversible processes",
                        OptionB = "Two isobaric and two isochoric processes",
                        OptionC = "Four isothermal reversible processes",
                        OptionD = "Two adiabatic and two isobaric processes",
                        CorrectOption = "A", Subject = "Physics", ConceptTagId = thermoTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Hard, Marks = 5, TimesAttempted = 6, TimesCorrect = 4
                    },

                    // Logical Reasoning
                    new()
                    {
                        Text = "If all Bloops are Razzies and all Razzies are Lazzies, then are all Bloops definitely Lazzies?",
                        OptionA = "Yes, unconditionally", OptionB = "No, never", OptionC = "Cannot be deduced", OptionD = "Only if Lazzies exist",
                        CorrectOption = "A", Subject = "Aptitude", ConceptTagId = logicTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Easy, Marks = 4, TimesAttempted = 20, TimesCorrect = 19
                    },
                    new()
                    {
                        Text = "Find the next number in the geometric series: 2, 1, 1/2, 1/4, ...",
                        OptionA = "1/3", OptionB = "1/8", OptionC = "2/8", OptionD = "1/16",
                        CorrectOption = "B", Subject = "Aptitude", ConceptTagId = logicTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Easy, Marks = 4, TimesAttempted = 22, TimesCorrect = 20
                    },
                    new()
                    {
                        Text = "Pointing to a photograph, a man said: 'I have no brothers or sisters, and that man\\'s father is my father\\'s son.' Whose photo was it?",
                        OptionA = "His own", OptionB = "His son's", OptionC = "His father's", OptionD = "His nephew's",
                        CorrectOption = "B", Subject = "Aptitude", ConceptTagId = logicTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Medium, Marks = 4, TimesAttempted = 18, TimesCorrect = 14
                    },

                    // Quantitative Aptitude
                    new()
                    {
                        Text = "A train running at 60 km/h crosses a static signal pole in 9 seconds. What is the length of the train in meters?",
                        OptionA = "120 m", OptionB = "180 m", OptionC = "150 m", OptionD = "324 m",
                        CorrectOption = "C", Subject = "Aptitude", ConceptTagId = quantTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Medium, Marks = 4, TimesAttempted = 15, TimesCorrect = 12
                    },
                    new()
                    {
                        Text = "What is 15% of 40% of 500?",
                        OptionA = "20", OptionB = "30", OptionC = "40", OptionD = "50",
                        CorrectOption = "B", Subject = "Aptitude", ConceptTagId = quantTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Easy, Marks = 4, TimesAttempted = 25, TimesCorrect = 22
                    },
                    new()
                    {
                        Text = "The sum of ages of 5 children born at intervals of 3 years each is 50 years. What is the age of the youngest child?",
                        OptionA = "4 years", OptionB = "8 years", OptionC = "10 years", OptionD = "2 years",
                        CorrectOption = "A", Subject = "Aptitude", ConceptTagId = quantTag.ConceptTagId,
                        DifficultyLevel = DifficultyLevel.Hard, Marks = 4, TimesAttempted = 12, TimesCorrect = 8
                    }
                };

                await context.Questions.AddRangeAsync(questions);
                await context.SaveChangesAsync();
            }

            // Keep the existing seeded catalog usable with the teacher workflow.
            if (!await context.QuestionSets.AnyAsync())
            {
                var teacherUser = await context.Users.FirstAsync(u => u.Role == UserRole.Teacher);
                var starterSet = new QuestionSet
                {
                    Title = "Starter Question Set",
                    Description = "Seeded questions available for your first exam.",
                    Subject = "Mixed subjects",
                    CreatedByUserId = teacherUser.UserId
                };
                starterSet.Questions = await context.Questions.ToListAsync();
                await context.QuestionSets.AddAsync(starterSet);
                await context.SaveChangesAsync();
            }

            // 4. Seed Exams
            if (!await context.Exams.AnyAsync())
            {
                var teacherUser = await context.Users.FirstAsync(u => u.Role == UserRole.Teacher);
                var allQuestions = await context.Questions.ToListAsync();
                var dsQuestions = allQuestions.Where(q => q.Subject == "Computer Science").ToList();
                var thermoQuestions = allQuestions.Where(q => q.Subject == "Physics").ToList();
                var aptitudeQuestions = allQuestions.Where(q => q.Subject == "Aptitude").ToList();

                // Exam 0: Diagnostic Assessment (Used for new account baseline)
                var diagExam = new Exam
                {
                    Title = "Cognify Diagnostic & Calibration Assessment",
                    Description = "Baseline evaluation across Data Structures, Logical Reasoning, and Aptitude.",
                    CreatedByUserId = teacherUser.UserId,
                    DurationMinutes = 20,
                    StartTime = DateTime.UtcNow.AddDays(-30),
                    EndTime = DateTime.UtcNow.AddYears(1),
                    TotalMarks = 20,
                    NegativeMarking = false,
                    ShuffleQuestions = false,
                    ScoringMode = ScoringMode.Both
                };
                await context.Exams.AddAsync(diagExam);
                await context.SaveChangesAsync();

                // Add questions to Exam 0
                var diagQuestions = new List<Question>
                {
                    allQuestions.First(q => q.Text.Contains("LIFO")),
                    allQuestions.First(q => q.Text.Contains("balanced AVL")),
                    allQuestions.First(q => q.Text.Contains("Bloops")),
                    allQuestions.First(q => q.Text.Contains("geometric series")),
                    allQuestions.First(q => q.Text.Contains("train running at 60 km/h"))
                };
                for (int i = 0; i < diagQuestions.Count; i++)
                {
                    context.ExamQuestions.Add(new ExamQuestion
                    {
                        ExamId = diagExam.ExamId,
                        QuestionId = diagQuestions[i].QuestionId,
                        SequenceOrder = i + 1
                    });
                }

                // Exam 1: DSA Midterm (Active Quest)
                var dsaExam = new Exam
                {
                    Title = "Data Structures & Algorithms Midterm",
                    Description = "In-depth adaptive test on Trees, Graphs, Sorting, and Heaps.",
                    CreatedByUserId = teacherUser.UserId,
                    DurationMinutes = 45,
                    StartTime = DateTime.UtcNow.AddHours(-1), // Open now
                    EndTime = DateTime.UtcNow.AddDays(7),
                    TotalMarks = 40,
                    NegativeMarking = true,
                    ShuffleQuestions = true,
                    ScoringMode = ScoringMode.Both
                };
                await context.Exams.AddAsync(dsaExam);
                await context.SaveChangesAsync();

                for (int i = 0; i < dsQuestions.Count; i++)
                {
                    context.ExamQuestions.Add(new ExamQuestion
                    {
                        ExamId = dsaExam.ExamId,
                        QuestionId = dsQuestions[i].QuestionId,
                        SequenceOrder = i + 1
                    });
                }

                // Exam 2: Thermodynamics Quiz (Upcoming in 2 days)
                var thermoExam = new Exam
                {
                    Title = "Thermodynamics Quiz",
                    Description = "Confidence-weighted quiz covering entropy, heat transfer, and cyclic processes.",
                    CreatedByUserId = teacherUser.UserId,
                    DurationMinutes = 30,
                    StartTime = DateTime.UtcNow.AddDays(2), // Locked until then
                    EndTime = DateTime.UtcNow.AddDays(6),
                    TotalMarks = 30,
                    NegativeMarking = false,
                    ShuffleQuestions = true,
                    ScoringMode = ScoringMode.ConfidenceWeighted
                };
                await context.Exams.AddAsync(thermoExam);
                await context.SaveChangesAsync();

                for (int i = 0; i < thermoQuestions.Count; i++)
                {
                    context.ExamQuestions.Add(new ExamQuestion
                    {
                        ExamId = thermoExam.ExamId,
                        QuestionId = thermoQuestions[i].QuestionId,
                        SequenceOrder = i + 1
                    });
                }

                // Exam 3: General Aptitude & Logical Reasoning (Active Quest)
                var aptExam = new Exam
                {
                    Title = "General Aptitude & Reasoning Challenge",
                    Description = "Timed challenge on deductive logic, arithmetic sequences, and series.",
                    CreatedByUserId = teacherUser.UserId,
                    DurationMinutes = 40,
                    StartTime = DateTime.UtcNow.AddMinutes(-10), // Open now
                    EndTime = DateTime.UtcNow.AddDays(14),
                    TotalMarks = 30,
                    NegativeMarking = true,
                    ShuffleQuestions = true,
                    ScoringMode = ScoringMode.Standard
                };
                await context.Exams.AddAsync(aptExam);
                await context.SaveChangesAsync();

                for (int i = 0; i < aptitudeQuestions.Count; i++)
                {
                    context.ExamQuestions.Add(new ExamQuestion
                    {
                        ExamId = aptExam.ExamId,
                        QuestionId = aptitudeQuestions[i].QuestionId,
                        SequenceOrder = i + 1
                    });
                }

                await context.SaveChangesAsync();
            }

            // 5. Ensure existing users have initial data assigned
            var existingUsers = await context.Users.Where(u => u.Role == UserRole.Student).ToListAsync();
            foreach (var user in existingUsers)
            {
                await EnsureUserInitializedAsync(context, user.UserId);
            }

            await AdaptiveTestQuestionSetSeeder.SeedAsync(context);
        }

        private static async Task EnsureQuestionSetSchemaAsync(AppDbContext context)
        {
            // EnsureCreated does not change an existing database. This keeps databases
            // created by earlier versions compatible with the question-set workflow.
            const string sql = """
                IF OBJECT_ID(N'[QuestionSets]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [QuestionSets]
                    (
                        [QuestionSetId] int IDENTITY(1,1) NOT NULL,
                        [Title] nvarchar(max) NOT NULL,
                        [Description] nvarchar(max) NULL,
                        [Subject] nvarchar(max) NOT NULL,
                        [CreatedByUserId] int NOT NULL,
                        [CreatedOn] datetime2 NOT NULL,
                        [IsBuiltIn] bit NOT NULL CONSTRAINT [DF_QuestionSets_IsBuiltIn] DEFAULT 0,
                        CONSTRAINT [PK_QuestionSets] PRIMARY KEY ([QuestionSetId])
                    );
                END;

                IF COL_LENGTH(N'QuestionSets', N'IsBuiltIn') IS NULL
                    ALTER TABLE [QuestionSets] ADD [IsBuiltIn] bit NOT NULL
                        CONSTRAINT [DF_QuestionSets_IsBuiltIn] DEFAULT 0;

                IF COL_LENGTH(N'Questions', N'QuestionSetId') IS NULL
                    ALTER TABLE [Questions] ADD [QuestionSetId] int NULL;

                IF COL_LENGTH(N'Exams', N'QuestionSetId') IS NULL
                    ALTER TABLE [Exams] ADD [QuestionSetId] int NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Questions_QuestionSets_QuestionSetId')
                    ALTER TABLE [Questions] ADD CONSTRAINT [FK_Questions_QuestionSets_QuestionSetId]
                        FOREIGN KEY ([QuestionSetId]) REFERENCES [QuestionSets] ([QuestionSetId]) ON DELETE SET NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Exams_QuestionSets_QuestionSetId')
                    ALTER TABLE [Exams] ADD CONSTRAINT [FK_Exams_QuestionSets_QuestionSetId]
                        FOREIGN KEY ([QuestionSetId]) REFERENCES [QuestionSets] ([QuestionSetId]);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_QuestionSets_Users_CreatedByUserId')
                    ALTER TABLE [QuestionSets] ADD CONSTRAINT [FK_QuestionSets_Users_CreatedByUserId]
                        FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([UserId]);
                """;

            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureAdaptiveExamSchemaAsync(AppDbContext context)
        {
            const string sql = """
                IF COL_LENGTH(N'Exams', N'AdaptiveQuestionCount') IS NULL
                    ALTER TABLE [Exams] ADD [AdaptiveQuestionCount] int NOT NULL
                        CONSTRAINT [DF_Exams_AdaptiveQuestionCount] DEFAULT 0;

                IF COL_LENGTH(N'Exams', N'IsPublic') IS NULL
                    ALTER TABLE [Exams] ADD [IsPublic] bit NOT NULL
                        CONSTRAINT [DF_Exams_IsPublic] DEFAULT 1;
                """;

            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureAttemptReportSchemaAsync(AppDbContext context)
        {
            const string sql = """
                IF COL_LENGTH(N'ExamAttempts', N'ExamTitleSnapshot') IS NULL
                    ALTER TABLE [ExamAttempts] ADD [ExamTitleSnapshot] nvarchar(max) NULL;

                IF COL_LENGTH(N'ExamAttempts', N'ExamTotalMarksSnapshot') IS NULL
                    ALTER TABLE [ExamAttempts] ADD [ExamTotalMarksSnapshot] int NULL;

                IF COL_LENGTH(N'ExamAttempts', N'IsAdaptiveSnapshot') IS NULL
                    ALTER TABLE [ExamAttempts] ADD [IsAdaptiveSnapshot] bit NULL;

                IF COL_LENGTH(N'ExamAttempts', N'IsConfidenceWeightedSnapshot') IS NULL
                    ALTER TABLE [ExamAttempts] ADD [IsConfidenceWeightedSnapshot] bit NULL;

                IF COL_LENGTH(N'ExamAttempts', N'NegativeMarkingSnapshot') IS NULL
                    ALTER TABLE [ExamAttempts] ADD [NegativeMarkingSnapshot] bit NULL;

                DECLARE @examAttemptForeignKey sysname;
                SELECT @examAttemptForeignKey = [name]
                FROM sys.foreign_keys
                WHERE [parent_object_id] = OBJECT_ID(N'[ExamAttempts]')
                    AND [referenced_object_id] = OBJECT_ID(N'[Exams]');

                IF @examAttemptForeignKey IS NOT NULL
                BEGIN
                    DECLARE @dropConstraintSql nvarchar(max);
                    SET @dropConstraintSql =
                        N'ALTER TABLE [ExamAttempts] DROP CONSTRAINT ' + QUOTENAME(@examAttemptForeignKey);
                    EXEC sys.sp_executesql @dropConstraintSql;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM sys.columns
                    WHERE [object_id] = OBJECT_ID(N'[ExamAttempts]')
                        AND [name] = N'ExamId'
                        AND [is_nullable] = 0
                )
                    ALTER TABLE [ExamAttempts] ALTER COLUMN [ExamId] int NULL;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE [parent_object_id] = OBJECT_ID(N'[ExamAttempts]')
                        AND [referenced_object_id] = OBJECT_ID(N'[Exams]')
                )
                    ALTER TABLE [ExamAttempts] ADD CONSTRAINT [FK_ExamAttempts_Exams_ExamId]
                        FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([ExamId]) ON DELETE SET NULL;
                """;

            await context.Database.ExecuteSqlRawAsync(sql);
        }

        public static async Task EnsureUserInitializedAsync(AppDbContext context, int userId)
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null) return;

            // If user already has any attempts, they are already initialized
            var hasAttempts = await context.ExamAttempts.AnyAsync(a => a.UserId == userId);
            if (hasAttempts) return;

            // Find baseline diagnostic exam
            var diagExam = await context.Exams
                .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                .FirstOrDefaultAsync(e => e.Title.Contains("Diagnostic"));

            if (diagExam == null)
            {
                return;
            }

            // Create initial baseline completed attempt
            var attempt = new ExamAttempt
            {
                ExamId = diagExam.ExamId,
                UserId = userId,
                StartTime = DateTime.UtcNow.AddDays(-1).AddMinutes(-22),
                SubmitTime = DateTime.UtcNow.AddDays(-1),
                Status = AttemptStatus.Submitted,
                TotalScore = 16.0,
                OverconfidenceIndex = 8.5,
                UnderconfidenceIndex = 11.0
            };

            await context.ExamAttempts.AddAsync(attempt);
            await context.SaveChangesAsync();

            // Populate Attempt Answers linked to actual questions in the exam
            var examQuestions = diagExam.ExamQuestions.OrderBy(eq => eq.SequenceOrder).ToList();
            if (examQuestions.Count >= 5)
            {
                // Q1: DS (Correct, High)
                context.AttemptAnswers.Add(new AttemptAnswer
                {
                    AttemptId = attempt.AttemptId,
                    QuestionId = examQuestions[0].QuestionId,
                    SelectedOption = examQuestions[0].Question!.CorrectOption,
                    ConfidenceLevel = ConfidenceLevel.High,
                    IsCorrect = true,
                    MarksAwarded = 4.0,
                    SequencePosition = 1,
                    DifficultyAtServeTime = examQuestions[0].Question!.DifficultyLevel
                });

                // Q2: DS (Correct, High)
                context.AttemptAnswers.Add(new AttemptAnswer
                {
                    AttemptId = attempt.AttemptId,
                    QuestionId = examQuestions[1].QuestionId,
                    SelectedOption = examQuestions[1].Question!.CorrectOption,
                    ConfidenceLevel = ConfidenceLevel.High,
                    IsCorrect = true,
                    MarksAwarded = 4.0,
                    SequencePosition = 2,
                    DifficultyAtServeTime = examQuestions[1].Question!.DifficultyLevel
                });

                // Q3: Logic (Correct, High)
                context.AttemptAnswers.Add(new AttemptAnswer
                {
                    AttemptId = attempt.AttemptId,
                    QuestionId = examQuestions[2].QuestionId,
                    SelectedOption = examQuestions[2].Question!.CorrectOption,
                    ConfidenceLevel = ConfidenceLevel.High,
                    IsCorrect = true,
                    MarksAwarded = 4.0,
                    SequencePosition = 3,
                    DifficultyAtServeTime = examQuestions[2].Question!.DifficultyLevel
                });

                // Q4: Logic (Correct, Medium)
                context.AttemptAnswers.Add(new AttemptAnswer
                {
                    AttemptId = attempt.AttemptId,
                    QuestionId = examQuestions[3].QuestionId,
                    SelectedOption = examQuestions[3].Question!.CorrectOption,
                    ConfidenceLevel = ConfidenceLevel.Medium,
                    IsCorrect = true,
                    MarksAwarded = 4.0,
                    SequencePosition = 4,
                    DifficultyAtServeTime = examQuestions[3].Question!.DifficultyLevel
                });

                // Q5: Quant (Wrong, Medium)
                var wrongOpt = examQuestions[4].Question!.CorrectOption == "A" ? "B" : "A";
                context.AttemptAnswers.Add(new AttemptAnswer
                {
                    AttemptId = attempt.AttemptId,
                    QuestionId = examQuestions[4].QuestionId,
                    SelectedOption = wrongOpt,
                    ConfidenceLevel = ConfidenceLevel.Medium,
                    IsCorrect = false,
                    MarksAwarded = 0.0,
                    SequencePosition = 5,
                    DifficultyAtServeTime = examQuestions[4].Question!.DifficultyLevel
                });

                await context.SaveChangesAsync();
            }

            // Populate Concept Performances
            var dsTag = await context.ConceptTags.FirstOrDefaultAsync(c => c.Name == "Data Structures");
            var logicTag = await context.ConceptTags.FirstOrDefaultAsync(c => c.Name == "Logical Reasoning");
            var quantTag = await context.ConceptTags.FirstOrDefaultAsync(c => c.Name == "Quantitative Aptitude");

            if (dsTag != null)
            {
                context.ConceptPerformances.Add(new ConceptPerformance
                {
                    AttemptId = attempt.AttemptId,
                    ConceptTagId = dsTag.ConceptTagId,
                    TotalQuestions = 2,
                    CorrectCount = 2 // 100% mastery
                });
            }

            if (logicTag != null)
            {
                context.ConceptPerformances.Add(new ConceptPerformance
                {
                    AttemptId = attempt.AttemptId,
                    ConceptTagId = logicTag.ConceptTagId,
                    TotalQuestions = 2,
                    CorrectCount = 2 // 100% mastery
                });
            }

            if (quantTag != null)
            {
                context.ConceptPerformances.Add(new ConceptPerformance
                {
                    AttemptId = attempt.AttemptId,
                    ConceptTagId = quantTag.ConceptTagId,
                    TotalQuestions = 2,
                    CorrectCount = 1 // 50% mastery
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
