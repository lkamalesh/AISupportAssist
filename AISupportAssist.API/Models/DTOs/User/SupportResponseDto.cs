namespace AISupportAssist.API.Models.DTOs.User
{
    public class SupportResponseDto
    {
        public string Answer { get; set; } = string.Empty;

        public bool RequiresHumanReview { get; set; }

        public double ConfidenceScore { get; set; }

    }
}
