using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using EDSEditorGUI2.ViewModels;
using EDSEditorGUI2.Views;

using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Data.Converters;

namespace EDSEditorGUI2;

public class ValidationErrorConverter : IValueConverter
{
    public static readonly ValidationErrorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Exception ex)
        {
            if (ex is InvalidCastException || ex is FormatException)
            {
                return "输入格式不正确，请输入有效的数字";
            }
            return ex.Message;
        }
        if (value is System.ComponentModel.DataAnnotations.ValidationResult vr)
        {
            return vr.ErrorMessage ?? "验证失败";
        }
        if (value != null)
        {
            var str = value.ToString();
            if (str != null && str.Contains("Could not convert"))
            {
                return "输入格式不正确，请输入有效的数字";
            }
            return str;
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ConfigurationManager.Load();
        ChangeLanguage(ConfigurationManager.Settings.CurrentLanguage);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            
            if (desktop.Args != null && desktop.Args.Length > 0)
            {
                var filePath = desktop.Args[0];
                ((MainWindow)desktop.MainWindow).OpenProjectProgrammatically(filePath);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void ChangeLanguage(string langCode)
    {
        var app = Current;
        if (app == null) return;

        var res = new Avalonia.Markup.Xaml.Styling.ResourceInclude(new System.Uri("avares://EDSEditorGUI2/App.axaml"))
        {
            Source = new System.Uri($"avares://EDSEditorGUI2/Assets/Langs/{langCode}.axaml")
        };

        app.Resources.MergedDictionaries.Clear();
        app.Resources.MergedDictionaries.Add(res);
    }
}