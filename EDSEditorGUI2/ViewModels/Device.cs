using CommunityToolkit.Mvvm.ComponentModel;
using libEDSsharp;

namespace EDSEditorGUI2.ViewModels
{
    public partial class Device : ObservableObject
    {
        public Device()
        {
        }

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

        private libEDSsharp.EDSsharp _eds;
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
        private DevicePDOViewModel _txPdo;

        [ObservableProperty]
        private DevicePDOViewModel _rxPdo;

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
            updatedEds.dc.NetNumber = this.DeviceCommissioning.NetNumber;
            updatedEds.dc.NetworkName = this.DeviceCommissioning.NetName;
            updatedEds.dc.CANopenManager = this.DeviceCommissioning.CanopenManager;
            updatedEds.dc.LSS_SerialNumber = this.DeviceCommissioning.LssSerialNo;

            return updatedEds;
        }

        public void OnClickCommand()
        {
            // do something
        }
    }
}

