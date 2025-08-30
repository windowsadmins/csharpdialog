using System.Text.Json.Serialization;

namespace csharpDialog.Core.Models;

/// <summary>
/// JSON configuration for advanced dialog setups
/// </summary>
public class JsonDialogConfiguration
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("buttons")]
    public List<JsonButton> Buttons { get; set; } = new();

    [JsonPropertyName("progress")]
    public JsonProgress? Progress { get; set; }

    [JsonPropertyName("listItems")]
    public List<JsonListItem> ListItems { get; set; } = new();

    [JsonPropertyName("styling")]
    public JsonStyling? Styling { get; set; }

    [JsonPropertyName("behavior")]
    public JsonBehavior? Behavior { get; set; }

    [JsonPropertyName("workflow")]
    public JsonWorkflow? Workflow { get; set; }
}

/// <summary>
/// JSON button configuration with advanced options
/// </summary>
public class JsonButton
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("style")]
    public string? Style { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("isCancel")]
    public bool IsCancel { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [JsonPropertyName("tooltip")]
    public string? Tooltip { get; set; }

    [JsonPropertyName("shortcut")]
    public string? Shortcut { get; set; }
}

/// <summary>
/// JSON progress configuration
/// </summary>
public class JsonProgress
{
    [JsonPropertyName("value")]
    public int Value { get; set; } = 0;

    [JsonPropertyName("maximum")]
    public int Maximum { get; set; } = 100;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("showPercentage")]
    public bool ShowPercentage { get; set; } = true;

    [JsonPropertyName("indeterminate")]
    public bool Indeterminate { get; set; } = false;

    [JsonPropertyName("style")]
    public string? Style { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }
}

/// <summary>
/// JSON list item configuration
/// </summary>
public class JsonListItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "none";

    [JsonPropertyName("statusText")]
    public string? StatusText { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// JSON styling configuration
/// </summary>
public class JsonStyling
{
    [JsonPropertyName("theme")]
    public string? Theme { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; }

    [JsonPropertyName("foregroundColor")]
    public string? ForegroundColor { get; set; }

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("fontSize")]
    public int? FontSize { get; set; }

    [JsonPropertyName("borderStyle")]
    public string? BorderStyle { get; set; }

    [JsonPropertyName("opacity")]
    public double? Opacity { get; set; }

    [JsonPropertyName("animations")]
    public JsonAnimations? Animations { get; set; }
}

/// <summary>
/// JSON animation configuration
/// </summary>
public class JsonAnimations
{
    [JsonPropertyName("fadeIn")]
    public bool FadeIn { get; set; } = false;

    [JsonPropertyName("slideIn")]
    public string? SlideIn { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; } = 300;

    [JsonPropertyName("easing")]
    public string? Easing { get; set; }
}

/// <summary>
/// JSON behavior configuration
/// </summary>
public class JsonBehavior
{
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    [JsonPropertyName("autoClose")]
    public bool AutoClose { get; set; } = false;

    [JsonPropertyName("moveable")]
    public bool Moveable { get; set; } = true;

    [JsonPropertyName("resizable")]
    public bool Resizable { get; set; } = false;

    [JsonPropertyName("topMost")]
    public bool TopMost { get; set; } = false;

    [JsonPropertyName("centerOnScreen")]
    public bool CenterOnScreen { get; set; } = true;

    [JsonPropertyName("showInTaskbar")]
    public bool ShowInTaskbar { get; set; } = true;
}

/// <summary>
/// JSON workflow configuration for multi-step dialogs
/// </summary>
public class JsonWorkflow
{
    [JsonPropertyName("steps")]
    public List<JsonWorkflowStep> Steps { get; set; } = new();

    [JsonPropertyName("currentStep")]
    public int CurrentStep { get; set; } = 0;

    [JsonPropertyName("allowBackNavigation")]
    public bool AllowBackNavigation { get; set; } = true;

    [JsonPropertyName("showProgress")]
    public bool ShowProgress { get; set; } = true;
}

/// <summary>
/// JSON workflow step configuration
/// </summary>
public class JsonWorkflowStep
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("buttons")]
    public List<JsonButton> Buttons { get; set; } = new();

    [JsonPropertyName("listItems")]
    public List<JsonListItem> ListItems { get; set; } = new();

    [JsonPropertyName("validation")]
    public JsonValidation? Validation { get; set; }
}

/// <summary>
/// JSON validation configuration
/// </summary>
public class JsonValidation
{
    [JsonPropertyName("required")]
    public bool Required { get; set; } = false;

    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}
