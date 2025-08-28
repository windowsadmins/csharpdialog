using System;
using System.Threading.Tasks;

namespace csharpDialog.Core
{
    /// <summary>
    /// Interface for dialog services
    /// </summary>
    public interface IDialogService
    {
        Task<DialogResult> ShowDialogAsync(DialogConfiguration configuration);
        DialogResult ShowDialog(DialogConfiguration configuration);
    }

    /// <summary>
    /// Factory for creating dialog services
    /// </summary>
    public static class DialogServiceFactory
    {
        public static IDialogService CreateWpfDialogService()
        {
            // This will be implemented in the WPF project
            throw new NotImplementedException("WPF Dialog Service not implemented yet");
        }

        public static IDialogService CreateConsoleDialogService()
        {
            return new ConsoleDialogService();
        }
    }

    /// <summary>
    /// Basic console implementation of dialog service
    /// </summary>
    internal class ConsoleDialogService : IDialogService
    {
        public async Task<DialogResult> ShowDialogAsync(DialogConfiguration configuration)
        {
            return await Task.Run(() => ShowDialog(configuration));
        }

        public DialogResult ShowDialog(DialogConfiguration configuration)
        {
            Console.WriteLine($"Title: {configuration.Title}");
            Console.WriteLine($"Message: {configuration.Message}");
            Console.WriteLine();

            if (configuration.Buttons.Count > 0)
            {
                Console.WriteLine("Options:");
                for (int i = 0; i < configuration.Buttons.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {configuration.Buttons[i].Text}");
                }
                Console.Write("Select an option: ");
                
                if (int.TryParse(Console.ReadLine(), out int choice) && 
                    choice > 0 && choice <= configuration.Buttons.Count)
                {
                    return new DialogResult
                    {
                        ButtonPressed = configuration.Buttons[choice - 1].Action
                    };
                }
            }
            else
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }

            return new DialogResult
            {
                ButtonPressed = "ok"
            };
        }
    }
}
