using AISupportAssist.API.Configuration;
using AISupportAssist.API.Interfaces;
using AISupportAssist.API.Models.DTOs.Groq;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AISupportAssist.API.Services
{
    public class GroqService : IGroqService
    {
        private readonly HttpClient _httpClient;
        private readonly GroqSettings _settings;

        public GroqService(HttpClient client, IOptions<GroqSettings> settings)
        {
            _httpClient = client;
            _settings = settings.Value;// Value is used to get the actual settings object from IOptions
                
        }

        public async Task<string> GenerateAnswerAsync(string prompt)
        {
            var request = new GroqRequestDto 
            { 
                Model = _settings.Model,
                Messages = [
                    new GroqMessageDto     
                    { 
                        Role = "user", 
                        Content = prompt 
                    }
                ]
            };

            var response = await _httpClient.PostAsJsonAsync("openai/v1/chat/completions", request);

            response.EnsureSuccessStatusCode();//

            var result = await response.Content.ReadFromJsonAsync<GroqResponseDto>();

            return result?.Choices?[0]?.Message?.Content ?? string.Empty;
        }
    }
}
