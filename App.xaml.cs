using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace ERechnungPruefung
{
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            var culture = CultureInfo.GetCultureInfo("de-DE");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            var window = new MainWindow();
            window.Show();
            if (e.Args.Length > 0)
                window.ViewModel.Load(e.Args[0]);
        }
    }
}
