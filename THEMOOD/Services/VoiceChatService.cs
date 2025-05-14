using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using THEMOOD.Models;

namespace THEMOOD.Services
{
    public class VoiceChatService
    {
        private readonly OpenAIService _openAIService;
        private static VoiceChatService _instance;
        private List<VoiceMessage> _messageHistory;

        public static VoiceChatService Instance => _instance ??= new VoiceChatService();

        private VoiceChatService()
        {
            _messageHistory = new List<VoiceMessage>();
            _openAIService = Application.Current.Handler.MauiContext.Services.GetService<OpenAIService>();
        }

        public List<VoiceMessage> GetMessageHistory()
        {
            return _messageHistory;
        }

        public void AddMessage(VoiceMessage message)
        {
            _messageHistory.Add(message);
        }

        public void ClearHistory()
        {
            _messageHistory.Clear();
        }

        public async Task<string> GetResponseWithContext(string userMessage)
        {
            // Build context from message history
            var context = BuildConversationContext();
            
            // Add the current message to the context
            context += $"\nUser: {userMessage}\nAssistant:";

            // Get response from OpenAI with context
            return await _openAIService.GetChatResponseAsync(context);
        }

        private string BuildConversationContext()
        {
            var context = new System.Text.StringBuilder();
            context.AppendLine("This is a conversation between a user and an AI assistant. Previous messages:");

            foreach (var message in _messageHistory.TakeLast(5)) // Keep last 5 messages for context
            {
                var role = message.IsFromUser ? "User" : "Assistant";
                context.AppendLine($"{role}: {message.Transcription}");
            }

            return context.ToString();
        }
    }
} 