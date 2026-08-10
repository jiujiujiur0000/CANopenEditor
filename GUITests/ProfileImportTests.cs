using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EDSEditorGUI2.ViewModels;
using EDSEditorGUI2.Views;

namespace GUITests;

public class ImportTests : IDisposable
{
    readonly MainWindow window;
    readonly MainWindowViewModel dc;
    readonly MenuItem? profileMenu;
    readonly TextBox? offsetsTextBox;
    readonly InsertObjectsWindow? dialog;

    readonly ComboBox? target;
    readonly Button? insert;
    readonly Button? cancel;

    public ImportTests()
    {
        dc = new MainWindowViewModel();
        window = new MainWindow
        {
            DataContext = dc
        };
        window.Show();

        // add device and select it
        dc.AddNewDevice(window);
        var deviceList = window.GetVisualDescendants().OfType<ListBox>().First();

        Dispatcher.UIThread.RunJobs();
        deviceList.SelectedItem = dc.Network[0];

        // import profile
        profileMenu = window.Find<MenuItem>("profileMenu");
        Assert.NotNull(profileMenu);
        profileMenu.Open();
        var DS301Menu = profileMenu.Items.OfType<MenuItem>().Where(x => x.Header!.ToString() == "DS301_profile.xpd").First();
        DS301Menu.Focus();
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        Dispatcher.UIThread.RunJobs();
        dialog = window.OwnedWindows.OfType<InsertObjectsWindow>().FirstOrDefault();
        Assert.NotNull(dialog);

        // fetch common ctrls
        offsetsTextBox = dialog.Find<TextBox>("InsertObjects_Offsets");
        Assert.NotNull(offsetsTextBox);

        target = dialog.Find<ComboBox>("InsertObjects_target");
        Assert.NotNull(target);
        insert = dialog.Find<Button>("InsertObjects_Insert");
        Assert.NotNull(insert);
        cancel = dialog.Find<Button>("InsertObjects_Cancel");
        Assert.NotNull(cancel);
    }

    [AvaloniaFact]
    public void CollisionCheckImportedOnly()
    {
        Assert.Single(dc.MergeStatus[0].Offsets);

        // set two offset numbers
        offsetsTextBox!.Text = "0 1";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, dc.MergeStatus[0].Offsets.Count);

