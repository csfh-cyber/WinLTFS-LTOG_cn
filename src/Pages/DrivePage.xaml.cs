using LTOG.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LTOG;

/// <summary>One drive's tab: header with mount controls, cartridge and drive dashboard, cartridge tools.</summary>
public sealed partial class DrivePage : UserControl
{
    public DriveViewModel ViewModel { get; }

    public DrivePage(DriveViewModel vm)
    {
        ViewModel = vm;
        InitializeComponent();
    }

    private void ReadOnly_Click(object sender, RoutedEventArgs e) =>
        ViewModel.ReadOnlyChecked = (sender as CheckBox)?.IsChecked == true;

    private async void MountButton_Click(object sender, RoutedEventArgs e)
    {
        switch (ViewModel.Phase)
        {
            case MountPhase.Idle when ViewModel.SelectedLetter == null:
                await UiDialogs.ShowAsync(new ContentDialog
                {
                    Title = "LTOG", Content = "请先选择一个可用盘符。",
                    CloseButtonText = "确定", XamlRoot = XamlRoot,
                });
                break;
            case MountPhase.Idle:
                await App.DriveStore.MountAsync(ViewModel);
                break;
            case MountPhase.Mounting:   // Cancel
            case MountPhase.Mounted:    // Unmount
                App.DriveStore.Unmount(ViewModel);
                break;
        }
    }

    private async void Eject_Click(object sender, RoutedEventArgs e) =>
        await App.DriveStore.EjectAsync(ViewModel);

    private async void Format_Click(object sender, RoutedEventArgs e)
    {
        if (!App.DriveStore.CanUseUtilities(ViewModel)) return;
        string dev = ViewModel.Drive.Device;

        var serialBox = new TextBox { Header = "磁带序列号（6 个字符）", MaxLength = 6 };
        var nameBox = new TextBox { Header = "卷标", PlaceholderText = "LTFS VOLUME" };
        var forceBox = new CheckBox { Content = "强制格式化（覆盖现有 LTFS 卷）" };
        var dialog = new ContentDialog
        {
            Title = $"格式化 {dev} 中的磁带",
            Content = new StackPanel
            {
                Spacing = 12,
                Children = { new TextBlock
                    { Text = "此操作将销毁磁带上的全部数据。", TextWrapping = TextWrapping.Wrap },
                    serialBox, nameBox, forceBox },
            },
            PrimaryButtonText = "格式化",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };
        if (await UiDialogs.ShowAsync(dialog) != ContentDialogResult.Primary) return;

        var args = new List<string> { "-i", LtfsEnv.LtfsConf, "-d", dev };
        if (!string.IsNullOrWhiteSpace(serialBox.Text)) { args.Add("-s"); args.Add(serialBox.Text.Trim()); }
        if (!string.IsNullOrWhiteSpace(nameBox.Text)) { args.Add("-n"); args.Add(nameBox.Text.Trim()); }
        if (forceBox.IsChecked == true) args.Add("-f");
        await App.DriveStore.RunToolAsync(ViewModel, LtfsEnv.MkltfsExe, args, $"Format cartridge — {dev}");
    }

    private async void Unformat_Click(object sender, RoutedEventArgs e)
    {
        if (!App.DriveStore.CanUseUtilities(ViewModel)) return;
        string dev = ViewModel.Drive.Device;

        var dialog = new ContentDialog
        {
            Title = $"取消 {dev} 中磁带的格式化",
            Content = "此操作将移除磁带的 LTFS 格式及全部数据，" +
                      "并恢复为单个未格式化分区。是否继续？",
            PrimaryButtonText = "取消格式化",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };
        if (await UiDialogs.ShowAsync(dialog) != ContentDialogResult.Primary) return;

        await App.DriveStore.RunToolAsync(ViewModel, LtfsEnv.UnltfsExe,
            new[] { "-i", LtfsEnv.LtfsConf, "-d", dev, "-y" }, $"Unformat cartridge — {dev}");
    }

    private async void Check_Click(object sender, RoutedEventArgs e)
    {
        if (!App.DriveStore.CanUseUtilities(ViewModel)) return;
        string dev = ViewModel.Drive.Device;
        await App.DriveStore.RunToolAsync(ViewModel, LtfsEnv.LtfsckExe,
            new[] { "-i", LtfsEnv.LtfsConf, dev }, $"Check filesystem — {dev}");
    }

    // ---- raw MAM table: copy rows as tab-separated text, header first

    private ListView? _mamMenuTarget;   // the table a context menu was opened on

    private static void CopyMamRows(IEnumerable<MamRow> rows)
    {
        var lines = rows.Select(r => string.Join('\t', r.Id, r.Name, r.Partition, r.Size, r.Value))
            .Prepend("ID\t属性\t分区\t大小\t值");
        var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dp.SetText(string.Join("\r\n", lines));
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
    }

    private void MamCopy_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender,
        Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs e)
    {
        if (e.Element is not ListView lv) return;
        CopyMamRows(lv.SelectedItems.Cast<MamRow>());
        e.Handled = true;
    }

    private void MamMenu_Opening(object sender, object e) =>
        _mamMenuTarget = (sender as MenuFlyout)?.Target as ListView;

    private void MamCopySelected_Click(object sender, RoutedEventArgs e)
    {
        if (_mamMenuTarget is { } lv) CopyMamRows(lv.SelectedItems.Cast<MamRow>());
    }

    private void MamCopyAll_Click(object sender, RoutedEventArgs e)
    {
        if (_mamMenuTarget is { } lv) CopyMamRows(lv.Items.Cast<MamRow>());
    }
}
