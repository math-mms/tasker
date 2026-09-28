using System.Configuration;
using System.Data;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Tasker;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            MessageBox.Show($"Unhandled exception: {args.ExceptionObject}", "Error");
        };

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Dispatcher exception: {args.Exception}", "Error");
            args.Handled = true;
        };

        base.OnStartup(e);
    }
}