        var numberOfOffsets = dc.MergeStatus[0].Offsets.Count;
        for (int i = 0; i < numberOfOffsets; i++)
        {
            foreach (var leftRow in dc.MergeStatus)
            {
                int rightCollumIndex = leftRow.Offsets[i].Index;
                for (int j = i; j >= 0; j--)
                {
                    if (j != i)
                    {
                        foreach (var rightRow in dc.MergeStatus)
                        {
                            int leftCollumIndex = rightRow.Offsets[j].Index;
                            if (rightCollumIndex == leftCollumIndex)
                            {
                                Assert.True(leftRow.Offsets[i].Collision);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Check that profilelist menu is correct
    /// </summary>
    [AvaloniaFact]
    public void ProfileMenuList()
    {
        Dictionary<string, bool> expectedEntries = [];

        List<string> profilelist = [.. Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Profiles"))];
        string homepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".edseditor");
        homepath = Path.Combine(homepath, "profiles");

        if (Directory.Exists(homepath))
        {
            profilelist.AddRange(Directory.GetFiles(homepath));
        }

        foreach (string file in profilelist)
        {
            string ext = Path.GetExtension(file).ToLower();
            if (ext == ".xpd" || ext == ".xdd")
            {
                var fileName = Path.GetFileName(file);
                expectedEntries[fileName] = false;
            }
        }

        foreach (var entry in expectedEntries)
        {
            var profileEntry = profileMenu!.Items.OfType<MenuItem>().Where(x => x.Header!.ToString() == entry.Key);
            Assert.Single(profileEntry);
        }
    }

    [AvaloniaFact]
    public void OffsettTextContainingNotValid()
    {
        Assert.Single(dc.MergeStatus[0].Offsets);

        // check that the system handles 
        // set two offset numbers
        offsetsTextBox!.Text = "something that is not a number";
        Dispatcher.UIThread.RunJobs();
        Assert.Single(dc.MergeStatus[0].Offsets);
        Assert.Equal(0, dc.MergeStatus[0].Offsets[0].Index - dc.MergeStatus[0].OriginalIndex);

        offsetsTextBox!.Text = "10 and something else and then a number: 20";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, dc.MergeStatus[0].Offsets.Count);
        Assert.Equal(16, dc.MergeStatus[0].Offsets[0].Index - dc.MergeStatus[0].OriginalIndex);
        Assert.Equal(32, dc.MergeStatus[0].Offsets[1].Index - dc.MergeStatus[0].OriginalIndex);

        offsetsTextBox!.Text = "10 and something else and then a number: 20";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, dc.MergeStatus[0].Offsets.Count);
        Assert.Equal(16, dc.MergeStatus[0].Offsets[0].Index - dc.MergeStatus[0].OriginalIndex);
        Assert.Equal(32, dc.MergeStatus[0].Offsets[1].Index - dc.MergeStatus[0].OriginalIndex);

        //Testing that very big numbers will be interpreted as 0
        offsetsTextBox!.Text = "99999999999999999999";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, dc.MergeStatus[0].Offsets[0].Index - dc.MergeStatus[0].OriginalIndex);
    }

    [AvaloniaFact]
    public void ImportWithoutConflict()
    {
        Assert.Single(dc.MergeStatus[0].Offsets);
        Dispatcher.UIThread.RunJobs();
        foreach (var ms in dc.MergeStatus) ms.Insert = true;
        insert!.Focus();

        var copyOfMergeStatus = new List<ODIndexMergeStatus>(dc.MergeStatus);
        // press enter to import.
        insert!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        // check that its no longer using memory
        Assert.Empty(dc.MergeStatus);

        foreach (var index in copyOfMergeStatus)
        {
            foreach (var offset in index.Offsets)
            {
                int expectedIndex = offset.Index;
                var resultDec = dc.SelectedDevice!.Objects.ContainsKey(expectedIndex.ToString());
                var resultHex = dc.SelectedDevice!.Objects.ContainsKey(expectedIndex.ToString("X4"));
                Assert.True(resultDec || resultHex);
            }
        }
    }

    [AvaloniaFact]
    public void ImportWithConflict()
    {
        offsetsTextBox!.Text = "0 2 1";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(3, dc.MergeStatus[0].Offsets.Count());
        var obj1000 = dc.MergeStatus.FirstOrDefault(ms => ms.OriginalIndex == 0x1000);
        if (obj1000 != null) obj1000.Insert = true;
        
        var obj1002 = dc.MergeStatus.FirstOrDefault(ms => ms.OriginalIndex == 0x1002);
        if (obj1002 != null) obj1002.Insert = true;

        insert!.Focus();

        // press enter to import.
        insert!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        
        // Handle collision dialog by clicking Overwrite (and apply to all)
        var applyToAll = window.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault(c => c.Name == "ApplyToAllCheckBox");
        Assert.NotNull(applyToAll);
        applyToAll.IsChecked = true;
        
        var overwriteBtn = window.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Tag as string == "Overwrite");
        Assert.NotNull(overwriteBtn);
        overwriteBtn.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        
        //window.CaptureRenderedFrame()!.Save("file.png");

        // check that its no longer using memory
        for(int i=0; i<50 && dc.MergeStatus.Count > 0; i++)
        {
            System.Threading.Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
        Assert.Empty(dc.MergeStatus);

        //1002 Check that 1002 is merged from the first offsett without collision
        var index1002 = dc.SelectedDevice!.Objects["1002"];
        Assert.Equal("Manufacturer status register", index1002.Name);

        //1004 Check that 1004 is merged from the first offsett without collision
        var index1004 = dc.SelectedDevice!.Objects["1004"];
        Assert.Equal("Manufacturer status register", index1004.Name);
    }

    [AvaloniaFact]
    public void NotImportingUnselectedEntries()
    {
        Dispatcher.UIThread.RunJobs();
        foreach (var ms in dc.MergeStatus) ms.Insert = true;
        dc.MergeStatus[0].Insert = false;
        Dispatcher.UIThread.RunJobs();
        insert!.Focus();

        // press enter to import.
        insert!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        // Check that 1000 is not merged as it is not selected for insertion
        Assert.False(dc.SelectedDevice!.Objects.TryGetValue("1000", out var index1001));
    }

    [AvaloniaFact]
    public void CancelImport()
    {
        Dispatcher.UIThread.RunJobs();
        foreach (var ms in dc.MergeStatus) ms.Insert = true;
        dc.MergeStatus[0].Insert = false;
        Dispatcher.UIThread.RunJobs();
        cancel!.Focus();

        // press enter to cansel import.
        cancel!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        // check that its no longer using memory
        Assert.Empty(dc.MergeStatus);
    }

    public void Dispose()
    {
        window.Close();
    }
}
