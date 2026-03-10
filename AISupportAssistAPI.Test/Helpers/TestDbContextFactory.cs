using AISupportAssist.API.Data;
using AISupportAssist.API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AISupportAssistAPI.Test.Helpers
{
    /// <summary>
    /// Helper class for creating test database contexts with in-memory database
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Creates a new AppDbContext with an in-memory database for testing
        /// </summary>
        /// <param name="databaseName">Optional unique database name. If not provided, a GUID will be used.</param>
        /// <returns>A new AppDbContext instance</returns>
        public static AppDbContext CreateInMemoryContext(string? databaseName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName ?? Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        /// <summary>
        /// Creates a context and seeds it with test data
        /// </summary>
        /// <returns>A seeded AppDbContext instance</returns>
        public static async Task<AppDbContext> CreateSeededContext()
        {
            var context = CreateInMemoryContext();
            await SeedTestData(context);
            return context;
        }

        /// <summary>
        /// Seeds the context with common test data
        /// </summary>
        public static async Task SeedTestData(AppDbContext context)
        {
            var faqs = new[]
            {
                new Faq
                {
                    Id = 1,
                    Question = "What is AI?",
                    Answer = "Artificial Intelligence is the simulation of human intelligence by machines."
                },
                new Faq
                {
                    Id = 2,
                    Question = "What is Machine Learning?",
                    Answer = "Machine Learning is a subset of AI that enables systems to learn from data."
                },
                new Faq
                {
                    Id = 3,
                    Question = "What is Deep Learning?",
                    Answer = "Deep Learning is a subset of Machine Learning using neural networks."
                }
            };

            await context.Faqs.AddRangeAsync(faqs);
            await context.SaveChangesAsync();
        }
    }
}
