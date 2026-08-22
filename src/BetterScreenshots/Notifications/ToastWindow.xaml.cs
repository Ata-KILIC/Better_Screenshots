using System.Windows;

namespace BetterScreenshots.Notifications;

public partial class ToastWindow : Window
{
    public ToastWindow(string message)
    {
        InitializeComponent();
        Message.Text = message;
        Loaded += (_, _) => { Left = SystemParameters.WorkArea.Right - ActualWidth - 18; Top = SystemParameters.WorkArea.Bottom - ActualHeight - 18; };
    }
}
