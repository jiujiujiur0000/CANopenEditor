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

        if (_vm.Slots.Count == 0) return;

        int row = 2;
        int mappingIndex = 1;

        foreach (var slot in _vm.Slots)
        {
            MappingGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            string cobHex = slot.COB ?? "";
            if (cobHex.Length > 6) cobHex = cobHex.Substring(0, 6) + "..."; // Shorten if too long

            var idBorder = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Avalonia.Thickness(0, 0, 1, 1), Padding = new Avalonia.Thickness(10, 0, 10, 0), IsHitTestVisible = false };
            idBorder.Child = new TextBlock { Text = mappingIndex.ToString(), VerticalAlignment = VerticalAlignment.Center };
            AddToMappingGrid(idBorder, row, 0);

            var cobBorder = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Avalonia.Thickness(0, 0, 1, 1), Padding = new Avalonia.Thickness(10, 0, 10, 0), IsHitTestVisible = false };
            cobBorder.Child = new TextBlock { Text = cobHex, VerticalAlignment = VerticalAlignment.Center };
            AddToMappingGrid(cobBorder, row, 1);

            var indexBorder = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Avalonia.Thickness(0, 0, 1, 1), Padding = new Avalonia.Thickness(10, 0, 10, 0), IsHitTestVisible = false };
            indexBorder.Child = new TextBlock { Text = slot.Communication, VerticalAlignment = VerticalAlignment.Center };
            AddToMappingGrid(indexBorder, row, 2);

            int currentBit = 0;
            if (slot.Slot != null && slot.Slot.Mapping != null)
            {
                foreach (var mapping in slot.Slot.Mapping)
                {
                    var entry = new EDSEditorGUI2.ViewModels.DevicePDOViewModel.MappingEntryViewModel(mapping);
                    int width = entry.BitWidth;
                    if (width > 0 && currentBit + width <= 64)
                    {
                        var isAvailable = !string.IsNullOrEmpty(entry.IndexString);
                        var bgBrush = isAvailable ? Brushes.Khaki : Brushes.LightGray;

                        var border = new Border
                        {
                            Background = bgBrush,
                            BorderBrush = Brushes.Gray,
                            BorderThickness = new Avalonia.Thickness(0, 0, 1, 1),
                            IsHitTestVisible = false,
                            Child = new TextBlock 
                            { 
                                Text = isAvailable ? (entry.IndexString + "/" + entry.SubIndexString + "/" + entry.Name) : "Empty", 
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
                        currentBit += width;
                    }
                }
            }

            // Fill remainder with Empty
            if (currentBit < 64)
            {
                int remaining = 64 - currentBit;
                var border = new Border
                {
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Avalonia.Thickness(0, 0, 1, 1),
                    IsHitTestVisible = false,
                    Child = new TextBlock 
                    { 
                        Text = "Empty", 
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.NoWrap,
                        ClipToBounds = true
                    }
                };
                AddToMappingGrid(border, row, 3 + currentBit, remaining);
            }

            // Click handler to select slot
            bool isSelected = (slot == _vm.SelectedSlot);
            var rowOverlay = new Border 
            { 
                Background = isSelected ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(50, 0, 120, 215)) : Brushes.Transparent, 
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) 
            };
            
            // Hover effect
            rowOverlay.PointerEntered += (s, e) => 
            {
                if (!isSelected) rowOverlay.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(20, 0, 0, 0));
            };
            rowOverlay.PointerExited += (s, e) => 
            {
                if (!isSelected) rowOverlay.Background = Brushes.Transparent;
            };

            Grid.SetRow(rowOverlay, row);
            Grid.SetColumn(rowOverlay, 0);
            Grid.SetColumnSpan(rowOverlay, 67); // Span across all 67 columns (0 to 66)
            rowOverlay.PointerPressed += (s, e) => { _vm.SelectedSlot = slot; };
            MappingGrid.Children.Insert(0, rowOverlay);

            row++;
            mappingIndex++;
        }
    }

    void CreateMappingBitsAndBytesIndication()
    {
        //Bits
        for (int i = 0; i < 64; i++)
        {
            var border = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Avalonia.Thickness(0, 0, 1, 1) };
            var indication = new TextBlock
            {
                Text = i.ToString(),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            if ((i % 8) == 0)
            {
                indication.Foreground = Brushes.Red;
            }
            border.Child = indication;
            AddToMappingGrid(border, 0, 3 + i);

            var newColumn = new ColumnDefinition(new GridLength(11.5 * 1.0));
            _bitColumns.Add(newColumn);

            MappingGrid.ColumnDefinitions.Add(newColumn);
        }
        //Bytes
        for (int i = 0; i < 8; i++)
        {
            var border = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Avalonia.Thickness(0, 0, 1, 1) };
            var indication = new TextBlock
            {
                Text = $"Byte {i}",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            };
            border.Child = indication;
            AddToMappingGrid(border, 1, 3 + (i*8), 8);
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
            column.Width = new GridLength(11.5 * zoom);
        }
    }
}
