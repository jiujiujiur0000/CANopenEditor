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
    
    public static readonly StyledProperty<Avalonia.Collections.DataGridCollectionView?> FilteredItemsProperty =
        AvaloniaProperty.Register<ODIndexRangeView, Avalonia.Collections.DataGridCollectionView?>(nameof(FilteredItems));

    public Avalonia.Collections.DataGridCollectionView? FilteredItems
    {
        get { return GetValue(FilteredItemsProperty); }
        set { SetValue(FilteredItemsProperty, value); }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

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
        }
        else
        {
            FilteredItems = null;
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
        await DialogHost.Show(Resources["NewIndexDialog"]!, "NoAnimationDialogHost", OnDialogClosing);
    }

    private void OnDialogClosing(object? sender, DialogClosingEventArgs e)
    {
        if (e.Parameter != null)
        {
            if (DataContext is ViewModels.ObjectDictionary dc && e.Parameter is NewIndexRequest param)
            {
                dc.AddIndex(param.Index, param.Name, param.Type);
            }
        }
    }

    private async void RemoveIndex(object? sender, RoutedEventArgs e)
    {
        await DialogHost.Show(Resources["NewIndexDialog"]!, "NoAnimationDialogHost");
    }

    private void ContextMenuSubObjectRemoveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void DataGrid_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
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