using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using static System.Net.Mime.MediaTypeNames;

namespace OmniWatch.ViewModels.MessageDialog
{
    public partial class MessageDialogBoxViewModel : ObservableObject
    {
        public enum MessageDialogType
        {
            Unknown,
            Warning,
            Error,
            Success,
            Information
        }


        [ObservableProperty] private string _title;
        [ObservableProperty] private string _message;
        [ObservableProperty] private string _icon;
        [ObservableProperty] private IBrush _headerColor;


        public MessageDialogBoxViewModel()
        {
            if (Design.IsDesignMode)
            {
                Title = "Information";
                SetupVisuals(MessageDialogType.Information);
                Message = "This is a sample message for design-time preview.";
            }
        }

        public MessageDialogBoxViewModel(string title, string message, MessageDialogType type)
        {
            Message = message;
            Title = string.IsNullOrEmpty(title) ? type.ToString() : title;

            SetupVisuals(type);
        }

        private void SetupVisuals(MessageDialogType type)
        {
            (Icon, HeaderColor) = type switch
            {
                MessageDialogType.Error =>
                    ("\uE4F6", GetBrush("ErrorBrush")),

                MessageDialogType.Warning =>
                    ("\uEE44", GetBrush("DarkOrangeBrush")),

                MessageDialogType.Success =>
                    ("\uEBA6", GetBrush("AccentBrush")),

                MessageDialogType.Information =>
                    ("\uE2CE", GetBrush("InformationBrush")),

                _ =>
                    ("\uE3E8", Brushes.Gray)
            };
        }

        private static IBrush GetBrush(string key)
        {
            if (Avalonia.Application.Current?.TryFindResource(key, out var value) == true)
                return value as IBrush ?? Brushes.Gray;

            return Brushes.Gray;
        }
    }
}
