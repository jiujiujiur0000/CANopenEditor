using CommunityToolkit.Mvvm.ComponentModel;

namespace EDSEditorGUI2.ViewModels;

public partial class ProjectInfo : ObservableValidator
{
    [ObservableProperty]
    private string _projectFile = string.Empty;

    [ObservableProperty]
    private string _projectFileVersion = string.Empty;

    [ObservableProperty]
    private string _xddFileStripped = string.Empty;

    [ObservableProperty]
    private string _edsFile = string.Empty;

    [ObservableProperty]
    private string _dcfFile = string.Empty;

    [ObservableProperty]
    private string _canopenNodeFile = string.Empty;

    [ObservableProperty]
    private string _canopenNodeFileVersion = string.Empty;

    [ObservableProperty]
    private string _documentationFile = string.Empty;
}
