using System;
using System.Collections.Generic;
using csharpDialog.Core.Models;

namespace csharpDialog.Core
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
        public string BackgroundColor { get; set; } = "#f8f9fa";
        public string TextColor { get; set; } = "#2c3e50";
        public string FontFamily { get; set; } = "Segoe UI";
        public int FontSize { get; set; } = 14;
        public bool EnableMarkdown { get; set; } = false;
        public string VideoPath { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        
        // Command file configuration
        public string CommandFilePath { get; set; } = string.Empty;
        public bool EnableCommandFile { get; set; } = false;
        public bool AutoClearCommandFile { get; set; } = true;
        
        // Progress bar configuration
        public bool ShowProgressBar { get; set; } = false;
        public int ProgressValue { get; set; } = 0;
        public int ProgressMaximum { get; set; } = 100;
        public string ProgressText { get; set; } = string.Empty;
        
        // List items configuration
        public List<ListItemConfiguration> ListItems { get; set; } = new List<ListItemConfiguration>();
        public bool ShowListItems { get; set; } = false;
        
        // Phase 4: Enhanced configuration properties
        public int Width 
        { 
            get => Size.Width; 
            set => Size.Width = value; 
        }
        
        public int Height 
        { 
            get => Size.Height; 
            set => Size.Height = value; 
        }
        
        public int TimeoutSeconds 
        { 
            get => Timeout ?? 0; 
            set => Timeout = value > 0 ? value : null; 
        }
        
        /// <summary>
        /// Metadata dictionary for storing additional configuration data
        /// Used for advanced styling, behavior, and custom properties
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
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
        public int Width { get; set; } = 450;
        public int Height { get; set; } = 280;
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
