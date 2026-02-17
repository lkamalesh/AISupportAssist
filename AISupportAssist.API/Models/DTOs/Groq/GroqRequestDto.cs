namespace AISupportAssist.API.Models.DTOs.Groq
{
    public class GroqRequestDto
    {
        public required string Model { get; set; } 
        public  required GroqMessageDto[] Messages { get; set; } 
    }
}
