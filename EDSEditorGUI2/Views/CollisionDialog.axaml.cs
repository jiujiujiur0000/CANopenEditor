using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;

namespace EDSEditorGUI2.Views;

public enum CollisionDialogResult
{
    Overwrite,
    Skip,
    OverwriteAll,
    SkipAll,
    Cancel
}

public partial class CollisionDialog : UserControl
{
    public CollisionDialog()
    {
        InitializeComponent();
    }

    public CollisionDialog(string index)
    {
        InitializeComponent();
        MessageText.Text = $"对象字典中已存在索引 {index}，您希望如何处理？";
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            if (System.Enum.TryParse<CollisionDialogResult>(tag, out var result))
            {
                DialogHost.Close("RootDialogHost", result);
            }
        }
    }
}
