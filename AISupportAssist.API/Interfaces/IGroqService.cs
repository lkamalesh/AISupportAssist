namespace AISupportAssist.API.Interfaces
{
    public interface IGroqService
    {
        Task<string> GenerateAnswerAsync(string prompt);
    }
}
