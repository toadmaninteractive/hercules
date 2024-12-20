using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace Hercules
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var innerException = e.Exception.InnerException ?? e.Exception;
            Logger.LogException(innerException);
            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (UrlDispatcher.TryDispatch())
            {
                Environment.Exit(0);
                return;
            }
            base.OnStartup(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ViewModelTypes.CacheTypes(GetType().Assembly);
            ApplicationCommands.Close.InputGestures.Add(new KeyGesture(Key.W, ModifierKeys.Control));
            ServicePointManager.DefaultConnectionLimit = 1024;
            StyleManager.ApplicationTheme = new Windows7Theme();

            CultureInfo culture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}
