import sys

filename = r"c:\Users\14588\workspace\CANopenEditor\libEDSsharp\CanOpenEDSMapping.cs"
with open(filename, "r", encoding="utf-8") as f:
    content = f.read()

# We want to replace the body of MapFromProtobuffer and MapToProtobuffer
# and add a static constructor.

# First, extract the config of MapFromProtobuffer
# The config starts at "var config = new MapperConfiguration(cfg =>" in MapFromProtobuffer
# And ends at "}, LoggerFactory.Create(builder => { builder.AddDebug(); }));"

new_content = content

header_injection = """
        private static readonly IMapper _fromProtoMapper;
        private static readonly IMapper _toProtoMapper;

        static MappingEDS()
        {
            var fromProtoConfig = new MapperConfiguration(cfg =>
            {
                // workaround for https://github.com/AutoMapper/AutoMapper/issues/2959
                // Cant update untill after .net framwork is gone
                cfg.ShouldMapMethod = (m => m.Name == "ARandomStringThatDoesNotMatchAnyFunctionName");
                cfg.CreateMap<string, UInt16>().ConvertUsing<ODStringToShortTypeResolver>();
                cfg.CreateMap<CanOpenDevice, EDSsharp>()
                .ForMember(dest => dest.Dirty, opt => opt.Ignore())
                .ForMember(dest => dest.xddfilename_1_1, opt => opt.Ignore())
                .ForMember(dest => dest.xddfilenameStripped, opt => opt.Ignore())
                .ForMember(dest => dest.edsfilename, opt => opt.Ignore())
                .ForMember(dest => dest.dcffilename, opt => opt.Ignore())
                .ForMember(dest => dest.ODfilename, opt => opt.Ignore())
                .ForMember(dest => dest.ODfileVersion, opt => opt.Ignore())
                .ForMember(dest => dest.mdfilename, opt => opt.Ignore())
                .ForMember(dest => dest.xmlfilename, opt => opt.Ignore())
                .ForMember(dest => dest.xddfilename_1_0, opt => opt.Ignore())
                .ForMember(dest => dest.xddTemplate, opt => opt.Ignore())
                .ForMember(dest => dest.dummy_ods, opt => opt.Ignore())
                .ForMember(dest => dest.CO_storageGroups, opt => opt.Ignore())
                .ForMember(dest => dest.md, opt => opt.Ignore())
                .ForMember(dest => dest.oo, opt => opt.Ignore())
                .ForMember(dest => dest.mo, opt => opt.Ignore())
                .ForMember(dest => dest.c, opt => opt.Ignore())
                .ForMember(dest => dest.du, opt => opt.Ignore())
                .ForMember(dest => dest.td, opt => opt.Ignore())
                .ForMember(dest => dest.sm, opt => opt.Ignore())
                .ForMember(dest => dest.cm, opt => opt.Ignore())
                .ForMember(dest => dest.modules, opt => opt.Ignore())
                .ForMember(dest => dest.NodeID, opt => opt.Ignore())
                .ForMember(dest => dest.projectFilename, opt => opt.MapFrom(src => src.DeviceInfo.ProductName))
                .ForMember(dest => dest.NodeID, opt => opt.MapFrom(src => src.DeviceCommissioning.NodeId))
                .ForMember(dest => dest.fi, opt => opt.MapFrom(src => src.FileInfo))
                .ForMember(dest => dest.di, opt => opt.MapFrom(src => src.DeviceInfo))
                .ForMember(dest => dest.dc, opt => opt.MapFrom(src => src.DeviceCommissioning))
                .ForMember(dest => dest.ods, opt => opt.MapFrom(src => src.Objects));
                cfg.CreateMap<CanOpen_FileInfo, FileInfo>()
                .ForMember(dest => dest.FileName, opt => opt.Ignore())
                .ForMember(dest => dest.LastEDS, opt => opt.Ignore())
                .ForMember(dest => dest.EDSVersionMajor, opt => opt.Ignore())
                .ForMember(dest => dest.EDSVersionMinor, opt => opt.Ignore())
                .ForMember(dest => dest.EDSVersion, opt => opt.Ignore())
                .ForMember(dest => dest.exportFolder, opt => opt.Ignore())
                .ForMember(dest => dest.FileRevision, opt => opt.MapFrom(src => (byte)src.FileVersion.ElementAtOrDefault(0)))
                .ForMember(dest => dest.CreationDateTime, opt => opt.MapFrom(src => src.CreationTime.ToDateTime()))
                .ForMember(dest => dest.CreationDate, opt => opt.MapFrom(src => src.CreationTime.ToDateTime().ToString("MM-dd-yyyy", System.Globalization.CultureInfo.InvariantCulture)))
                .ForMember(dest => dest.CreationTime, opt => opt.MapFrom(src => src.CreationTime.ToDateTime().ToString("h:mmtt", System.Globalization.CultureInfo.InvariantCulture)))
                .ForMember(dest => dest.ModificationDateTime, opt => opt.MapFrom(src => src.ModificationTime.ToDateTime()))
                .ForMember(dest => dest.ModificationDate, opt => opt.MapFrom(src => src.ModificationTime.ToDateTime().ToString("MM-dd-yyyy", System.Globalization.CultureInfo.InvariantCulture)))
                .ForMember(dest => dest.ModificationTime, opt => opt.MapFrom(src => src.ModificationTime.ToDateTime().ToString("h:mmtt", System.Globalization.CultureInfo.InvariantCulture)));
                cfg.CreateMap<CanOpen_DeviceInfo, DeviceInfo>()
                .ForMember(dest => dest.BaudRate_10, opt => opt.MapFrom(src => src.BaudRate10))
                .ForMember(dest => dest.BaudRate_20, opt => opt.MapFrom(src => src.BaudRate20))
                .ForMember(dest => dest.BaudRate_50, opt => opt.MapFrom(src => src.BaudRate50))
                .ForMember(dest => dest.BaudRate_125, opt => opt.MapFrom(src => src.BaudRate125))
                .ForMember(dest => dest.BaudRate_250, opt => opt.MapFrom(src => src.BaudRate250))
                .ForMember(dest => dest.BaudRate_500, opt => opt.MapFrom(src => src.BaudRate500))
                .ForMember(dest => dest.BaudRate_800, opt => opt.MapFrom(src => src.BaudRate800))
                .ForMember(dest => dest.BaudRate_1000, opt => opt.MapFrom(src => src.BaudRate1000))
                .ForMember(dest => dest.BaudRate_auto, opt => opt.MapFrom(src => src.BaudRateAuto))
                .ForMember(dest => dest.VendorNumber, opt => opt.MapFrom(src => src.VendorNumber))
                .ForMember(dest => dest.ProductNumber, opt => opt.MapFrom(src => src.ProductNumber))
                .ForMember(dest => dest.RevisionNumber, opt => opt.MapFrom(src => src.RevisionNumber))
                .ForMember(dest => dest.SimpleBootUpMaster, opt => opt.Ignore())
                .ForMember(dest => dest.SimpleBootUpSlave, opt => opt.Ignore())
                .ForMember(dest => dest.Granularity, opt => opt.MapFrom(src => (byte)src.Granularity))
                .ForMember(dest => dest.DynamicChannelsSupported, opt => opt.Ignore())
                .ForMember(dest => dest.CompactPDO, opt => opt.Ignore())
                .ForMember(dest => dest.GroupMessaging, opt => opt.Ignore())
                .ForMember(dest => dest.NrOfRXPDO, opt => opt.Ignore()) // TODO Calculate this
                .ForMember(dest => dest.NrOfTXPDO, opt => opt.Ignore()) // TODO Calculate this
                .ForMember(dest => dest.LSS_Supported, opt => opt.MapFrom(src => src.LssSlave))
                .ForMember(dest => dest.LSS_Master, opt => opt.MapFrom(src => src.LssMaster))
                .ForMember(dest => dest.NG_Slave, opt => opt.Ignore())
                .ForMember(dest => dest.NG_Master, opt => opt.Ignore())
                .ForMember(dest => dest.NrOfNG_MonitoredNodes, opt => opt.Ignore());
                cfg.CreateMap<CanOpen_DeviceCommissioning, DeviceCommissioning>()
                .ForMember(dest => dest.NetNumber, opt => opt.Ignore())
                .ForMember(dest => dest.NetworkName, opt => opt.Ignore())
                .ForMember(dest => dest.CANopenManager, opt => opt.Ignore())
                .ForMember(dest => dest.LSS_SerialNumber, opt => opt.Ignore());
                cfg.CreateMap<OdObject, CustomProperties>()
                .ForMember(dest => dest.CO_accessSRDO, opt => opt.Ignore())
                .ForMember(dest => dest.CO_stringLengthMin, opt => opt.Ignore())
                .ForMember(dest => dest.CO_disabled, opt => opt.MapFrom(src => src.Disabled))
                .ForMember(dest => dest.CO_countLabel, opt => opt.MapFrom(src => src.CountLabel))
                .ForMember(dest => dest.CO_storageGroup, opt => opt.MapFrom(src => src.StorageGroup))
                .ForMember(dest => dest.CO_flagsPDO, opt => opt.MapFrom(src => src.FlagsPDO));
                cfg.CreateMap<OdObject.Types.ObjectType, ObjectType>().ConvertUsing<ODTypeResolver>();
                cfg.CreateMap<OdSubObject, PDOMappingType>().ConvertUsing<ODPDOTypeConverter>();
                cfg.CreateMap<OdObject, ODentry>()
                .ForMember(dest => dest.Index, opt => opt.Ignore())
                .ForMember(dest => dest.parameter_name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.denotation, opt => opt.MapFrom(src => src.Alias))
                .ForMember(dest => dest.datatype, opt => opt.Ignore())
                .ForMember(dest => dest.accesstype, opt => opt.Ignore())
                .ForMember(dest => dest.defaultvalue, opt => opt.Ignore())
                .ForMember(dest => dest.LowLimit, opt => opt.Ignore())
                .ForMember(dest => dest.HighLimit, opt => opt.Ignore())
                .ForMember(dest => dest.actualvalue, opt => opt.Ignore())
                .ForMember(dest => dest.ObjFlags, opt => opt.Ignore())
                .ForMember(dest => dest.CompactSubObj, opt => opt.Ignore())
                .ForMember(dest => dest.count, opt => opt.Ignore())
                .ForMember(dest => dest.ObjExtend, opt => opt.Ignore())
                .ForMember(dest => dest.PDOtype, opt => opt.Ignore())
                .ForMember(dest => dest.Label, opt => opt.Ignore())
                .ForMember(dest => dest.parent, opt => opt.Ignore())
                .ForMember(dest => dest.prop, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.uniqueID, opt => opt.Ignore())
                .AfterMap((src, dest, ctx) => {
                    if (dest.objecttype == ObjectType.VAR && (src.SubObjects.TryGetValue("0", out var subObj) || src.SubObjects.TryGetValue("00", out subObj))) {
                        dest.datatype = ctx.Mapper.Map<DataType>(subObj.DataType);
                        dest.accesstype = ctx.Mapper.Map<EDSsharp.AccessType>(subObj);
                        dest.defaultvalue = subObj.DefaultValue;
                        dest.actualvalue = subObj.ActualValue;
                        dest.LowLimit = subObj.LowLimit;
                        dest.HighLimit = subObj.HighLimit;
                        dest.prop.CO_accessSRDO = (libEDSsharp.AccessSRDO)subObj.Srdo;
                        dest.prop.CO_stringLengthMin = subObj.StringLengthMin;
                        dest.PDOtype = ctx.Mapper.Map<PDOMappingType>(subObj);
                        dest.subobjects.Clear();
                    }
                });
                cfg.CreateMap<OdSubObject, EDSsharp.AccessType>().ConvertUsing<ODAccessTypeResolver>();

                cfg.CreateMap<Google.Protobuf.Collections.MapField<string, OdObject>, SortedDictionary<ushort, ODentry>>()
                    .ConvertUsing((src, dest, ctx) => {
                        var dict = new SortedDictionary<ushort, ODentry>();
                        foreach (var kvp in src) {
                            if (ushort.TryParse(kvp.Key, System.Globalization.NumberStyles.HexNumber, null, out ushort key)) {
                                dict.Add(key, ctx.Mapper.Map<ODentry>(kvp.Value));
                            }
                        }
                        return dict;
                    });
                
                cfg.CreateMap<Google.Protobuf.Collections.MapField<string, OdSubObject>, SortedDictionary<ushort, ODentry>>()
                    .ConvertUsing((src, dest, ctx) => {
                        var dict = new SortedDictionary<ushort, ODentry>();
                        foreach (var kvp in src) {
                            if (ushort.TryParse(kvp.Key, System.Globalization.NumberStyles.HexNumber, null, out ushort key)) {
                                dict.Add(key, ctx.Mapper.Map<ODentry>(kvp.Value));
                            }
                        }
                        return dict;
                    });

                cfg.CreateMap<OdSubObject, ODentry>()
                .ForMember(dest => dest.parameter_name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Index, opt => opt.Ignore())
                .ForMember(dest => dest.denotation, opt => opt.MapFrom(src => src.Alias))
                .ForMember(dest => dest.accesstype, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.ObjFlags, opt => opt.Ignore())
                .ForMember(dest => dest.CompactSubObj, opt => opt.Ignore())
                .ForMember(dest => dest.count, opt => opt.Ignore())
                .ForMember(dest => dest.ObjExtend, opt => opt.Ignore())
                .ForMember(dest => dest.PDOtype, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Label, opt => opt.Ignore())
                .ForMember(dest => dest.parent, opt => opt.Ignore())
                .ForMember(dest => dest.prop, opt => opt.Ignore())
                .ForPath(dest => dest.prop.CO_accessSRDO, opt => opt.MapFrom(src => src.Srdo))
                .ForPath(dest => dest.prop.CO_stringLengthMin, opt => opt.MapFrom(src => src.StringLengthMin))
                .ForMember(dest => dest.uniqueID, opt => opt.Ignore())
                .ForMember(dest => dest.objecttype, opt => opt.Ignore())
                .ForMember(dest => dest.Description, opt => opt.Ignore())
                .ForMember(dest => dest.subobjects, opt => opt.Ignore());
            }, LoggerFactory.Create(builder => { builder.AddDebug(); }));
            fromProtoConfig.AssertConfigurationIsValid();
            _fromProtoMapper = fromProtoConfig.CreateMapper();

            var toProtoConfig = new MapperConfiguration(cfg =>
            {
                // workaround for https://github.com/AutoMapper/AutoMapper/issues/2959
                // Cant update untill after .net framwork is gone
                cfg.ShouldMapMethod = (m => m.Name == "ARandomStringThatDoesNotMatchAnyFunctionName");
                cfg.CreateMap<EDSsharp, CanOpenDevice>()
                .ForMember(dest => dest.FileInfo, opt => opt.MapFrom(src => src.fi))
                .ForMember(dest => dest.DeviceInfo, opt => opt.MapFrom(src => src.di))
                .ForMember(dest => dest.DeviceCommissioning, opt => opt.MapFrom(src => src.dc))
                .ForMember(dest => dest.Objects, opt => opt.Ignore())
                .ForMember(dest => dest.NrSupportedModules, opt => opt.MapFrom(src => src.sm.NrOfEntries))
                .ForMember(dest => dest.Modules, opt => opt.MapFrom(src => src.modules))
                .AfterMap((src, dest, ctx) => {
                    if (src.ods != null) {
                        foreach (var kvp in src.ods) {
                            dest.Objects.Add(kvp.Key.ToString("X4"), ctx.Mapper.Map<OdObject>(kvp.Value));
                        }
                    }
                    if (src.cm?.connectedmodulelist != null)
                    {
                        foreach (var kvp in src.cm.connectedmodulelist)
                        {
                            if (dest.Modules.TryGetValue((uint)kvp.Key, out var module))
                            {
                                module.IsConnected = true;
                            }
                        }
                    }
                });
                cfg.CreateMap<libEDSsharp.ModuleInfo, LibCanOpen.CanOpenModuleInfo>();
                cfg.CreateMap<libEDSsharp.Module, LibCanOpen.CanOpenModule>()
                .ForMember(dest => dest.Info, opt => opt.MapFrom(src => src.mi))
                .ForMember(dest => dest.ExtendedObjects, opt => opt.MapFrom(src => src.mse.objectlist.Values))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.mc.comments))
                .ForMember(dest => dest.IsConnected, opt => opt.Ignore()); // We map IsConnected dynamically later
                cfg.CreateMap<FileInfo, CanOpen_FileInfo>()
                .ForMember(dest => dest.CreationTime, opt => opt.MapFrom(new EDSDateAndTimeResolver("creation")))
                .ForMember(dest => dest.ModificationTime, opt => opt.MapFrom(new EDSDateAndTimeResolver("modification")));
                cfg.CreateMap<DeviceInfo, CanOpen_DeviceInfo>()
                .ForMember(dest => dest.BaudRate10, opt => opt.MapFrom(src => src.BaudRate_10))
                .ForMember(dest => dest.BaudRate20, opt => opt.MapFrom(src => src.BaudRate_20))
                .ForMember(dest => dest.BaudRate50, opt => opt.MapFrom(src => src.BaudRate_50))
                .ForMember(dest => dest.BaudRate125, opt => opt.MapFrom(src => src.BaudRate_125))
                .ForMember(dest => dest.BaudRate250, opt => opt.MapFrom(src => src.BaudRate_250))
                .ForMember(dest => dest.BaudRate500, opt => opt.MapFrom(src => src.BaudRate_500))
                .ForMember(dest => dest.BaudRate800, opt => opt.MapFrom(src => src.BaudRate_800))
                .ForMember(dest => dest.BaudRate1000, opt => opt.MapFrom(src => src.BaudRate_1000))
                .ForMember(dest => dest.BaudRateAuto, opt => opt.MapFrom(src => src.BaudRate_auto))
                .ForMember(dest => dest.VendorNumber, opt => opt.MapFrom(src => src.VendorNumber))
                .ForMember(dest => dest.ProductNumber, opt => opt.MapFrom(src => src.ProductNumber))
                .ForMember(dest => dest.RevisionNumber, opt => opt.MapFrom(src => src.RevisionNumber))
                .ForMember(dest => dest.Granularity, opt => opt.MapFrom(src => (uint)src.Granularity))
                .ForMember(dest => dest.LssSlave, opt => opt.MapFrom(src => src.LSS_Supported))
                .ForMember(dest => dest.LssMaster, opt => opt.MapFrom(src => src.LSS_Master));
                cfg.CreateMap<DeviceCommissioning, CanOpen_DeviceCommissioning>();
                cfg.CreateMap<ODentry, OdObject>(MemberList.None)
                .ForMember(dest => dest.Disabled, opt => opt.MapFrom(src => src.prop.CO_disabled))
                .ForMember(dest => dest.Alias, opt => opt.MapFrom(src => src.denotation))
                .ForMember(dest => dest.StorageGroup, opt => opt.MapFrom(src => src.prop.CO_storageGroup))
                .ForMember(dest => dest.FlagsPDO, opt => opt.MapFrom(src => src.prop.CO_flagsPDO))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.parameter_name))
                .ForMember(dest => dest.ObjectType, opt => opt.MapFrom(src => src.objecttype))
                .ForMember(dest => dest.CountLabel, opt => opt.MapFrom(src => src.prop.CO_countLabel))
                .ForMember(dest => dest.SubObjects, opt => opt.Ignore())
                .AfterMap((src, dest, ctx) => {
                    if (src.subobjects != null) {
                        foreach (var kvp in src.subobjects) {
                            dest.SubObjects.Add(kvp.Key.ToString("X2"), ctx.Mapper.Map<OdSubObject>(kvp.Value));
                        }
                    }
                    if (src.objecttype == ObjectType.VAR) {
                        var subObj = ctx.Mapper.Map<OdSubObject>(src);
                        if (!dest.SubObjects.ContainsKey("00"))
                            dest.SubObjects.Add("00", subObj);
                        else
                            dest.SubObjects["00"] = subObj;
                    }
                });
                cfg.CreateMap<ObjectType, OdObject.Types.ObjectType>().ConvertUsing<ODTypeResolver>();
                cfg.CreateMap<EDSsharp.AccessType, OdSubObject.Types.AccessSDO>().ConvertUsing<ODAccessTypeResolver>();
                cfg.CreateMap<EDSsharp.AccessType, OdSubObject.Types.AccessPDO>().ConvertUsing<ODAccessTypeResolver>();
                
                cfg.CreateMap<ODentry, OdSubObject>(MemberList.None)
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.parameter_name))
                .ForMember(dest => dest.Alias, opt => opt.MapFrom(src => src.denotation))
                .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => src.datatype))
                .ForMember(dest => dest.Sdo, opt => opt.MapFrom(src => src.accesstype))
                .ForMember(dest => dest.Pdo, opt => opt.MapFrom(src => GetPdo(src)))
                .ForMember(dest => dest.Srdo, opt => opt.MapFrom(src => src.prop.CO_accessSRDO))
                .ForMember(dest => dest.StringLengthMin, opt => opt.MapFrom(src => src.prop.CO_stringLengthMin));
            }, LoggerFactory.Create(builder => { builder.AddDebug(); }));

            _toProtoMapper = toProtoConfig.CreateMapper();
        }
"""

