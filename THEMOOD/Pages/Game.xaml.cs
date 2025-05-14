using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace THEMOOD.Pages;

public partial class Game : ContentView
{
	private readonly Random _random = new Random();
	private readonly List<Bubble> _bubbles = new List<Bubble>();
	private int _score = 0;
	private bool _isGameRunning = false;
	private readonly int _maxBubbles = 15;
	private readonly int _bubbleSpawnDelay = 1000; // ms

	public Game()
	{
		InitializeComponent();
	}

	private async void StartButton_Clicked(object sender, EventArgs e)
	{
		if (_isGameRunning)
			return;

		_isGameRunning = true;
		_score = 0;
		ScoreLabel.Text = "Score: 0";
		StartButton.IsEnabled = false;

		// Clear any existing bubbles
		_bubbles.Clear();
		GameArea.Children.Clear();

		// Start the game loop
		await RunGameLoop();

		StartButton.IsEnabled = true;
		StartButton.Text = "Play Again";
		_isGameRunning = false;
	}

	private async Task RunGameLoop()
	{
		// Run the game for 60 seconds
		int gameTime = 60;
		StartButton.Text = $"Time: {gameTime}";

		// Create a task for the game timer
		var gameTimer = Task.Run(async () =>
		{
			while (gameTime > 0 && _isGameRunning)
			{
				await Task.Delay(1000);
				gameTime--;

				MainThread.BeginInvokeOnMainThread(() =>
				{
					StartButton.Text = $"Time: {gameTime}";
				});
			}

			_isGameRunning = false;
		});

		// Create a task for spawning bubbles
		var bubbleSpawner = Task.Run(async () =>
		{
			while (_isGameRunning)
			{
				await Task.Delay(_bubbleSpawnDelay);

				MainThread.BeginInvokeOnMainThread(() =>
				{
					if (_bubbles.Count < _maxBubbles)
					{
						CreateBubble();
					}
				});
			}
		});

		// Create a task for updating bubble positions
		var bubbleMover = Task.Run(async () =>
		{
			while (_isGameRunning)
			{
				await Task.Delay(16); // ~60fps

				MainThread.BeginInvokeOnMainThread(() =>
				{
					UpdateBubbles();
				});
			}
		});

		// Wait for all tasks to complete
		await Task.WhenAll(gameTimer, bubbleSpawner, bubbleMover);
	}

	private void CreateBubble()
	{
		// Get window bounds
		double width = this.Width;
		double height = this.Height;

		if (width <= 0 || height <= 0)
		{
			width = 400;
			height = 800;
		}

		// Create a new bubble
		int size = _random.Next(40, 120);

		var bubble = new Bubble
		{
			X = _random.Next(0, (int)(width - size)),
			Y = height,
			Size = size,
			SpeedX = (_random.NextDouble() * 2 - 1) * 2,
			SpeedY = -(_random.NextDouble() * 3 + 2),
			Color = GetRandomColor()
		};

		// Create the visual element for the bubble
		var frame = new Frame
		{
			CornerRadius = size / 2,
			WidthRequest = size,
			HeightRequest = size,
			HasShadow = true,
			BackgroundColor = bubble.Color,
			Opacity = 0.8
		};

		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += (s, e) => BubbleTapped(bubble);
		frame.GestureRecognizers.Add(tapGesture);

		// Set the position of the bubble in the AbsoluteLayout
		AbsoluteLayout.SetLayoutBounds(frame, new Rect(bubble.X, bubble.Y, size, size));

		// Add the bubble to our collections
		bubble.Visual = frame;
		_bubbles.Add(bubble);
		GameArea.Children.Add(frame);
	}

	private void UpdateBubbles()
	{
		List<Bubble> bubblesToRemove = new List<Bubble>();

		foreach (var bubble in _bubbles)
		{
			// Update position
			bubble.X += bubble.SpeedX;
			bubble.Y += bubble.SpeedY;

			// Check bounds
			double width = this.Width > 0 ? this.Width : 400;

			// Bounce off walls
			if (bubble.X <= 0 || bubble.X >= width - bubble.Size)
			{
				bubble.SpeedX = -bubble.SpeedX;
				bubble.X = Math.Clamp(bubble.X, 0, width - bubble.Size);
			}

			// Remove if it goes off the top
			if (bubble.Y < -bubble.Size)
			{
				bubblesToRemove.Add(bubble);
				continue;
			}

			// Update the visual element position
			if (bubble.Visual != null)
			{
				AbsoluteLayout.SetLayoutBounds(bubble.Visual, new Rect(bubble.X, bubble.Y, bubble.Size, bubble.Size));
			}
		}

		// Remove bubbles that went off screen
		foreach (var bubble in bubblesToRemove)
		{
			GameArea.Children.Remove(bubble.Visual);
			_bubbles.Remove(bubble);
		}
	}

	private void BubbleTapped(Bubble bubble)
	{
		if (!_isGameRunning)
			return;

		// Add score based on bubble size (smaller bubbles are worth more)
		int points = (int)(1000 / bubble.Size);
		_score += points;
		ScoreLabel.Text = $"Score: {_score}";

		// Visual feedback
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			if (bubble.Visual != null)
			{
				await bubble.Visual.ScaleTo(1.2, 50, Easing.SpringOut);
				await bubble.Visual.ScaleTo(0, 100, Easing.SpringIn);

				GameArea.Children.Remove(bubble.Visual);
				_bubbles.Remove(bubble);
			}
		});
	}

	private Color GetRandomColor()
	{
		// Soothing color palette for bubbles
		Color[] colors = new Color[]
		{
			Color.FromArgb("#FF9AA2"), // Soft pink
			Color.FromArgb("#FFB7B2"), // Light coral
			Color.FromArgb("#FFDAC1"), // Peach
			Color.FromArgb("#E2F0CB"), // Mint
			Color.FromArgb("#B5EAD7"), // Aqua
			Color.FromArgb("#C7CEEA"), // Lavender
			Color.FromArgb("#ADE8F4")  // Sky blue
		};

		return colors[_random.Next(colors.Length)];
	}
}

public class Bubble
{
	public double X { get; set; }
	public double Y { get; set; }
	public double SpeedX { get; set; }
	public double SpeedY { get; set; }
	public int Size { get; set; }
	public Color Color { get; set; }
	public View Visual { get; set; }
}