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
    string _revisionNumber = "1";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "请输入有效的数字")]
    [RegularExpression(@"^\d+$", ErrorMessage = "请输入有效的数字")]
    string _granularity = "8";

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
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "请输入有效的数字")]
    [RegularExpression(@"^\d+$", ErrorMessage = "请输入有效的数字")]
    string _rpdoCount = "0";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "请输入有效的数字")]
    [RegularExpression(@"^\d+$", ErrorMessage = "请输入有效的数字")]
    string _tpdoCount = "0";

    [ObservableProperty]
    bool _nodeGuardingSlave;

    [ObservableProperty]
    bool _nodeGuardingMaster;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "请输入有效的数字")]
    [RegularExpression(@"^(?:[0-9]|[1-9][0-9]|1[0-1][0-9]|12[0-7])$", ErrorMessage = "监控节点数必须在0到127之间")]
    string _numberOfMonitoredNodes = "0";
}
