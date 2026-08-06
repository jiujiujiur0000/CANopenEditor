using CommunityToolkit.Mvvm.ComponentModel;

namespace EDSEditorGUI2.ViewModels;

public partial class DeviceInfo : ObservableObject
{
    [ObservableProperty]
    string _vendorName = string.Empty;

    [ObservableProperty]
    string _vendorNumber = string.Empty;

    [ObservableProperty]
    string _productName = string.Empty;

    [ObservableProperty]
    string _productNumber = string.Empty;

    [ObservableProperty]
    string _revisionNumber = string.Empty;

    [ObservableProperty]
    uint _granularity = 8;

    [ObservableProperty]
    bool _baudRate10;

    [ObservableProperty]
    bool _baudRate20;

    [ObservableProperty]
    bool _baudRate50;

    [ObservableProperty]
    bool _baudRate125;

    [ObservableProperty]
    bool _baudRate250;

    [ObservableProperty]
    bool _baudRate500;

    [ObservableProperty]
    bool _baudRate800;

    [ObservableProperty]
    bool _baudRate1000;

    [ObservableProperty]
    bool _baudRateAuto;

    [ObservableProperty]
    bool _lssSlave;

    [ObservableProperty]
    bool _lssMaster;

    [ObservableProperty]
    uint _rpdoCount;

    [ObservableProperty]
    uint _tpdoCount;

    [ObservableProperty]
    bool _nodeGuardingSlave;

    [ObservableProperty]
    bool _nodeGuardingMaster;

    [ObservableProperty]
    uint _numberOfMonitoredNodes;
}
