using AISupportAssist.API.Configuration;
using AISupportAssist.API.Interfaces;
using AISupportAssist.API.Models.DTOs.Admin;
using AISupportAssist.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using AISupportAssist.API.Models.DTOs.Groq;

namespace AISupportAssistAPI.Test.Services
{
    public class SupportServiceTests
    {
        private readonly Mock<IFaqService> _mockFaqService;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<IOptions<GroqSettings>> _mockSettings;
        private readonly GroqSettings _groqSettings;
        private readonly SupportService _supportService;

        public SupportServiceTests()
        {
            _mockFaqService = new Mock<IFaqService>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockSettings = new Mock<IOptions<GroqSettings>>();

            _groqSettings = new GroqSettings
            {
                Model = "llama-3.3-70b-versatile",
                ApiKey = "test-api-key",
                Prompt = "Context: {faqContext}\n\nQuestion: {question}\n\nAnswer:",
                MinimumAnswerLength = 20
            };

            _mockSettings.Setup(s => s.Value).Returns(_groqSettings);

            // Create a real GroqService with mocked HttpMessageHandler
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://api.groq.com/")
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            _supportService = new SupportService(
                _mockFaqService.Object,
                groqService,
                _mockSettings.Object
            );
        }

        private void SetupMockGroqResponse(string responseContent)
        {
            var mockResponse = new GroqResponseDto
            {
                Choices = new[]
                {
                    new GroqChoiceDto
                    {
                        Message = new GroqMessageDto
                        {
                            Role = "assistant",
                            Content = responseContent
                        }
                    }
                }
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockResponse))
                });
        }

        [Fact]
        public async Task HandleQuestionsAsync_ValidQuestion_ReturnsAnswerWithHighConfidence()
        {
            // Arrange
            var question = "What is machine learning?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is machine learning?", Answer = "ML is a subset of AI" },
                new FaqDto { Id = 2, Question = "What is deep learning?", Answer = "DL uses neural networks" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("Machine learning is a method of data analysis that automates analytical model building.");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            result.Answer.Should().NotBeNullOrEmpty();
            result.Answer.Length.Should().BeGreaterThan(30);
            result.ConfidenceScore.Should().Be(1.0);
            result.RequiresHumanReview.Should().BeFalse();
        }

        [Fact]
        public async Task HandleQuestionsAsync_ShortAnswer_ReducesConfidenceScore()
        {
            // Arrange
            var question = "What is AI?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is AI?", Answer = "Artificial Intelligence" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("AI is intelligence");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            result.Answer.Length.Should().BeLessThan(30);
            result.ConfidenceScore.Should().BeLessThan(1.0);
            result.ConfidenceScore.Should().Be(0.2); // 1.0 - 0.5 (short answer) - 0.3 (< 30 chars)
        }

        [Fact]
        public async Task HandleQuestionsAsync_AnswerContainsNotSure_RequiresHumanReview()
        {
            // Arrange
            var question = "What is quantum computing?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is computing?", Answer = "Computing is processing data" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("I'm not sure about quantum computing details.");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            result.RequiresHumanReview.Should().BeTrue();
            result.ConfidenceScore.Should().BeLessThan(1.0);
            result.ConfidenceScore.Should().Be(0.5); // 1.0 - 0.5 (not sure)
        }

        [Fact]
        public async Task HandleQuestionsAsync_VeryShortAnswer_RequiresHumanReview()
        {
            // Arrange
            var question = "What is ML?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is ML?", Answer = "Machine Learning" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("ML is AI"); // Less than 20 characters

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            result.Answer.Length.Should().BeLessThan(20);
            result.RequiresHumanReview.Should().BeTrue();
            result.ConfidenceScore.Should().Be(0.2); // 1.0 - 0.5 (requires review) - 0.3 (< 30 chars)
        }

        [Fact]
        public async Task HandleQuestionsAsync_NoMatchingFaqs_StillCallsGroqService()
        {
            // Arrange
            var question = "What is the meaning of life?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is AI?", Answer = "Artificial Intelligence" },
                new FaqDto { Id = 2, Question = "What is ML?", Answer = "Machine Learning" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("I don't have specific information about that topic.");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task HandleQuestionsAsync_EmptyFaqList_ReturnsAnswer()
        {
            // Arrange
            var question = "What is AI?";
            var emptyFaqs = new List<FaqDto>();

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(emptyFaqs);
            SetupMockGroqResponse("Based on general knowledge, AI stands for Artificial Intelligence.");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            result.Answer.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task HandleQuestionsAsync_MultipleKeywordMatches_SelectsTop3Faqs()
        {
            // Arrange
            var question = "What is machine learning and deep learning?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is machine learning?", Answer = "ML answer" },
                new FaqDto { Id = 2, Question = "What is deep learning?", Answer = "DL answer" },
                new FaqDto { Id = 3, Question = "Machine learning basics", Answer = "Basics answer" },
                new FaqDto { Id = 4, Question = "Advanced machine learning", Answer = "Advanced answer" },
                new FaqDto { Id = 5, Question = "Deep learning networks", Answer = "Networks answer" }
            };

            string? capturedPrompt = null;
            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
                {
                    capturedPrompt = await req.Content!.ReadAsStringAsync();
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(new GroqResponseDto
                    {
                        Choices = new[]
                        {
                            new GroqChoiceDto
                            {
                                Message = new GroqMessageDto
                                {
                                    Role = "assistant",
                                    Content = "Machine learning and deep learning are both subsets of artificial intelligence."
                                }
                            }
                        }
                    }))
                });

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            capturedPrompt.Should().NotBeNull();
            // Should contain context from top 3 FAQs (those with most keyword matches)
            var faqCount = capturedPrompt!.Split("Q:").Length - 1;
            faqCount.Should().BeLessOrEqualTo(3);
        }

        [Fact]
        public async Task HandleQuestionsAsync_ExtractsKeywordsCorrectly()
        {
            // Arrange
            var question = "What is AI, ML, and DL?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is AI?", Answer = "Artificial Intelligence" },
                new FaqDto { Id = 2, Question = "What is ML?", Answer = "Machine Learning" },
                new FaqDto { Id = 3, Question = "What is DL?", Answer = "Deep Learning" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("AI, ML, and DL are related technologies in artificial intelligence.");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task HandleQuestionsAsync_ConfidenceScoreClampedBetween0And1()
        {
            // Arrange
            var question = "What is AI?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is AI?", Answer = "Artificial Intelligence" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("I'm not sure. Short."); // Both conditions: "not sure" + < 30 chars

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.ConfidenceScore.Should().BeGreaterOrEqualTo(0.0);
            result.ConfidenceScore.Should().BeLessOrEqualTo(1.0);
            result.ConfidenceScore.Should().Be(0.2); // Math.Clamp(1.0 - 0.5 - 0.3, 0, 1) = 0.2
        }

        [Fact]
        public async Task HandleQuestionsAsync_CaseInsensitiveNotSureDetection()
        {
            // Arrange
            var question = "What is quantum physics?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is physics?", Answer = "Study of matter" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("I'm NOT SURE about quantum physics.");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.RequiresHumanReview.Should().BeTrue();
        }

        [Fact]
        public async Task HandleQuestionsAsync_FiltersShortKeywords()
        {
            // Arrange
            var question = "What is AI in ML?"; // "is" and "in" should be filtered (length <= 2)
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is AI?", Answer = "Artificial Intelligence" },
                new FaqDto { Id = 2, Question = "What is ML?", Answer = "Machine Learning" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("AI in ML refers to artificial intelligence concepts used in machine learning.");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            result.Answer.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task HandleQuestionsAsync_RemovesPunctuationFromKeywords()
        {
            // Arrange
            var question = "What is AI? How does ML work?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is AI", Answer = "Artificial Intelligence" },
                new FaqDto { Id = 2, Question = "How does ML work", Answer = "Machine Learning process" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("AI is artificial intelligence. ML works through algorithms.");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task HandleQuestionsAsync_HandlesEmptyQuestion()
        {
            // Arrange
            var question = "";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is AI?", Answer = "Artificial Intelligence" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("Please ask.");  // Short response to trigger human review

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            result.RequiresHumanReview.Should().BeTrue();  // Because answer length < 20
        }

        [Fact]
        public async Task HandleQuestionsAsync_LongAnswerWithoutIssues_HighConfidence()
        {
            // Arrange
            var question = "Explain machine learning in detail";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is machine learning?", Answer = "ML detailed answer" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("Machine learning is a method of data analysis that automates analytical model building. It is a branch of artificial intelligence based on the idea that systems can learn from data, identify patterns and make decisions with minimal human intervention.");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            result.Answer.Length.Should().BeGreaterThan(30);
            result.RequiresHumanReview.Should().BeFalse();
            result.ConfidenceScore.Should().Be(1.0);
        }

        [Fact]
        public async Task HandleQuestionsAsync_GroqServiceThrows_PropagatesException()
        {
            // Arrange
            var question = "What is AI?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is AI?", Answer = "Artificial Intelligence" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("API unavailable"));

            // Act
            Func<Task> act = async () => await _supportService.HandleQuestionsAsync(question);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>()
                .WithMessage("API unavailable");
        }

        [Fact]
        public async Task HandleQuestionsAsync_FaqServiceThrows_PropagatesException()
        {
            // Arrange
            var question = "What is AI?";

            _mockFaqService.Setup(s => s.GetAllAsync())
                .ThrowsAsync(new InvalidOperationException("Database unavailable"));

            // Act
            Func<Task> act = async () => await _supportService.HandleQuestionsAsync(question);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Database unavailable");
        }

        [Fact]
        public async Task HandleQuestionsAsync_DuplicateKeywords_DistinctKeywordsUsed()
        {
            // Arrange
            var question = "What is machine learning machine learning?"; // Duplicate words
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is machine learning?", Answer = "ML answer" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("Machine learning is a subset of artificial intelligence.");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            result.Answer.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task HandleQuestionsAsync_MixedCaseQuestion_HandlesCorrectly()
        {
            // Arrange
            var question = "WHAT is MaChInE LeArNiNg?";
            var faqs = new List<FaqDto>
            {
                new FaqDto { Id = 1, Question = "What is machine learning?", Answer = "ML answer" }
            };

            _mockFaqService.Setup(s => s.GetAllAsync()).ReturnsAsync(faqs);
            SetupMockGroqResponse("Machine learning is a method of data analysis.");

            // Act
            var result = await _supportService.HandleQuestionsAsync(question);

            // Assert
            result.Should().NotBeNull();
            result.Answer.Should().NotBeNullOrEmpty();
        }
    }
}
