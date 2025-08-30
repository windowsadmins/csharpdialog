using System.Threading.Tasks;

namespace CSharpDialog.Core.Services
{
    public interface IDialogManager
    {
        Task SendCommandAsync(string command);
        Task<bool> UpdateStyleAsync(string element, string property, object value);
        Task<bool> ApplyThemeAsync(string themeName);
        Task<bool> ShowDialogAsync();
        Task CloseDialogAsync();
        bool IsDialogOpen { get; }
    }
}
