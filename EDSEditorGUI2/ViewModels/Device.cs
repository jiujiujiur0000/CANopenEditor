using CommunityToolkit.Mvvm.ComponentModel;
using libEDSsharp;
using System.Linq;
using Avalonia.Threading;
using System.ComponentModel;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.Specialized;
using System.Collections.Generic;
using Avalonia.Controls;

namespace EDSEditorGUI2.ViewModels
{
    public partial class Device : ObservableObject
    {
        public Device()
        {
            SetupListeners();
        }

        private void SetupListeners()
        {
            if (DeviceInfo != null)
                DeviceInfo.PropertyChanged += DeviceInfo_PropertyChanged;
            if (DeviceCommissioning != null)
                DeviceCommissioning.PropertyChanged += DeviceCommissioning_PropertyChanged;
            if (Objects != null)
                Objects.CollectionChanged += Objects_CollectionChanged;
        }

        partial void OnProjectInfoChanged(ProjectInfo? oldValue, ProjectInfo newValue)
        {
            if (oldValue != null) oldValue.PropertyChanged -= SubObject_PropertyChanged;
            if (newValue != null) newValue.PropertyChanged += SubObject_PropertyChanged;
        }

        partial void OnFileInfoChanged(FileInfo? oldValue, FileInfo newValue)
        {
            if (oldValue != null) oldValue.PropertyChanged -= SubObject_PropertyChanged;
            if (newValue != null) newValue.PropertyChanged += SubObject_PropertyChanged;
        }

        private void SubObject_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            CheckAndRequestAutoSave();
        }

        partial void OnDeviceInfoChanged(DeviceInfo? oldValue, DeviceInfo newValue)
        {
            if (oldValue != null) oldValue.PropertyChanged -= DeviceInfo_PropertyChanged;
            if (newValue != null) newValue.PropertyChanged += DeviceInfo_PropertyChanged;
        }

        partial void OnDeviceCommissioningChanged(DeviceCommissioning? oldValue, DeviceCommissioning newValue)
        {
            if (oldValue != null) oldValue.PropertyChanged -= DeviceCommissioning_PropertyChanged;
            if (newValue != null) newValue.PropertyChanged += DeviceCommissioning_PropertyChanged;
        }

        partial void OnObjectsChanged(ObjectDictionary? oldValue, ObjectDictionary newValue)
        {
            if (oldValue != null) oldValue.CollectionChanged -= Objects_CollectionChanged;
            if (newValue != null) newValue.CollectionChanged += Objects_CollectionChanged;
        }

        private bool _isHandlingMutex = false;

        public event System.EventHandler? AutoSaveRequested;

        private void CheckAndRequestAutoSave()
        {
            this.IsDirty = true;
            if (!DeviceInfo.HasErrors && !DeviceCommissioning.HasErrors)
            {
                AutoSaveRequested?.Invoke(this, System.EventArgs.Empty);
            }
        }

        private void DeviceInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            CheckAndRequestAutoSave();
            if (_isHandlingMutex) return;

