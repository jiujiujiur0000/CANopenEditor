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
        
        subindexGrid.AddHandler(Avalonia.Input.InputElement.PointerPressedEvent, SubindexGrid_PointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        subindexGrid.AddHandler(Avalonia.Input.InputElement.PointerMovedEvent, SubindexGrid_PointerMoved, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }
    
    private Avalonia.Point? _dragStartPoint;

    private void SubindexGrid_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragStartPoint = e.GetCurrentPoint(this).Position;
        }
        else
        {
            _dragStartPoint = null;
        }
    }

    private async void SubindexGrid_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (_dragStartPoint == null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var point = e.GetCurrentPoint(this);
        var distance = Math.Sqrt(Math.Pow(point.Position.X - _dragStartPoint.Value.X, 2) + Math.Pow(point.Position.Y - _dragStartPoint.Value.Y, 2));

        if (distance > 5 && subindexGrid.SelectedItem is DevicePDOViewModel.AvailableObjectViewModel draggedItem)
        {
            _dragStartPoint = null;

            var dragData = new Avalonia.Input.DataObject();
            dragData.Set("AvailableObjectViewModel", draggedItem);

            await Avalonia.Input.DragDrop.DoDragDrop(e, dragData, Avalonia.Input.DragDropEffects.Copy);
        }
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
            int currentOrdinal = 0;
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
                            IsHitTestVisible = true,
                            Child = new TextBlock 
                            { 
                                Text = isAvailable ? (entry.IndexString + "/" + entry.SubIndexString + "/" + entry.Name) : "Empty", 
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                TextWrapping = TextWrapping.NoWrap,
                                ClipToBounds = true
                            }
                        };
                        
                        ToolTip.SetTip(border, isAvailable ? (entry.IndexString + "/" + entry.SubIndexString + "/" + entry.Name) : "Empty");
                        
                        if (_vm != null)
                        {
                            var menu = new ContextMenu();
                            var items = new System.Collections.Generic.List<MenuItem>();
                            
                            if (isAvailable)
                            {
                                object? removeHeaderObj = null;
                                this.TryFindResource("str_pdo_remove_item", out removeHeaderObj);
                                var removeMenuItem = new MenuItem { Header = removeHeaderObj?.ToString() ?? "Remove Item" };
                                removeMenuItem.Click += (s, e) => {
                                    _vm.RemoveMapping(slot, mapping);
                                    Dispatcher.UIThread.InvokeAsync(UpdateGraphicalMappings);
                                    MarkDirty();
                                };
                                items.Add(removeMenuItem);
                            }

                            object? insertHeaderObj = null;
                            this.TryFindResource("str_pdo_insert_item", out insertHeaderObj);
                            var insertMenuItem = new MenuItem { Header = insertHeaderObj?.ToString() ?? "Insert Padding" };
                            int captureOrdinal = currentOrdinal;
                            insertMenuItem.Click += (s, e) => {
                                _vm.InsertDummyMapping(slot, captureOrdinal);
                                Dispatcher.UIThread.InvokeAsync(UpdateGraphicalMappings);
                                MarkDirty();
                            };
                            items.Add(insertMenuItem);
                            
                            menu.ItemsSource = items;
                            border.ContextMenu = menu;
                        }
                        
                        SetupDropTarget(border, slot, currentOrdinal);

                        AddToMappingGrid(border, row, 3 + currentBit, width);
                        currentBit += width;
                        currentOrdinal++;
                    }
                    else
                    {
                        currentBit += width;
                        currentOrdinal++;
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
                    IsHitTestVisible = true,
                    Child = new TextBlock 
                    { 
                        Text = "Empty", 
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.NoWrap,
                        ClipToBounds = true
                    }
                };

                ToolTip.SetTip(border, "Empty");

                if (_vm != null)
                {
                    var menu = new ContextMenu();
                    var items = new System.Collections.Generic.List<MenuItem>();

                    object? insertHeaderObj = null;
                    this.TryFindResource("str_pdo_insert_item", out insertHeaderObj);
                    var insertMenuItem = new MenuItem { Header = insertHeaderObj?.ToString() ?? "Insert Padding" };
                    int captureOrdinal = currentOrdinal;
                    insertMenuItem.Click += (s, e) => {
                        _vm.InsertDummyMapping(slot, captureOrdinal);
                        Dispatcher.UIThread.InvokeAsync(UpdateGraphicalMappings);
                        MarkDirty();
                    };
                    items.Add(insertMenuItem);
                    
                    menu.ItemsSource = items;
                    border.ContextMenu = menu;
                }
                
                SetupDropTarget(border, slot, currentOrdinal);
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

    private void SetupDropTarget(Control target, DevicePDOViewModel.PDOSlotViewModel slot, int ordinal)
    {
        Avalonia.Input.DragDrop.SetAllowDrop(target, true);
        target.AddHandler(Avalonia.Input.DragDrop.DragOverEvent, (s, e) =>
        {
            if (e.Data.Contains("AvailableObjectViewModel"))
            {
                e.DragEffects = Avalonia.Input.DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = Avalonia.Input.DragDropEffects.None;
            }
            e.Handled = true;
        }, Avalonia.Interactivity.RoutingStrategies.Bubble);

        target.AddHandler(Avalonia.Input.DragDrop.DropEvent, (s, e) =>
        {
            if (e.Data.Get("AvailableObjectViewModel") is DevicePDOViewModel.AvailableObjectViewModel vmItem && _vm != null)
            {
                _vm.InsertMapping(slot, ordinal, vmItem);
                Dispatcher.UIThread.InvokeAsync(UpdateGraphicalMappings);
                MarkDirty();
            }
            e.Handled = true;
        }, Avalonia.Interactivity.RoutingStrategies.Bubble);
    }

    private void MarkDirty()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is MainWindow mw)
            {
                mw.TriggerAutoSave();
            }
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
                FontWeight = Avalonia.Media.FontWeight.Medium,
                Foreground = Avalonia.Application.Current!.FindResource("SystemControlForegroundBaseMediumBrush") as IBrush ?? Brushes.Gray,
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
