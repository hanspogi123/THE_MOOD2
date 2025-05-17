using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Plugin.Maui.Audio;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using THEMOOD.ViewModels;
using THEMOOD.Pages;

namespace THEMOOD.Pages;

public partial class GamePage : ContentPage
{
    private readonly Random _random = new Random();
    private readonly List<BubbleEntity> _bubbles = new List<BubbleEntity>();
    private int _score = 0;
    private int _highScore = 0; // Added for high score
    private bool _isGameRunning = false;
    private readonly int _maxBubbles = 15; // Slightly increased for more action (was 15)
    private readonly int _bubbleSpawnDelay = 500; // User reverted this value
    private readonly IAudioManager _audioManager;
    private IAudioPlayer _backgroundMusicPlayer;
    private byte[] _bubblePopSoundBytes; // To store sound data for creating multiple players
    private byte[] _backgroundMusicBytes; // Store the music bytes
    private CancellationTokenSource _gameCancellationTokenSource;
    private int _currentGameTime;
    private Task _gameTimerTask;
    private Task _bubbleSpawnerTask;
    private Task _bubbleMoverTask;
    private readonly object _bubblesLock = new object();
    private bool _backgroundMusicInitialized = false; // Flag to track initialization

    public GamePage(IAudioManager audioManager)
    {
        InitializeComponent();
        _audioManager = audioManager;
        InitializeSoundEffects();
        InitializeBackgroundMusic();

        // Use a tap gesture recognizer for the entire game area
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnGameAreaTapped;
        GameArea.GestureRecognizers.Add(tapGesture);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Load High Score
        _highScore = Preferences.Get("HighScore", 0);
        HighScoreLabel.Text = $"High Score: {_highScore}";

        // Start background music with proper error handling and checking
        PlayBackgroundMusic();

        StartGame();
    }

