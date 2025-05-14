using System;

namespace THEMOOD.Services
{
    public class AudioChatMessage
    {
        public string Text { get; set; }
        public bool IsFromUser { get; set; }
        public DateTime Timestamp { get; set; }
    }
}