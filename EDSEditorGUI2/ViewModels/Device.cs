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
            Attach1018Listeners();
        }

        private bool _isHandlingMutex = false;
        private bool _isSyncingIdentity = false;

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
            if (_isHandlingMutex || _isSyncingIdentity) return;

            if (e.PropertyName == nameof(DeviceInfo.LssSlave))
            {
                if (DeviceInfo.LssSlave && DeviceCommissioning.NodeId.HasValue)
                {
                    CheckMutexAndPromptAsync("str_mutex_lss_nodeid",
                        onConfirm: () => { DeviceCommissioning.NodeId = null; },
                        onCancel: () => { DeviceInfo.LssSlave = false; }
                    );
                }
                if (DeviceInfo.LssSlave && !DeviceCommissioning.LssSerialNo.HasValue)
                {
                    SyncFrom1018ToDevice();
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
            else if (e.PropertyName == nameof(DeviceInfo.VendorNumber) ||
                     e.PropertyName == nameof(DeviceInfo.ProductNumber) ||
                     e.PropertyName == nameof(DeviceInfo.RevisionNumber))
            {
                SyncFromDeviceTo1018();
            }
        }

        private void DeviceCommissioning_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            CheckAndRequestAutoSave();
            if (_isHandlingMutex || _isSyncingIdentity) return;

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
            else if (e.PropertyName == nameof(DeviceCommissioning.LssSerialNo))
            {
                SyncFromDeviceTo1018();
            }
        }

        private void Objects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CheckAndRequestAutoSave();
            
            if (_isHandlingMutex) return;

            if (DeviceInfo.NodeGuardingSlave && (Objects.ContainsKey("1017") || Objects.ContainsKey("0x1017")))
            {
                CheckMutexAndPromptAsync("str_mutex_1017_ngs",
                    onConfirm: () => { DeviceInfo.NodeGuardingSlave = false; },
                    onCancel: () => { 
                        Objects.Remove("1017"); 
                        Objects.Remove("0x1017");
                    }
                );
            }

            if (TryGet1018Object(out _))
            {
                Attach1018Listeners();
                SyncFrom1018ToDevice();
            }
        }

        public void Attach1018Listeners()
        {
            if (Objects == null) return;

            if (TryGet1018Object(out var obj1018))
            {
                obj1018.SubObjects.CollectionChanged -= SubObjects1018_CollectionChanged;
                obj1018.SubObjects.CollectionChanged += SubObjects1018_CollectionChanged;

                foreach (var kvp in obj1018.SubObjects)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.PropertyChanged -= SubObject1018_PropertyChanged;
                        kvp.Value.PropertyChanged += SubObject1018_PropertyChanged;
                    }
                }
            }
        }

        public bool TryGet1018Object(out OdObject obj1018)
        {
            obj1018 = null!;
            if (Objects == null) return false;
            if (Objects.TryGetValue("1018", out obj1018!)) return true;
            if (Objects.TryGetValue("0x1018", out obj1018!)) return true;
            if (Objects.TryGetValue("1018h", out obj1018!)) return true;
            var kvp = Objects.FirstOrDefault(x => x.Key.TrimStart('0', 'x', 'X').Equals("1018", System.StringComparison.OrdinalIgnoreCase));
            if (kvp.Value != null)
            {
                obj1018 = kvp.Value;
                return true;
            }
            return false;
        }

        private void SubObjects1018_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (KeyValuePair<string, OdSubObject> kvp in e.NewItems)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.PropertyChanged -= SubObject1018_PropertyChanged;
                        kvp.Value.PropertyChanged += SubObject1018_PropertyChanged;
                    }
                }
            }
            SyncFrom1018ToDevice();
        }

        private void SubObject1018_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isSyncingIdentity) return;
            if (e.PropertyName == nameof(OdSubObject.DefaultValue))
            {
                SyncFrom1018ToDevice();
            }
        }

        public void SyncFrom1018ToDevice()
        {
            if (_isSyncingIdentity) return;
            if (!TryGet1018Object(out var obj1018)) return;

            _isSyncingIdentity = true;
            try
            {
                foreach (var sub in obj1018.SubObjects)
                {
                    if (sub.Value == null) continue;
                    string rawKey = sub.Key.Trim().TrimStart('0', 'x', 'X');
                    if (string.IsNullOrEmpty(rawKey)) rawKey = "0";

                    if (rawKey == "1") // Vendor-ID
                    {
                        if (DeviceInfo.VendorNumber != sub.Value.DefaultValue)
                        {
                            DeviceInfo.VendorNumber = sub.Value.DefaultValue;
                        }
                    }
                    else if (rawKey == "2") // Product Code
                    {
                        if (DeviceInfo.ProductNumber != sub.Value.DefaultValue)
                        {
                            DeviceInfo.ProductNumber = sub.Value.DefaultValue;
                        }
                    }
                    else if (rawKey == "3") // Revision Number
                    {
                        if (DeviceInfo.RevisionNumber != sub.Value.DefaultValue)
                        {
                            DeviceInfo.RevisionNumber = sub.Value.DefaultValue;
                        }
                    }
                    else if (rawKey == "4") // Serial Number
                    {
                        if (TryParseUInt32(sub.Value.DefaultValue, out uint serialVal))
                        {
                            if (DeviceCommissioning.LssSerialNo != serialVal)
                            {
                                DeviceCommissioning.LssSerialNo = serialVal;
                            }
                        }
                    }
                }
            }
            finally
            {
                _isSyncingIdentity = false;
            }
        }

        public void SyncFromDeviceTo1018()
        {
            if (_isSyncingIdentity) return;
            if (!TryGet1018Object(out var obj1018)) return;

            _isSyncingIdentity = true;
            try
            {
                foreach (var sub in obj1018.SubObjects)
                {
                    if (sub.Value == null) continue;
                    string rawKey = sub.Key.Trim().TrimStart('0', 'x', 'X');
                    if (string.IsNullOrEmpty(rawKey)) rawKey = "0";

                    if (rawKey == "1")
                    {
                        if (sub.Value.DefaultValue != DeviceInfo.VendorNumber)
                        {
                            sub.Value.DefaultValue = DeviceInfo.VendorNumber;
                        }
                    }
                    else if (rawKey == "2")
                    {
                        if (sub.Value.DefaultValue != DeviceInfo.ProductNumber)
                        {
                            sub.Value.DefaultValue = DeviceInfo.ProductNumber;
                        }
                    }
                    else if (rawKey == "3")
                    {
                        if (sub.Value.DefaultValue != DeviceInfo.RevisionNumber)
                        {
                            sub.Value.DefaultValue = DeviceInfo.RevisionNumber;
                        }
                    }
                    else if (rawKey == "4")
                    {
                        if (DeviceCommissioning.LssSerialNo.HasValue)
                        {
                            string formatted = $"0x{DeviceCommissioning.LssSerialNo.Value:X8}";
                            if (sub.Value.DefaultValue != formatted)
                            {
                                sub.Value.DefaultValue = formatted;
                            }
                        }
                    }
                }
            }
            finally
            {
                _isSyncingIdentity = false;
            }
        }

        private static bool TryParseUInt32(string? input, out uint result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.Trim();
            if (input.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
            {
                return uint.TryParse(input.Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out result);
            }
            return uint.TryParse(input, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out result);
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
                    Attach1018Listeners();
                    SyncFrom1018ToDevice();
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

