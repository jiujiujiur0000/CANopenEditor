using System.Collections.Generic;
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

public class CollisionTask
{
    public int Index { get; set; }
    public CollisionDialogResult Result { get; set; }
}

public partial class CollisionDialog : UserControl
{
    private List<CollisionTask> _tasks = new();
    private int _currentIndex = 0;

    public CollisionDialog()
    {
        InitializeComponent();
    }

    public CollisionDialog(List<CollisionTask> tasks)
    {
        InitializeComponent();
        _tasks = tasks;
        _currentIndex = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_currentIndex < _tasks.Count)
        {
            string indexHex = $"0x{_tasks[_currentIndex].Index:X4}";
            MessageText.Text = $"对象字典中已存在索引 {indexHex}，您希望如何处理？\n(当前进度: {_currentIndex + 1} / {_tasks.Count})";
            ApplyToAllCheckBox.IsChecked = false;
        }
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            if (System.Enum.TryParse<CollisionDialogResult>(tag, out var result))
            {
                if (result == CollisionDialogResult.Cancel)
                {
                    DialogHost.Close("RootDialogHost", "Cancel");
                    return;
                }

                bool applyToAll = ApplyToAllCheckBox.IsChecked ?? false;

                if (applyToAll)
                {
                    CollisionDialogResult applyResult = result == CollisionDialogResult.Overwrite ? CollisionDialogResult.Overwrite : CollisionDialogResult.Skip;
                    for (int i = _currentIndex; i < _tasks.Count; i++)
                    {
                        _tasks[i].Result = applyResult;
                    }
                    DialogHost.Close("RootDialogHost", "OK");
                    return;
                }

                _tasks[_currentIndex].Result = result;
                _currentIndex++;

                if (_currentIndex >= _tasks.Count)
                {
                    DialogHost.Close("RootDialogHost", "OK");
                }
                else
                {
                    UpdateUI();
                }
            }
        }
    }
}
