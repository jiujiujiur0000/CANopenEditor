using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using libEDSsharp;
using System;
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

        private EDSsharp _eds = null!;
        private PDOHelper _helper = null!;
        private bool _isTx;

        public DevicePDOViewModel(EDSsharp eds, bool isTx)
        {
            _eds = eds;
            _isTx = isTx;
            
            if (_eds != null)
            {
                _helper = new PDOHelper(_eds);
                _helper.build_PDOlists();
                UpdatePDOList();
                UpdateAvailableObjects();
            }
        }

        public partial class PDOSlotViewModel : ObservableObject
        {
            public PDOSlot Slot { get; }
            public PDOSlotViewModel(PDOSlot slot)
            {
                Slot = slot;
            }
            public string Name => (Slot.ConfigurationIndex >= 0x1800 ? "TPDO " : "RPDO ") + (Slot.ConfigurationIndex >= 0x1800 ? (Slot.ConfigurationIndex - 0x1800 + 1) : (Slot.ConfigurationIndex - 0x1400 + 1));
            public string DescriptionComm => Slot.DescriptionComm;
            
            public string Communication
            {
                get => "0x" + Slot.ConfigurationIndex.ToString("X4");
            }
            
            public string Mapping
            {
                get => "0x" + Slot.MappingIndex.ToString("X4");
            }
            
            public string COB
            {
                get => "0x" + Slot.COB.ToString("X");
                set { if (uint.TryParse(value.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out uint val)) { Slot.COB = val; OnPropertyChanged(); OnPropertyChanged(nameof(Invalid)); } }
            }
            
            public string TransmissionType
            {
                get => Slot.transmissiontype.ToString();
                set { if (byte.TryParse(value, out byte val)) { Slot.transmissiontype = val; OnPropertyChanged(); OnPropertyChanged(nameof(IsSyncStartEnabled)); } }
            }

            public bool IsSyncStartEnabled => Slot.transmissiontype != 254 && Slot.transmissiontype != 255;
            
            public string Inhibit
            {
                get => Slot.inhibit.ToString();
                set { if (ushort.TryParse(value, out ushort val)) { Slot.inhibit = val; OnPropertyChanged(); } }
            }
            
            public string EventTimer
            {
                get => Slot.eventtimer.ToString();
                set { if (ushort.TryParse(value, out ushort val)) { Slot.eventtimer = val; OnPropertyChanged(); } }
            }
            
            public string SyncStart
            {
                get => Slot.syncstart.ToString();
                set { if (byte.TryParse(value, out byte val)) { Slot.syncstart = val; OnPropertyChanged(); } }
            }
            
            public bool Invalid
            {
                get => Slot.invalid;
                set { Slot.invalid = value; OnPropertyChanged(); OnPropertyChanged(nameof(COB)); }
            }
        }

        [ObservableProperty]
        private ObservableCollection<PDOSlotViewModel> _slots = new();

        private PDOSlotViewModel _selectedSlot = null!;
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
            public int BitWidth => Entry.width;
        }

        [ObservableProperty]
        private ObservableCollection<MappingEntryViewModel> _mappings = new();

        private void UpdateMappings()
        {
            Mappings.Clear();
            if (SelectedSlot != null && SelectedSlot.Slot != null && SelectedSlot.Slot.Mapping != null)
            {
                foreach (var mapping in SelectedSlot.Slot.Mapping)
                {
                    Mappings.Add(new MappingEntryViewModel(mapping));
                }
            }
        }

        public void InsertMapping(PDOSlotViewModel slotViewModel, int ordinal, AvailableObjectViewModel item)
        {
            if (slotViewModel == null || slotViewModel.Slot == null || item == null) return;
            
            slotViewModel.Slot.insertMapping(ordinal, new PDOMappingEntry(item.Entry, item.Entry.Sizeofdatatype()));
            _helper.buildmappingsfromlists(false);
            
            UpdateMappings();
            // Fire CollectionChanged to trigger redraw
            OnPropertyChanged(nameof(Mappings));
            // Trigger property changed on SelectedSlot so the View refreshes bindings
            OnPropertyChanged(nameof(SelectedSlot));
        }

        public void RemoveMapping(PDOSlotViewModel slotViewModel, PDOMappingEntry entryToRemove)
        {
            if (slotViewModel == null || slotViewModel.Slot == null) return;
            
            slotViewModel.Slot.Mapping.Remove(entryToRemove);
            _helper.buildmappingsfromlists(false);
            
            UpdateMappings();
            OnPropertyChanged(nameof(Mappings));
            OnPropertyChanged(nameof(SelectedSlot));
        }

        public void InsertDummyMapping(PDOSlotViewModel slotViewModel, int ordinal)
        {
            if (slotViewModel == null || slotViewModel.Slot == null) return;
            
            int width_limit = 64;
            foreach (var m in slotViewModel.Slot.Mapping) width_limit -= m.width;
            if (width_limit <= 0) return;

            libEDSsharp.PDOMappingEntry od = new libEDSsharp.PDOMappingEntry();
            od.entry = _eds.dummy_ods[0x002];
            od.width = System.Math.Min(od.entry.Sizeofdatatype(), width_limit);
            
            slotViewModel.Slot.Mapping.Insert(ordinal, od);
            _helper.buildmappingsfromlists(false);
            
            UpdateMappings();
            OnPropertyChanged(nameof(Mappings));
            OnPropertyChanged(nameof(SelectedSlot));
        }

        public void UpdatePDOList()
        {
            Slots.Clear();
            if (_helper == null) return;
            foreach (var slot in _helper.pdoslots.Where(s => s.isTXPDO() == _isTx))
            {
                var vm = new PDOSlotViewModel(slot);
                vm.PropertyChanged += (sender, args) =>
                {
                    _helper.buildmappingsfromlists(false);
                };
                Slots.Add(vm);
            }
            if (Slots.Count > 0)
            {
                SelectedSlot = Slots[0];
            }
            else
            {
                SelectedSlot = null!;
            }
        }

        public void RefreshSlots()
        {
            if (_helper == null) return;
            _helper.build_PDOlists();
            UpdatePDOList();
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
        [RelayCommand]
        public void AddPDO()
        {
            if (_helper == null) return;
            var gap = _helper.findPDOslotgap(_isTx);
            if (gap != 0)
            {
                _helper.addPDOslot(gap);
                _helper.buildmappingsfromlists(false);
                UpdatePDOList();
                var addedSlot = Slots.FirstOrDefault(s => s.Slot.ConfigurationIndex == gap);
                if (addedSlot != null)
                {
                    SelectedSlot = addedSlot;
                }
            }
        }

        [RelayCommand]
        public void RemovePDO()
        {
            if (_helper == null || SelectedSlot == null) return;
            _helper.removePDOslot(SelectedSlot.Slot.ConfigurationIndex);
            _helper.buildmappingsfromlists(false);
            UpdatePDOList();
        }
    }
}
