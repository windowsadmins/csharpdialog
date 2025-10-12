using System.ComponentModel;

namespace csharpDialog.Core.Models;

/// <summary>
/// Configuration for a list item in the dialog
/// Supports dynamic status updates for real-time progress tracking
/// </summary>
public class ListItemConfiguration : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private string _icon = string.Empty;
    private string _iconUrl = string.Empty;
    private ListItemStatus _status = ListItemStatus.None;
    private string _statusText = string.Empty;
    private double _progress = 0.0;
    private bool _isVisible = true;

    /// <summary>
    /// The main title/text of the list item
    /// </summary>
    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    /// <summary>
    /// Optional subtitle for additional information
    /// </summary>
    public string Subtitle
    {
        get => _subtitle;
        set
        {
            if (_subtitle != value)
            {
                _subtitle = value;
                OnPropertyChanged(nameof(Subtitle));
            }
        }
    }

    /// <summary>
    /// Optional custom icon path, emoji, or icon name
    /// If empty, uses status icon
    /// Examples: "chrome.png", "C:\Icons\chrome.png", "🌐"
    /// </summary>
    public string Icon
    {
        get => _icon;
        set
        {
            if (_icon != value)
            {
                _icon = value;
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(DisplayIcon));
            }
        }
    }

    /// <summary>
    /// Optional icon URL for remote icons (repository-based icons)
    /// Takes precedence over Icon property if both are specified
    /// Example: "https://cimian.company.com/deployment/icons/chrome.png"
    /// Icons from URLs are cached locally for performance
    /// </summary>
    public string IconUrl
    {
        get => _iconUrl;
        set
        {
            if (_iconUrl != value)
            {
                _iconUrl = value;
                OnPropertyChanged(nameof(IconUrl));
                OnPropertyChanged(nameof(DisplayIcon));
            }
        }
    }

    /// <summary>
    /// Current status of the list item
    /// </summary>
    public ListItemStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(DisplayIcon));
                OnPropertyChanged(nameof(StatusString));
            }
        }
    }

    /// <summary>
    /// Additional status text for detailed information
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    /// <summary>
    /// Progress value (0.0 to 100.0) for progress status
    /// </summary>
    public double Progress
    {
        get => _progress;
        set
        {
            var clampedValue = Math.Max(0.0, Math.Min(100.0, value));
            if (Math.Abs(_progress - clampedValue) > 0.01)
            {
                _progress = clampedValue;
                OnPropertyChanged(nameof(Progress));
            }
        }
    }

    /// <summary>
    /// Whether this item is currently visible
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    /// <summary>
    /// Unique identifier for the list item (used for updates)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Index of the item in the list (used for ordering)
    /// </summary>
    public int Index { get; set; } = 0;

    /// <summary>
    /// Gets the icon to display (priority: IconUrl > Icon > Status Icon)
    /// </summary>
    public string DisplayIcon
    {
        get
        {
            // Priority 1: URL-based icon (from repository)
            if (!string.IsNullOrEmpty(IconUrl))
                return IconUrl;
            
            // Priority 2: Local icon path or emoji
            if (!string.IsNullOrEmpty(Icon))
                return Icon;
            
            // Priority 3: Status-based icon
            return StatusIconProvider.GetIcon(Status);
        }
    }

    /// <summary>
    /// Gets the string representation of the current status
    /// </summary>
    public string StatusString => StatusIconProvider.ToString(Status);

    public ListItemConfiguration()
    {
        Id = Guid.NewGuid().ToString();
    }

    public ListItemConfiguration(string title) : this()
    {
        Title = title;
    }

    /// <summary>
    /// Updates the status and optional status text
    /// </summary>
    public void UpdateStatus(ListItemStatus status, string statusText = "")
    {
        Status = status;
        if (!string.IsNullOrEmpty(statusText))
        {
            StatusText = statusText;
        }
    }

    /// <summary>
    /// Updates the status from a string value
    /// </summary>
    public void UpdateStatus(string status, string statusText = "")
    {
        UpdateStatus(StatusIconProvider.FromString(status), statusText);
    }

    /// <summary>
    /// Updates the progress value and sets status to Progress if > 0
    /// </summary>
    public void UpdateProgress(double progressValue)
    {
        Progress = progressValue;
        if (progressValue > 0 && Status == ListItemStatus.None)
        {
            Status = ListItemStatus.Progress;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString()
    {
        var parts = new List<string> { $"{DisplayIcon} {Title}" };
        
        if (!string.IsNullOrEmpty(Subtitle))
            parts.Add($"Subtitle: {Subtitle}");
            
        if (Status != ListItemStatus.None)
            parts.Add($"Status: {StatusString}");
            
        if (!string.IsNullOrEmpty(StatusText))
            parts.Add($"StatusText: {StatusText}");
            
        if (Progress > 0)
            parts.Add($"Progress: {Progress:F1}%");

        return string.Join(", ", parts);
    }
}
