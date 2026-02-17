namespace AISupportAssist.API.Configuration
{
    public class GroqSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public int MinimumAnswerLength { get; set; }
    }
}
