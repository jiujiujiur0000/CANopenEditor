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
    }
}
