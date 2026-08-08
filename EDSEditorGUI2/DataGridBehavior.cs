using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace EDSEditorGUI2
{
    public class DataGridBehavior
    {
        public static readonly AttachedProperty<bool> LockAutoMinWidthProperty =
            AvaloniaProperty.RegisterAttached<DataGridBehavior, DataGrid, bool>("LockAutoMinWidth");

        public static void SetLockAutoMinWidth(DataGrid element, bool value)
        {
            element.SetValue(LockAutoMinWidthProperty, value);
        }

        public static bool GetLockAutoMinWidth(DataGrid element)
        {
            return element.GetValue(LockAutoMinWidthProperty);
        }

        static DataGridBehavior()
        {
            LockAutoMinWidthProperty.Changed.AddClassHandler<DataGrid>(OnLockAutoMinWidthChanged);
        }

        private static void OnLockAutoMinWidthChanged(DataGrid grid, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool b && b)
            {
                grid.LayoutUpdated += Grid_LayoutUpdated;
            }
            else
            {
                grid.LayoutUpdated -= Grid_LayoutUpdated;
            }
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DataGridColumn, object> _processedColumns = new();

        private static void Grid_LayoutUpdated(object? sender, EventArgs e)
        {
            if (sender is DataGrid grid)
            {
                foreach (var col in grid.Columns)
                {
                    if (col.ActualWidth > 0 && !_processedColumns.TryGetValue(col, out _))
                    {
                        // Convert any auto/star sizing to absolute pixel width so it never changes
                        col.Width = new DataGridLength(col.ActualWidth, DataGridLengthUnitType.Pixel);
                        _processedColumns.Add(col, new object());
                    }
                }
            }
        }
    }
}
