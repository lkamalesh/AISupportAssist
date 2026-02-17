namespace AISupportAssist.API.Models.DTOs.Groq
{
    public class GroqMessageDto
    {
        public required string Role { get; set; } 
        public required string Content { get; set; } 
    }
}
