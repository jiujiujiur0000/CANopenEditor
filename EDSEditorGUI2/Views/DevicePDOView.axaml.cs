using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;
using System;
using Avalonia.Layout;
using EDSEditorGUI2.ViewModels;
using System.Collections.Specialized;
using Avalonia.Threading;

namespace EDSEditorGUI2.Views;

public partial class DevicePDOView : UserControl
{
    private List<ColumnDefinition> _bitColumns = [];
    private DevicePDOViewModel? _vm;

    public DevicePDOView()
    {
        InitializeComponent();

        MappingGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        MappingGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        CreateMappingBitsAndBytesIndication();
        Zoom.Value = 100;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (_vm != null)
        {
            _vm.Mappings.CollectionChanged -= Mappings_CollectionChanged;
        }

        _vm = DataContext as DevicePDOViewModel;
        
        if (_vm != null)
        {
            _vm.Mappings.CollectionChanged += Mappings_CollectionChanged;
            Dispatcher.UIThread.InvokeAsync(UpdateGraphicalMappings);
        }
    }

    private void Mappings_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(UpdateGraphicalMappings);
    }

    private void UpdateGraphicalMappings()
    {
        if (_vm == null) return;
        
        var elementsToRemove = new List<Control>();
        foreach (var child in MappingGrid.Children)
        {
            if (Grid.GetRow((Control)child) >= 2)
            {
                elementsToRemove.Add((Control)child);
            }
        }
        foreach (var el in elementsToRemove)
        {
            MappingGrid.Children.Remove(el);
        }

        while (MappingGrid.RowDefinitions.Count > 2)
        {
            MappingGrid.RowDefinitions.RemoveAt(MappingGrid.RowDefinitions.Count - 1);
        }

        if (_vm.Mappings.Count == 0) return;

        int row = 2;
        int currentBit = 0;
        int mappingIndex = 1;

        string cobHex = _vm.SelectedSlot?.COB ?? "";
        if (cobHex.Length > 6) cobHex = cobHex.Substring(0, 6) + "..."; // Shorten if too long

        foreach (var mapping in _vm.Mappings)
        {
            MappingGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var idBlock = new TextBlock { Text = mappingIndex.ToString(), VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 10, 10) };
            AddToMappingGrid(idBlock, row, 0);

            var cobBlock = new TextBlock { Text = cobHex, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 10, 10) };
            AddToMappingGrid(cobBlock, row, 1);

            var indexBlock = new TextBlock { Text = string.IsNullOrEmpty(mapping.IndexString) ? "Empty" : mapping.IndexString, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 10, 10) };
            AddToMappingGrid(indexBlock, row, 2);

            int width = mapping.BitWidth;
            if (width > 0 && currentBit + width <= 64)
            {
                var isAvailable = !string.IsNullOrEmpty(mapping.IndexString);
                var bgBrush = isAvailable ? Brushes.Khaki : Brushes.LightGray;

                var border = new Border
                {
                    Background = bgBrush,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Avalonia.Thickness(1),
                    Margin = new Avalonia.Thickness(0, 0, 0, 5),
                    Child = new TextBlock 
                    { 
                        Text = isAvailable ? (mapping.IndexString + "/" + mapping.SubIndexString + "/" + mapping.Name) : "Empty", 
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.NoWrap,
                        ClipToBounds = true
                    }
                };

                AddToMappingGrid(border, row, 3 + currentBit, width);
                currentBit += width;
            }
            else
            {
                currentBit += width; // Might exceed 64
            }

            row++;
            mappingIndex++;
        }
    }

    void CreateMappingBitsAndBytesIndication()
    {
        //Bits
        for (int i = 0; i < 64; i++)
        {
            var indication = new TextBlock
            {
                Text = i.ToString(),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            if ((i % 8) == 0)
            {
                indication.Foreground = Brushes.Red;
            }
            AddToMappingGrid(indication, 0, 3 + i);

            var newColumn = new ColumnDefinition(new GridLength(10 * 1.0));
            _bitColumns.Add(newColumn);

            MappingGrid.ColumnDefinitions.Add(newColumn);
        }
        //Bytes
        for (int i = 0; i < 8; i++)
        {
            var indication = new TextBlock
            {
                Text = $"Byte {i}",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,

                TextWrapping = TextWrapping.Wrap,
            };
            AddToMappingGrid(indication, 1, 3 + (i*8), 8);
        }
    }
    void AddToMappingGrid(Control element, int row,int column, int columnspam = 1)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        Grid.SetColumnSpan(element, columnspam);
        MappingGrid.Children.Add(element);
    }

    private void Zoom_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "Value")
        {
            decimal newValue = Zoom.Value ?? 0;
            ChangeMappingZoom((double)newValue);
        }
    }
    /// <summary>
    /// Changes the zoom level on pdo mapping
    /// </summary>
    /// <param name="zoomLevel">zoom level in percent</param>
    private void ChangeMappingZoom(double zoomPercent)
    {
        var zoom = zoomPercent / 100;
        foreach (var column in _bitColumns)
        {
            column.Width = new GridLength(10 * zoom);
        }
    }
}
