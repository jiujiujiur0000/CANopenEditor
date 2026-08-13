using Avalonia.Controls;
using Avalonia.Interactivity;
using LibCanOpen;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EDSEditorGUI2.Views;

public partial class DeviceODView : UserControl
{
    private List<DataGrid> _odViews = [];
    public DeviceODView()
    {
        InitializeComponent();
        ODView_Com.grid.SelectionChanged += IndexGridSelectionChanged;
        ODView_Manufacture.grid.SelectionChanged += IndexGridSelectionChanged;
        ODView_Device.grid.SelectionChanged += IndexGridSelectionChanged;

        subindexGrid.SelectionChanged += subindexGridSelectionChanged;

        _odViews.Add(ODView_Com.grid);
        _odViews.Add(ODView_Manufacture.grid);
        _odViews.Add(ODView_Device.grid);

        foreach (var v in Enum.GetNames(typeof(OdSubObject.Types.DataType)))
        {
            combo_datatype.Items.Add(v);
        }

        foreach (var v in Enum.GetNames(typeof(OdSubObject.Types.AccessSDO)))
        {
            combo_sdo.Items.Add(v);
        }

        foreach (var v in Enum.GetNames(typeof(OdSubObject.Types.AccessPDO)))
        {
            combo_pdo.Items.Add(v);
        }

        foreach (var v in Enum.GetNames(typeof(OdSubObject.Types.AccessSRDO)))
        {
            combo_srdo.Items.Add(v);
        }
    }

    private void IndexGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid s && DataContext is ViewModels.ObjectDictionary dc)
        {
            MainWindow? mw = TopLevel.GetTopLevel(this) as MainWindow;
            if (mw != null) mw.IsProgrammaticChange = true;
            try
            {
                if (s.SelectedItem is KeyValuePair<string, ViewModels.OdObject> selected)
                {
                    dc.SelectedObject = selected;
                    foreach (var dg in _odViews)
                    {
                        if (dg != s)
                        {
                            dg.SelectedItem = null;
                        }
                    }
                    
                    if (selected.Value.SubObjects.Count > 0)
                    {
                        subindexGrid.SelectedItem = selected.Value.SubObjects[0];
                    }
                    else
                    {
                        subindexGrid.SelectedItem = null;
                    }
                }
            }
            finally
            {
                if (mw != null) mw.IsProgrammaticChange = false;
            }
        }
    }
    private void SubObjectDataGrid_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (sender is DataGrid dg && e.Source is Avalonia.Controls.Control control && control.DataContext is KeyValuePair<string, ViewModels.OdSubObject>)
            {
                dg.SelectedItem = control.DataContext;
            }
        }
    }

    private void subindexGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid s && DataContext is ViewModels.ObjectDictionary dc)
        {
            MainWindow? mw = TopLevel.GetTopLevel(this) as MainWindow;
            if (mw != null) mw.IsProgrammaticChange = true;
            try
            {
                if (s.SelectedItem is KeyValuePair<string, ViewModels.OdSubObject> selected)
                {
                    dc.SelectedSubObject = selected;
                    dc.SelectedSubObjects.Clear();
                    foreach (var row in s.SelectedItems)
                    {
                        if (row is KeyValuePair<string, ViewModels.OdSubObject> subObj)
                        {
                            dc.SelectedSubObjects.Add(subObj);
                        }
                    }
                }
            }
            finally
            {
                if (mw != null) mw.IsProgrammaticChange = false;
            }
        }
    }
    private void ContextMenuSubObjectAddClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ObjectDictionary dc)
        {
            var selectedObj = dc.SelectedObject.Value;
            ViewModels.OdSubObject? lastAdded = null;
            
            var selectedRows = dc.SelectedSubObjects.ToList();
            if (selectedRows.Count == 0)
            {
                lastAdded = selectedObj.AddSubEntry(new KeyValuePair<string, ViewModels.OdSubObject>());
            }
            else
            {
                foreach (var row in selectedRows)
                {
                    lastAdded = selectedObj.AddSubEntry(row);
                }
            }
            
            if (lastAdded != null)
            {
                var kvpToSelect = selectedObj.SubObjects.FirstOrDefault(x => x.Value == lastAdded);
                if (kvpToSelect.Value != null)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        subindexGrid.SelectedItem = kvpToSelect;
                        subindexGrid.ScrollIntoView(kvpToSelect, null);
                    });
                }
            }

            if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
            {
                mainWindow.TriggerAutoSave();
            }
        }
    }
    private void ContextMenuSubObjectRemoveClick(object? sender, RoutedEventArgs e)
    {
        bool renumber = sender == contextMenu_subObject_removeSubItemToolStripMenuItem;

        if (DataContext is ViewModels.ObjectDictionary dc)
        {
            var selectedObject = dc.SelectedObject.Value;

            //Clone the list because we cant modify the list we iterate on 
            var selectedObj = dc.SelectedSubObjects.ToList();
            foreach (var item in selectedObj)
            {
                selectedObject.RemoveSubEntry(item, renumber);
            }

            if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
            {
                mainWindow.TriggerAutoSave();
            }
        }
    }
}
