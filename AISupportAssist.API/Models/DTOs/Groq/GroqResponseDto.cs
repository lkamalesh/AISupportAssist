namespace AISupportAssist.API.Models.DTOs.Groq
{
    public class GroqResponseDto
    {
        public required GroqChoiceDto[] Choices { get; set; }
    }
}