    private void PlayBackgroundMusic()
    {
        try
        {
            // Only create a new player if needed
            if (_backgroundMusicPlayer == null && _backgroundMusicBytes != null && _backgroundMusicBytes.Length > 0)
            {
                // Recreate the player if it was disposed
                _backgroundMusicPlayer = _audioManager.CreatePlayer(new MemoryStream(_backgroundMusicBytes));

                if (_backgroundMusicPlayer != null)
                {
                    _backgroundMusicPlayer.Loop = true;
                    _backgroundMusicPlayer.Volume = 0.5;
                    _backgroundMusicInitialized = true;
                    System.Diagnostics.Debug.WriteLine("Background music player created successfully");
                }
            }

            // Only play if we have a valid player and it's not already playing
            if (_backgroundMusicPlayer != null && _backgroundMusicInitialized)
            {
                if (!_backgroundMusicPlayer.IsPlaying)
                {
                    _backgroundMusicPlayer.Play();
                    System.Diagnostics.Debug.WriteLine("Background music started playing");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Background music is already playing");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Background music player is null or not initialized");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing background music: {ex}");
            // Try to recover
            _backgroundMusicPlayer?.Dispose();
            _backgroundMusicPlayer = null;
            _backgroundMusicInitialized = false;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Stop background music without disposing
        try
        {
            if (_backgroundMusicPlayer != null && _backgroundMusicPlayer.IsPlaying)
            {
                _backgroundMusicPlayer.Pause(); // Use Pause instead of Stop to allow resuming
                System.Diagnostics.Debug.WriteLine("Background music paused");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error pausing background music: {ex}");
        }
        _ = StopGame();
    }

    private async void InitializeSoundEffects()
    {
        try
        {
            var assembly = typeof(GamePage).Assembly;
            using var stream = assembly.GetManifestResourceStream("THEMOOD.Resources.Raw.bubblepop.mp3");

            if (stream != null)
            {
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                _bubblePopSoundBytes = memoryStream.ToArray(); // Store sound data as byte array
                System.Diagnostics.Debug.WriteLine("Bubble pop sound initialized successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Error: bubblepop.mp3 not found as embedded resource.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing sound effects: {ex}");
        }
    }

    private async void InitializeBackgroundMusic()
    {
        try
        {
            var assembly = typeof(GamePage).Assembly;
            using var stream = assembly.GetManifestResourceStream("THEMOOD.Resources.Raw.bg_music.mp3");

            if (stream != null)
            {
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                _backgroundMusicBytes = memoryStream.ToArray(); // Store the bytes
                System.Diagnostics.Debug.WriteLine($"Background music bytes loaded: {_backgroundMusicBytes.Length} bytes");

                // We'll create the player when needed in PlayBackgroundMusic()
                _backgroundMusicInitialized = false;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Error: bg_music.mp3 not found as embedded resource.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing background music: {ex}");
            _backgroundMusicInitialized = false;
        }
    }

    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        if (_isGameRunning)
        {
            bool shouldExit = await DisplayAlert(
                "Exit Game?",
                "Are you sure you want to exit? Your progress will be lost.",
                "Exit",
                "Cancel");

            if (!shouldExit)
                return;
        }

        // Clean up and dispose the background music player when truly leaving the game
        CleanupBackgroundMusic();

        await StopGame();

        // Create a new Game view instance
        var gameView = new Game();

        // Set the Game view as the main content
        NavBarViewModel.SetMainPageContent?.Invoke(gameView);

        // Then navigate back to MainPage
        await Shell.Current.GoToAsync("..");
    }

    private void CleanupBackgroundMusic()
    {
        try
        {
            if (_backgroundMusicPlayer != null)
            {
                _backgroundMusicPlayer.Stop();
                _backgroundMusicPlayer.Dispose();
                _backgroundMusicPlayer = null;
                _backgroundMusicInitialized = false;
                System.Diagnostics.Debug.WriteLine("Background music player disposed");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disposing background music: {ex}");
        }
    }

    private void StartButton_Clicked(object sender, EventArgs e)
    {
        StartGame();
    }

    private void StartGame()
    {
        if (_isGameRunning)
            return;

        // Ensure HighScoreLabel reflects the current _highScore value when starting a new game
        HighScoreLabel.Text = $"High Score: {_highScore}";

        _isGameRunning = true;
        _score = 0;
        _currentGameTime = 45; // Extended time for more relaxed gameplay
        ScoreLabel.Text = "Score: 0";
        TimerLabel.Text = $"Time: {_currentGameTime}"; // Update TimerLabel
        StartButton.IsVisible = false;

        // Clear any existing bubbles
        lock (_bubblesLock)
        {
            _bubbles.Clear();
            GameArea.Children.Clear();
        }

        // Create new cancellation token and start game
        _gameCancellationTokenSource = new CancellationTokenSource();
        RunGameLoop(_gameCancellationTokenSource.Token);
    }

    private async Task StopGame()
    {
        _isGameRunning = false;
        _gameCancellationTokenSource?.Cancel();
        // It's safer to dispose CancellationTokenSource after tasks are likely completed or cancelled.
        // Consider awaiting tasks or adding a small delay if issues arise.
        // _gameCancellationTokenSource?.Dispose(); 
        // _gameCancellationTokenSource = null;

        // Check for new high score before resetting score
        if (_score > _highScore)
        {
            _highScore = _score;
            Preferences.Set("HighScore", _highScore);
            HighScoreLabel.Text = $"High Score: {_highScore}";
        }

        _currentGameTime = 0; // Reset timer value

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            lock (_bubblesLock)
            {
                _bubbles.Clear();
                GameArea.Children.Clear();
            }
            StartButton.IsVisible = true;
            StartButton.Text = "Start Game"; // Or "Play Again?"
            ScoreLabel.Text = "Score: 0";
            TimerLabel.Text = $"Time: {_currentGameTime}"; // Reset timer display
            // HighScoreLabel is already updated if a new high score was made
            _score = 0; // Reset score *after* checking for high score
        });

        // Dispose CancellationTokenSource here after operations
        _gameCancellationTokenSource?.Dispose();
        _gameCancellationTokenSource = null;
    }

    private void OnGameAreaTapped(object sender, TappedEventArgs e)
    {
        if (!_isGameRunning) return;

        var point = e.GetPosition(GameArea);
        if (point.HasValue)
        {
            CheckBubbleTouch(point.Value);
        }
    }

    private void CheckBubbleTouch(Point touchPoint)
    {
        // We'll use a more generous touch radius for mobile
        const double TOUCH_RADIUS = 20;

        BubbleEntity bubbleToPop = null;
        double closestDistance = double.MaxValue;

        lock (_bubblesLock)
        {
            foreach (var bubble in _bubbles)
            {
                if (bubble.IsPopping) continue;

                // Calculate distance to bubble center
                double bubbleCenterX = bubble.PosX + bubble.BubbleSize / 2;
                double bubbleCenterY = bubble.PosY + bubble.BubbleSize / 2;
                double distance = Math.Sqrt(Math.Pow(touchPoint.X - bubbleCenterX, 2) + Math.Pow(touchPoint.Y - bubbleCenterY, 2));

                // If touch is inside the bubble (plus padding) and closer than any other bubble
                if (distance < (bubble.BubbleSize / 2 + TOUCH_RADIUS) && distance < closestDistance)
                {
                    bubbleToPop = bubble;
                    closestDistance = distance;
                }
            }
        }

        if (bubbleToPop != null)
        {
            BubbleTapped(bubbleToPop);
        }
    }

    private async void BubbleTapped(BubbleEntity bubble)
    {
        if (!_isGameRunning || bubble.IsPopping)
            return;

        bubble.IsPopping = true;

        // Calculate points - bigger bubbles are worth less
        int points = (int)(1200 / bubble.BubbleSize);
        _score += points;
        ScoreLabel.Text = $"Score: {_score}";

        PlayBubblePopSound();

        try
        {
            // Show a floating score text
            ShowFloatingScoreText($"+{points}", bubble.PosX + bubble.BubbleSize / 2, bubble.PosY + bubble.BubbleSize / 2);

            if (bubble.BubbleVisual != null)
            {
                // Make the pop animation more satisfying
                await Task.WhenAll(
                    bubble.BubbleVisual.ScaleTo(1.3, 80, Easing.SpringOut),
                    bubble.BubbleVisual.FadeTo(0.9, 80)
                );

                await Task.WhenAll(
                    bubble.BubbleVisual.ScaleTo(0, 150, Easing.SpringIn),
                    bubble.BubbleVisual.FadeTo(0, 150)
                );

                lock (_bubblesLock)
                {
                    if (_bubbles.Contains(bubble))
                    {
                        GameArea.Children.Remove((IView)bubble.BubbleVisual);
                        _bubbles.Remove(bubble);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error popping bubble: {ex}");
        }
    }

    private void ShowFloatingScoreText(string text, double x, double y)
    {
        try
        {
            var label = new Label
            {
                Text = text,
                TextColor = Colors.White,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Shadow = new Shadow
                {
                    Brush = Brush.Black,
                    Opacity = 0.8f,
                    Offset = new Point(1, 1),
                    Radius = 4
                }
            };

            AbsoluteLayout.SetLayoutBounds(label, new Rect(x, y, 60, 30));
            GameArea.Children.Add(label);

            // Animate the score floating up and fading out
            Task.WhenAll(
                label.TranslateTo(0, -40, 800, Easing.SinOut),
                label.FadeTo(0, 800)
            ).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    GameArea.Children.Remove(label);
                });
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing floating text: {ex}");
        }
    }

    private void PlayBubblePopSound()
    {
        try
        {
            if (_bubblePopSoundBytes != null && _bubblePopSoundBytes.Length > 0 && _audioManager != null)
            {
                // Create a new player for each sound to allow overlapping plays
                var player = _audioManager.CreatePlayer(new MemoryStream(_bubblePopSoundBytes));
                player.Play();

                // Optional: Handle player disposal if Plugin.Maui.Audio doesn't do it automatically
                // for short-lived players. One way is to use the PlaybackEnded event.
                // player.PlaybackEnded += (s, e) => 
                // {
                //     if (s is IAudioPlayer p) p.Dispose();
                // };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Cannot play pop sound: sound data or audio manager not available.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex}");
        }
    }

    private async Task RunGameLoop(CancellationToken cancellationToken)
    {
        try
        {
            StartGameTasks(_currentGameTime, cancellationToken);
            await Task.WhenAll(_gameTimerTask, _bubbleSpawnerTask, _bubbleMoverTask);
        }
        catch (OperationCanceledException)
        {
            // Tasks were cancelled, normal during page switching
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Game error: {ex}");
        }
        finally
        {
            if (_currentGameTime <= 0)
            {
                await GameOver();
            }
        }
    }

    private async Task GameOver()
    {
        _isGameRunning = false; // Ensure game is marked as not running

        string gameOverMessage = $"Your score: {_score}";
        if (_score > Preferences.Get("HighScore", 0)) // Check against persisted high score for the message
        {
            gameOverMessage += $"\nNEW HIGH SCORE: {_score}!";
        }
        else
        {
            gameOverMessage += $"\nHigh Score: {Preferences.Get("HighScore", 0)}";
        }

        await StopGame();

        // Create a new Game view instance
        var gameView = new Game();

        // Set the Game view as the main content
        NavBarViewModel.SetMainPageContent?.Invoke(gameView);

        // Then navigate back to MainPage
        await Shell.Current.GoToAsync("..");
    }

    private void StartGameTasks(int startTime, CancellationToken cancellationToken)
    {
        _gameTimerTask = Task.Run(async () =>
        {
            while (_currentGameTime > 0 && _isGameRunning && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
                _currentGameTime--;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TimerLabel.Text = $"Time: {_currentGameTime}"; // Update TimerLabel
                });
            }

            if (_currentGameTime <= 0 && _isGameRunning) // Ensure GameOver is called only if game was running and time ran out
            {
                _isGameRunning = false; // Mark game as not running before invoking GameOver
                MainThread.BeginInvokeOnMainThread(async () => await GameOver());
            }
        }, cancellationToken);

        _bubbleSpawnerTask = Task.Run(async () =>
        {
            while (_isGameRunning && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_bubbleSpawnDelay, cancellationToken);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lock (_bubblesLock)
                    {
                        if (_bubbles.Count < _maxBubbles)
                        {
                            CreateBubble();
                        }
                    }
                });
            }
        }, cancellationToken);

        _bubbleMoverTask = Task.Run(async () =>
        {
            while (_isGameRunning && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(16, cancellationToken);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateBubbles();
                });
            }
        }, cancellationToken);
    }

    private void CreateBubble()
    {
        double width = this.Width;
        double height = this.Height;

        if (width <= 0 || height <= 0)
        {
            width = 400;
            height = 800;
        }

        int size = _random.Next(70, 150); // Slightly larger bubbles for easier tapping

        var bubble = new BubbleEntity
        {
            PosX = _random.Next(0, (int)(width - size)),
            PosY = height,
            BubbleSize = size,
            SpeedX = (_random.NextDouble() * 2.0 - 1.0) * 1.5, // Increased horizontal speed range
            SpeedY = -(_random.NextDouble() * 2.0 + 1.5), // Increased vertical speed
            BubbleColor = GetRandomColor(),
            IsPopping = false
        };

        var frame = new Frame
        {
            CornerRadius = size / 2,
            WidthRequest = size,
            HeightRequest = size,
            HasShadow = true,
            BackgroundColor = bubble.BubbleColor,
            Opacity = 0.8,
            BorderColor = Colors.White.WithAlpha(0.4f),
            InputTransparent = true
        };

        // No need for tap gesture on individual bubbles - we use the game area tap gesture instead

        AbsoluteLayout.SetLayoutBounds(frame, new Rect(bubble.PosX, bubble.PosY, size, size));

        bubble.BubbleVisual = frame;
        _bubbles.Add(bubble);
        GameArea.Children.Add((IView)frame);
    }

    private void UpdateBubbles()
    {
        List<BubbleEntity> bubblesToRemove = new List<BubbleEntity>();

        lock (_bubblesLock)
        {
            foreach (var bubble in _bubbles)
            {
                if (bubble.IsPopping)
                    continue;

                bubble.PosX += bubble.SpeedX;
                bubble.PosY += bubble.SpeedY;

                double width = this.Width > 0 ? this.Width : 400;

                // Bounce off walls
                if (bubble.PosX <= 0 || bubble.PosX >= width - bubble.BubbleSize)
                {
                    bubble.SpeedX = -bubble.SpeedX;
                    bubble.PosX = Math.Clamp(bubble.PosX, 0, width - bubble.BubbleSize);
                }

                // Remove if off screen
                if (bubble.PosY < -bubble.BubbleSize)
                {
                    bubblesToRemove.Add(bubble);
                    continue;
                }

                if (bubble.BubbleVisual != null && !bubble.IsPopping)
                {
                    AbsoluteLayout.SetLayoutBounds(bubble.BubbleVisual, new Rect(bubble.PosX, bubble.PosY, bubble.BubbleSize, bubble.BubbleSize));
                }
            }

            foreach (var bubble in bubblesToRemove)
            {
                if (_bubbles.Contains(bubble))
                {
                    GameArea.Children.Remove((IView)bubble.BubbleVisual);
                    _bubbles.Remove(bubble);
                }
            }
        }
    }

    private Color GetRandomColor()
    {
        Color[] colors = new Color[]
        {
            Color.FromArgb("#FF9AA2"),
            Color.FromArgb("#FFB7B2"),
            Color.FromArgb("#FFDAC1"),
            Color.FromArgb("#E2F0CB"),
            Color.FromArgb("#B5EAD7"),
            Color.FromArgb("#C7CEEA"),
            Color.FromArgb("#ADE8F4")
        };

        return colors[_random.Next(colors.Length)];
    }
}

public class BubbleEntity
{
    public double PosX { get; set; }
    public double PosY { get; set; }
    public double SpeedX { get; set; }
    public double SpeedY { get; set; }
    public int BubbleSize { get; set; }
    public Color BubbleColor { get; set; }
    public View BubbleVisual { get; set; }
    public bool IsPopping { get; set; }
}
