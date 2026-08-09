using CommunityToolkit.Mvvm.ComponentModel;

using System.ComponentModel.DataAnnotations;

namespace EDSEditorGUI2.ViewModels;

public partial class DeviceInfo : ObservableValidator
{
    [ObservableProperty]
    [Required(ErrorMessage = "供应商名称不能为空")]
    string _vendorName = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "供应商编号不能为空")]
    [RegularExpression(@"^(0[xX])?[0-9a-fA-F]+$|^\d+$", ErrorMessage = "必须是有效的数字或十六进制")]
    string _vendorNumber = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "产品名称不能为空")]
    string _productName = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "产品编号不能为空")]
    [RegularExpression(@"^(0[xX])?[0-9a-fA-F]+$|^\d+$", ErrorMessage = "必须是有效的数字或十六进制")]
    string _productNumber = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "修订版本号不能为空")]
    [RegularExpression(@"^(0[xX])?[0-9a-fA-F]+$|^\d+$", ErrorMessage = "必须是有效的数字或十六进制")]
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
    [Range(0, 127, ErrorMessage = "监控节点数不能超过127")]
    uint _numberOfMonitoredNodes;
}
