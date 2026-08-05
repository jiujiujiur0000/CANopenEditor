using System;
using libEDSsharp;
using EDSEditorGUI2.Mapper;

class Program
{
    static void Main()
    {
        try {
            var coxml = new CanOpenXDD_1_1();
            var eds = coxml.ReadXML(@"c:\Users\14588\workspace\CANopenEditor\project.xdd");
            var proto = MappingEDS.MapToProtobuffer(eds);
            var vm = ProtobufferViewModelMapper.MapFromProtobuffer(proto);
            Console.WriteLine("Success: " + vm.Objects.Count + " objects mapped.");
        } catch (Exception ex) {
            Console.WriteLine(ex.ToString());
        }
    }
}
