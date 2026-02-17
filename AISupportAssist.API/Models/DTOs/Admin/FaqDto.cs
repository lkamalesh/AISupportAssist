namespace AISupportAssist.API.Models.DTOs.Admin
{
    public class FaqDto
    {
        public int Id { get; set; }
        public required string Question { get; set; } 
        public required string Answer { get; set; } 
    }
}
