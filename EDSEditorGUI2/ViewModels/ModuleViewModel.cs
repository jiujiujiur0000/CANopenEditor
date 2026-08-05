using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace EDSEditorGUI2.ViewModels;

public partial class ModuleViewModel : ObservableObject
{
    [ObservableProperty]
    private uint _nrSupportedModules;

    public ObservableCollection<ModuleItemViewModel> Modules { get; } = new();

    [ObservableProperty]
    private ModuleItemViewModel? _selectedModule;
}
