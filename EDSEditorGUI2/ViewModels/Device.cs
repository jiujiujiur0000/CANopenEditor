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
            }
        }

        [ObservableProperty]
        private DevicePDOViewModel _txPdo;

        [ObservableProperty]
        private DevicePDOViewModel _rxPdo;

        [ObservableProperty]
        private FileInfo _fileInfo = new();

        [ObservableProperty]
        private DeviceInfo _deviceInfo = new();

        [ObservableProperty]
        private DeviceCommissioning _deviceCommissioning = new();

        [ObservableProperty]
        private ObjectDictionary _objects = new();

        [ObservableProperty]
        private ModuleViewModel _moduleInfo = new();

        public void OnClickCommand()
        {
            // do something
        }
    }
}

