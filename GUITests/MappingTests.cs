using EDSEditorGUI2.Mapper;
using LibCanOpen;

namespace GUITests
{
    public class MappingTests
    {
        [Fact]
        public void MappingFromProtobuffer()
        {
            // testing for exception in the mapping assert.
            var sut = new CanOpenDevice();
            ProtobufferViewModelMapper.MapFromProtobuffer(sut);
        }

        [Fact]
        public void Test1018MappingAndSync()
        {
            string xpdPath = @"C:\Users\14588\workspace\CANopenEditor\EDSEditorGUI\Profiles\DS301_profile.xpd";
            libEDSsharp.CanOpenXDD_1_1 coxml_1_1 = new libEDSsharp.CanOpenXDD_1_1();
            var eds = coxml_1_1.ReadXML(xpdPath);
            var proto = libEDSsharp.MappingEDS.MapToProtobuffer(eds);
            var vm = ProtobufferViewModelMapper.MapFromProtobuffer(proto);

            var dc = new EDSEditorGUI2.ViewModels.MainWindowViewModel();
            dc.AddNewDevice();
            var dev = dc.SelectedDevice!;
            Assert.NotNull(dev);

            // Merge vm objects into dev
            foreach (var kvp in vm.Objects)
            {
                dev.Objects[kvp.Key] = kvp.Value;
            }

            Assert.Equal("0x00000000", dev.DeviceInfo.VendorNumber);
            Assert.Equal("0x00000000", dev.DeviceInfo.ProductNumber);
            Assert.Equal("0x00000000", dev.DeviceInfo.RevisionNumber);
            Assert.Equal("0x00000000", dev.DeviceCommissioning.LssSerialNo);
        }

        [Fact]
        public void Test1018SmartMergePreservesCustomInput()
        {
            string xpdPath = @"C:\Users\14588\workspace\CANopenEditor\EDSEditorGUI\Profiles\DS301_profile.xpd";
            libEDSsharp.CanOpenXDD_1_1 coxml_1_1 = new libEDSsharp.CanOpenXDD_1_1();
            var eds = coxml_1_1.ReadXML(xpdPath);
            var proto = libEDSsharp.MappingEDS.MapToProtobuffer(eds);
            var vm = ProtobufferViewModelMapper.MapFromProtobuffer(proto);

            var dc = new EDSEditorGUI2.ViewModels.MainWindowViewModel();
            dc.AddNewDevice();
            var dev = dc.SelectedDevice!;
            Assert.NotNull(dev);

            // User pre-fills custom values before inserting 301 template
            dev.DeviceInfo.VendorNumber = "0x0000005A";
            dev.DeviceCommissioning.LssSerialNo = "0x12345678";

            // Merge vm objects into dev
            foreach (var kvp in vm.Objects)
            {
                dev.Objects[kvp.Key] = kvp.Value;
            }

            // Verify user inputs are preserved in DeviceInfo/Commissioning
            Assert.Equal("0x0000005A", dev.DeviceInfo.VendorNumber);
            Assert.Equal("0x00000000", dev.DeviceInfo.ProductNumber); // was empty, so filled from template
            Assert.Equal("0x00000000", dev.DeviceInfo.RevisionNumber); // was default 1, so filled from template
            Assert.Equal("0x12345678", dev.DeviceCommissioning.LssSerialNo);

            // Verify 0x1018 object in dictionary is also updated with user inputs
            var obj1018 = dev.Objects["1018"];
            Assert.Equal("0x0000005A", obj1018.SubObjects.First(x => x.Key == "01" || x.Key == "1").Value.DefaultValue);
            Assert.Equal("0x12345678", obj1018.SubObjects.First(x => x.Key == "04" || x.Key == "4").Value.DefaultValue);
        }
    }
}