new_content = new_content.replace('public class MappingEDS\n    {', 'public class MappingEDS\n    {\n' + header_injection)

# Now replace the body of MapFromProtobuffer
old_from_body = """            var config = new MapperConfiguration(cfg =>
            {
                // workaround for https://github.com/AutoMapper/AutoMapper/issues/2959
                // Cant update untill after .net framwork is gone
                cfg.ShouldMapMethod = (m => m.Name == "ARandomStringThatDoesNotMatchAnyFunctionName");
                cfg.CreateMap<string, UInt16>().ConvertUsing<ODStringToShortTypeResolver>();
                cfg.CreateMap<CanOpenDevice, EDSsharp>()
                .ForMember(dest => dest.Dirty, opt => opt.Ignore())
                .ForMember(dest => dest.xddfilename_1_1, opt => opt.Ignore())
                .ForMember(dest => dest.xddfilenameStripped, opt => opt.Ignore())
                .ForMember(dest => dest.edsfilename, opt => opt.Ignore())
                .ForMember(dest => dest.dcffilename, opt => opt.Ignore())
                .ForMember(dest => dest.ODfilename, opt => opt.Ignore())
                .ForMember(dest => dest.ODfileVersion, opt => opt.Ignore())
                .ForMember(dest => dest.mdfilename, opt => opt.Ignore())
                .ForMember(dest => dest.xmlfilename, opt => opt.Ignore())
                .ForMember(dest => dest.xddfilename_1_0, opt => opt.Ignore())
                .ForMember(dest => dest.xddTemplate, opt => opt.Ignore())
                .ForMember(dest => dest.dummy_ods, opt => opt.Ignore())
                .ForMember(dest => dest.CO_storageGroups, opt => opt.Ignore())
                .ForMember(dest => dest.md, opt => opt.Ignore())
                .ForMember(dest => dest.oo, opt => opt.Ignore())
                .ForMember(dest => dest.mo, opt => opt.Ignore())
                .ForMember(dest => dest.c, opt => opt.Ignore())
                .ForMember(dest => dest.du, opt => opt.Ignore())
                .ForMember(dest => dest.td, opt => opt.Ignore())
                .ForMember(dest => dest.sm, opt => opt.Ignore())
                .ForMember(dest => dest.cm, opt => opt.Ignore())
                .ForMember(dest => dest.modules, opt => opt.Ignore())
                .ForMember(dest => dest.NodeID, opt => opt.Ignore())
                .ForMember(dest => dest.projectFilename, opt => opt.MapFrom(src => src.DeviceInfo.ProductName))
                .ForMember(dest => dest.NodeID, opt => opt.MapFrom(src => src.DeviceCommissioning.NodeId))
                .ForMember(dest => dest.fi, opt => opt.MapFrom(src => src.FileInfo))
                .ForMember(dest => dest.di, opt => opt.MapFrom(src => src.DeviceInfo))
                .ForMember(dest => dest.dc, opt => opt.MapFrom(src => src.DeviceCommissioning))
                .ForMember(dest => dest.ods, opt => opt.MapFrom(src => src.Objects));
                cfg.CreateMap<CanOpen_FileInfo, FileInfo>()
                .ForMember(dest => dest.FileName, opt => opt.Ignore())
                .ForMember(dest => dest.LastEDS, opt => opt.Ignore())
                .ForMember(dest => dest.EDSVersionMajor, opt => opt.Ignore())
                .ForMember(dest => dest.EDSVersionMinor, opt => opt.Ignore())
                .ForMember(dest => dest.EDSVersion, opt => opt.Ignore())
                .ForMember(dest => dest.exportFolder, opt => opt.Ignore())
                .ForMember(dest => dest.FileRevision, opt => opt.MapFrom(src => (byte)src.FileVersion.ElementAtOrDefault(0)))
                .ForMember(dest => dest.CreationDateTime, opt => opt.MapFrom(src => src.CreationTime.ToDateTime()))
                .ForMember(dest => dest.CreationDate, opt => opt.MapFrom(src => src.CreationTime.ToDateTime().ToString("MM-dd-yyyy", System.Globalization.CultureInfo.InvariantCulture)))
                .ForMember(dest => dest.CreationTime, opt => opt.MapFrom(src => src.CreationTime.ToDateTime().ToString("h:mmtt", System.Globalization.CultureInfo.InvariantCulture)))
                .ForMember(dest => dest.ModificationDateTime, opt => opt.MapFrom(src => src.ModificationTime.ToDateTime()))
                .ForMember(dest => dest.ModificationDate, opt => opt.MapFrom(src => src.ModificationTime.ToDateTime().ToString("MM-dd-yyyy", System.Globalization.CultureInfo.InvariantCulture)))
                .ForMember(dest => dest.ModificationTime, opt => opt.MapFrom(src => src.ModificationTime.ToDateTime().ToString("h:mmtt", System.Globalization.CultureInfo.InvariantCulture)));
                cfg.CreateMap<CanOpen_DeviceInfo, DeviceInfo>()
                .ForMember(dest => dest.BaudRate_10, opt => opt.MapFrom(src => src.BaudRate10))
                .ForMember(dest => dest.BaudRate_20, opt => opt.MapFrom(src => src.BaudRate20))
                .ForMember(dest => dest.BaudRate_50, opt => opt.MapFrom(src => src.BaudRate50))
                .ForMember(dest => dest.BaudRate_125, opt => opt.MapFrom(src => src.BaudRate125))
                .ForMember(dest => dest.BaudRate_250, opt => opt.MapFrom(src => src.BaudRate250))
                .ForMember(dest => dest.BaudRate_500, opt => opt.MapFrom(src => src.BaudRate500))
                .ForMember(dest => dest.BaudRate_800, opt => opt.MapFrom(src => src.BaudRate800))
                .ForMember(dest => dest.BaudRate_1000, opt => opt.MapFrom(src => src.BaudRate1000))
                .ForMember(dest => dest.BaudRate_auto, opt => opt.MapFrom(src => src.BaudRateAuto))
                .ForMember(dest => dest.VendorNumber, opt => opt.MapFrom(src => src.VendorNumber))
                .ForMember(dest => dest.ProductNumber, opt => opt.MapFrom(src => src.ProductNumber))
                .ForMember(dest => dest.RevisionNumber, opt => opt.MapFrom(src => src.RevisionNumber))
                .ForMember(dest => dest.SimpleBootUpMaster, opt => opt.Ignore())
                .ForMember(dest => dest.SimpleBootUpSlave, opt => opt.Ignore())
                .ForMember(dest => dest.Granularity, opt => opt.MapFrom(src => (byte)src.Granularity))
                .ForMember(dest => dest.DynamicChannelsSupported, opt => opt.Ignore())
                .ForMember(dest => dest.CompactPDO, opt => opt.Ignore())
                .ForMember(dest => dest.GroupMessaging, opt => opt.Ignore())
                .ForMember(dest => dest.NrOfRXPDO, opt => opt.Ignore()) // TODO Calculate this
                .ForMember(dest => dest.NrOfTXPDO, opt => opt.Ignore()) // TODO Calculate this
                .ForMember(dest => dest.LSS_Supported, opt => opt.MapFrom(src => src.LssSlave))
                .ForMember(dest => dest.LSS_Master, opt => opt.MapFrom(src => src.LssMaster))
                .ForMember(dest => dest.NG_Slave, opt => opt.Ignore())
                .ForMember(dest => dest.NG_Master, opt => opt.Ignore())
                .ForMember(dest => dest.NrOfNG_MonitoredNodes, opt => opt.Ignore());
                cfg.CreateMap<CanOpen_DeviceCommissioning, DeviceCommissioning>()
                .ForMember(dest => dest.NetNumber, opt => opt.Ignore())
                .ForMember(dest => dest.NetworkName, opt => opt.Ignore())
                .ForMember(dest => dest.CANopenManager, opt => opt.Ignore())
                .ForMember(dest => dest.LSS_SerialNumber, opt => opt.Ignore());
                cfg.CreateMap<OdObject, CustomProperties>()
                .ForMember(dest => dest.CO_accessSRDO, opt => opt.Ignore())
                .ForMember(dest => dest.CO_stringLengthMin, opt => opt.Ignore())
                .ForMember(dest => dest.CO_disabled, opt => opt.MapFrom(src => src.Disabled))
                .ForMember(dest => dest.CO_countLabel, opt => opt.MapFrom(src => src.CountLabel))
                .ForMember(dest => dest.CO_storageGroup, opt => opt.MapFrom(src => src.StorageGroup))
                .ForMember(dest => dest.CO_flagsPDO, opt => opt.MapFrom(src => src.FlagsPDO));
                cfg.CreateMap<OdObject.Types.ObjectType, ObjectType>().ConvertUsing<ODTypeResolver>();
                cfg.CreateMap<OdSubObject, PDOMappingType>().ConvertUsing<ODPDOTypeConverter>();
                cfg.CreateMap<OdObject, ODentry>()
                .ForMember(dest => dest.Index, opt => opt.Ignore())
                .ForMember(dest => dest.parameter_name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.denotation, opt => opt.MapFrom(src => src.Alias))
                .ForMember(dest => dest.datatype, opt => opt.Ignore())
                .ForMember(dest => dest.accesstype, opt => opt.Ignore())
                .ForMember(dest => dest.defaultvalue, opt => opt.Ignore())
                .ForMember(dest => dest.LowLimit, opt => opt.Ignore())
                .ForMember(dest => dest.HighLimit, opt => opt.Ignore())
                .ForMember(dest => dest.actualvalue, opt => opt.Ignore())
                .ForMember(dest => dest.ObjFlags, opt => opt.Ignore())
                .ForMember(dest => dest.CompactSubObj, opt => opt.Ignore())
                .ForMember(dest => dest.count, opt => opt.Ignore())
                .ForMember(dest => dest.ObjExtend, opt => opt.Ignore())
                .ForMember(dest => dest.PDOtype, opt => opt.Ignore())
                .ForMember(dest => dest.Label, opt => opt.Ignore())
                .ForMember(dest => dest.parent, opt => opt.Ignore())
                .ForMember(dest => dest.prop, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.uniqueID, opt => opt.Ignore())
                .AfterMap((src, dest, ctx) => {
                    if (dest.objecttype == ObjectType.VAR && (src.SubObjects.TryGetValue("0", out var subObj) || src.SubObjects.TryGetValue("00", out subObj))) {
                        dest.datatype = ctx.Mapper.Map<DataType>(subObj.DataType);
                        dest.accesstype = ctx.Mapper.Map<EDSsharp.AccessType>(subObj);
                        dest.defaultvalue = subObj.DefaultValue;
                        dest.actualvalue = subObj.ActualValue;
                        dest.LowLimit = subObj.LowLimit;
                        dest.HighLimit = subObj.HighLimit;
                        dest.prop.CO_accessSRDO = (libEDSsharp.AccessSRDO)subObj.Srdo;
                        dest.prop.CO_stringLengthMin = subObj.StringLengthMin;
                        dest.PDOtype = ctx.Mapper.Map<PDOMappingType>(subObj);
                        dest.subobjects.Clear();
                    }
                });
                cfg.CreateMap<OdSubObject, EDSsharp.AccessType>().ConvertUsing<ODAccessTypeResolver>();

                cfg.CreateMap<Google.Protobuf.Collections.MapField<string, OdObject>, SortedDictionary<ushort, ODentry>>()
                    .ConvertUsing((src, dest, ctx) => {
                        var dict = new SortedDictionary<ushort, ODentry>();
                        foreach (var kvp in src) {
                            if (ushort.TryParse(kvp.Key, System.Globalization.NumberStyles.HexNumber, null, out ushort key)) {
                                dict.Add(key, ctx.Mapper.Map<ODentry>(kvp.Value));
                            }
                        }
                        return dict;
                    });
                
                cfg.CreateMap<Google.Protobuf.Collections.MapField<string, OdSubObject>, SortedDictionary<ushort, ODentry>>()
                    .ConvertUsing((src, dest, ctx) => {
                        var dict = new SortedDictionary<ushort, ODentry>();
                        foreach (var kvp in src) {
                            if (ushort.TryParse(kvp.Key, System.Globalization.NumberStyles.HexNumber, null, out ushort key)) {
                                dict.Add(key, ctx.Mapper.Map<ODentry>(kvp.Value));
                            }
                        }
                        return dict;
                    });

                cfg.CreateMap<OdSubObject, ODentry>()
                .ForMember(dest => dest.parameter_name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Index, opt => opt.Ignore())
                .ForMember(dest => dest.denotation, opt => opt.MapFrom(src => src.Alias))
                .ForMember(dest => dest.accesstype, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.ObjFlags, opt => opt.Ignore())
                .ForMember(dest => dest.CompactSubObj, opt => opt.Ignore())
                .ForMember(dest => dest.count, opt => opt.Ignore())
                .ForMember(dest => dest.ObjExtend, opt => opt.Ignore())
                .ForMember(dest => dest.PDOtype, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Label, opt => opt.Ignore())
                .ForMember(dest => dest.parent, opt => opt.Ignore())
                .ForMember(dest => dest.prop, opt => opt.Ignore())
                .ForPath(dest => dest.prop.CO_accessSRDO, opt => opt.MapFrom(src => src.Srdo))
                .ForPath(dest => dest.prop.CO_stringLengthMin, opt => opt.MapFrom(src => src.StringLengthMin))
                .ForMember(dest => dest.uniqueID, opt => opt.Ignore())
                .ForMember(dest => dest.objecttype, opt => opt.Ignore())
                .ForMember(dest => dest.Description, opt => opt.Ignore())
                .ForMember(dest => dest.subobjects, opt => opt.Ignore());
            }, LoggerFactory.Create(builder => { builder.AddDebug(); }));
            config.AssertConfigurationIsValid();
            var mapper = config.CreateMapper();

            var result = mapper.Map<EDSsharp>(source);"""
