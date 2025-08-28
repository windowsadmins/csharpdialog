using System;
using System.Collections.Generic;

namespace CsharpDialog.Core
{
    /// <summary>
    /// Represents the configuration for a dialog box
    /// </summary>
    public class DialogConfiguration
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<DialogButton> Buttons { get; set; } = new List<DialogButton>();
        public DialogSize Size { get; set; } = new DialogSize();
        public int? Timeout { get; set; }
        public bool CenterOnScreen { get; set; } = true;
        public bool Topmost { get; set; } = false;
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public string TextColor { get; set; } = "#000000";
        public string FontFamily { get; set; } = "Segoe UI";
        public int FontSize { get; set; } = 12;
        public bool EnableMarkdown { get; set; } = false;
        public string VideoPath { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a button in the dialog
    /// </summary>
    public class DialogButton
    {
        public string Text { get; set; } = "OK";
        public string Action { get; set; } = "ok";
        public bool IsDefault { get; set; } = false;
        public bool IsCancel { get; set; } = false;
    }

    /// <summary>
    /// Represents the size configuration for the dialog
    /// </summary>
    public class DialogSize
    {
        public int Width { get; set; } = 400;
        public int Height { get; set; } = 300;
        public bool AutoSize { get; set; } = true;
    }

    /// <summary>
    /// Represents the result of a dialog interaction
    /// </summary>
    public class DialogResult
    {
        public string ButtonPressed { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool TimedOut { get; set; } = false;
        public Dictionary<string, string> FormData { get; set; } = new Dictionary<string, string>();
    }
}
