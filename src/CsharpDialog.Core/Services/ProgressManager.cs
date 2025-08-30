using System.ComponentModel;

namespace csharpDialog.Core.Services;

/// <summary>
/// Manages enhanced progress operations including increment, reset, and smooth transitions
/// </summary>
public class ProgressManager : INotifyPropertyChanged
{
    private int _currentProgress = 0;
    private string _progressText = string.Empty;
    private readonly object _lock = new();

    /// <summary>
    /// Current progress value (0-100)
    /// </summary>
    public int CurrentProgress
    {
        get => _currentProgress;
        private set
        {
            if (_currentProgress != value)
            {
                _currentProgress = Math.Clamp(value, 0, 100);
                OnPropertyChanged(nameof(CurrentProgress));
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }
    }

    /// <summary>
    /// Current progress as a decimal (0.0-1.0) for WPF binding
    /// </summary>
    public double ProgressPercentage => CurrentProgress / 100.0;

    /// <summary>
    /// Current progress text
    /// </summary>
    public string ProgressText
    {
        get => _progressText;
        private set
        {
            if (_progressText != value)
            {
                _progressText = value ?? string.Empty;
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    /// <summary>
    /// Set progress to a specific value
    /// </summary>
    /// <param name="value">Progress value (0-100)</param>
    /// <param name="text">Optional progress text</param>
    public void SetProgress(int value, string? text = null)
    {
        lock (_lock)
        {
            CurrentProgress = value;
            if (text != null)
            {
                ProgressText = text;
            }
        }
    }

    /// <summary>
    /// Increment progress by a specific amount
    /// </summary>
    /// <param name="increment">Amount to increment (can be negative to decrement)</param>
    /// <param name="text">Optional progress text</param>
    /// <returns>New progress value after increment</returns>
    public int IncrementProgress(int increment, string? text = null)
    {
        lock (_lock)
        {
            CurrentProgress = CurrentProgress + increment;
            if (text != null)
            {
                ProgressText = text;
            }
            return CurrentProgress;
        }
    }

    /// <summary>
    /// Reset progress to zero
    /// </summary>
    /// <param name="text">Optional progress text for the reset state</param>
    public void ResetProgress(string? text = null)
    {
        lock (_lock)
        {
            CurrentProgress = 0;
            ProgressText = text ?? "Starting...";
        }
    }

    /// <summary>
    /// Update only the progress text without changing the value
    /// </summary>
    /// <param name="text">New progress text</param>
    public void UpdateProgressText(string text)
    {
        lock (_lock)
        {
            ProgressText = text;
        }
    }

    /// <summary>
    /// Get a formatted string representation of current progress
    /// </summary>
    /// <returns>Formatted progress string</returns>
    public string GetFormattedProgress()
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(ProgressText))
            {
                return $"{CurrentProgress}%";
            }
            return $"{ProgressText} ({CurrentProgress}%)";
        }
    }

    /// <summary>
    /// Check if progress is complete (100%)
    /// </summary>
    public bool IsComplete => CurrentProgress >= 100;

    /// <summary>
    /// Check if progress has started (> 0%)
    /// </summary>
    public bool HasStarted => CurrentProgress > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
