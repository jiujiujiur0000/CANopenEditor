using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DialogHostAvalonia;
using EDSEditorGUI2.Mapper;
using EDSEditorGUI2.ViewModels;
using libEDSsharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace EDSEditorGUI2.Views;

public partial class MainWindow : Window
{
    public async void OpenProjectProgrammatically(string filePath)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine($"[Perf] Started OpenProjectProgrammatically at {sw.ElapsedMilliseconds} ms");

        try
        {
            if (DataContext is MainWindowViewModel dc)
            {
                dc.IsLoading = true;
                IsProgrammaticChange = true;
                
                Console.WriteLine($"[Perf] Starting Task.Run at {sw.ElapsedMilliseconds} ms");
                // Run heavy parsing and mapping in background task to avoid UI freeze
                var deviceViews = await System.Threading.Tasks.Task.Run(() =>
                {
                    CanOpenXDD_1_1 coxml_1_1 = new();
                    List<EDSsharp>? edss = null;
                    if (filePath.ToLower().EndsWith(".cpj"))
                    {
                        edss = coxml_1_1.ReadMultiXML(filePath);
                        if (edss == null) // Fallback if extension is wrong
                        {
                            var singleEds = coxml_1_1.ReadXML(filePath);
                            if (singleEds != null) edss = new List<EDSsharp> { singleEds };
                        }
                    }
                    else
                    {
                        var singleEds = coxml_1_1.ReadXML(filePath);
                        if (singleEds != null)
                        {
                            edss = new List<EDSsharp> { singleEds };
                        }
                        else // Fallback if accidentally saved as multi-xml
                        {
                            edss = coxml_1_1.ReadMultiXML(filePath);
                        }
                    }

                    if (edss == null) return (List<ViewModels.Device>?)null;

                    var parsedViews = new List<ViewModels.Device>();
                    foreach (var eds in edss)
                    {
                        eds.projectFilename = filePath;
                        if (Path.GetExtension(filePath).ToLower() == ".xdd" || Path.GetExtension(filePath).ToLower() == ".xpd") 
                        {
                            eds.xddfilename_1_1 = filePath;
                        }
                        
                        var proto = MappingEDS.MapToProtobuffer(eds);
                        var deviceView = ProtobufferViewModelMapper.MapFromProtobuffer(proto);
                        deviceView.Eds = eds;
                        parsedViews.Add(deviceView);
                    }
                    return parsedViews;
                });
                
                Console.WriteLine($"[Perf] Task.Run finished at {sw.ElapsedMilliseconds} ms");

                if (deviceViews != null)
                {
                    dc.Network.Clear();
                    dc.CurrentProjectPath = filePath;

                    foreach (var deviceView in deviceViews)
                    {
                        dc.Network.Add(deviceView);
                    }
                    
                    if (dc.Network.Count > 0)
                    {
                        var sw_inst = System.Diagnostics.Stopwatch.StartNew();
                        var dummy = new DeviceView();
                        sw_inst.Stop();
                        Console.WriteLine($"[Perf] new DeviceView() took {sw_inst.ElapsedMilliseconds} ms");

                        Console.WriteLine($"[Perf] Before Setting SelectedDevice at {sw.ElapsedMilliseconds} ms");
                        var sw2 = System.Diagnostics.Stopwatch.StartNew();
                        dc.SelectedDevice = dc.Network[0];
                        sw2.Stop();
                        Console.WriteLine($"[Perf] After Setting SelectedDevice took {sw2.ElapsedMilliseconds} ms");
                    }
                    dc.IsDirty = false;
                    if (dc.SelectedDevice != null) dc.SelectedDevice.IsDirty = false;
                    dc.AddRecentFile(filePath);
                    
                    var captureSw = sw.ElapsedMilliseconds;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => { 
                        Console.WriteLine($"[Perf] UI Settle Background Task Executed after {sw.ElapsedMilliseconds - captureSw} ms layout delay");
                        dc.IsLoading = false;
                        IsProgrammaticChange = false; 
                    }, Avalonia.Threading.DispatcherPriority.Background);
                }
                else
                {
                    dc.IsLoading = false;
                }
            }
        }
        catch(Exception e) { 
            Console.WriteLine(e); 
            if (DataContext is MainWindowViewModel dc) dc.IsLoading = false;
        }
    }

    private static int _projectCounter = 1;

    readonly FilePickerFileType xpd = new("CANopen XPD 1.1")
    {
        Patterns = ["*.xpd"]
    };
    readonly FilePickerFileType xdd = new("CANopen XDD 1.1")
    {
        Patterns = ["*.xdd"]
    };
    readonly FilePickerFileType xdc = new("CANopen XDC 1.1")
    {
        Patterns = ["*.xdc"]
    };

    private Avalonia.Threading.DispatcherTimer? _autoSaveTimer;

    public MainWindow()
    {
        InitializeComponent();
        ApplySavedTheme();
        
        // Auto-save feature: trigger on lost focus, toggle changes, or text changes
        this.AddHandler(Avalonia.Controls.Button.ClickEvent, OnAnyInteractionTriggerAutoSave, RoutingStrategies.Bubble);
        this.AddHandler(Avalonia.Input.InputElement.KeyUpEvent, OnAnyInteractionTriggerAutoSave, RoutingStrategies.Bubble);
        this.AddHandler(Avalonia.Controls.Primitives.SelectingItemsControl.SelectionChangedEvent, OnAnyInteractionTriggerAutoSave, RoutingStrategies.Bubble);
        LoadProfileList();
        this.Loaded += MainWindow_Loaded;
        
        // Start background warmup to improve first-time load performance
        _ = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                // Pre-JIT XmlSerializers (slow first time)
                _ = new System.Xml.Serialization.XmlSerializer(typeof(CanOpenXSD_1_1.ISO15745ProfileContainer));
                _ = new System.Xml.Serialization.XmlSerializer(typeof(CanOpenProject_1_1));
            }
            catch { }
        });
        
        // Warm up heavy UI controls on UI thread during idle time
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _ = new DeviceView();
            }
            catch { }
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    public bool IsProgrammaticChange { get; set; } = false;

    private void OnAnyInteractionTriggerAutoSave(object? sender, RoutedEventArgs e)
    {
        if (IsProgrammaticChange) return;

        if (e.Source is Avalonia.Controls.Control control)
        {
            // 蹇界暐娣诲姞绱㈠紩寮圭獥涓殑杈撳叆鎺т欢锛岄槻姝㈣竟鎵撳瓧杈硅Е鍙戣嚜鍔ㄤ繚瀛?
            if (control.Name == "index" || control.Name == "name" || control.Name == "type")
            {
                return;
            }

            // 蹇界暐鍋忓ソ璁剧疆(PreferencesView)寮圭獥閲岀殑鎵€鏈夎緭鍏ヤ慨鏀?
            Avalonia.StyledElement? p = control;
            while (p != null)
            {
                if (p.GetType().Name == "PreferencesView") return;
                p = p.Parent;
            }
        }



        // 瀵逛簬鎸夐敭鎶捣浜嬩欢锛屽彧鍝嶅簲鏉ヨ嚜瀹為檯杈撳叆鎺т欢鐨勪簨浠?
        if (e.RoutedEvent == Avalonia.Input.InputElement.KeyUpEvent)
        {
            if (e.Source is not Avalonia.Controls.TextBox && 
                e.Source is not Avalonia.Controls.NumericUpDown)
            {
                return;
            }
        }

        // 瀵逛簬鐐瑰嚮浜嬩欢锛屽彧鍝嶅簲鏉ヨ嚜 CheckBox 鎴?RadioButton 鐨勪簨浠?
        // (娉ㄦ剰锛欳omboBox 涓嬫媺妗嗗睍寮€鏃朵篃浼氳Е鍙戝唴閮?ToggleButton 鐨勭偣鍑伙紝蹇呴』鎺掗櫎)
        if (e.RoutedEvent == Avalonia.Controls.Button.ClickEvent)
        {
            if (e.Source is not Avalonia.Controls.CheckBox && 
                e.Source is not Avalonia.Controls.RadioButton)
            {
                return;
            }
        }

        // 瀵逛簬閫夋嫨鍙樺寲浜嬩欢锛屽彧鍝嶅簲 ComboBox
        if (e.RoutedEvent == Avalonia.Controls.Primitives.SelectingItemsControl.SelectionChangedEvent)
        {
            if (e.Source is not Avalonia.Controls.ComboBox cb)
            {
                return;
            }
            if (!cb.IsFocused && !cb.IsDropDownOpen)
            {
                return;
            }
        }

        TriggerAutoSave();
    }

    public void TriggerAutoSave()
    {
        if (DataContext is MainWindowViewModel dc)
        {
            dc.IsDirty = true;
            if (dc.SelectedDevice != null)
            {
                dc.SelectedDevice.IsDirty = true;
                dc.SelectedDevice.FileInfo.ModificationTime = DateTime.Now;
            }
            
            if (string.IsNullOrEmpty(dc.CurrentProjectPath))
            {
                return; // Don't auto-save if there's no project path
            }
        }

        if (_autoSaveTimer == null)
        {
            _autoSaveTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _autoSaveTimer.Tick += (s, args) =>
            {
                _autoSaveTimer.Stop();
                if (DataContext is MainWindowViewModel dc && !string.IsNullOrEmpty(dc.CurrentProjectPath))
                {
                    DoSaveProject(dc.CurrentProjectPath);
                }
            };
        }
        
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private bool _isForceClosing = false;

    protected override async void OnClosing(Avalonia.Controls.WindowClosingEventArgs e)
    {
        if (_isForceClosing)
        {
            base.OnClosing(e);
            return;
        }

        if (DataContext is MainWindowViewModel dc && dc.IsDirty)
        {
            // If AutoSave is ON and we have a path, we can save silently
            if (!string.IsNullOrEmpty(dc.CurrentProjectPath))
            {
                DoSaveProject(dc.CurrentProjectPath);
            }
            else
            {
                // We have unsaved changes and cannot silently save.
                e.Cancel = true;
                var result = await ShowSaveConfirmDialog();
                if (result == "Save")
                {
                    SaveProjectAsClick(null, null);
                    // Do not close immediately, let them save first.
                    // If they successfully save, IsDirty will become false.
                }
                else if (result == "Discard")
                {
                    _isForceClosing = true;
                    Close();
                }
                // If Cancel, do nothing
                return;
            }
        }

        if (_autoSaveTimer != null && _autoSaveTimer.IsEnabled)
        {
            _autoSaveTimer.Stop();
        }
        
        base.OnClosing(e);
    }
    
    private async System.Threading.Tasks.Task<string> ShowSaveConfirmDialog()
    {
        if (Resources["SaveConfirmDialog"] is Avalonia.Controls.Control dialog)
        {
            var result = await DialogHostAvalonia.DialogHost.Show(dialog, "RootDialogHost");
            return result?.ToString() ?? "Cancel";
        }
        return "Cancel";
    }

    private void ApplySavedTheme()
    {
        var app = Avalonia.Application.Current;
        if (app != null)
        {
            var savedTheme = ConfigurationManager.Settings.CurrentTheme;
            if (savedTheme == "Light")
            {
                app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
            }
            else if (savedTheme == "Dark")
            {
                app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            }
            else
            {
                app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
            }
        }
    }

    private void LoadProfileList()
    {
        // load default profiles from the install directory
        // load user profiles from the My Documents\.edseditor\profiles\ folder
        // Personal is my documents in windows and ~ in mono

        try
        {
            List<string> profilelist = [];
            string defaultProfilesDir = Path.Combine(AppContext.BaseDirectory, "Profiles");
            if (Directory.Exists(defaultProfilesDir))
            {
                profilelist.AddRange(Directory.GetFiles(defaultProfilesDir));
            }
            string homepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".edseditor");
            homepath = Path.Combine(homepath, "profiles");

            if (Directory.Exists(homepath))
            {
                profilelist.AddRange(Directory.GetFiles(homepath));
            }

            List<MenuItem> newMenuItems = [];

            var openItem = new MenuItem { Tag = "opendialog" };
            if (Avalonia.Application.Current != null)
            {
                openItem.Bind(MenuItem.HeaderProperty, Avalonia.Application.Current.GetResourceObservable("str_profile_open_file"));
            }
            newMenuItems.Add(openItem);
            foreach (string file in profilelist)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".xpd" || ext == ".xdd")
                {
                    newMenuItems.Add(new MenuItem { Header = Path.GetFileName(file), Tag = file });
                }
            }

            foreach (var i in newMenuItems)
            {
                i.Click += OnProfileMenuClick;
                profileMenu.Items.Add(i);
            }

        }
        catch (Exception e)
        {
            Debug.WriteLine($"Loading profiles has failed for the following reason : {e}");
        }
    }
    /// <summary>
    /// Combines different filepicker entrie into one
    /// </summary>
    /// <param name="name">the name of the new filepicker</param>
    /// <param name="types">list of filepicker</param>
    /// <returns>a combination of all filepicker types</returns>
    private static FilePickerFileType CombineFilePickerType(string name, List<FilePickerFileType> types)
    {
        List<string> patterns = [];

        foreach (var t in types)
        {
            if (t.Patterns is not null)
            {
                foreach (var p in t.Patterns)
                {
                    patterns.Add(p);
                }
            }
        }

        return new FilePickerFileType(name) { Patterns = patterns };
    }

    /// <summary>
    /// Eventhandler for any of the profile submenues
    /// </summary>
    /// <param name="sender">event trigger object</param>
    /// <param name="args">event arguments</param>
    /// <exception cref="Exception">On logical errors that sould not happend</exception>
    private async void OnProfileMenuClick(object? sender, RoutedEventArgs args)
    {
        var s = (MenuItem)sender!;
        string filePath;

        if (s.Tag is string fileSource)
        {
            if (fileSource == "opendialog")
            {
                // Get top level from the current control. Alternatively, you can use Window reference instead.
                var topLevel = TopLevel.GetTopLevel(this) ?? throw new Exception("Internal GUI error");

                // Start async operation to open the dialog.
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open Text File",
                    AllowMultiple = false,
                    FileTypeFilter = [CombineFilePickerType("All supported files", [xpd, xdd, xdc]), xpd, xdd, xdc]
                });

                if (files.Count >= 1)
                {
                    filePath = files[0].TryGetLocalPath() ?? files[0].Path.ToString();
                }
                else
                {
                    return;
                }
            }
            else
            {
                filePath = fileSource;
            }
            CanOpenXDD_1_1 coxml_1_1 = new();
            var eds = coxml_1_1.ReadXML(filePath);
            
            var proto = MappingEDS.MapToProtobuffer(eds);
            var viewModel = ProtobufferViewModelMapper.MapFromProtobuffer(proto);

            if (viewModel != null)
            {
                await MergeObjectsIntoDeviceAsync(viewModel);
            }
        }
    }

    public async System.Threading.Tasks.Task MergeObjectsIntoDeviceAsync(ViewModels.Device sourceDevice)
    {
        if (sourceDevice != null && DataContext is MainWindowViewModel dc && dc.SelectedDevice != null)
        {
            dc.InitMergeStatus(sourceDevice, [0]);
            
            var dialog = new InsertObjectsWindow()
            {
                DataContext = dc
            };
            
            var result = await dialog.ShowDialog<string>(this);
            
            if (result == "insert" && dc.SelectedDevice != null)
            {
                // Gathering all collision tasks first
                List<CollisionTask> tasks = new List<CollisionTask>();
                foreach (var insertObj in dc.MergeStatus)
                {
                    if (insertObj.Insert)
                    {
                        foreach (var offset in insertObj.Offsets)
                        {
                            if (offset.Collision)
                            {
                                tasks.Add(new CollisionTask { Index = Math.Abs(offset.Index) });
                            }
                        }
                    }
                }

                if (tasks.Count > 0)
                {
                    var collisionDialog = new CollisionDialog(tasks);
                    var dialogResult = await DialogHostAvalonia.DialogHost.Show(collisionDialog, "RootDialogHost");
                    if (dialogResult as string == "Cancel")
                    {
                        goto EndInsertion;
                    }
                }

                int conflictIndex = 0;
                foreach (var insertObj in dc.MergeStatus)
                {
                    if (insertObj.Insert)
                    {
                        foreach (var offset in insertObj.Offsets)
                        {
                            int finalIndex = offset.Index;
                            if (offset.Collision)
                            {
                                finalIndex = Math.Abs(finalIndex);
                            }

                            string strIndex = finalIndex.ToString("X4");

                            if (offset.Collision)
                            {
                                var decision = tasks[conflictIndex].Result;
                                conflictIndex++;
                                if (decision == CollisionDialogResult.Overwrite)
                                {
                                    dc.SelectedDevice.Objects[strIndex] = insertObj.Object;
                                }
                            }
                            else
                            {
                                dc.SelectedDevice.Objects[strIndex] = insertObj.Object;
                            }
                        }
                    }
                }
                EndInsertion:;
            }
            dc.MergeStatus.Clear();
        }
    }

    private async void OpenFileClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this) ?? throw new Exception("Internal GUI error");
        
        var edsFilter = new FilePickerFileType("Electronic Data Sheet (*.eds)") { Patterns = ["*.eds"] };
        var dcfFilter = new FilePickerFileType("Device Configuration File (*.dcf)") { Patterns = ["*.dcf"] };
        
        var xpdFilter = new FilePickerFileType("CANopen Project (*.xpd, *.json, *.binpb)") { Patterns = ["*.xpd", "*.json", "*.binpb"] };
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open CANopen Device",
            AllowMultiple = true,
            FileTypeFilter = [CombineFilePickerType("All supported files", [xdd, xdc, xpdFilter, edsFilter, dcfFilter]), xdd, xdc, xpdFilter, edsFilter, dcfFilter]
        });

        if (files.Count > 0)
        {
            if (DataContext is MainWindowViewModel dc)
            {
                dc.IsLoading = true;
                IsProgrammaticChange = true;
                
                var loadedDevices = await System.Threading.Tasks.Task.Run(() =>
                {
                    var parsedDevices = new List<ViewModels.Device>();
                    foreach (var file in files)
                    {
                        string filePath = file.TryGetLocalPath() ?? file.Path.ToString();
                        
                        try
                        {
                            EDSsharp? eds = null;
                            string ext = Path.GetExtension(filePath).ToLower();
                            if (ext == ".xdd" || ext == ".xdc" || ext == ".xpd")
                            {
                                CanOpenXDD_1_1 coxml_1_1 = new();
                                eds = coxml_1_1.ReadXML(filePath);
                                if (eds == null) // Fallback for corrupted extensions
                                {
                                    var edss = coxml_1_1.ReadMultiXML(filePath);
                                    if (edss != null && edss.Count > 0)
                                        eds = edss[0];
                                }
                                
                                if (eds != null)
                                {
                                    eds.projectFilename = filePath;
                                    if (ext == ".xdd")
                                    {
                                        eds.xddfilename_1_1 = filePath;
                                    }
                                }
                                else
                                {
                                    continue; // Skip if completely failed
                                }
                            }
                            else
                            {
                                eds = new EDSsharp();
                                eds.Loadfile(filePath);
                                if (ext == ".eds") eds.edsfilename = filePath;
                                else if (ext == ".dcf") eds.dcffilename = filePath;
                                else if (ext == ".md") eds.mdfilename = filePath;
                            }

                            var proto = MappingEDS.MapToProtobuffer(eds);
                            var deviceView = ProtobufferViewModelMapper.MapFromProtobuffer(proto);
                            deviceView.Eds = eds;
                            parsedDevices.Add(deviceView);
                        }
                        catch (Exception ex)
                        {
                            System.IO.File.WriteAllText("crash.log", ex.ToString());
                            Debug.WriteLine($"Failed to open file {filePath}: {ex.Message}");
                        }
                    }
                    return parsedDevices;
                });
                
                foreach (var deviceView in loadedDevices)
                {
                    dc.Network.Add(deviceView);
                    dc.SelectedDevice = deviceView;
                    dc.IsDirty = false; // Reset dirty flag after loading
                    if (dc.SelectedDevice != null) dc.SelectedDevice.IsDirty = false;
                    dc.AddRecentFile(deviceView.Eds.projectFilename ?? deviceView.Eds.edsfilename ?? "");
                }
                
                Avalonia.Threading.Dispatcher.UIThread.Post(() => 
                { 
                    dc.IsLoading = false;
                    IsProgrammaticChange = false; 
                }, Avalonia.Threading.DispatcherPriority.Background);
            }
        }
    }

    private ViewModels.Device? GetTargetDevice(RoutedEventArgs args)
    {
        if (args.Source is MenuItem menuItem && menuItem.DataContext is ViewModels.Device deviceContext)
        {
            return deviceContext;
        }
        if (DataContext is MainWindowViewModel dc)
        {
            return dc.SelectedDevice;
        }
        return null;
    }

    private void RemoveDeviceClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel dc && dc.SelectedDevice != null)
        {
            dc.CloseDevice(dc.SelectedDevice);
            TriggerAutoSave();
        }
    }

    private void NewDeviceClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel dc)
        {
            dc.AddNewDevice();
        }
    }

    public async void ExportCurrentDeviceEdsClick(object? sender, RoutedEventArgs args)
    {
        var targetDevice = GetTargetDevice(args);
        if (targetDevice == null) return;

        var topLevel = TopLevel.GetTopLevel(this) ?? throw new Exception("Internal GUI error");
        var xdd11 = new FilePickerFileType("CANopen XDD v1.1 (*.xdd)") { Patterns = ["*.xdd"] };
        var edsFilter = new FilePickerFileType("Electronic Data Sheet (*.eds)") { Patterns = ["*.eds"] };
        var dcfFilter = new FilePickerFileType("Device Configuration File (*.dcf)") { Patterns = ["*.dcf"] };
        
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Device File",
            DefaultExtension = ".xdd",
            SuggestedFileName = string.IsNullOrWhiteSpace(targetDevice.DeviceInfo.ProductName) ? "Device" : targetDevice.DeviceInfo.ProductName,
            FileTypeChoices = [xdd11, edsFilter, dcfFilter]
        });

        if (file != null)
        {
            string filePath = file.TryGetLocalPath() ?? file.Path.ToString();
            string ext = Path.GetExtension(filePath).ToLower();
            var eds = targetDevice.GetUpdatedEds();
            
            try
            {
                if (ext == ".xdd")
                {
                    libEDSsharp.CanOpenXDD_1_1 coxml = new();
                    coxml.WriteXML(filePath, eds, true, false);
                }
                else if (ext == ".eds")
                {
                    eds.Savefile(filePath, libEDSsharp.InfoSection.Filetype.File_EDS);
                }
                else if (ext == ".dcf")
                {
                    eds.Savefile(filePath, libEDSsharp.InfoSection.Filetype.File_DCF);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Export failed: {ex}");
            }
        }
    }

    private async void ExportCurrentDeviceSource(RoutedEventArgs args, libEDSsharp.ExporterFactory.Exporter version)
    {
        var targetDevice = GetTargetDevice(args);
        if (targetDevice == null) return;

        var topLevel = TopLevel.GetTopLevel(this) ?? throw new Exception("Internal GUI error");
        var cFiles = new FilePickerFileType("C Source & Header (*.c, *.h)") { Patterns = ["*.c", "*.h"] };
        
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export CANopenNode Source",
            SuggestedFileName = version == libEDSsharp.ExporterFactory.Exporter.CANOPENNODE_V4 ? "OD" : "CO_OD",
            FileTypeChoices = [cFiles]
        });

        if (file != null)
        {
            string filePath = file.TryGetLocalPath() ?? file.Path.ToString();
            
            // Update the UI/ViewModel directly so the user can see the exported version and file
            targetDevice.ProjectInfo.CanopenNodeFile = System.IO.Path.GetFileName(filePath);
            targetDevice.ProjectInfo.CanopenNodeFileVersion = version == libEDSsharp.ExporterFactory.Exporter.CANOPENNODE_V4 ? "V4" : "V1";
            
            // Get the updated EDS for exporting, which now includes the above changes
            var eds = targetDevice.GetUpdatedEds();
            
            try
            {
                var exporter = libEDSsharp.ExporterFactory.getExporter(version);
                exporter.export(filePath, eds);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Export failed: {ex}");
            }
        }
    }

    public void ExportCurrentDeviceSourceClick(object? sender, RoutedEventArgs args)
    {
        ExportCurrentDeviceSource(args, ConfigurationManager.Settings.CurrentExporter);
    }

    public async void ExportCurrentDevicePCanOpenNodeClick(object? sender, RoutedEventArgs args)
    {
        var targetDevice = GetTargetDevice(args);
        if (targetDevice == null) return;

        var topLevel = TopLevel.GetTopLevel(this) ?? throw new Exception("Internal GUI error");
        var jsonFilter = new FilePickerFileType("PCanOpenNode JSON (*.json)") { Patterns = ["*.json"] };
        var binpbFilter = new FilePickerFileType("PCanOpenNode Protobuf (*.binpb)") { Patterns = ["*.binpb"] };
        
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export PCanOpenNode Project",
            DefaultExtension = ".json",
            SuggestedFileName = string.IsNullOrWhiteSpace(targetDevice.DeviceInfo.ProductName) ? "Device" : targetDevice.DeviceInfo.ProductName,
            FileTypeChoices = [jsonFilter, binpbFilter]
        });

        if (file != null)
        {
            string filePath = file.TryGetLocalPath() ?? file.Path.ToString();
            string ext = Path.GetExtension(filePath).ToLower();
            var eds = targetDevice.GetUpdatedEds();
            
            try
            {
                libEDSsharp.CanOpenXDD_1_1 exporter = new();
                if (ext == ".json")
                {
                    exporter.WriteProtobuf(filePath, eds, true);
                }
                else if (ext == ".binpb")
                {
                    exporter.WriteProtobuf(filePath, eds, false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Export PCanOpenNode failed: {ex}");
            }
        }
    }

    public async void ExportNetworkXddClick(object? sender, RoutedEventArgs args)
    {
        if (DataContext is MainWindowViewModel dc && dc.Network != null && dc.Network.Count > 0)
        {
            var topLevel = TopLevel.GetTopLevel(this) ?? throw new Exception("Internal GUI error");
            var nxddFilter = new FilePickerFileType("Network XML Device Description (*.nxdd)") { Patterns = ["*.nxdd"] };
            var nxdcFilter = new FilePickerFileType("Network XML Device Configuration (*.nxdc)") { Patterns = ["*.nxdc"] };

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Network XML",
                DefaultExtension = ".nxdd",
                SuggestedFileName = "CANopenNetwork",
                FileTypeChoices = [nxddFilter, nxdcFilter]
            });

            if (file != null)
            {
                string filePath = file.TryGetLocalPath() ?? file.Path.ToString();
                string ext = Path.GetExtension(filePath).ToLower();
                
                var networkFiles = new List<libEDSsharp.EDSsharp>();
                foreach (var device in dc.Network)
                {
                    networkFiles.Add(device.GetUpdatedEds());
                }

                try
                {
                    libEDSsharp.CanOpenXDD_1_1 exporter = new();
                    if (ext == ".nxdd")
                    {
                        exporter.WriteMultiXML(filePath, networkFiles, false);
                    }
                    else if (ext == ".nxdc")
                    {
                        exporter.WriteMultiXML(filePath, networkFiles, true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Export Network XML failed: {ex}");
                }
            }
        }
    }

    public async void NewProjectClick(object sender, RoutedEventArgs args)
    {
        if (DataContext is MainWindowViewModel dc)
        {
            if (dc.IsDirty)
            {
                var result = await ShowSaveConfirmDialog();
                if (result == "Save")
                {
                    bool saved = await SaveProjectAsync();
                    if (!saved) return;
                }
                else if (result == "Cancel")
                {
                    return;
                }
            }
        }

        var topLevel = TopLevel.GetTopLevel(this) ?? throw new Exception("Internal GUI error");
        var cpj = new FilePickerFileType("CANopen Project (*.cpj)") { Patterns = ["*.cpj"] };
        var xdd = new FilePickerFileType("XML Device Description (*.xdd)") { Patterns = ["*.xdd"] };
        var xpd = new FilePickerFileType("XML Project Description (*.xpd)") { Patterns = ["*.xpd"] };

        var docsFolder = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(Avalonia.Platform.Storage.WellKnownFolder.Documents);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Create New CANopen Project",
            DefaultExtension = "cpj",
            FileTypeChoices = [cpj, xdd, xpd],
            SuggestedFileName = "NewProject" + _projectCounter.ToString(),
            SuggestedStartLocation = docsFolder
        });

        _projectCounter++;

        if (file != null)
        {
            string filePath = file.TryGetLocalPath() ?? file.Path.ToString();
            if (DataContext is MainWindowViewModel currentDc)
            {
                currentDc.NewProject();
                currentDc.CurrentProjectPath = filePath;
                DoSaveProject(filePath);
            }
        }
    }

    public async void CloseProjectClick(object? sender, RoutedEventArgs? args)
    {
        if (DataContext is MainWindowViewModel dc)
        {
            if (dc.IsDirty)
            {
                var result = await ShowSaveConfirmDialog();
                if (result == "Save")
                {
                    bool saved = await SaveProjectAsync();
                    if (!saved) return;
                }
                else if (result == "Cancel")
                {
                    return;
                }
            }
            
            IsProgrammaticChange = true;
            dc.CloseAllDevices();
            IsProgrammaticChange = false;
        }
    }

    public async void OpenProjectClick(object sender, RoutedEventArgs args)
    {
        var topLevel = TopLevel.GetTopLevel(this) ?? throw new Exception("Internal GUI error");

        var xpdFilter = new FilePickerFileType("XML Project Description (*.xpd)") { Patterns = ["*.xpd"] };
        var cpjFilter = new FilePickerFileType("CANopen Project (*.cpj)") { Patterns = ["*.cpj"] };
        var edsFilter = new FilePickerFileType("Electronic Data Sheet (*.eds)") { Patterns = ["*.eds"] };
        var dcfFilter = new FilePickerFileType("Device Configuration File (*.dcf)") { Patterns = ["*.dcf"] };
        
        var allFilter = CombineFilePickerType("All supported files", [cpjFilter, xpdFilter, xdd, xdc, edsFilter, dcfFilter]);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open CANopen Project",
            AllowMultiple = false,
            FileTypeFilter = [allFilter, cpjFilter, xpdFilter, xdd, xdc, edsFilter, dcfFilter]
        });

        if (files.Count > 0)
        {
            string filePath = files[0].TryGetLocalPath() ?? files[0].Path.ToString();
            if (DataContext is MainWindowViewModel dc)
            {
                if (dc.IsDirty)
                {
                    var result = await ShowSaveConfirmDialog();
                    if (result == "Save")
                    {
                        bool saved = await SaveProjectAsync();
                        if (!saved) return;
                    }
                    else if (result == "Cancel")
                    {
                        return;
                    }
                }

                OpenProjectProgrammatically(filePath);
            }
        }
    }

    public void QuitClick(object sender, RoutedEventArgs args)
    {
        Close();
    }

    public async System.Threading.Tasks.Task<bool> SaveProjectAsAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this) ?? throw new Exception("Internal GUI error");
        var cpj = new FilePickerFileType("CANopen Project (Multi-Device) (*.cpj)") { Patterns = ["*.cpj"] };
        var xpd = new FilePickerFileType("XML Project Description (Single Device) (*.xpd)") { Patterns = ["*.xpd"] };
        var xdd = new FilePickerFileType("XML Device Description (*.xdd)") { Patterns = ["*.xdd"] };

        var docsFolder = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(Avalonia.Platform.Storage.WellKnownFolder.Documents);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save CANopen Project",
            DefaultExtension = "cpj",
            FileTypeChoices = [cpj, xpd, xdd],
            SuggestedFileName = "NewCANopenProject",
            SuggestedStartLocation = docsFolder
        });

        if (file != null)
        {
            string filePath = file.TryGetLocalPath() ?? file.Path.ToString();
            if (DataContext is MainWindowViewModel dc)
            {
                dc.CurrentProjectPath = filePath;
            }
            DoSaveProject(filePath);
            return true;
        }
        return false;
    }

    public async System.Threading.Tasks.Task<bool> SaveProjectAsync()
    {
        if (DataContext is MainWindowViewModel dc && !string.IsNullOrEmpty(dc.CurrentProjectPath))
        {
            DoSaveProject(dc.CurrentProjectPath);
            return true;
        }
        else
        {
            return await SaveProjectAsAsync();
        }
    }

    public async void SaveProjectAsClick(object? sender, RoutedEventArgs? args)
    {
        await SaveProjectAsAsync();
    }

    public async void SaveProjectClick(object? sender, RoutedEventArgs? args)
    {
        await SaveProjectAsync();
    }

    private void DoSaveProject(string filePath)
    {
        try
        {
            if (DataContext is MainWindowViewModel dc)
            {
                List<EDSsharp> edss = new List<EDSsharp>();
                foreach (var device in dc.Network)
                {
                    edss.Add(device.GetUpdatedEds());
                }

                CanOpenXDD_1_1 coxml_1_1 = new();
                if (filePath.ToLower().EndsWith(".cpj"))
                {
                    coxml_1_1.WriteMultiXML(filePath, edss, true);
                }
                else if (edss.Count > 0)
                {
                    coxml_1_1.WriteXML(filePath, edss[0], true, false);
                }
                dc.IsDirty = false;
                if (dc.SelectedDevice != null) dc.SelectedDevice.IsDirty = false;
                dc.AddRecentFile(filePath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save project {filePath}: {ex.Message}");
            DialogHostAvalonia.DialogHost.Show(Resources["SaveErrorDialog"]!, "RootDialogHost");
        }
    }

    private async void OpenRecentFileClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is string filePath)
        {
            if (File.Exists(filePath))
            {
                if (DataContext is MainWindowViewModel dc)
                {
                    if (dc.IsDirty)
                    {
                        var result = await ShowSaveConfirmDialog();
                        if (result == "Save")
                        {
                            SaveProjectAsClick(null, null);
                            return;
                        }
                        else if (result == "Cancel")
                        {
                            return;
                        }
                    }

                    OpenProjectProgrammatically(filePath);
                }
            }
            else
            {
                if (DataContext is MainWindowViewModel dc)
                {
                    dc.RemoveRecentFile(filePath);
                }
                var dialog = (Control)Resources["FileNotFoundDialog"]!;
                dialog.DataContext = Avalonia.Application.Current?.FindResource("str_msg_file_not_found")?.ToString() + filePath;
                _ = DialogHostAvalonia.DialogHost.Show(dialog, "RootDialogHost");
            }
        }
    }

    private async void ClearRecentFilesClick(object? sender, RoutedEventArgs e)
    {
        var dialog = (Control)Resources["ClearRecentConfirmDialog"]!;
        var result = await DialogHostAvalonia.DialogHost.Show(dialog, "RootDialogHost");
        if (result?.ToString() == "OK")
        {
            if (DataContext is MainWindowViewModel dc)
            {
                dc.ClearRecentFiles();
            }
        }
    }

    private async void OpenPreferences(object? sender, RoutedEventArgs e)
    {
        await DialogHostAvalonia.DialogHost.Show(Resources["PreferencesDialog"]!, "RootDialogHost");
    }

    private string _aboutVersion = "";
    private string _aboutCopyright = "";
    private string _defaultCheckUpdateText = "检查更新";
    private bool _hasUpdateAvailable = false;
    private int _updateMessageToken = 0;
    
    private async void OpenAbout(object? sender, RoutedEventArgs e)
    {
        var aboutDialog = (Control)Resources["AboutDialog"]!;
        
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        if (string.IsNullOrEmpty(version) || version == "1.0.0.0") version = "1.1.0";
        if (version.EndsWith(".0") && version.Split('.').Length == 4) version = version.Substring(0, version.Length - 2);

        _aboutVersion = "Version " + version;
        _aboutCopyright = $"Copyright © 2024-{DateTime.Now.Year} Open Source Community. All rights reserved.";
        _defaultCheckUpdateText = Avalonia.Application.Current?.FindResource("str_about_check_update")?.ToString() ?? "检查更新";

        // Hide red dot when opening About
        var redDot = this.FindControl<Avalonia.Controls.Shapes.Ellipse>("MenuAboutRedDot");
        if (redDot != null) redDot.IsVisible = false;

        aboutDialog.DataContext = new {
            Version = _aboutVersion,
            Copyright = _aboutCopyright,
            UpdateMessage = _hasUpdateAvailable ? "发现新版本！请点击立即更新。" : "",
            CheckUpdateBtnText = _hasUpdateAvailable ? "立即更新" : _defaultCheckUpdateText
        };
        
        await DialogHostAvalonia.DialogHost.Show(aboutDialog, "RootDialogHost");
    }

    private void OpenGitHubClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/jiujiujiur0000/CANopenEditor",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private async void CheckUpdateClick(object? sender, RoutedEventArgs e)
    {
        if (_hasUpdateAvailable)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/jiujiujiur0000/CANopenEditor/releases/latest",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return;
        }

        int currentToken = System.Threading.Interlocked.Increment(ref _updateMessageToken);
        var aboutDialog = (Control)Resources["AboutDialog"]!;
        
        aboutDialog.DataContext = new {
            Version = _aboutVersion,
            Copyright = _aboutCopyright,
            UpdateMessage = "正在检查更新...",
            CheckUpdateBtnText = _defaultCheckUpdateText
        };
        
        await CheckUpdateAsync(aboutDialog);
    }

    private async System.Threading.Tasks.Task CheckUpdateAsync(Control dialog)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "CANopenEditor");
            var response = await client.GetStringAsync("https://api.github.com/repos/jiujiujiur0000/CANopenEditor/releases/latest");
            using var doc = System.Text.Json.JsonDocument.Parse(response);
            string latestVersion = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            
            string currentVersionString = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            if (string.IsNullOrEmpty(currentVersionString) || currentVersionString == "1.0.0.0") currentVersionString = "1.1.0";
            if (currentVersionString.EndsWith(".0") && currentVersionString.Split('.').Length == 4) currentVersionString = currentVersionString.Substring(0, currentVersionString.Length - 2);

            if (Version.TryParse(latestVersion.Trim('v', 'V'), out var vLatest) && Version.TryParse(currentVersionString, out var vCurrent))
            {
                if (vLatest > vCurrent)
                {
                    UpdateAboutState(dialog, $"发现新版本 v{vLatest}！", true);
                }
                else
                {
                    UpdateAboutState(dialog, $"当前已是最新版本 (v{vCurrent})。", false);
                }
            }
            else
            {
                UpdateAboutState(dialog, $"获取最新版本成功: {latestVersion}\n当前版本: v{currentVersionString}", false);
            }
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            UpdateAboutState(dialog, "检查更新失败：\n请求过于频繁，请稍后再试。\n或者直接点击“GitHub 仓库”查看最新版本。", false);
        }
        catch (Exception ex)
        {
            UpdateAboutState(dialog, "检查更新失败：\n" + ex.Message, false);
        }
    }

    private async void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await System.Threading.Tasks.Task.Delay(5000);
        await CheckUpdateSilentlyAsync();
    }

    private async System.Threading.Tasks.Task CheckUpdateSilentlyAsync()
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "CANopenEditor");
            var response = await client.GetStringAsync("https://api.github.com/repos/jiujiujiur0000/CANopenEditor/releases/latest");
            using var doc = System.Text.Json.JsonDocument.Parse(response);
            string latestVersion = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            
            string currentVersionString = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            if (string.IsNullOrEmpty(currentVersionString) || currentVersionString == "1.0.0.0") currentVersionString = "1.1.0";
            if (currentVersionString.EndsWith(".0") && currentVersionString.Split('.').Length == 4) currentVersionString = currentVersionString.Substring(0, currentVersionString.Length - 2);

            if (Version.TryParse(latestVersion.Trim('v', 'V'), out var vLatest) && Version.TryParse(currentVersionString, out var vCurrent))
            {
                if (vLatest > vCurrent)
                {
                    _hasUpdateAvailable = true;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                        var redDot = this.FindControl<Avalonia.Controls.Shapes.Ellipse>("MenuAboutRedDot");
                        if (redDot != null) redDot.IsVisible = true;
                    });
                }
            }
        }
        catch
        {
            // Ignore errors for silent check
        }
    }

    private async void UpdateAboutState(Control dialog, string message, bool hasUpdate)
    {
        _hasUpdateAvailable = hasUpdate;
        string btnText = hasUpdate ? "立即更新" : _defaultCheckUpdateText;
        int currentToken = System.Threading.Interlocked.Increment(ref _updateMessageToken);

        Avalonia.Threading.Dispatcher.UIThread.Post(() => { 
            dialog.DataContext = new {
                Version = _aboutVersion,
                Copyright = _aboutCopyright,
                UpdateMessage = message,
                CheckUpdateBtnText = btnText
            };
        });

        await System.Threading.Tasks.Task.Delay(5000);
        
        if (currentToken == _updateMessageToken)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => { 
                dialog.DataContext = new {
                    Version = _aboutVersion,
                    Copyright = _aboutCopyright,
                    UpdateMessage = "",
                    CheckUpdateBtnText = btnText
                };
            });
        }
    }


        private void Window_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            // Clear focus from any TextBox when clicking on a blank area
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
            topLevel?.FocusManager?.ClearFocus();
        }

        public void ReportDocumentationClick(object? sender, RoutedEventArgs args)
        {
            if (DataContext is MainWindowViewModel dc && dc.SelectedDevice != null)
            {
                try
                {
                    string dir = GetTemporaryDirectory();
                    CopyStyleCss(dir);

                    string tempHtml = Path.Combine(dir, "documentation.html");
                    string tempMd = Path.Combine(dir, "documentation.md");

                    var eds = dc.SelectedDevice.GetUpdatedEds();

                    DocumentationGenHtml docgenHtml = new DocumentationGenHtml();
                    docgenHtml.genhtmldoc(tempHtml, eds);

                    DocumentationGenMarkup docgenMarkup = new DocumentationGenMarkup();
                    docgenMarkup.genmddoc(tempMd, eds);

                    OpenUrlInBrowser(tempHtml);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to generate documentation: {ex}");
                }
            }
        }

        public void ReportNetworkPDOClick(object? sender, RoutedEventArgs args)
        {
            if (DataContext is MainWindowViewModel dc && dc.Network != null)
            {
                try
                {
                    string dir = GetTemporaryDirectory();
                    CopyStyleCss(dir);

                    string tempHtml = Path.Combine(dir, "network.html");

                    var networkFiles = new List<libEDSsharp.EDSsharp>();
                    foreach (var device in dc.Network)
                    {
                        networkFiles.Add(device.GetUpdatedEds());
                    }

                    NetworkPDOreport npr = new NetworkPDOreport();
                    npr.gennetpdodoc(tempHtml, networkFiles);

                    OpenUrlInBrowser(tempHtml);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to generate network PDO report: {ex}");
                }
            }
        }

        private string GetTemporaryDirectory()
        {
            string tempDirectory;
            do
            {
                tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            } while (Directory.Exists(tempDirectory));
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private void CopyStyleCss(string targetDir)
        {
            string cssPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "style.css");
            if (File.Exists(cssPath))
            {
                File.Copy(cssPath, Path.Combine(targetDir, "style.css"), true);
            }
        }

        private void OpenUrlInBrowser(string url)
        {
            try
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open URL in browser: {ex}");
            }
        }
    }