            if (e.PropertyName == nameof(DeviceInfo.LssSlave))
            {
                if (DeviceInfo.LssSlave && DeviceCommissioning.NodeId.HasValue)
                {
                    CheckMutexAndPromptAsync("str_mutex_lss_nodeid",
                        onConfirm: () => { DeviceCommissioning.NodeId = null; },
                        onCancel: () => { DeviceInfo.LssSlave = false; }
                    );
                }
            }
            else if (e.PropertyName == nameof(DeviceInfo.NodeGuardingSlave))
            {
                if (DeviceInfo.NodeGuardingSlave && Objects.ContainsKey("1017"))
                {
                    CheckMutexAndPromptAsync("str_mutex_ngs_1017",
                        onConfirm: () => { Objects.Remove("1017"); },
                        onCancel: () => { DeviceInfo.NodeGuardingSlave = false; }
                    );
                }
            }
        }

        private void DeviceCommissioning_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            CheckAndRequestAutoSave();
            if (_isHandlingMutex) return;

            if (e.PropertyName == nameof(DeviceCommissioning.NodeId))
            {
                if (DeviceCommissioning.NodeId.HasValue && DeviceInfo.LssSlave)
                {
                    CheckMutexAndPromptAsync("str_mutex_nodeid_lss",
                        onConfirm: () => { DeviceInfo.LssSlave = false; },
                        onCancel: () => { DeviceCommissioning.NodeId = null; }
                    );
                }
            }
        }

        private void Objects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CheckAndRequestAutoSave();
            
            if (_isHandlingMutex) return;

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (KeyValuePair<string, OdObject> kvp in e.NewItems)
                {
                    if (kvp.Key == "1017" && DeviceInfo.NodeGuardingSlave)
                    {
                        CheckMutexAndPromptAsync("str_mutex_1017_ngs",
                            onConfirm: () => { DeviceInfo.NodeGuardingSlave = false; },
                            onCancel: () => { Objects.Remove("1017"); }
                        );
                        break;
                    }
                }
            }
        }

        private void CheckMutexAndPromptAsync(string resourceKey, System.Action onConfirm, System.Action onCancel)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                _isHandlingMutex = true;
                try
                {
                    var app = Avalonia.Application.Current;
                    var mainWindow = (app?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                    if (mainWindow != null && mainWindow.FindResource("MutexConfirmDialog") is Avalonia.Controls.Control dialog)
                    {
                        var msg = app?.FindResource(resourceKey)?.ToString();
                        dialog.DataContext = msg;
                        var result = await DialogHostAvalonia.DialogHost.Show(dialog, "RootDialogHost");
                        if (result as string == "OK")
                        {
                            onConfirm?.Invoke();
                        }
                        else
                        {
                            onCancel?.Invoke();
                        }
                    }
                    else
                    {
                        onCancel?.Invoke();
                    }
                }
                finally
                {
                    _isHandlingMutex = false;
                }
            });
        }

        [ObservableProperty]
        private bool _isDirty;

        public override string ToString()
        {
            if (DeviceInfo == null)
            {
                return "unnamed device";
            }
            else
            {
                return DeviceInfo.ProductName;
            }
        }

        private libEDSsharp.EDSsharp _eds = null!;
        public libEDSsharp.EDSsharp Eds 
        { 
            get => _eds;
            set 
            {
                _eds = value;
                TxPdo = new DevicePDOViewModel(_eds, true);
                RxPdo = new DevicePDOViewModel(_eds, false);
                
                if (_eds != null)
                {
                    ProjectInfo.ProjectFile = System.IO.Path.GetFileName(_eds.projectFilename);
                    if (!string.IsNullOrEmpty(_eds.xddfilename_1_1))
                        ProjectInfo.ProjectFileVersion = "v1.1";
                    else if (!string.IsNullOrEmpty(_eds.xddfilename_1_0) && _eds.xddfilename_1_0 == _eds.projectFilename)
                        ProjectInfo.ProjectFileVersion = "v1.0";
                    else
                        ProjectInfo.ProjectFileVersion = "";

                    ProjectInfo.XddFileStripped = System.IO.Path.GetFileName(_eds.xddfilenameStripped);
                    ProjectInfo.EdsFile = System.IO.Path.GetFileName(_eds.edsfilename);
                    ProjectInfo.DcfFile = System.IO.Path.GetFileName(_eds.dcffilename);
                    ProjectInfo.CanopenNodeFile = System.IO.Path.GetFileName(_eds.ODfilename);
                    ProjectInfo.CanopenNodeFileVersion = _eds.ODfileVersion;
                    ProjectInfo.DocumentationFile = System.IO.Path.GetFileName(_eds.mdfilename);
                    
                    DeviceInfo.RpdoCount = _eds.di.NrOfRXPDO.ToString();
                    DeviceInfo.TpdoCount = _eds.di.NrOfTXPDO.ToString();
                    DeviceInfo.NodeGuardingSlave = _eds.di.NG_Slave;
                    DeviceInfo.NodeGuardingMaster = _eds.di.NG_Master;
                    DeviceInfo.NumberOfMonitoredNodes = _eds.di.NrOfNG_MonitoredNodes.ToString();
                    
                    DeviceCommissioning.NetNumber = _eds.dc.NetNumber == 0 ? null : _eds.dc.NetNumber;
                    DeviceCommissioning.NetName = _eds.dc.NetworkName;
                    DeviceCommissioning.CanopenManager = _eds.dc.CANopenManager;
                    DeviceCommissioning.LssSerialNo = _eds.dc.LSS_SerialNumber == 0 ? null : _eds.dc.LSS_SerialNumber;
                }
            }
        }

        [ObservableProperty]
        private DevicePDOViewModel _txPdo = null!;

        [ObservableProperty]
        private DevicePDOViewModel _rxPdo = null!;

        [ObservableProperty]
        private FileInfo _fileInfo = new();

        [ObservableProperty]
        private ProjectInfo _projectInfo = new();

        [ObservableProperty]
        private DeviceInfo _deviceInfo = new();

        [ObservableProperty]
        private DeviceCommissioning _deviceCommissioning = new();

        [ObservableProperty]
        private ObjectDictionary _objects = new();

        [ObservableProperty]
        private bool _isSyncing;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsObjectDictionaryTabSelected))]
        private int _selectedTabIndex = 0;

        partial void OnSelectedTabIndexChanged(int value)
        {
            if (value == 2 || value == 3)
            {
                SyncAvailableObjects();
            }
            else if (value == 1)
            {
                SyncODFromEDS();
            }
        }

        private void SyncODFromEDS()
        {
            if (_eds == null) return;
            IsSyncing = true;
            try
            {
                var proto = libEDSsharp.MappingEDS.MapToProtobuffer(_eds);
                var deviceView = Mapper.ProtobufferViewModelMapper.MapFromProtobuffer(proto);
                
                foreach (var kvp in deviceView.Objects)
                {
                    if (ushort.TryParse(kvp.Key.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ushort index))
                    {
                        if (index >= 0x1400 && index <= 0x1BFF)
                        {
                            if (_objects.ContainsKey(kvp.Key))
                            {
                                foreach (var sub in kvp.Value.SubObjects)
                                {
                                    var matchingSub = _objects[kvp.Key].SubObjects.FirstOrDefault(s => s.Key == sub.Key);
                                    if (matchingSub.Value != null)
                                    {
                                        matchingSub.Value.DefaultValue = sub.Value.DefaultValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private void SyncAvailableObjects()
        {
            if (_eds == null) return;
            IsSyncing = true;
            try
            {
                var updatedEds = GetUpdatedEds();
                
                var keysToRemove = new System.Collections.Generic.List<ushort>();
                foreach(var k in _eds.ods.Keys)
                {
                    if ((k < 0x1400 || k > 0x1BFF) && !updatedEds.ods.ContainsKey(k))
                    {
                        keysToRemove.Add(k);
                    }
                }
                foreach (var k in keysToRemove)
                {
                    _eds.ods.Remove(k);
                }

                foreach (var kvp in updatedEds.ods)
                {
                    if (kvp.Key >= 0x1400 && kvp.Key <= 0x1BFF)
                    {
                        if (!_eds.ods.ContainsKey(kvp.Key))
                        {
                            _eds.ods.Add(kvp.Key, kvp.Value);
                        }
                    }
                    else
                    {
                        _eds.ods[kvp.Key] = kvp.Value;
                    }
                }

                _eds.dc = updatedEds.dc;

                TxPdo?.RefreshSlots();
                RxPdo?.RefreshSlots();
            }
            finally
            {
                IsSyncing = false;
            }

            TxPdo?.UpdateAvailableObjects();
            RxPdo?.UpdateAvailableObjects();
        }

        public bool IsObjectDictionaryTabSelected => SelectedTabIndex == 1;

        [ObservableProperty]
        private ModuleViewModel _moduleInfo = new();

        public libEDSsharp.EDSsharp GetUpdatedEds()
        {
            var proto = Mapper.ProtobufferViewModelMapper.MapToProtobuffer(this);
            var updatedEds = libEDSsharp.MappingEDS.MapFromProtobuffer(proto);
            
            updatedEds.projectFilename = this.ProjectInfo.ProjectFile;
            updatedEds.xddfilenameStripped = this.ProjectInfo.XddFileStripped;
            updatedEds.edsfilename = this.ProjectInfo.EdsFile;
            updatedEds.dcffilename = this.ProjectInfo.DcfFile;
            updatedEds.ODfilename = this.ProjectInfo.CanopenNodeFile;
            updatedEds.ODfileVersion = this.ProjectInfo.CanopenNodeFileVersion;
            updatedEds.mdfilename = this.ProjectInfo.DocumentationFile;

            if (updatedEds.di == null) updatedEds.di = new libEDSsharp.DeviceInfo();
            updatedEds.di.NrOfRXPDO = ushort.TryParse(this.DeviceInfo.RpdoCount, out var r) ? r : (ushort)0;
            updatedEds.di.NrOfTXPDO = ushort.TryParse(this.DeviceInfo.TpdoCount, out var t) ? t : (ushort)0;
            updatedEds.di.NG_Slave = this.DeviceInfo.NodeGuardingSlave;
            updatedEds.di.NG_Master = this.DeviceInfo.NodeGuardingMaster;
            updatedEds.di.NrOfNG_MonitoredNodes = ushort.TryParse(this.DeviceInfo.NumberOfMonitoredNodes, out var n) ? n : (ushort)0;

            if (updatedEds.dc == null) updatedEds.dc = new libEDSsharp.DeviceCommissioning();
            updatedEds.dc.NodeID = (byte)(this.DeviceCommissioning.NodeId ?? 0);
            updatedEds.dc.NodeName = this.DeviceCommissioning.NodeName;
            updatedEds.dc.Baudrate = (ushort)(this.DeviceCommissioning.Baudrate ?? 0);
            updatedEds.dc.NetNumber = this.DeviceCommissioning.NetNumber ?? 0;
            updatedEds.dc.NetworkName = this.DeviceCommissioning.NetName;
            updatedEds.dc.CANopenManager = this.DeviceCommissioning.CanopenManager;
            updatedEds.dc.LSS_SerialNumber = this.DeviceCommissioning.LssSerialNo ?? 0;

            if (_eds != null && _eds.ods != null)
            {
                foreach (var kvp in _eds.ods)
                {
                    if (kvp.Key >= 0x1400 && kvp.Key <= 0x1BFF)
                    {
                        updatedEds.ods[kvp.Key] = kvp.Value;
                    }
                }
            }

            return updatedEds;
        }

        public void OnClickCommand()
        {
            // do something
        }
    }
}

