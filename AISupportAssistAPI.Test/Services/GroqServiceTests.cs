using AISupportAssist.API.Configuration;
using AISupportAssist.API.Models.DTOs.Groq;
using AISupportAssist.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace AISupportAssistAPI.Test.Services
{
    public class GroqServiceTests
    {
        private readonly Mock<IOptions<GroqSettings>> _mockSettings;
        private readonly GroqSettings _groqSettings;
        private const string TestBaseUrl = "https://api.groq.com/";

        public GroqServiceTests()
        {
            _groqSettings = new GroqSettings
            {
                Model = "llama-3.3-70b-versatile",
                ApiKey = "test-api-key",
                Prompt = "Test prompt",
                MinimumAnswerLength = 20
            };

            _mockSettings = new Mock<IOptions<GroqSettings>>();
            _mockSettings.Setup(s => s.Value).Returns(_groqSettings);
        }

        [Fact]
        public async Task GenerateAnswerAsync_SuccessfulResponse_ReturnsAnswerContent()
        {
            // Arrange
            var expectedAnswer = "This is the AI generated answer";
            var mockResponse = new GroqResponseDto
            {
                Choices = new[]
                {
                    new GroqChoiceDto
                    {
                        Message = new GroqMessageDto
                        {
                            Role = "assistant",
                            Content = expectedAnswer
                        }
                    }
                }
            };

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
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

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            var result = await groqService.GenerateAnswerAsync("What is AI?");

            // Assert
            result.Should().Be(expectedAnswer);
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("openai/v1/chat/completions")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GenerateAnswerAsync_EmptyChoicesArray_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            var mockResponse = new GroqResponseDto
            {
                Choices = Array.Empty<GroqChoiceDto>()
            };

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
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

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            Func<Task> act = async () => await groqService.GenerateAnswerAsync("Test prompt");

            // Assert
            // Empty choices array causes IndexOutOfRangeException in current implementation
            await act.Should().ThrowAsync<IndexOutOfRangeException>();
        }

        [Fact]
        public async Task GenerateAnswerAsync_HttpError_ThrowsHttpRequestException()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Bad Request")
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            Func<Task> act = async () => await groqService.GenerateAnswerAsync("Test prompt");

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GenerateAnswerAsync_ServerError_ThrowsHttpRequestException()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Internal Server Error")
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            Func<Task> act = async () => await groqService.GenerateAnswerAsync("Test prompt");

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GenerateAnswerAsync_UsesCorrectModel_SendsModelInRequest()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var mockResponse = new GroqResponseDto
            {
                Choices = new[]
                {
                    new GroqChoiceDto
                    {
                        Message = new GroqMessageDto { Role = "assistant", Content = "Answer" }
                    }
                }
            };

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockResponse))
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            await groqService.GenerateAnswerAsync("Test prompt");

            // Assert
            capturedRequest.Should().NotBeNull();
            var requestContent = await capturedRequest!.Content!.ReadAsStringAsync();
            requestContent.Should().Contain(_groqSettings.Model);
        }

        [Fact]
        public async Task GenerateAnswerAsync_SendsPromptInRequest()
        {
            // Arrange
            var testPrompt = "What is machine learning?";
            HttpRequestMessage? capturedRequest = null;
            var mockResponse = new GroqResponseDto
            {
                Choices = new[]
                {
                    new GroqChoiceDto
                    {
                        Message = new GroqMessageDto { Role = "assistant", Content = "Answer" }
                    }
                }
            };

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockResponse))
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            await groqService.GenerateAnswerAsync(testPrompt);

            // Assert
            capturedRequest.Should().NotBeNull();
            var requestContent = await capturedRequest!.Content!.ReadAsStringAsync();
            requestContent.Should().Contain(testPrompt);
        }

        [Fact]
        public async Task GenerateAnswerAsync_MultipleChoices_ReturnsFirstChoice()
        {
            // Arrange
            var firstAnswer = "First answer";
            var secondAnswer = "Second answer";
            var mockResponse = new GroqResponseDto
            {
                Choices = new[]
                {
                    new GroqChoiceDto
                    {
                        Message = new GroqMessageDto { Role = "assistant", Content = firstAnswer }
                    },
                    new GroqChoiceDto
                    {
                        Message = new GroqMessageDto { Role = "assistant", Content = secondAnswer }
                    }
                }
            };

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
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

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            var result = await groqService.GenerateAnswerAsync("Test prompt");

            // Assert
            result.Should().Be(firstAnswer);
        }

        [Fact]
        public async Task GenerateAnswerAsync_LongPrompt_HandlesCorrectly()
        {
            // Arrange
            var longPrompt = new string('X', 5000);
            var mockResponse = new GroqResponseDto
            {
                Choices = new[]
                {
                    new GroqChoiceDto
                    {
                        Message = new GroqMessageDto { Role = "assistant", Content = "Answer" }
                    }
                }
            };

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
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

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            var act = async () => await groqService.GenerateAnswerAsync(longPrompt);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GenerateAnswerAsync_NullResponse_ReturnsEmptyString()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("null")
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            var result = await groqService.GenerateAnswerAsync("Test prompt");

            // Assert
            result.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GenerateAnswerAsync_EmptyPrompt_SendsEmptyPromptToApi()
        {
            // Arrange
            var emptyPrompt = string.Empty;
            HttpRequestMessage? capturedRequest = null;
            var mockResponse = new GroqResponseDto
            {
                Choices = new[]
                {
                    new GroqChoiceDto
                    {
                        Message = new GroqMessageDto { Role = "assistant", Content = "Answer" }
                    }
                }
            };

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockResponse))
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            await groqService.GenerateAnswerAsync(emptyPrompt);

            // Assert
            capturedRequest.Should().NotBeNull();
            var requestContent = await capturedRequest!.Content!.ReadAsStringAsync();
            
            // Deserialize with case-insensitive options to match API serialization
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var requestDto = JsonSerializer.Deserialize<GroqRequestDto>(requestContent, options);
            
            requestDto.Should().NotBeNull();
            requestDto!.Messages.Should().HaveCount(1);
            requestDto.Messages[0].Content.Should().Be(emptyPrompt);
        }

        [Fact]
        public async Task GenerateAnswerAsync_Unauthorized_ThrowsHttpRequestException()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent("Unauthorized")
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            Func<Task> act = async () => await groqService.GenerateAnswerAsync("Test prompt");

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GenerateAnswerAsync_Timeout_ThrowsTaskCanceledException()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timed out"));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestBaseUrl)
            };

            var groqService = new GroqService(httpClient, _mockSettings.Object);

            // Act
            Func<Task> act = async () => await groqService.GenerateAnswerAsync("Test prompt");

            // Assert
            await act.Should().ThrowAsync<TaskCanceledException>();
        }
    }
}
