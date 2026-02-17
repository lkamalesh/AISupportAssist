using AISupportAssist.API.Models.DTOs.User;

namespace AISupportAssist.API.Interfaces
{
    public interface ISupportService
    {
        Task<SupportResponseDto> HandleQuestionsAsync(string question);
    }
}
