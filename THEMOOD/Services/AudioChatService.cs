using System;
using System.Threading.Tasks;

namespace THEMOOD.Services
{
    public interface IChatService
    {
        Task<string> SendMessageAsync(string message);
    }

    public class AudioChatService : IChatService
    {
        private readonly string _apiKey;

        public AudioChatService(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

        public async Task<string> SendMessageAsync(string message)
        {
            // TODO: Implement your chat API call here
            // This is a placeholder implementation
            return await Task.FromResult($"Response to: {message}");
        }
    }
}