new_from_body = "            var result = _fromProtoMapper.Map<EDSsharp>(source);"
new_content = new_content.replace(old_from_body, new_from_body)

old_to_body = """            var config = new MapperConfiguration(cfg =>
            {
                // workaround for https://github.com/AutoMapper/AutoMapper/issues/2959
                // Cant update untill after .net framwork is gone
                cfg.ShouldMapMethod = (m => m.Name == "ARandomStringThatDoesNotMatchAnyFunctionName");
                cfg.CreateMap<EDSsharp, CanOpenDevice>()
                .ForMember(dest => dest.FileInfo, opt => opt.MapFrom(src => src.fi))
                .ForMember(dest => dest.DeviceInfo, opt => opt.MapFrom(src => src.di))
                .ForMember(dest => dest.DeviceCommissioning, opt => opt.MapFrom(src => src.dc))
                .ForMember(dest => dest.Objects, opt => opt.Ignore())
                .ForMember(dest => dest.NrSupportedModules, opt => opt.MapFrom(src => src.sm.NrOfEntries))
                .ForMember(dest => dest.Modules, opt => opt.MapFrom(src => src.modules))
                .AfterMap((src, dest, ctx) => {
                    if (src.ods != null) {
                        foreach (var kvp in src.ods) {
                            dest.Objects.Add(kvp.Key.ToString("X4"), ctx.Mapper.Map<OdObject>(kvp.Value));
                        }
                    }
                    if (src.cm?.connectedmodulelist != null)
                    {
                        foreach (var kvp in src.cm.connectedmodulelist)
                        {
                            if (dest.Modules.TryGetValue((uint)kvp.Key, out var module))
                            {
                                module.IsConnected = true;
                            }
                        }
                    }
                });
                cfg.CreateMap<libEDSsharp.ModuleInfo, LibCanOpen.CanOpenModuleInfo>();
                cfg.CreateMap<libEDSsharp.Module, LibCanOpen.CanOpenModule>()
                .ForMember(dest => dest.Info, opt => opt.MapFrom(src => src.mi))
                .ForMember(dest => dest.ExtendedObjects, opt => opt.MapFrom(src => src.mse.objectlist.Values))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.mc.comments))
                .ForMember(dest => dest.IsConnected, opt => opt.Ignore()); // We map IsConnected dynamically later
                cfg.CreateMap<FileInfo, CanOpen_FileInfo>()
                .ForMember(dest => dest.CreationTime, opt => opt.MapFrom(new EDSDateAndTimeResolver("creation")))
                .ForMember(dest => dest.ModificationTime, opt => opt.MapFrom(new EDSDateAndTimeResolver("modification")));
                cfg.CreateMap<DeviceInfo, CanOpen_DeviceInfo>()
                .ForMember(dest => dest.BaudRate10, opt => opt.MapFrom(src => src.BaudRate_10))
                .ForMember(dest => dest.BaudRate20, opt => opt.MapFrom(src => src.BaudRate_20))
                .ForMember(dest => dest.BaudRate50, opt => opt.MapFrom(src => src.BaudRate_50))
                .ForMember(dest => dest.BaudRate125, opt => opt.MapFrom(src => src.BaudRate_125))
                .ForMember(dest => dest.BaudRate250, opt => opt.MapFrom(src => src.BaudRate_250))
                .ForMember(dest => dest.BaudRate500, opt => opt.MapFrom(src => src.BaudRate_500))
                .ForMember(dest => dest.BaudRate800, opt => opt.MapFrom(src => src.BaudRate_800))
                .ForMember(dest => dest.BaudRate1000, opt => opt.MapFrom(src => src.BaudRate_1000))
                .ForMember(dest => dest.BaudRateAuto, opt => opt.MapFrom(src => src.BaudRate_auto))
                .ForMember(dest => dest.VendorNumber, opt => opt.MapFrom(src => src.VendorNumber))
                .ForMember(dest => dest.ProductNumber, opt => opt.MapFrom(src => src.ProductNumber))
                .ForMember(dest => dest.RevisionNumber, opt => opt.MapFrom(src => src.RevisionNumber))
                .ForMember(dest => dest.Granularity, opt => opt.MapFrom(src => (uint)src.Granularity))
                .ForMember(dest => dest.LssSlave, opt => opt.MapFrom(src => src.LSS_Supported))
                .ForMember(dest => dest.LssMaster, opt => opt.MapFrom(src => src.LSS_Master));
                cfg.CreateMap<DeviceCommissioning, CanOpen_DeviceCommissioning>();
                cfg.CreateMap<ODentry, OdObject>(MemberList.None)
                .ForMember(dest => dest.Disabled, opt => opt.MapFrom(src => src.prop.CO_disabled))
                .ForMember(dest => dest.Alias, opt => opt.MapFrom(src => src.denotation))
                .ForMember(dest => dest.StorageGroup, opt => opt.MapFrom(src => src.prop.CO_storageGroup))
                .ForMember(dest => dest.FlagsPDO, opt => opt.MapFrom(src => src.prop.CO_flagsPDO))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.parameter_name))
                .ForMember(dest => dest.ObjectType, opt => opt.MapFrom(src => src.objecttype))
                .ForMember(dest => dest.CountLabel, opt => opt.MapFrom(src => src.prop.CO_countLabel))
                .ForMember(dest => dest.SubObjects, opt => opt.Ignore())
                .AfterMap((src, dest, ctx) => {
                    if (src.subobjects != null) {
                        foreach (var kvp in src.subobjects) {
                            dest.SubObjects.Add(kvp.Key.ToString("X2"), ctx.Mapper.Map<OdSubObject>(kvp.Value));
                        }
                    }
                    if (src.objecttype == ObjectType.VAR) {
                        var subObj = ctx.Mapper.Map<OdSubObject>(src);
                        if (!dest.SubObjects.ContainsKey("00"))
                            dest.SubObjects.Add("00", subObj);
                        else
                            dest.SubObjects["00"] = subObj;
                    }
                });
                cfg.CreateMap<ObjectType, OdObject.Types.ObjectType>().ConvertUsing<ODTypeResolver>();
                cfg.CreateMap<EDSsharp.AccessType, OdSubObject.Types.AccessSDO>().ConvertUsing<ODAccessTypeResolver>();
                cfg.CreateMap<EDSsharp.AccessType, OdSubObject.Types.AccessPDO>().ConvertUsing<ODAccessTypeResolver>();
                
                cfg.CreateMap<ODentry, OdSubObject>(MemberList.None)
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.parameter_name))
                .ForMember(dest => dest.Alias, opt => opt.MapFrom(src => src.denotation))
                .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => src.datatype))
                .ForMember(dest => dest.Sdo, opt => opt.MapFrom(src => src.accesstype))
                .ForMember(dest => dest.Pdo, opt => opt.MapFrom(src => GetPdo(src)))
                .ForMember(dest => dest.Srdo, opt => opt.MapFrom(src => src.prop.CO_accessSRDO))
                .ForMember(dest => dest.StringLengthMin, opt => opt.MapFrom(src => src.prop.CO_stringLengthMin));
            }, LoggerFactory.Create(builder => { builder.AddDebug(); }));

            // config.AssertConfigurationIsValid();
            var mapper = config.CreateMapper();
            return mapper.Map<CanOpenDevice>(source);"""

new_to_body = "            return _toProtoMapper.Map<CanOpenDevice>(source);"
new_content = new_content.replace(old_to_body, new_to_body)

with open(filename, "w", encoding="utf-8") as f:
    f.write(new_content)
