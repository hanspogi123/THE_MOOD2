using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace THEMOOD.Services
{
    public class OpenAIAudioService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const string OpenAIBaseUrl = "https://api.openai.com/v1";

        public OpenAIAudioService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> TranscribeAudioAsync(Stream audioStream)
        {
            try
            {
                // Reset the stream position to beginning
                if (audioStream.CanSeek)
                    audioStream.Position = 0;

                // Create multipart form content
                using var formContent = new MultipartFormDataContent();

                // Convert stream to byte array for more reliable handling
                using var memoryStream = new MemoryStream();
                await audioStream.CopyToAsync(memoryStream);
                var audioBytes = memoryStream.ToArray();

                // Add file content
                var fileContent = new ByteArrayContent(audioBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/mp3");
                formContent.Add(fileContent, "file", "recording.mp3");

                // Add model parameter
                formContent.Add(new StringContent("whisper-1"), "model");

                // Send request to OpenAI API
                var response = await _httpClient.PostAsync($"{OpenAIBaseUrl}/audio/transcriptions", formContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error transcribing audio: {response.StatusCode}, {errorContent}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                return doc.RootElement.GetProperty("text").GetString() ?? "No transcription available.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TranscribeAudioAsync: {ex}");
                throw;
            }
        }

        public async Task<Stream> SynthesizeSpeechAsync(string text)
        {
            try
            {
                // Prepare request payload
                var payload = new
                {
                    model = "tts-1",
                    input = text,
                    voice = "alloy" // Options: alloy, echo, fable, onyx, nova, shimmer
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                // Send request to OpenAI API
                var response = await _httpClient.PostAsync($"{OpenAIBaseUrl}/audio/speech", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error synthesizing speech: {response.StatusCode}, {errorContent}");
                }

                // Get the audio stream
                var audioStream = await response.Content.ReadAsStreamAsync();

                // Create a memory stream that we can return and seek through
                var memoryStream = new MemoryStream();
                await audioStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                return memoryStream;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SynthesizeSpeechAsync: {ex}");
                throw;
            }
        }
    }
}