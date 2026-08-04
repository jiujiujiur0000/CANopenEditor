using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;

namespace EDSEditorGUI2.Views
{
    public partial class PreferencesView : UserControl
    {
        public PreferencesView()
        {
            InitializeComponent();
            DataContext = new ViewModels.PreferencesViewModel();
        }

        private void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            // Close the DialogHost after saving
            DialogHost.Close(null);
        }
    }
}
