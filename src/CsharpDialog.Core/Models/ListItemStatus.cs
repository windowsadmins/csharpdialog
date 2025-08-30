namespace csharpDialog.Core.Models
{
    /// <summary>
    /// Represents the status of a list item in csharpDialog
    /// Compatible with swiftDialog status types
    /// </summary>
    public enum ListItemStatus
    {
        /// <summary>
        /// Default state - no specific status
        /// </summary>
        None,
        
        /// <summary>
        /// Item is waiting to be processed
        /// Icon: ⏳ (hourglass)
        /// </summary>
        Wait,
        
        /// <summary>
        /// Item completed successfully
        /// Icon: ✅ (check mark)
        /// </summary>
        Success,
        
        /// <summary>
        /// Item failed to complete
        /// Icon: ❌ (cross mark)
        /// </summary>
        Fail,
        
        /// <summary>
        /// Item encountered an error
        /// Icon: ⚠️ (warning triangle)
        /// </summary>
        Error,
        
        /// <summary>
        /// Item is pending/queued for processing
        /// Icon: 🔵 (blue circle)
        /// </summary>
        Pending,
        
        /// <summary>
        /// Item is currently in progress
        /// Icon: 🔄 (spinning arrows)
        /// </summary>
        Progress
    }

    /// <summary>
    /// Provides status icon mappings for list items
    /// </summary>
    public static class StatusIconProvider
    {
        /// <summary>
        /// Gets the Unicode icon for a given status
        /// </summary>
        public static string GetIcon(ListItemStatus status)
        {
            return status switch
            {
                ListItemStatus.Wait => "⏳",
                ListItemStatus.Success => "✅",
                ListItemStatus.Fail => "❌",
                ListItemStatus.Error => "⚠️",
                ListItemStatus.Pending => "🔵",
                ListItemStatus.Progress => "🔄",
                ListItemStatus.None => "",
                _ => ""
            };
        }

        /// <summary>
        /// Gets the status from a string (case-insensitive)
        /// Compatible with swiftDialog status strings
        /// </summary>
        public static ListItemStatus FromString(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "wait" => ListItemStatus.Wait,
                "success" => ListItemStatus.Success,
                "fail" => ListItemStatus.Fail,
                "error" => ListItemStatus.Error,
                "pending" => ListItemStatus.Pending,
                "progress" => ListItemStatus.Progress,
                "none" or "" or null => ListItemStatus.None,
                _ => ListItemStatus.None
            };
        }

        /// <summary>
        /// Gets the string representation of a status
        /// </summary>
        public static string ToString(ListItemStatus status)
        {
            return status switch
            {
                ListItemStatus.Wait => "wait",
                ListItemStatus.Success => "success",
                ListItemStatus.Fail => "fail",
                ListItemStatus.Error => "error",
                ListItemStatus.Pending => "pending",
                ListItemStatus.Progress => "progress",
                ListItemStatus.None => "none",
                _ => "none"
            };
        }
    }
}
