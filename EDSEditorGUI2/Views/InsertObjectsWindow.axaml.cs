using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using EDSEditorGUI2.ViewModels;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EDSEditorGUI2.Views;

public partial class InsertObjectsWindow : Window
{
    public InsertObjectsWindow()
    {
        InitializeComponent();
    }

    public void OnOffsetTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel dc && null != InsertObjects_Offsets.Text)
        {
            // look for "words" containing numbers
            string pattern = @"\b\d+\b";
            List<int> offsets = [];

            foreach (Match match in Regex.Matches(InsertObjects_Offsets.Text, pattern,
                                    RegexOptions.None,
                                    TimeSpan.FromSeconds(1)))
            {
                if (int.TryParse(match.Value, out int result))
                {
                    offsets.Add(result);
                }
            }
            if (offsets.Count == 0)
            {
                offsets.Add(0); // fallback
            }

            dc.UpdateMergeStatus(offsets);

            int columnsNeeded = 2 + offsets.Count;
            while (grid.Columns.Count != columnsNeeded)
            {
                // need to add or remove columns
                if (grid.Columns.Count > columnsNeeded)
                {
                    grid.Columns.RemoveAt(grid.Columns.Count - 1);
                }
                else
                {
                    int offset = offsets[grid.Columns.Count - 2];
                    int index = grid.Columns.Count - 2;
                    var cellTemplate = new FuncDataTemplate<ODIndexMergeStatus>((item, scope) =>
                    {
                        var textBlock = new TextBlock
                        {
                            Margin = new Avalonia.Thickness(12, 0),
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            [!TextBlock.TextProperty] = new Binding($"Offsets[{index}].Index") { Converter = new Converter.AbsoluteHexConverter() },
                            [!TextBlock.ForegroundProperty] = new Binding($"Offsets[{index}].Collision") { Converter = new Converter.BrushConverter() },
                        };
                        return textBlock;
                    });
                    DataGridTemplateColumn colOffset = new()
                    {
                        CellTemplate = cellTemplate,
                        Header = $"Offset {offset}",
                        IsReadOnly = true,
                        Width = new DataGridLength(120)
                    };
                    grid.Columns.Add(colOffset);
                }
            }
            // Update column headers and ensure widths
            for (var i = 0; i < offsets.Count; i++)
            {
                int offset = offsets[i];
                if (grid.Columns[2 + i].Header.ToString() != $"Offset {offset}")
                {
                    grid.Columns[2 + i].Header = $"Offset {offset}";
                }
                grid.Columns[2 + i].Width = new DataGridLength(120);
            }

            // Auto-scroll to the rightmost column to ensure the user knows it's there
            if (grid.Columns.Count > 2 && dc.MergeStatus.Count > 0)
            {
                var item = dc.MergeStatus[0];
                var column = grid.Columns[grid.Columns.Count - 1];
                Avalonia.Threading.Dispatcher.UIThread.Post(() => 
                {
                    grid.ScrollIntoView(item, column);
                }, Avalonia.Threading.DispatcherPriority.Background);
            }
        }
    }

    private void CancelClick(object? sender, RoutedEventArgs e)
    {
        Close("cancel");
    }

    private void InsertClick(object? sender, RoutedEventArgs e)
    {
        Close("insert");
    }
}
