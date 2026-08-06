using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using libEDSsharp;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private ExporterFactory.Exporter _selectedExporter;

        [ObservableProperty]
        private string _selectedLanguage;

        public class ExporterOption
        {
            public ExporterFactory.Exporter Value { get; }
            public string Display { get; }
            public ExporterOption(ExporterFactory.Exporter value, string display) { Value = value; Display = display; }
            public override string ToString() => Display;
        }

        public List<ExporterOption> AvailableExporters { get; } = new()
        {
            new(ExporterFactory.Exporter.CANOPENNODE_V4, "V4"),
            new(ExporterFactory.Exporter.CANOPENNODE_LEGACY, "Legacy (V1)")
        };

        private ExporterOption _selectedExporterOption;
        public ExporterOption SelectedExporterOption
        {
            get => _selectedExporterOption;
            set
            {
                SetProperty(ref _selectedExporterOption, value);
                _selectedExporter = value?.Value ?? ExporterFactory.Exporter.CANOPENNODE_V4;
            }
        }

        public class LanguageOption
        {
            public string Code { get; set; }
            public string Display { get; set; }
            public LanguageOption(string code, string display) { Code = code; Display = display; }
            public override string ToString() => Display;
        }

        public List<LanguageOption> AvailableLanguages { get; } = new() 
        { 
            new("en-US", "English"), 
            new("zh-CN", "中文") 
        };

        [ObservableProperty]
        private string _selectedTheme;

        private LanguageOption _selectedLanguageOption;
        public LanguageOption SelectedLanguageOption
        {
            get => _selectedLanguageOption;
            set
            {
                SetProperty(ref _selectedLanguageOption, value);
                SelectedLanguage = value?.Code ?? "en-US";
            }
        }

        public PreferencesViewModel()
        {
            UInt32 mask = Warnings.warning_mask;
            GenericWarning = (mask & 0x01) == 0x01;
            RenameWarning = (mask & 0x02) == 0x02;
            BuildWarning = (mask & 0x04) == 0x04;
            StringWarning = (mask & 0x08) == 0x08;
            StructWarning = (mask & 0x10) == 0x10;

            _selectedExporter = ConfigurationManager.Settings.CurrentExporter;
            _selectedExporterOption = AvailableExporters.FirstOrDefault(x => x.Value == _selectedExporter) ?? AvailableExporters[0];
            SelectedLanguage = ConfigurationManager.Settings.CurrentLanguage;
            _selectedLanguageOption = AvailableLanguages.FirstOrDefault(x => x.Code == SelectedLanguage) ?? AvailableLanguages[0];

            SelectedTheme = ConfigurationManager.Settings.CurrentTheme ?? "Default";
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
            ConfigurationManager.Settings.CurrentExporter = _selectedExporter;
            ConfigurationManager.Settings.CurrentLanguage = SelectedLanguage;
            ConfigurationManager.Settings.CurrentTheme = SelectedTheme ?? "Default";

            // Save to JSON
            ConfigurationManager.Save();

            // Apply language globally
            App.ChangeLanguage(SelectedLanguage);

            // Apply theme globally
            var app = Avalonia.Application.Current;
            if (app != null)
            {
                app.RequestedThemeVariant = ConfigurationManager.Settings.CurrentTheme switch
                {
                    "Light" => Avalonia.Styling.ThemeVariant.Light,
                    "Dark" => Avalonia.Styling.ThemeVariant.Dark,
                    _ => Avalonia.Styling.ThemeVariant.Default
                };
            }
        }
    }
}
