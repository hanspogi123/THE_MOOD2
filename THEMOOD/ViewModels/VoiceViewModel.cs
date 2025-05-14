using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.Audio;
using THEMOOD.Services;
using THEMOOD.Models;
using System.Threading.Tasks;
using System;
using System.IO;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace THEMOOD.ViewModels
{
    public partial class VoiceViewModel : ObservableObject
    {
        private readonly IAudioManager _audioManager;
        private readonly OpenAIService _openAIService;
        private readonly VoiceChatService _voiceChatService;
        private IAudioRecorder _audioRecorder;
        private IAudioPlayer _currentPlayer;
        private string _recordedAudioPath;
        private Stream _currentAudioStream;

        [ObservableProperty]
        private ObservableCollection<VoiceMessage> _messages;

        [ObservableProperty]
        private bool _isRecording;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private bool _isStatusVisible;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private bool _isTyping;

        [ObservableProperty]
        private bool _isTranscribing;

        [ObservableProperty]
        private bool _hasRecording;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private string _currentAudioPath;

        [ObservableProperty]
        private string _transcription;

        [ObservableProperty]
        private string _response;

        [RelayCommand]
        private Task NavigateToChatAsync()
        {
            var navVM = NavBarViewModel.Instance;
            NavBarViewModel.SetMainPageContent?.Invoke(new THEMOOD.Pages.Chat());
            return Task.CompletedTask;
        }

        [RelayCommand]
        private void ClearConversation()
        {
            Messages.Clear();
            _voiceChatService.ClearHistory();
            ShowStatus("Conversation cleared");
        }

        public string RecordingButtonText => IsRecording ? "Stop" : "Start";

        public VoiceViewModel(IAudioManager audioManager, OpenAIService openAIService)
        {
            _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
            _openAIService = openAIService ?? throw new ArgumentNullException(nameof(openAIService));
            _voiceChatService = VoiceChatService.Instance;
            
            // Initialize messages from service
            _messages = new ObservableCollection<VoiceMessage>(_voiceChatService.GetMessageHistory());
            
            InitializeAudioAsync().ConfigureAwait(false);
        }

        private async Task InitializeAudioAsync()
        {
            try
            {
                // Cross-platform permissions check
                var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Microphone>();
                    if (status != PermissionStatus.Granted)
                    {
                        ShowStatus("Microphone permission is required.");
                        return;
                    }
                }

                // Create recorder with platform-specific settings
                _audioRecorder = _audioManager.CreateRecorder();

                // Configure platform-specific settings
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    // Windows-specific audio settings
                    Console.WriteLine("Using Windows-specific audio recorder");
                }
                else if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    // Android-specific audio settings
                    Console.WriteLine("Using Android-specific audio recorder");
                }

                _currentPlayer = _audioManager.CreatePlayer(new MemoryStream());
                ShowStatus("Audio initialized successfully.");
            }
            catch (Exception ex)
            {
                ShowStatus($"Error initializing audio: {ex.Message}");
                Console.WriteLine($"Error initializing audio: {ex}");
            }
        }

        [RelayCommand]
        private async Task PlayRecordingAsync()
        {
            if (string.IsNullOrEmpty(_recordedAudioPath) || !File.Exists(_recordedAudioPath))
            {
                ShowStatus("No recording available to play.");
                return;
            }

            try
            {
                ShowStatus("Playing recording...");
                using var fileStream = File.OpenRead(_recordedAudioPath);
                await PlayAudioResponseAsync(fileStream);
            }
            catch (Exception ex)
            {
                ShowStatus($"Error playing recording: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to play recording.", "OK");
            }
        }

        [RelayCommand]
        private async Task ToggleRecordingAsync()
        {
            if (IsRecording)
            {
                await StopRecordingAsync();
            }
            else
            {
                await StartRecordingAsync();
            }
        }

        private async Task StartRecordingAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    ShowStatus("Microphone permission is required.");
                    IsRecording = false;
                    return;
                }

                // Platform-specific file path handling
                string basePath;
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    // Windows-specific path handling
                    string[] basePaths = new[]
                    {
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        FileSystem.AppDataDirectory,
                        FileSystem.CacheDirectory
                    };

                    basePath = null;
                    foreach (var path in basePaths)
                    {
                        try
                        {
                            string fullPath = Path.Combine(path, "THEMOOD", "Recordings");
                            Directory.CreateDirectory(fullPath);
                            if (Directory.Exists(fullPath))
                            {
                                basePath = fullPath;
                                break;
                            }
                        }
                        catch (Exception dirEx)
                        {
                            Console.WriteLine($"Failed to create directory in {path}: {dirEx.Message}");
                        }
                    }

                    if (string.IsNullOrEmpty(basePath))
                    {
                        throw new InvalidOperationException("Cannot find a suitable directory for recordings.");
                    }
                }
                else
                {
                    // Android-specific path handling
                    basePath = FileSystem.CacheDirectory;
                }

                // Generate unique filename
                string fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.wav";
                _recordedAudioPath = Path.Combine(basePath, fileName);

                // Pre-create the file to ensure it exists
                try
                {
                    using (var fileStream = File.Create(_recordedAudioPath))
                    {
                        fileStream.Close();
                    }
                }
                catch (Exception preCreateEx)
                {
                    Console.WriteLine($"Error pre-creating file: {preCreateEx}");
                    throw new InvalidOperationException($"Cannot create recording file: {preCreateEx.Message}", preCreateEx);
                }

                // Extensive logging
                Console.WriteLine($"Attempting to record to: {_recordedAudioPath}");
                Console.WriteLine($"File exists before recording: {File.Exists(_recordedAudioPath)}");
                Console.WriteLine($"Directory full path: {Path.GetFullPath(Path.GetDirectoryName(_recordedAudioPath))}");

                // Start recording
                IsRecording = true;
                HasRecording = false;
                ShowStatus("Preparing to record...");

                // Verify file still exists before recording
                if (!File.Exists(_recordedAudioPath))
                {
                    throw new FileNotFoundException("Recording file was deleted before recording could start.");
                }

                // Start recording
                await _audioRecorder.StartAsync(_recordedAudioPath);
                ShowStatus("Recording started successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Recording Start Error Details:");
                Console.WriteLine($"Exception Type: {ex.GetType().FullName}");
                Console.WriteLine($"Error Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception Type: {ex.InnerException.GetType().FullName}");
                    Console.WriteLine($"Inner Exception Message: {ex.InnerException.Message}");
                }

                await Shell.Current.DisplayAlert(
                    "Recording Error",
                    $"Failed to start recording:\n{ex.Message}\n\nPlease check console for details.",
                    "OK"
                );

                IsRecording = false;
            }
        }

        private async Task StopRecordingAsync()
        {
            try
            {
                IsRecording = false;
                ShowStatus("Processing...");
                await _audioRecorder.StopAsync();

                // Platform-specific wait time
                int waitTime = DeviceInfo.Platform == DevicePlatform.WinUI ? 1500 : 500;
                await Task.Delay(waitTime);

                if (string.IsNullOrEmpty(_recordedAudioPath) || !File.Exists(_recordedAudioPath))
                {
                    ShowStatus("No audio file found.");
                    await Shell.Current.DisplayAlert("Error", $"Audio file not found at path: {_recordedAudioPath}", "OK");
                    return;
                }

                // Verify the file has content
                var fileInfo = new FileInfo(_recordedAudioPath);
                if (fileInfo.Length == 0)
                {
                    ShowStatus("Recording failed - empty file.");
                    await Shell.Current.DisplayAlert("Error", "Recording failed - empty file.", "OK");
                    return;
                }

                // Show recording details
                ShowStatus($"Recording successful! File size: {fileInfo.Length} bytes");
                HasRecording = true;

                // Transcribe the audio
                ShowStatus("Transcribing audio...");
                IsTranscribing = true;
                var transcription = await _openAIService.TranscribeAudioAsync(_recordedAudioPath);
                IsTranscribing = false;

                // Create and add user message
                var userMessage = new VoiceMessage
                {
                    Transcription = transcription,
                    IsFromUser = true,
                    Timestamp = DateTime.Now
                };
                Messages.Add(userMessage);
                _voiceChatService.AddMessage(userMessage);

                // Get chat response with context
                ShowStatus("Getting response...");
                IsTyping = true;
                IsProcessing = true;
                var chatResponse = await _voiceChatService.GetResponseWithContext(transcription);

                // Create and add assistant message
                var assistantMessage = new VoiceMessage
                {
                    Transcription = chatResponse,
                    IsFromUser = false,
                    Timestamp = DateTime.Now
                };
                Messages.Add(assistantMessage);
                _voiceChatService.AddMessage(assistantMessage);

                // Convert response to speech and play it
                ShowStatus("Converting response to speech...");
                using var speechStream = await _openAIService.GetSpeechAsync(chatResponse);
                await PlayAudioResponseAsync(speechStream);

                ShowStatus("Ready");
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}");
                Console.WriteLine($"Error in StopRecordingAsync: {ex}");
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsTyping = false;
                IsProcessing = false;
                IsTranscribing = false;
            }
        }

        private async Task PlayAudioResponseAsync(Stream audioStream)
        {
            try
            {
                // Clean up any existing audio
                await CleanupCurrentAudioAsync();

                // Create a copy of the stream that we can control
                _currentAudioStream = new MemoryStream();
                await audioStream.CopyToAsync(_currentAudioStream);
                _currentAudioStream.Position = 0;

                // Create a new player with the response stream
                _currentPlayer = _audioManager.CreatePlayer(_currentAudioStream);
                _currentPlayer.PlaybackEnded += OnPlaybackEnded;

                // Start playback
                _currentPlayer.Play();
                IsPlaying = true;
            }
            catch (Exception ex)
            {
                ShowStatus($"Error playing response: {ex.Message}");
                Console.WriteLine($"Error playing response: {ex}");
                await Shell.Current.DisplayAlert("Error", "Failed to play response.", "OK");
            }
        }

        private async void OnPlaybackEnded(object sender, EventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                IsPlaying = false;
                ShowStatus("Ready to record!");
                await CleanupCurrentAudioAsync();
            });
        }

        private async Task CleanupCurrentAudioAsync()
        {
            try
            {
                if (_currentPlayer != null)
                {
                    _currentPlayer.PlaybackEnded -= OnPlaybackEnded;
                    if (_currentPlayer.IsPlaying)
                    {
                        _currentPlayer.Stop();
                    }
                    _currentPlayer.Dispose();
                    _currentPlayer = null;
                }

                if (_currentAudioStream != null)
                {
                    await _currentAudioStream.DisposeAsync();
                    _currentAudioStream = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up audio: {ex.Message}");
            }
        }

        private void ShowStatus(string message)
        {
            StatusMessage = message;
            IsStatusVisible = true;
        }

        public async Task CleanupAsync()
        {
            if (IsRecording)
            {
                await StopRecordingAsync();
            }
            await CleanupCurrentAudioAsync();

            // Clean up recorded audio file
            try
            {
                if (!string.IsNullOrEmpty(_recordedAudioPath) && File.Exists(_recordedAudioPath))
                {
                    File.Delete(_recordedAudioPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up recorded audio: {ex.Message}");
            }
        }
    }
}