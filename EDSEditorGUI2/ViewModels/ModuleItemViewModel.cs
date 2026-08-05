using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace EDSEditorGUI2.ViewModels;

public partial class ModuleItemViewModel : ObservableObject
{
    [ObservableProperty]
    private uint _index;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private string _productVersion = string.Empty;

    [ObservableProperty]
    private string _productRevision = string.Empty;

    [ObservableProperty]
    private string _orderCode = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnectedBrush))]
    private bool _isConnected;

    public Avalonia.Media.IBrush IsConnectedBrush => _isConnected ? Avalonia.Media.Brushes.MediumSeaGreen : Avalonia.Media.Brushes.Tomato;

    [ObservableProperty]
    private string _comments = string.Empty;

    public ObservableCollection<ModuleObjectViewModel> ExtendedObjects { get; } = new();
}

public partial class ModuleObjectViewModel : ObservableObject
{
    [ObservableProperty]
    private string _indexHex = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;
}
