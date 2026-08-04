using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using libEDSsharp;
using System;
using System.Collections.Generic;

namespace EDSEditorGUI2.ViewModels
{
    public partial class PreferencesViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _genericWarning;

        [ObservableProperty]
        private bool _renameWarning;

        [ObservableProperty]
        private bool _buildWarning;

        [ObservableProperty]
        private bool _stringWarning;

        [ObservableProperty]
        private bool _structWarning;

        [ObservableProperty]
        private ExporterFactory.Exporter _selectedExporter;

        [ObservableProperty]
        private string _selectedLanguage;

        public IEnumerable<ExporterFactory.Exporter> AvailableExporters =>
            (IEnumerable<ExporterFactory.Exporter>)Enum.GetValues(typeof(ExporterFactory.Exporter));

        public List<string> AvailableLanguages => new() { "en-US", "zh-CN" };

        public PreferencesViewModel()
        {
            UInt32 mask = Warnings.warning_mask;
            GenericWarning = (mask & 0x01) == 0x01;
            RenameWarning = (mask & 0x02) == 0x02;
            BuildWarning = (mask & 0x04) == 0x04;
            StringWarning = (mask & 0x08) == 0x08;
            StructWarning = (mask & 0x10) == 0x10;

            SelectedExporter = ConfigurationManager.Settings.CurrentExporter;
            SelectedLanguage = ConfigurationManager.Settings.CurrentLanguage;
        }

        [RelayCommand]
        private void Save()
        {
            UInt32 mask = 0xFFE0; // preserve upper bits

            if (GenericWarning) mask |= 0x01;
            if (RenameWarning) mask |= 0x02;
            if (BuildWarning) mask |= 0x04;
            if (StringWarning) mask |= 0x08;
            if (StructWarning) mask |= 0x10;

            Warnings.warning_mask = mask;
            ConfigurationManager.Settings.CurrentExporter = SelectedExporter;
            ConfigurationManager.Settings.CurrentLanguage = SelectedLanguage;

            // Save to JSON
            ConfigurationManager.Save();

            // Apply language globally
            App.ChangeLanguage(SelectedLanguage);
        }
    }
}
