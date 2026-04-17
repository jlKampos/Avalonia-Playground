namespace OmniWatch.ViewModels
{
    public class ErrorWindowViewModel
    {
        public string ErrorMessage { get; }

        public ErrorWindowViewModel(string message)
        {
            ErrorMessage = message;
        }
    }
}
