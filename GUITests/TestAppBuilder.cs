using Avalonia;
using Avalonia.Headless;
using EDSEditorGUI2;

using Xunit;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UseSkia() // enable Skia renderer
        .UseHeadless(new AvaloniaHeadlessPlatformOptions() { UseHeadlessDrawing = false });
}