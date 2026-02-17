using AISupportAssist.API.Configuration;
using AISupportAssist.API.Data;
using AISupportAssist.API.Interfaces;
using AISupportAssist.API.Models.DTOs.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AISupportAssist.API.Services
{
    public class SupportService : ISupportService
    {
        private readonly IFaqService _faqService;
        private readonly IGroqService _groqService;
        private readonly GroqSettings _settings;

        public SupportService(IFaqService faqservice, GroqService grokservice, IOptions<GroqSettings> settings)
        {
            _faqService = faqservice;  
            _groqService = grokservice;
            _settings = settings.Value;
        }

        public async Task<SupportResponseDto> HandleQuestionsAsync(string question)
        {

            var keywords = question
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.Trim('.', ',', '?', '!', ':', ';'))
                .Where(k => k.Length > 2)
                .Distinct()
                .ToList();


            var faqs = await _faqService.GetAllAsync();

            var bestMatch = faqs
                .Select(f => 
                {
                    var faqLower = f.Question.ToLowerInvariant();
                    var score = keywords.Count(k => faqLower.Contains(k));

                    return new
                    {

                        Faq = f,
                        Score = score
                    };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(3)
                .Select(x => x.Faq)
                .ToList();

            var faqContext = string.Join("\n",
                bestMatch.Select(f => $"Q: {f.Question}\nA: {f.Answer}"));

            var prompt = _settings.Prompt
                .Replace("{faqContext}", faqContext)
                .Replace("{question}", question);

            var aiAnswer = await _groqService.GenerateAnswerAsync(prompt);

            var requiresHumanReview =
                aiAnswer.Contains("not sure", StringComparison.OrdinalIgnoreCase) || aiAnswer.Length < 20;

            double confidence = 1.0;

            if (requiresHumanReview)
                confidence -= 0.5;

            if (aiAnswer.Length < 30)
                confidence -= 0.3;

            confidence = Math.Clamp(confidence, 0, 1);

            return new SupportResponseDto
            {
                Answer = aiAnswer,
                RequiresHumanReview = requiresHumanReview,
                ConfidenceScore = confidence
            };
        }
    }
}
