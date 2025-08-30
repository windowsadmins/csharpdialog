using System;
using System.Collections.Generic;
using System.Linq;
using csharpDialog.Core.Models;

namespace csharpDialog.Core
{
    /// <summary>
    /// Command line argument parser for dialog configuration
    /// </summary>
    public class CommandLineParser
    {
        public static DialogConfiguration ParseArguments(string[] args)
        {
            var config = new DialogConfiguration();
            
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                string value = i + 1 < args.Length ? args[i + 1] : string.Empty;

                switch (arg)
                {
                    case "--title":
                    case "-t":
                        config.Title = value;
                        i++;
                        break;
                    case "--message":
                    case "-m":
                        config.Message = value;
                        i++;
                        break;
                    case "--icon":
                    case "-i":
                        config.Icon = value;
                        i++;
                        break;
                    case "--button1":
                        if (!string.IsNullOrEmpty(value))
                        {
                            config.Buttons.Add(new DialogButton { Text = value, Action = "button1", IsDefault = true });
                            i++;
                        }
                        break;
                    case "--button2":
                        if (!string.IsNullOrEmpty(value))
                        {
                            config.Buttons.Add(new DialogButton { Text = value, Action = "button2" });
                            i++;
                        }
                        break;
                    case "--timeout":
                        if (int.TryParse(value, out int timeout))
                        {
                            config.Timeout = timeout;
                            i++;
                        }
                        break;
                    case "--width":
                        if (int.TryParse(value, out int width))
                        {
                            config.Size.Width = width;
                            config.Size.AutoSize = false;
                            i++;
                        }
                        break;
                    case "--height":
                        if (int.TryParse(value, out int height))
                        {
                            config.Size.Height = height;
                            config.Size.AutoSize = false;
                            i++;
                        }
                        break;
                    case "--centeronscreen":
                        config.CenterOnScreen = true;
                        break;
                    case "--topmost":
                        config.Topmost = true;
                        break;
                    case "--backgroundcolor":
                        config.BackgroundColor = value;
                        i++;
                        break;
                    case "--textcolor":
                        config.TextColor = value;
                        i++;
                        break;
                    case "--fontfamily":
                        config.FontFamily = value;
                        i++;
                        break;
                    case "--fontsize":
                        if (int.TryParse(value, out int fontSize))
                        {
                            config.FontSize = fontSize;
                            i++;
                        }
                        break;
                    case "--markdown":
                        config.EnableMarkdown = true;
                        break;
                    case "--video":
                        config.VideoPath = value;
                        i++;
                        break;
                    case "--image":
                        config.ImagePath = value;
                        i++;
                        break;
                    case "--commandfile":
                        config.CommandFilePath = value;
                        config.EnableCommandFile = true;
                        i++;
                        break;
                    case "--progress":
                        config.ShowProgressBar = true;
                        if (int.TryParse(value, out int progressMax))
                        {
                            config.ProgressMaximum = progressMax;
                            i++;
                        }
                        break;
                    case "--progresstext":
                        config.ProgressText = value;
                        config.ShowProgressBar = true;
                        i++;
                        break;
                    case "--listitem":
                        // Basic list item support - enhanced version will come in Phase 2
                        config.ListItems.Add(new ListItemConfiguration(value));
                        config.ShowListItems = true;
                        i++;
                        break;
                    case "--help":
                    case "-h":
                        ShowHelp();
                        Environment.Exit(0);
                        break;
                }
            }

            // Set default button if no buttons specified
            if (config.Buttons.Count == 0)
            {
                config.Buttons.Add(new DialogButton { Text = "OK", Action = "ok", IsDefault = true });
            }

            return config;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("csharpDialog - Windows Dialog Utility");
            Console.WriteLine("A Windows port of swiftDialog");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  csharpdialog [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --title, -t <text>       Set dialog title");
            Console.WriteLine("  --message, -m <text>     Set dialog message");
            Console.WriteLine("  --icon, -i <path>        Set dialog icon");
            Console.WriteLine("  --button1 <text>         Set first button text");
            Console.WriteLine("  --button2 <text>         Set second button text");
            Console.WriteLine("  --timeout <seconds>      Auto-close after timeout");
            Console.WriteLine("  --width <pixels>         Set dialog width");
            Console.WriteLine("  --height <pixels>        Set dialog height");
            Console.WriteLine("  --centeronscreen         Center dialog on screen");
            Console.WriteLine("  --topmost                Keep dialog on top");
            Console.WriteLine("  --backgroundcolor <hex>  Set background color");
            Console.WriteLine("  --textcolor <hex>        Set text color");
            Console.WriteLine("  --fontfamily <name>      Set font family");
            Console.WriteLine("  --fontsize <size>        Set font size");
            Console.WriteLine("  --markdown               Enable markdown in message");
            Console.WriteLine("  --video <path>           Display video");
            Console.WriteLine("  --image <path>           Display image");
            Console.WriteLine("  --commandfile <path>     Monitor command file for updates");
            Console.WriteLine("  --progress [max]         Show progress bar");
            Console.WriteLine("  --progresstext <text>    Set progress text");
            Console.WriteLine("  --listitem <text>        Add list item");
            Console.WriteLine("  --help, -h               Show this help");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  csharpdialog --title \"Hello\" --message \"World\"");
            Console.WriteLine("  csharpdialog -t \"Confirm\" -m \"Are you sure?\" --button1 \"Yes\" --button2 \"No\"");
        }
    }
}
