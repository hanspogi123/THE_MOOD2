using System;
using System.IO;
using System.Threading.Tasks;

namespace THEMOOD.Services
{
    public class AudioProcessor
    {
        private const int TargetSampleRate = 24000;
        private const int TargetChannels = 1;
        private const int BitsPerSample = 16;

        public static async Task<byte[]> ConvertToPCM16Mono24kHz(byte[] audioData)
        {
            try
            {
                // Create a memory stream from the audio data
                using var inputStream = new MemoryStream(audioData);
                using var outputStream = new MemoryStream();

                // Read the WAV header
                using var reader = new BinaryReader(inputStream);
                using var writer = new BinaryWriter(outputStream);

                // Write WAV header
                WriteWavHeader(writer, TargetSampleRate, TargetChannels, BitsPerSample);

                // Read the audio data
                var audioBuffer = new byte[inputStream.Length - 44]; // Skip WAV header
                await inputStream.ReadAsync(audioBuffer, 0, audioBuffer.Length);

                // Convert to mono if necessary
                var monoBuffer = ConvertToMono(audioBuffer);

                // Resample to 24kHz if necessary
                var resampledBuffer = ResampleAudio(monoBuffer, TargetSampleRate);

                // Convert to PCM16
                var pcm16Buffer = ConvertToPCM16(resampledBuffer);

                // Write the processed audio data
                writer.Write(pcm16Buffer);

                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing audio: {ex.Message}");
                return audioData; // Return original data if processing fails
            }
        }

        private static void WriteWavHeader(BinaryWriter writer, int sampleRate, int channels, int bitsPerSample)
        {
            // RIFF header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(0); // File size - 8 (to be filled later)
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Chunk size
            writer.Write((short)1); // Audio format (1 for PCM)
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bitsPerSample / 8); // Byte rate
            writer.Write((short)(channels * bitsPerSample / 8)); // Block align
            writer.Write((short)bitsPerSample);

            // data chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(0); // Chunk size (to be filled later)
        }

        private static byte[] ConvertToMono(byte[] stereoData)
        {
            // Assuming 16-bit audio
            var samples = stereoData.Length / 2;
            var monoData = new byte[samples];

            for (int i = 0; i < samples; i += 2)
            {
                // Average left and right channels
                monoData[i / 2] = (byte)((stereoData[i] + stereoData[i + 1]) / 2);
            }

            return monoData;
        }

        private static byte[] ResampleAudio(byte[] audioData, int targetSampleRate)
        {
            // Simple linear resampling
            // In a real implementation, you would use a more sophisticated resampling algorithm
            var sourceSampleRate = 44100; // Assuming source is 44.1kHz
            var ratio = (double)targetSampleRate / sourceSampleRate;
            var targetLength = (int)(audioData.Length * ratio);
            var resampledData = new byte[targetLength];

            for (int i = 0; i < targetLength; i++)
            {
                var sourceIndex = (int)(i / ratio);
                if (sourceIndex < audioData.Length)
                {
                    resampledData[i] = audioData[sourceIndex];
                }
            }

            return resampledData;
        }

        private static byte[] ConvertToPCM16(byte[] audioData)
        {
            var pcm16Data = new byte[audioData.Length * 2];

            for (int i = 0; i < audioData.Length; i++)
            {
                // Convert 8-bit to 16-bit
                short sample = (short)((audioData[i] - 128) * 256);
                pcm16Data[i * 2] = (byte)(sample & 0xFF);
                pcm16Data[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }

            return pcm16Data;
        }
    }
} 