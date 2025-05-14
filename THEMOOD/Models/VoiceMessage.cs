namespace THEMOOD.Models
{
    public class VoiceMessage
    {
        public string Transcription { get; set; }
        public bool IsFromUser { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 