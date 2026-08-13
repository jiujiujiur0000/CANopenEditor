using CommunityToolkit.Mvvm.ComponentModel;
using System;

using System.ComponentModel.DataAnnotations;

namespace EDSEditorGUI2.ViewModels;

public partial class DeviceCommissioning : ObservableValidator
{
    [ObservableProperty]
    [Range(1, 127, ErrorMessage = "节点ID必须在1到127之间")]
    private UInt32? _nodeId;

    [ObservableProperty]
    [Required(ErrorMessage = "节点名称不能为空")]
    private string _nodeName = string.Empty;

    [ObservableProperty]
    private UInt32? _baudrate;

    [ObservableProperty]
    private UInt32? _netNumber;

    [ObservableProperty]
    [Required(ErrorMessage = "网络名称不能为空")]
    private string _netName = string.Empty;

    [ObservableProperty]
    private bool _canopenManager;

    [ObservableProperty]
    private UInt32? _lssSerialNo;
}
