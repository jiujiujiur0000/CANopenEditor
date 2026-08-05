using CommunityToolkit.Mvvm.ComponentModel;
using libEDSsharp;
using System.Collections.ObjectModel;
using System.Linq;

namespace EDSEditorGUI2.ViewModels
{
    public partial class DevicePDOViewModel : ObservableObject
    {
        public class AvailableObjectViewModel
        {
            public ODentry Entry { get; }
            public AvailableObjectViewModel(ODentry entry, ushort subIndex)
            {
                Entry = entry;
                IndexString = "0x" + entry.Index.ToString("X4");
                SubIndexString = "0x" + subIndex.ToString("X2");
                Name = entry.parameter_name;
                ObjectTypeString = entry.datatype.ToString();
                TypeSize = entry.Sizeofdatatype().ToString();
            }

            public string IndexString { get; }
            public string SubIndexString { get; }
            public string Name { get; }
            public string ObjectTypeString { get; }
            public string TypeSize { get; }
        }

        private EDSsharp _eds;
        private PDOHelper _helper;
        private bool _isTx;

        public DevicePDOViewModel(EDSsharp eds, bool isTx)
        {
            _eds = eds;
            _isTx = isTx;
            
            if (_eds != null)
            {
                _helper = new PDOHelper(_eds);
                UpdatePDOList();
                UpdateAvailableObjects();
            }
        }

        public class PDOSlotViewModel
        {
            public PDOSlot Slot { get; }
            public PDOSlotViewModel(PDOSlot slot)
            {
                Slot = slot;
            }
            public string DescriptionComm => Slot.DescriptionComm;
            public string COB => "0x" + Slot.COB.ToString("X");
            public string TransmissionType => Slot.transmissiontype.ToString();
            public string Inhibit => Slot.inhibit.ToString();
            public string EventTimer => Slot.eventtimer.ToString();
            public string SyncStart => Slot.syncstart.ToString();
        }

        [ObservableProperty]
        private ObservableCollection<PDOSlotViewModel> _slots = new();

        private PDOSlotViewModel _selectedSlot;
        public PDOSlotViewModel SelectedSlot
        {
            get => _selectedSlot;
            set
            {
                if (SetProperty(ref _selectedSlot, value))
                {
                    UpdateMappings();
                    OnPropertyChanged(nameof(IsSlotSelected));
                }
            }
        }

        public bool IsSlotSelected => SelectedSlot != null;

        [ObservableProperty]
        private ObservableCollection<AvailableObjectViewModel> _availableObjects = new();

        public class MappingEntryViewModel
        {
            public PDOMappingEntry Entry { get; }
            public MappingEntryViewModel(PDOMappingEntry entry)
            {
                Entry = entry;
                if (entry.entry != null)
                {
                    IndexString = "0x" + entry.entry.Index.ToString("X4");
                    SubIndexString = "0x" + (entry.entry.parent != null ? entry.entry.parent.subobjects.FirstOrDefault(x => x.Value == entry.entry).Key.ToString("X2") : "00");
                    Name = entry.entry.parameter_name;
                    Width = entry.width.ToString();
                }
                else
                {
                    IndexString = "";
                    SubIndexString = "";
                    Name = "Empty";
                    Width = "";
                }
            }
            public string IndexString { get; }
            public string SubIndexString { get; }
            public string Name { get; }
            public string Width { get; }
        }

        [ObservableProperty]
        private ObservableCollection<MappingEntryViewModel> _mappings = new();

        private void UpdateMappings()
        {
            Mappings.Clear();
            if (SelectedSlot != null && SelectedSlot.Slot != null)
            {
                foreach (var mapping in SelectedSlot.Slot.Mapping)
                {
                    Mappings.Add(new MappingEntryViewModel(mapping));
                }
            }
        }

        public void UpdatePDOList()
        {
            Slots.Clear();
            if (_helper == null) return;
            foreach (var slot in _helper.pdoslots.Where(s => s.isTXPDO() == _isTx))
            {
                Slots.Add(new PDOSlotViewModel(slot));
            }
        }

        public void UpdateAvailableObjects()
        {
            AvailableObjects.Clear();
            if (_eds == null) return;
            foreach (var kvp in _eds.ods)
            {
                var od = kvp.Value;
                if (od.prop.CO_disabled == true) continue;

                if (od.objecttype == ObjectType.VAR && (od.PDOtype == PDOMappingType.optional || (_isTx && (od.PDOtype == PDOMappingType.TPDO)) || (!_isTx && (od.PDOtype == PDOMappingType.RPDO))))
                {
                    AvailableObjects.Add(new AvailableObjectViewModel(od, 0));
                }

                foreach (var kvp2 in od.subobjects)
                {
                    var odsub = kvp2.Value;
                    if (kvp2.Key == 0) continue;
                    
                    if (odsub.PDOtype == PDOMappingType.optional || (_isTx && (odsub.PDOtype == PDOMappingType.TPDO)) || (!_isTx && (odsub.PDOtype == PDOMappingType.RPDO)))
                    {
                        AvailableObjects.Add(new AvailableObjectViewModel(odsub, kvp2.Key));
                    }
                }
            }
        }
    }
}
