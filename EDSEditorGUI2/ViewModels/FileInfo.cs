using CommunityToolkit.Mvvm.ComponentModel;
using System;

using System.ComponentModel.DataAnnotations;

namespace EDSEditorGUI2.ViewModels;

public partial class FileInfo : ObservableValidator
{
    [ObservableProperty]
    [Required(ErrorMessage = "文件版本不能为空")]
    string _fileVersion = "1.0";

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private DateTime _creationTime;

    [ObservableProperty]
    [Required(ErrorMessage = "创建者不能为空")]
    string _createdBy = string.Empty;

    [ObservableProperty]
    private DateTime _modificationTime;

    [ObservableProperty]
    [Required(ErrorMessage = "修改者不能为空")]
    string _modifiedBy = string.Empty;
}
