using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDSEditorGUI2.Mapper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EDSEditorGUI2.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    int Counter = 1;
    public bool HasNoDevice => Network.Count == 0;
    
    public ObservableCollection<string> RecentFiles { get; } = new();
    
    public bool HasRecentFiles => RecentFiles.Count > 0;
    
    public void LoadRecentFiles()
    {
        RecentFiles.Clear();
        foreach (var file in ConfigurationManager.Settings.RecentFiles)
        {
            RecentFiles.Add(file);
        }
        OnPropertyChanged(nameof(HasRecentFiles));
    }

    public void AddRecentFile(string path)
    {
        if (RecentFiles.Contains(path))
        {
            RecentFiles.Remove(path);
        }
        RecentFiles.Insert(0, path);
        if (RecentFiles.Count > 10)
        {
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }
        ConfigurationManager.Settings.RecentFiles.Clear();
        ConfigurationManager.Settings.RecentFiles.AddRange(RecentFiles);
        ConfigurationManager.Save();
        OnPropertyChanged(nameof(HasRecentFiles));
    }

    [RelayCommand]
    public void RemoveRecentFile(string path)
    {
        if (RecentFiles.Contains(path))
        {
            RecentFiles.Remove(path);
            ConfigurationManager.Settings.RecentFiles.Clear();
            ConfigurationManager.Settings.RecentFiles.AddRange(RecentFiles);
            ConfigurationManager.Save();
            OnPropertyChanged(nameof(HasRecentFiles));
        }
    }

    [RelayCommand]
    public void ClearRecentFiles()
    {
        RecentFiles.Clear();
        ConfigurationManager.Settings.RecentFiles.Clear();
        ConfigurationManager.Save();
        OnPropertyChanged(nameof(HasRecentFiles));
    }

    [ObservableProperty]
    private string? _currentProjectPath;

    public string? CurrentProjectName
    {
        get
        {
            if (string.IsNullOrEmpty(CurrentProjectPath)) return null;
            return System.IO.Path.GetFileNameWithoutExtension(CurrentProjectPath);
        }
    }

    partial void OnCurrentProjectPathChanged(string? value)
    {
        OnPropertyChanged(nameof(CurrentProjectName));
        OnPropertyChanged(nameof(IsProjectMode));
        OnPropertyChanged(nameof(CanRemoveDevice));
    }

    public bool IsProjectMode
    {
        get
        {
            if (string.IsNullOrEmpty(CurrentProjectPath)) return Network.Count > 0;
            string ext = System.IO.Path.GetExtension(CurrentProjectPath).ToLower();
            return ext == ".cpj" || ext == ".xpd" || ext == ".rpj"; // Assuming .rpj is a typo for cpj, but just in case
        }
    }

    public bool CanRemoveDevice => IsProjectMode && SelectedDevice != null;



    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool _isDirty;

    public string WindowTitle => IsDirty ? "CANopen Editor 2 *" : "CANopen Editor 2";

    [ObservableProperty]
    private bool _isLoading;

    public MainWindowViewModel()
    {
        LoadRecentFiles();
        
        Network.CollectionChanged += (s, e) => 
        {
            OnPropertyChanged(nameof(HasNoDevice));
            OnPropertyChanged(nameof(HasDevice));
            OnPropertyChanged(nameof(IsProjectMode));
            OnPropertyChanged(nameof(CanRemoveDevice));

            if (e.NewItems != null)
            {
                foreach (Device device in e.NewItems)
                {
                    device.PropertyChanged += Device_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (Device device in e.OldItems)
                {
                    device.PropertyChanged -= Device_PropertyChanged;
                }
            }
        };
    }

    private void Device_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Device.IsDirty))
        {
            if (sender is Device device && device.IsDirty)
            {
                this.IsDirty = true;
            }
        }
    }

    public bool HasDevice => Network.Count > 0;

    public void AddNewDevice(object? sender = null)
    {
        var device = new LibCanOpen.CanOpenDevice
        {
            DeviceInfo = new()
            {
                ProductName = "NewDevice" + Counter.ToString(),
                Granularity = 8
            },
            DeviceCommissioning = new(),
            FileInfo = new()
            {
                CreationTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow),
                ModificationTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
            }
        };

        Counter++;

        //string dir = Environment.OSVersion.Platform == PlatformID.Win32NT ? "\\" : "/";
        //eds.projectFilename = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + dir + "project";

        //DeviceView device = new DeviceView(eds, network);
        //device.UpdateODViewForEDS += Device_UpdateODViewForEDS;

        //eds.OnDataDirty += Eds_onDataDirty;

        //device.Dock = DockStyle.Fill;
        //device.dispatch_updateOD();

        var deviceView = ProtobufferViewModelMapper.MapFromProtobuffer(device);
        deviceView.Eds = new libEDSsharp.EDSsharp();
        Network.Add(deviceView);
        SelectedDevice = deviceView;
    }

    [RelayCommand]
    public void CloseDevice(Device device)
    {
        if (device != null && Network.Contains(device))
        {
            Network.Remove(device);
            if (SelectedDevice == device)
            {
                SelectedDevice = Network.Count > 0 ? Network[0] : null;
            }
        }
    }

    [RelayCommand]
    public void CloseAllDevices()
    {
        Network.Clear();
        SelectedDevice = null;
        CurrentProjectPath = null;
        IsDirty = false;
    }

    [RelayCommand]
    public void NewProject()
    {
        Network.Clear();
        AddNewDevice();
    }

    public void InitMergeStatus(Device profile, List<int> offsets)
    {
        MergeStatus.Clear();
        if (SelectedDevice is not null && profile is not null && profile.Objects is not null)
        {
            foreach (var obj in profile.Objects)
            {
                int mergeIndex = Convert.ToInt32(obj.Key, 16);
                List<ODIndexMergeOffsetStatus> objectOffset = [];
                foreach (var offset in offsets)
                {
                    objectOffset.Add(new(mergeIndex + offset, false));
                }

                ODIndexMergeStatus ms = new()
                {
                    Insert = true,
                    OriginalObject = $"0x{mergeIndex:X4} - {obj.Value.Name}",
                    Offsets = objectOffset,
                    OriginalIndex = mergeIndex,
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
                    _object = obj.Value,
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
                    TextBrush = new SolidColorBrush(Colors.Black),
                };

                ms.PropertyChanged += MergeStatus_PropertyChanged;

                MergeStatus.Add(ms);
            }
            UpdateMergeStatus(offsets);
        }
    }

    private void MergeStatus_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ODIndexMergeStatus.Insert) && sender is ODIndexMergeStatus status)
        {
            int index = status.OriginalIndex;
            int twinIndex = -1;
            
            if (index >= 0x1800 && index <= 0x18FF) twinIndex = index + 0x0200;
            else if (index >= 0x1A00 && index <= 0x1AFF) twinIndex = index - 0x0200;
            else if (index >= 0x1400 && index <= 0x14FF) twinIndex = index + 0x0200;
            else if (index >= 0x1600 && index <= 0x16FF) twinIndex = index - 0x0200;

            if (twinIndex != -1)
            {
                var twin = MergeStatus.FirstOrDefault(x => x.OriginalIndex == twinIndex);
                if (twin != null && twin.Insert != status.Insert)
                {
                    twin.Insert = status.Insert;
                }
            }
        }
    }

    /// <summary>
    /// Update profile merge status by checking for collisions
    /// </summary>
    /// <param name="offsets">list of offsets in profile import</param>
    public void UpdateMergeStatus(List<int> offsets)
    {
        if (SelectedDevice is not null && MergeStatus.Count != 0)
        {
            foreach (var obj in MergeStatus)
            {
                //first calculate all the offsets
                //remember that the number of offsets could have changed
                List<ODIndexMergeOffsetStatus> objectOffset = [];
                foreach (var offset in offsets)
                {
                    int mergeIndex = obj.OriginalIndex + offset;
                    objectOffset.Add(new(mergeIndex, false));
                }
                obj.Offsets = objectOffset;
            }

            // check for collision with selected device objects
            foreach (var obj in MergeStatus)
            {
                foreach (var offsetStatus in obj.Offsets)
                {
                    foreach (var ob in SelectedDevice.Objects)
                    {
                        if (offsetStatus.Index == ob.Key.ToInteger())
                        {
                            offsetStatus.Collision = true;
                            offsetStatus.Index *= -1;
                        }
                    }
                }
            }

            // check for collision with other offsets objects, collum by collum
            var numberOfOffsets = MergeStatus[0].Offsets.Count;

            // Check each collum from left to right.
            // you only check for collision with collums to the left
            for (int i = 0; i < numberOfOffsets; i++)
            {
                foreach (var leftRow in MergeStatus)
                {
                    int rightCollumIndex = leftRow.Offsets[i].Index;
                    for (int j = i; j >= 0; j--)
                    {
                        if (j != i)
                        {
                            foreach (var rightRow in MergeStatus)
                            {
                                int leftCollumIndex = rightRow.Offsets[j].Index;
                                if (rightCollumIndex == leftCollumIndex)
                                {
                                    leftRow.Offsets[i].Collision = true;
                                }
                            }
                        }
                    }
                }
            }

            // Update IndexCollision status for the entire row
            foreach (var row in MergeStatus)
            {
                row.IndexCollision = row.Offsets.Exists(o => o.Collision);
            }
        }
    }
#pragma warning disable CA1822 // Mark members as static
    public string Greeting => "Welcome to Avalonia!";
#pragma warning restore CA1822 // Mark members as static
    public ObservableCollection<Device> Network { get; set; } = [];

    //Used for profile import
    public ObservableCollection<ODIndexMergeStatus> MergeStatus { get; set; } = [];

    [ObservableProperty]
    public int _insertObjectsOffset;

    private Device? _selectedDevice;
    public Device? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                OnPropertyChanged(nameof(CanRemoveDevice));
                DisplayedDevice = value;
            }
        }
    }

    [ObservableProperty]
    private Device? _displayedDevice;
}
