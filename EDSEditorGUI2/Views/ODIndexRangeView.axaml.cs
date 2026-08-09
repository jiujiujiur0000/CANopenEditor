using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using EDSEditorGUI2.Converter;
using LibCanOpen;
using System;
using System.Linq;

namespace EDSEditorGUI2.Views;

public partial class ODIndexRangeView : UserControl
{
    public ODIndexRangeView()
    {
        InitializeComponent();
        var values = Enum.GetNames(typeof(OdObject.Types.ObjectType)).Skip(1).ToArray();
        type.ItemsSource = values;

        // Grid loading row is no longer needed, using DataGridCollectionView instead
    }

    private Avalonia.Collections.DataGridCollectionView? _collectionView;
    private bool _isDataLoaded = false;
    
    public static readonly StyledProperty<Avalonia.Collections.DataGridCollectionView?> FilteredItemsProperty =
        AvaloniaProperty.Register<ODIndexRangeView, Avalonia.Collections.DataGridCollectionView?>(nameof(FilteredItems));

    public Avalonia.Collections.DataGridCollectionView? FilteredItems
    {
        get { return GetValue(FilteredItemsProperty); }
        set { SetValue(FilteredItemsProperty, value); }
    }
    
    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!_isDataLoaded)
        {
            LoadData();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _isDataLoaded = false;
        
        if (this.IsEffectivelyVisible || this.Parent == null) // Parent check is tricky, but IsEffectivelyVisible is safe
        {
             // wait, if we are in TabControl, we might be attached when switched
        }
        
        if (this.VisualRoot != null)
        {
            LoadData();
        }
        else
        {
            FilteredItems = null; // Clear to free memory
        }
    }

    private void LoadData()
    {
        if (_isDataLoaded) return;
        if (DataContext is System.Collections.IEnumerable collection)
        {
            int min = Convert.ToInt32(MinIndex, 16);
            int max = Convert.ToInt32(MaxIndex, 16);
            
            _collectionView = new Avalonia.Collections.DataGridCollectionView(collection);
            _collectionView.Filter = item =>
            {
                if (item is System.Collections.Generic.KeyValuePair<string, ViewModels.OdObject> dc)
                {
                    if (int.TryParse(dc.Key, System.Globalization.NumberStyles.HexNumber, null, out int index))
                    {
                        return min <= index && index <= max;
                    }
                }
                return false;
            };
            FilteredItems = _collectionView;
            _isDataLoaded = true;
        }
    }

    public static readonly StyledProperty<string> HeadingProperty =
        AvaloniaProperty.Register<ODIndexRangeView, string>(nameof(HeadingProperty));
    public string Heading
    {
        get { return GetValue(HeadingProperty); }
        set { SetValue(HeadingProperty, value); }
    }

    public static readonly StyledProperty<string> MinIndexProperty =
        AvaloniaProperty.Register<ODIndexRangeView, string>(nameof(MinIndexProperty));
    public string MinIndex
    {
        get { return GetValue(MinIndexProperty); }
        set { SetValue(MinIndexProperty, value); }
    }

    public static readonly StyledProperty<string> MaxIndexProperty =
        AvaloniaProperty.Register<ODIndexRangeView, string>(nameof(MaxIndexProperty));
    public string MaxIndex
    {
        get { return GetValue(MaxIndexProperty); }
        set { SetValue(MaxIndexProperty, value); }
    }

    private async void AddIndex(object? sender, RoutedEventArgs e)
    {
        if (Resources["NewIndexDialog"] is StackPanel dialog)
        {
            var errorText = dialog.Children.OfType<TextBlock>().FirstOrDefault(x => x.Name == "errorText");
            if (errorText != null) errorText.IsVisible = false;
        }
        await DialogHost.Show(Resources["NewIndexDialog"]!, "NoAnimationDialogHost", OnDialogClosing);
    }

    private void OnDialogClosing(object? sender, DialogClosingEventArgs e)
    {
        if (e.Parameter != null)
        {
            if (DataContext is ViewModels.ObjectDictionary dc && e.Parameter is NewIndexRequest param)
            {
                int min = Convert.ToInt32(MinIndex, 16);
                int max = Convert.ToInt32(MaxIndex, 16);

                if (param.Index < min || param.Index > max)
                {
                    if (Resources["NewIndexDialog"] is StackPanel pnl)
                    {
                        var errorText = pnl.Children.OfType<TextBlock>().FirstOrDefault(x => x.Name == "errorText");
                        if (errorText != null)
                        {
                            errorText.Text = $"索引超出范围。有效范围: 0x{min:X4} - 0x{max:X4}。";
                            errorText.IsVisible = true;
                        }
                    }
                    e.Cancel();
                    return;
                }

                if (string.IsNullOrWhiteSpace(param.Name))
                {
                    if (Resources["NewIndexDialog"] is StackPanel pnl)
                    {
                        var errorText = pnl.Children.OfType<TextBlock>().FirstOrDefault(x => x.Name == "errorText");
                        if (errorText != null)
                        {
                            errorText.Text = $"名称不能为空！";
                            errorText.IsVisible = true;
                        }
                    }
                    e.Cancel();
                    return;
                }

                if (dc.ContainsKey(param.Index.ToString("X4")))
                {
                    if (Resources["NewIndexDialog"] is StackPanel pnl)
                    {
                        var errorText = pnl.Children.OfType<TextBlock>().FirstOrDefault(x => x.Name == "errorText");
                        if (errorText != null)
                        {
                            errorText.Text = $"索引 0x{param.Index:X4} 已经存在！";
                            errorText.IsVisible = true;
                        }
                    }
                    e.Cancel();
                    return;
                }

                dc.AddIndex(param.Index, param.Name, param.Type);
            }
        }
    }

    private void RemoveIndex(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ObjectDictionary dc && grid.SelectedItem is System.Collections.Generic.KeyValuePair<string, ViewModels.OdObject> selected)
        {
            dc.Remove(selected.Key);
        }
    }

    private void ToggleIndex(object? sender, RoutedEventArgs e)
    {
        if (grid.SelectedItem is System.Collections.Generic.KeyValuePair<string, ViewModels.OdObject> selected)
        {
            selected.Value.Disabled = !selected.Value.Disabled;
        }
    }

    private void ContextMenuSubObjectRemoveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void DataGrid_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
    }

    private void DataGrid_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (e.Source is Avalonia.Controls.Control control && control.DataContext is System.Collections.Generic.KeyValuePair<string, ViewModels.OdObject>)
            {
                grid.SelectedItem = control.DataContext;
            }
        }
    }

    private void DataGrid_LoadingRow(object? sender, Avalonia.Controls.DataGridRowEventArgs e)
    {
        if (e.Row.DataContext is System.Collections.Generic.KeyValuePair<string, ViewModels.OdObject> pair)
        {
            if (pair.Value.Disabled)
            {
                e.Row.Classes.Add("disabled");
            }
            else
            {
                e.Row.Classes.Remove("disabled");
            }
        }
    }
}