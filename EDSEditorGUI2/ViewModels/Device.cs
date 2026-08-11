using CommunityToolkit.Mvvm.ComponentModel;
using libEDSsharp;
using System.Linq;

namespace EDSEditorGUI2.ViewModels
{
    public partial class Device : ObservableObject
    {
        public Device()
        {
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
                    
                    DeviceInfo.RpdoCount = _eds.di.NrOfRXPDO;
                    DeviceInfo.TpdoCount = _eds.di.NrOfTXPDO;
                    DeviceInfo.NodeGuardingSlave = _eds.di.NG_Slave;
                    DeviceInfo.NodeGuardingMaster = _eds.di.NG_Master;
                    DeviceInfo.NumberOfMonitoredNodes = _eds.di.NrOfNG_MonitoredNodes;
                    
                    DeviceCommissioning.NetNumber = _eds.dc.NetNumber;
                    DeviceCommissioning.NetName = _eds.dc.NetworkName;
                    DeviceCommissioning.CanopenManager = _eds.dc.CANopenManager;
                    DeviceCommissioning.LssSerialNo = _eds.dc.LSS_SerialNumber;
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

        private void SyncAvailableObjects()
        {
            if (_eds == null) return;
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

            TxPdo?.RefreshSlots();
            RxPdo?.RefreshSlots();
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
            updatedEds.di.NrOfRXPDO = (ushort)this.DeviceInfo.RpdoCount;
            updatedEds.di.NrOfTXPDO = (ushort)this.DeviceInfo.TpdoCount;
            updatedEds.di.NG_Slave = this.DeviceInfo.NodeGuardingSlave;
            updatedEds.di.NG_Master = this.DeviceInfo.NodeGuardingMaster;
            updatedEds.di.NrOfNG_MonitoredNodes = (ushort)this.DeviceInfo.NumberOfMonitoredNodes;

            if (updatedEds.dc == null) updatedEds.dc = new libEDSsharp.DeviceCommissioning();
            updatedEds.dc.NodeID = (byte)this.DeviceCommissioning.NodeId;
            updatedEds.dc.NodeName = this.DeviceCommissioning.NodeName;
            updatedEds.dc.Baudrate = (ushort)this.DeviceCommissioning.Baudrate;
            updatedEds.dc.NetNumber = this.DeviceCommissioning.NetNumber;
            updatedEds.dc.NetworkName = this.DeviceCommissioning.NetName;
            updatedEds.dc.CANopenManager = this.DeviceCommissioning.CanopenManager;
            updatedEds.dc.LSS_SerialNumber = this.DeviceCommissioning.LssSerialNo;

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

