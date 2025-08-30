using System.Text.Json;
using csharpDialog.Core.Models;

namespace csharpDialog.Core.Services;

/// <summary>
/// Service for handling JSON dialog configurations
/// </summary>
public class JsonConfigurationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    /// <summary>
    /// Parse JSON configuration from string
    /// </summary>
    public static JsonDialogConfiguration? ParseConfiguration(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<JsonDialogConfiguration>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new JsonConfigurationException($"Invalid JSON configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parse JSON configuration from file
    /// </summary>
    public static async Task<JsonDialogConfiguration?> ParseConfigurationFromFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Configuration file not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath);
            return ParseConfiguration(json);
        }
        catch (JsonConfigurationException)
        {
            throw; // Re-throw JSON parsing errors
        }
        catch (Exception ex)
        {
            throw new JsonConfigurationException($"Error reading configuration file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Convert JSON configuration to standard DialogConfiguration
    /// </summary>
    public static DialogConfiguration ConvertToDialogConfiguration(JsonDialogConfiguration jsonConfig)
    {
        var config = new DialogConfiguration();

        // Basic properties
        if (!string.IsNullOrEmpty(jsonConfig.Title))
            config.Title = jsonConfig.Title;

        if (!string.IsNullOrEmpty(jsonConfig.Message))
            config.Message = jsonConfig.Message;

        // Convert buttons
        foreach (var jsonButton in jsonConfig.Buttons)
        {
            config.Buttons.Add(new DialogButton
            {
                Text = jsonButton.Text,
                Action = jsonButton.Action,
                IsDefault = jsonButton.IsDefault,
                IsCancel = jsonButton.IsCancel
            });
        }

        // Convert progress
        if (jsonConfig.Progress != null)
        {
            config.ShowProgressBar = true;
            config.ProgressValue = jsonConfig.Progress.Value;
            config.ProgressMaximum = jsonConfig.Progress.Maximum;
            config.ProgressText = jsonConfig.Progress.Text ?? string.Empty;
        }

        // Convert list items
        if (jsonConfig.ListItems.Count > 0)
        {
            config.ShowListItems = true;
            foreach (var jsonItem in jsonConfig.ListItems)
            {
                var status = StatusIconProvider.FromString(jsonItem.Status);
                config.ListItems.Add(new ListItemConfiguration
                {
                    Title = jsonItem.Title,
                    Status = status,
                    StatusText = jsonItem.StatusText ?? string.Empty
                });
            }
        }

        // Apply styling
        if (jsonConfig.Styling != null)
        {
            ApplyStyling(config, jsonConfig.Styling);
        }

        // Apply behavior
        if (jsonConfig.Behavior != null)
        {
            ApplyBehavior(config, jsonConfig.Behavior);
        }

        return config;
    }

    /// <summary>
    /// Apply styling configuration
    /// </summary>
    private static void ApplyStyling(DialogConfiguration config, JsonStyling styling)
    {
        if (styling.Width.HasValue)
            config.Width = styling.Width.Value;

        if (styling.Height.HasValue)
            config.Height = styling.Height.Value;

        // Store additional styling properties in metadata
        config.Metadata ??= new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(styling.Theme))
            config.Metadata["theme"] = styling.Theme;

        if (!string.IsNullOrEmpty(styling.Position))
            config.Metadata["position"] = styling.Position;

        if (!string.IsNullOrEmpty(styling.BackgroundColor))
            config.Metadata["backgroundColor"] = styling.BackgroundColor;

        if (!string.IsNullOrEmpty(styling.ForegroundColor))
            config.Metadata["foregroundColor"] = styling.ForegroundColor;

        if (!string.IsNullOrEmpty(styling.FontFamily))
            config.Metadata["fontFamily"] = styling.FontFamily;

        if (styling.FontSize.HasValue)
            config.Metadata["fontSize"] = styling.FontSize.Value;

        if (styling.Opacity.HasValue)
            config.Metadata["opacity"] = styling.Opacity.Value;

        if (styling.Animations != null)
            config.Metadata["animations"] = styling.Animations;
    }

    /// <summary>
    /// Apply behavior configuration
    /// </summary>
    private static void ApplyBehavior(DialogConfiguration config, JsonBehavior behavior)
    {
        if (behavior.Timeout.HasValue)
            config.TimeoutSeconds = behavior.Timeout.Value;

        // Store additional behavior properties in metadata
        config.Metadata ??= new Dictionary<string, object>();

        config.Metadata["autoClose"] = behavior.AutoClose;
        config.Metadata["moveable"] = behavior.Moveable;
        config.Metadata["resizable"] = behavior.Resizable;
        config.Metadata["topMost"] = behavior.TopMost;
        config.Metadata["centerOnScreen"] = behavior.CenterOnScreen;
        config.Metadata["showInTaskbar"] = behavior.ShowInTaskbar;
    }

    /// <summary>
    /// Validate JSON configuration
    /// </summary>
    public static ValidationResult ValidateConfiguration(JsonDialogConfiguration config)
    {
        var result = new ValidationResult();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(config.Title))
            result.Errors.Add("Title is required");

        if (string.IsNullOrWhiteSpace(config.Message))
            result.Errors.Add("Message is required");

        // Validate buttons
        if (config.Buttons.Count == 0)
            result.Warnings.Add("No buttons defined - dialog may be uncloseable");

        var defaultButtons = config.Buttons.Where(b => b.IsDefault).ToList();
        if (defaultButtons.Count > 1)
            result.Errors.Add("Only one button can be marked as default");

        var cancelButtons = config.Buttons.Where(b => b.IsCancel).ToList();
        if (cancelButtons.Count > 1)
            result.Errors.Add("Only one button can be marked as cancel");

        // Validate progress
        if (config.Progress != null)
        {
            if (config.Progress.Value < 0 || config.Progress.Value > config.Progress.Maximum)
                result.Errors.Add($"Progress value ({config.Progress.Value}) must be between 0 and {config.Progress.Maximum}");
        }

        // Validate list items
        foreach (var item in config.ListItems)
        {
            if (string.IsNullOrWhiteSpace(item.Title))
                result.Errors.Add("List item title cannot be empty");

            if (!IsValidStatus(item.Status))
                result.Warnings.Add($"Unknown status '{item.Status}' for list item '{item.Title}'");
        }

        // Validate styling
        if (config.Styling != null)
        {
            if (config.Styling.Width.HasValue && config.Styling.Width.Value <= 0)
                result.Errors.Add("Width must be positive");

            if (config.Styling.Height.HasValue && config.Styling.Height.Value <= 0)
                result.Errors.Add("Height must be positive");

            if (config.Styling.Opacity.HasValue && (config.Styling.Opacity.Value < 0 || config.Styling.Opacity.Value > 1))
                result.Errors.Add("Opacity must be between 0 and 1");
        }

        return result;
    }

    /// <summary>
    /// Generate sample JSON configuration
    /// </summary>
    public static string GenerateSampleConfiguration()
    {
        var sampleConfig = new JsonDialogConfiguration
        {
            Title = "Sample Dialog",
            Message = "This is a sample dialog configuration demonstrating advanced features.",
            Icon = "information",
            Buttons = new List<JsonButton>
            {
                new JsonButton
                {
                    Text = "Continue",
                    Action = "continue",
                    IsDefault = true,
                    Icon = "arrow-right",
                    Tooltip = "Proceed to next step"
                },
                new JsonButton
                {
                    Text = "Cancel",
                    Action = "cancel",
                    IsCancel = true,
                    Icon = "x",
                    Tooltip = "Cancel operation"
                }
            },
            Progress = new JsonProgress
            {
                Value = 0,
                Maximum = 100,
                Text = "Initializing...",
                ShowPercentage = true
            },
            ListItems = new List<JsonListItem>
            {
                new JsonListItem { Title = "Step 1", Status = "pending", StatusText = "Waiting..." },
                new JsonListItem { Title = "Step 2", Status = "none", StatusText = "Not started" },
                new JsonListItem { Title = "Step 3", Status = "none", StatusText = "Not started" }
            },
            Styling = new JsonStyling
            {
                Theme = "modern",
                Width = 500,
                Height = 400,
                Position = "center",
                Animations = new JsonAnimations
                {
                    FadeIn = true,
                    Duration = 300
                }
            },
            Behavior = new JsonBehavior
            {
                Timeout = 60,
                Moveable = true,
                CenterOnScreen = true,
                TopMost = false
            }
        };

        return JsonSerializer.Serialize(sampleConfig, SerializerOptions);
    }

    private static bool IsValidStatus(string status)
    {
        var validStatuses = new[] { "none", "wait", "pending", "progress", "success", "fail", "error" };
        return validStatuses.Contains(status.ToLowerInvariant());
    }
}

/// <summary>
/// Validation result for JSON configurations
/// </summary>
public class ValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public bool IsValid => Errors.Count == 0;
    public bool HasWarnings => Warnings.Count > 0;
}

/// <summary>
/// Exception for JSON configuration errors
/// </summary>
public class JsonConfigurationException : Exception
{
    public JsonConfigurationException(string message) : base(message) { }
    public JsonConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}
