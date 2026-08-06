using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using LibCanOpen;
using Microsoft.Extensions.Logging;
using System;

namespace EDSEditorGUI2.Mapper
{
    public class ProtobufferViewModelMapper
    {
        private static uint ParseRevisionNumber(string revStr)
        {
            if (string.IsNullOrEmpty(revStr)) return 0;
            return uint.TryParse(revStr, out uint rev) ? rev : 0;
        }

        public static ViewModels.Device MapFromProtobuffer(CanOpenDevice source)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Timestamp, DateTime>().ConvertUsing(ts => ts == null ? default : ts.ToDateTime());
                cfg.CreateMap<CanOpen_FileInfo, ViewModels.FileInfo>()
                .ForMember(dest => dest.FileVersion, opt => opt.MapFrom(src => src.FileVersion))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.CreationTime, opt => opt.MapFrom(src => src.CreationTime))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.ModificationTime, opt => opt.MapFrom(src => src.ModificationTime))
                .ForMember(dest => dest.ModifiedBy, opt => opt.MapFrom(src => src.ModifiedBy));
                cfg.CreateMap<Google.Protobuf.Collections.MapField<string, OdObject>, ViewModels.ObjectDictionary>().ConvertUsing<ODConverter>();
                cfg.CreateMap<Google.Protobuf.Collections.MapField<uint, CanOpenModule>, System.Collections.ObjectModel.ObservableCollection<ViewModels.ModuleItemViewModel>>().ConvertUsing<ModuleConverter>();
                cfg.CreateMap<CanOpenDevice, ViewModels.Device>(MemberList.None)
                .ForMember(d => d.Eds, opt => opt.Ignore())
                .ForMember(d => d.TxPdo, opt => opt.Ignore())
                .ForMember(d => d.RxPdo, opt => opt.Ignore())
                .ForMember(d => d.ProjectInfo, opt => opt.Ignore())
                .ForMember(dest => dest.FileInfo, opt => opt.MapFrom(src => src.FileInfo))
                .ForMember(dest => dest.DeviceInfo, opt => opt.MapFrom(src => src.DeviceInfo))
                .ForMember(dest => dest.DeviceCommissioning, opt => opt.MapFrom(src => src.DeviceCommissioning))
                .ForMember(dest => dest.Objects, opt => opt.MapFrom(src => src.Objects))
                .ForPath(dest => dest.ModuleInfo.NrSupportedModules, opt => opt.MapFrom(src => src.NrSupportedModules))
                .ForPath(dest => dest.ModuleInfo.Modules, opt => opt.MapFrom(src => src.Modules));
                cfg.CreateMap<CanOpen_DeviceInfo, ViewModels.DeviceInfo>(MemberList.None)
                    .ForMember(dest => dest.RevisionNumber, opt => opt.MapFrom(src => src.RevisionNumber.ToString()));
                cfg.CreateMap<CanOpen_DeviceCommissioning, ViewModels.DeviceCommissioning>(MemberList.None);
            }, LoggerFactory.Create(builder => { builder.AddDebug(); }));
            config.AssertConfigurationIsValid();
            var mapper = config.CreateMapper();
            var result = mapper.Map<ViewModels.Device>(source);
            return result;
        }
        public class ODConverter : ITypeConverter<Google.Protobuf.Collections.MapField<string, OdObject>, ViewModels.ObjectDictionary>,
            ITypeConverter< ViewModels.ObjectDictionary, Google.Protobuf.Collections.MapField<string, OdObject>>
        {
            public ViewModels.ObjectDictionary Convert(Google.Protobuf.Collections.MapField<string, OdObject> source, ViewModels.ObjectDictionary destination, ResolutionContext context)
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<OdObject, ViewModels.OdObject>(MemberList.None);
                    cfg.CreateMap<OdSubObject, ViewModels.OdSubObject>(MemberList.None);
                }, LoggerFactory.Create(builder => { builder.AddDebug(); }));
                config.AssertConfigurationIsValid();
                var mapper = config.CreateMapper();

                destination = [];
                foreach (var item in source)
                {
                    // Convert Protobuf decimal string key to ViewModel hex string key
                    if (int.TryParse(item.Key, out int decKey))
                    {
                        destination.Add(decKey.ToString("X4"), mapper.Map<ViewModels.OdObject>(item.Value));
                    }
                    else
                    {
                        destination.Add(item.Key, mapper.Map<ViewModels.OdObject>(item.Value));
                    }
                }
                return destination;
            }
            public Google.Protobuf.Collections.MapField<string, OdObject> Convert(ViewModels.ObjectDictionary source, Google.Protobuf.Collections.MapField<string, OdObject> destination, ResolutionContext context)
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<ViewModels.OdObject, OdObject>(MemberList.None)
                        .ForMember(dest => dest.SubObjects, opt => opt.Ignore())
                        .AfterMap((src, dest, ctx) => 
                        {
                            dest.SubObjects.Clear();
                            if (src.SubObjects != null)
                            {
                                foreach (var subItem in src.SubObjects)
                                {
                                    var mappedSub = ctx.Mapper.Map<OdSubObject>(subItem.Value);
                                    if (int.TryParse(subItem.Key, System.Globalization.NumberStyles.HexNumber, null, out int decSubKey))
                                    {
                                        dest.SubObjects.Add(decSubKey.ToString(), mappedSub);
                                    }
                                    else
                                    {
                                        dest.SubObjects.Add(subItem.Key, mappedSub);
                                    }
                                }
                            }
                        });
                    cfg.CreateMap<ViewModels.OdSubObject, OdSubObject>(MemberList.None);
                }, LoggerFactory.Create(builder => { builder.AddDebug(); }));
                config.AssertConfigurationIsValid();
                var mapper = config.CreateMapper();

                if (destination == null) destination = new Google.Protobuf.Collections.MapField<string, OdObject>();
                else destination.Clear();
                
                foreach (var item in source)
                {
                    // Convert ViewModel hex string key to Protobuf decimal string key
                    try
                    {
                        int decKey = System.Convert.ToInt32(item.Key, 16);
                        destination.Add(decKey.ToString(), mapper.Map<OdObject>(item.Value));
                    }
                    catch
                    {
                        destination.Add(item.Key, mapper.Map<OdObject>(item.Value));
                    }
                }
                return destination;
            }
        }

        public class ModuleConverter : ITypeConverter<Google.Protobuf.Collections.MapField<uint, CanOpenModule>, System.Collections.ObjectModel.ObservableCollection<ViewModels.ModuleItemViewModel>>
        {
            public System.Collections.ObjectModel.ObservableCollection<ViewModels.ModuleItemViewModel> Convert(Google.Protobuf.Collections.MapField<uint, CanOpenModule> source, System.Collections.ObjectModel.ObservableCollection<ViewModels.ModuleItemViewModel> destination, ResolutionContext context)
            {
                destination ??= new();
                foreach (var item in source)
                {
                    var vm = new ViewModels.ModuleItemViewModel
                    {
                        Index = item.Key,
                        ProductName = item.Value.Info?.ProductName ?? string.Empty,
                        ProductVersion = item.Value.Info?.ProductVersion ?? string.Empty,
                        ProductRevision = item.Value.Info?.ProductRevision ?? string.Empty,
                        OrderCode = item.Value.Info?.OrderCode ?? string.Empty,
                        IsConnected = item.Value.IsConnected,
                        Comments = string.Join("\r\n", item.Value.Comments)
                    };
                    foreach (var extObj in item.Value.ExtendedObjects)
                    {
                        vm.ExtendedObjects.Add(new ViewModels.ModuleObjectViewModel
                        {
                            IndexHex = $"0x{extObj:X4}",
                            Name = $"Index {extObj:X4}" // Name mapping is handled later or left simple for now
                        });
                    }
                    destination.Add(vm);
                }
                return destination;
            }
        }

        public static CanOpenDevice MapToProtobuffer(ViewModels.Device source)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<DateTime, Timestamp>().ConvertUsing(dt => 
                    dt == DateTime.MinValue ? new Timestamp() : Timestamp.FromDateTime(dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime())
                );
                cfg.CreateMap<ViewModels.FileInfo, CanOpen_FileInfo>(MemberList.None);
                cfg.CreateMap<ViewModels.ObjectDictionary, Google.Protobuf.Collections.MapField<string, OdObject>>().ConvertUsing<ODConverter>();
                
                cfg.CreateMap<ViewModels.Device, CanOpenDevice>(MemberList.None)
                .ForMember(dest => dest.FileInfo, opt => opt.MapFrom(src => src.FileInfo))
                .ForMember(dest => dest.DeviceInfo, opt => opt.MapFrom(src => src.DeviceInfo))
                .ForMember(dest => dest.DeviceCommissioning, opt => opt.MapFrom(src => src.DeviceCommissioning))
                .ForMember(dest => dest.Objects, opt => opt.MapFrom(src => src.Objects))
                .ForMember(dest => dest.NrSupportedModules, opt => opt.MapFrom(src => src.ModuleInfo.NrSupportedModules))
                .ForMember(dest => dest.Modules, opt => opt.Ignore());

                cfg.CreateMap<ViewModels.DeviceInfo, CanOpen_DeviceInfo>(MemberList.None)
                    .ForMember(dest => dest.RevisionNumber, opt => opt.MapFrom(src => ParseRevisionNumber(src.RevisionNumber)));
                cfg.CreateMap<ViewModels.DeviceCommissioning, CanOpen_DeviceCommissioning>(MemberList.None);
            }, LoggerFactory.Create(builder => { builder.AddDebug(); }));
            config.AssertConfigurationIsValid();
            var mapper = config.CreateMapper();
            var result = mapper.Map<CanOpenDevice>(source);
            return result;
        }
    }
}
