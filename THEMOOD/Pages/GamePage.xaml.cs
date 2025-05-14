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

namespace THEMOOD.Pages;

public partial class GamePage : ContentPage
{
    private readonly Random _random = new Random();
    private readonly List<BubbleEntity> _bubbles = new List<BubbleEntity>();
    private readonly ConcurrentQueue<BubbleEntity> _bubblesToRemove = new ConcurrentQueue<BubbleEntity>();
    private int _score = 0;
    private bool _isGameRunning = false;
    private readonly int _maxBubbles = 12;
    private readonly int _bubbleSpawnDelay = 1200;
    private readonly IAudioManager _audioManager;
    private IAudioPlayer _bubblePopPlayer;
    private CancellationTokenSource _gameCancellationTokenSource;
    private int _currentGameTime;
    private Task _gameTimerTask;
    private Task _bubbleSpawnerTask;
    private Task _bubbleMoverTask;
    private readonly SemaphoreSlim _bubblesSemaphore = new SemaphoreSlim(1, 1);
    private bool _isBusy = false;

    public GamePage(IAudioManager audioManager)
    {
        InitializeComponent();
        _audioManager = audioManager;
        InitializeSoundEffects();
        
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GameArea.GestureRecognizers.Add(panGesture);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartGame();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
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
                memoryStream.Position = 0;
                _bubblePopPlayer = _audioManager.CreatePlayer(memoryStream);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing sound effects: {ex}");
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

        await StopGame();
        await Shell.Current.GoToAsync("..");
    }

    private void StartButton_Clicked(object sender, EventArgs e)
    {
        StartGame();
    }

    private void StartGame()
    {
        if (_isGameRunning)
            return;

        _isGameRunning = true;
        _score = 0;
        _currentGameTime = 30;
        ScoreLabel.Text = "Score: 0";
        StartButton.IsVisible = false;

        // Clear any existing bubbles
        _bubbles.Clear();
        GameArea.Children.Clear();

        // Create new cancellation token and start game
        _gameCancellationTokenSource = new CancellationTokenSource();
        RunGameLoop(_gameCancellationTokenSource.Token);
    }

    private async Task StopGame()
    {
        _isGameRunning = false;
        _gameCancellationTokenSource?.Cancel();
        _gameCancellationTokenSource?.Dispose();
        _gameCancellationTokenSource = null;
        _currentGameTime = 0;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _bubbles.Clear();
            GameArea.Children.Clear();
            StartButton.IsVisible = true;
            StartButton.Text = "Start Game";
            ScoreLabel.Text = "Score: 0";
            _score = 0;
        });
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (!_isGameRunning) return;

        if (e.StatusType == GestureStatus.Running)
        {
            var touchPoint = new Point(e.TotalX, e.TotalY);
            CheckBubbleTouch(touchPoint);
        }
    }

    private void CheckBubbleTouch(Point touchPoint)
    {
        if (_isBusy) return;

        foreach (var bubble in _bubbles.ToArray())
        {
            if (bubble.IsPopping) continue;

            var bubbleBounds = new Rect(bubble.PosX, bubble.PosY, bubble.BubbleSize, bubble.BubbleSize);
            if (bubbleBounds.Contains(touchPoint))
            {
                BubbleTapped(bubble);
                break;
            }
        }
    }

    private async void BubbleTapped(BubbleEntity bubble)
    {
        if (!_isGameRunning || bubble.IsPopping || _isBusy)
            return;

        _isBusy = true;

        try
        {
            bubble.IsPopping = true;

            int points = (int)(1000 / bubble.BubbleSize);
            _score += points;
            ScoreLabel.Text = $"Score: {_score}";

            PlayBubblePopSound();

            if (bubble.BubbleVisual != null)
            {
                await bubble.BubbleVisual.ScaleTo(1.2, 30, Easing.SpringOut);
                await bubble.BubbleVisual.ScaleTo(0, 50, Easing.SpringIn);

                await _bubblesSemaphore.WaitAsync();
                try
                {
                    if (_bubbles.Contains(bubble))
                    {
                        GameArea.Children.Remove((IView)bubble.BubbleVisual);
                        _bubbles.Remove(bubble);
                    }
                }
                finally
                {
                    _bubblesSemaphore.Release();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error popping bubble: {ex}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void PlayBubblePopSound()
    {
        try
        {
            _bubblePopPlayer?.Play();
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
                await StopGame();
            }
        }
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
                    StartButton.Text = $"Time: {_currentGameTime}";
                });
            }

            if (_currentGameTime <= 0)
            {
                _isGameRunning = false;
            }
        }, cancellationToken);

        _bubbleSpawnerTask = Task.Run(async () =>
        {
            while (_isGameRunning && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_bubbleSpawnDelay, cancellationToken);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_bubbles.Count < _maxBubbles)
                    {
                        CreateBubble();
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

        int size = _random.Next(60, 140);

        var bubble = new BubbleEntity
        {
            PosX = _random.Next(0, (int)(width - size)),
            PosY = height,
            BubbleSize = size,
            SpeedX = (_random.NextDouble() * 2 - 1) * 1.5,
            SpeedY = -(_random.NextDouble() * 2 + 1.5),
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
            Opacity = 0.8
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => BubbleTapped(bubble);
        frame.GestureRecognizers.Add(tapGesture);

        AbsoluteLayout.SetLayoutBounds(frame, new Rect(bubble.PosX, bubble.PosY, size, size));

        bubble.BubbleVisual = frame;
        _bubbles.Add(bubble);
        GameArea.Children.Add((IView)frame);
    }

    private void UpdateBubbles()
    {
        List<BubbleEntity> bubblesToRemove = new List<BubbleEntity>();

        foreach (var bubble in _bubbles)
        {
            if (bubble.IsPopping)
                continue;

            bubble.PosX += bubble.SpeedX;
            bubble.PosY += bubble.SpeedY;

            double width = this.Width > 0 ? this.Width : 400;

            if (bubble.PosX <= 0 || bubble.PosX >= width - bubble.BubbleSize)
            {
                bubble.SpeedX = -bubble.SpeedX;
                bubble.PosX = Math.Clamp(bubble.PosX, 0, width - bubble.BubbleSize);
            }

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