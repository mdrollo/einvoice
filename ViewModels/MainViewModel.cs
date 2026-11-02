using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERechnungPruefung.Models;
using ERechnungPruefung.Services;
using Microsoft.Win32;
using System;
using System.IO;

namespace ERechnungPruefung.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private const int DokumentTab = 0;
        private const int ERechnungTab = 1;

        private readonly ERechnungParser _parser = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        public partial string FilePath { get; set; }

        [ObservableProperty]
        public partial Uri DocumentSource { get; set; }

        [ObservableProperty]
        public partial ERechnung ZugferdRechnung { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMessage))]
        public partial string Message { get; set; }

        [ObservableProperty]
        public partial int SelectedTabIndex { get; set; }

        public bool HasMessage => !string.IsNullOrEmpty(Message);

        public string Title => string.IsNullOrEmpty(FilePath)
            ? "E-Rechnung Prüfung"
            : $"E-Rechnung Prüfung – {Path.GetFileName(FilePath)}";

        [RelayCommand]
        private void OpenFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "E-Rechnung öffnen",
                Filter = "E-Rechnungen (*.pdf;*.xml)|*.pdf;*.xml|PDF (*.pdf)|*.pdf|XML (*.xml)|*.xml|Alle Dateien (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
                Load(dlg.FileName);
        }

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Message = $"Datei nicht gefunden: {filePath}";
                return;
            }

            FilePath = filePath;
            DocumentSource = new Uri(Path.GetFullPath(filePath));

            var result = _parser.ParseFile(filePath);
            ZugferdRechnung = result.Rechnung;
            Message = result.Success ? null : result.Message;
            SelectedTabIndex = result.Success ? ERechnungTab : DokumentTab;
        }
    }
}
