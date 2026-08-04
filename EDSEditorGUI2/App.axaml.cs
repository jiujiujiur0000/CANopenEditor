using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using EDSEditorGUI2.ViewModels;
using EDSEditorGUI2.Views;

namespace EDSEditorGUI2;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
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