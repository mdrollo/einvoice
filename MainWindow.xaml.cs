using ERechnungPruefung.ViewModels;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Windows;

namespace ERechnungPruefung
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            webView.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ERechnungPruefung", "WebView2")
            };
            DataContext = ViewModel;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
                ViewModel.Load(files[0]);
        }
    }
}
