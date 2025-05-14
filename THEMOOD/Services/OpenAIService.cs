using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace THEMOOD.Services
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string WhisperEndpoint = "https://api.openai.com/v1/audio/transcriptions";
        private const string ChatEndpoint = "https://api.openai.com/v1/chat/completions";
        private const string TtsEndpoint = "https://api.openai.com/v1/audio/speech";

        public OpenAIService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> TranscribeAudioAsync(string audioFilePath)
        {
            try
            {
                using var form = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(audioFilePath);
                using var streamContent = new StreamContent(fileStream);
                
                // Set the correct content type based on file extension and platform
                string contentType;
                string fileExtension = Path.GetExtension(audioFilePath).ToLower();
                
                if (fileExtension == ".wav")
                {
                    contentType = "audio/wav";
                }
                else if (fileExtension == ".mp3")
                {
                    contentType = "audio/mpeg";
                }
                else
                {
                    throw new ArgumentException($"Unsupported audio format: {fileExtension}");
                }
                
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                
                form.Add(streamContent, "file", Path.GetFileName(audioFilePath));
                form.Add(new StringContent("whisper-1"), "model");
                form.Add(new StringContent("text"), "response_format");

                var response = await _httpClient.PostAsync(WhisperEndpoint, form);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Transcription failed: {response.StatusCode} - {errorContent}");
                }

                var transcription = await response.Content.ReadAsStringAsync();
                return transcription ?? "No transcription available";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error transcribing audio: {ex}");
                throw;
            }
        }

        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            try
            {
                var request = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful and friendly voice assistant. Keep your responses concise and natural." },
                        new { role = "user", content = userMessage }
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ChatEndpoint, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ChatGPT Response: {responseContent}"); // Debug log

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseContent, options);

                if (chatResponse?.Choices == null || !chatResponse.Choices.Any())
                {
                    Console.WriteLine("No choices in response"); // Debug log
                    return "I'm sorry, I couldn't process that request.";
                }

                var message = chatResponse.Choices.FirstOrDefault()?.Message?.Content;
                if (string.IsNullOrEmpty(message))
                {
                    Console.WriteLine("Empty message content"); // Debug log
                    return "I'm sorry, I couldn't generate a response.";
                }

                return message;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting chat response: {ex}");
                throw;
            }
        }

        public async Task<Stream> GetSpeechAsync(string text)
        {
            try
            {
                var request = new
                {
                    model = "tts-1",
                    input = text,
                    voice = "alloy"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(TtsEndpoint, content);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting speech: {ex}");
                throw;
            }
        }

        private class ChatResponse
        {
            public List<Choice> Choices { get; set; }
        }

        private class Choice
        {
            public Message Message { get; set; }
        }

        private class Message
        {
            public string Content { get; set; }
        }
    }
} 