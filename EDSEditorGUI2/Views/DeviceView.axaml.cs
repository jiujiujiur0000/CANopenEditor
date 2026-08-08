using System;
using Avalonia.Controls;

namespace EDSEditorGUI2.Views;

public partial class DeviceView : UserControl
{
    private DeviceInfoView? _deviceInfoView;
    private DeviceODView? _deviceODView;
    private DevicePDOView? _txPdoView;
    private DevicePDOView? _rxPdoView;
    private ModuleView? _moduleView;

    public DeviceView()
    {
        InitializeComponent();
        
        var tabControl = this.FindControl<TabControl>("MainTabControl");
        if (tabControl != null)
        {
            tabControl.SelectionChanged += TabControl_SelectionChanged;
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UpdateTabContent();
    }

    private void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateTabContent();
    }

    private void UpdateTabContent()
    {
        if (DataContext is not EDSEditorGUI2.ViewModels.Device device) return;

        var tabControl = this.FindControl<TabControl>("MainTabControl");
        if (tabControl == null) return;

        var selectedTab = tabControl.SelectedItem as TabItem;
        if (selectedTab == null) return;

        if (selectedTab.Name == "TabDeviceInfo")
        {
            if (_deviceInfoView == null)
            {
                _deviceInfoView = new DeviceInfoView();
                selectedTab.Content = _deviceInfoView;
            }
            _deviceInfoView.DataContext = device;
        }
        else if (selectedTab.Name == "TabOD")
        {
            if (_deviceODView == null)
            {
                _deviceODView = new DeviceODView();
                selectedTab.Content = _deviceODView;
            }
            _deviceODView.DataContext = device.Objects;
        }
        else if (selectedTab.Name == "TabTxPdo")
        {
            if (_txPdoView == null)
            {
                _txPdoView = new DevicePDOView();
                selectedTab.Content = _txPdoView;
            }
            _txPdoView.DataContext = device.TxPdo;
        }
        else if (selectedTab.Name == "TabRxPdo")
        {
            if (_rxPdoView == null)
            {
                _rxPdoView = new DevicePDOView();
                selectedTab.Content = _rxPdoView;
            }
            _rxPdoView.DataContext = device.RxPdo;
        }
        else if (selectedTab.Name == "TabModule")
        {
            if (_moduleView == null)
            {
                _moduleView = new ModuleView();
                selectedTab.Content = _moduleView;
            }
            _moduleView.DataContext = device.ModuleInfo;
        }
    }
}