using System.Diagnostics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LTOG;

internal sealed class TapeSafetyPrompts(FrameworkElement root, IReadOnlyCollection<DriveViewModel> drives) : IDisposable
{
    private readonly CancellationTokenSource _closed = new();
    private bool _startupShown;
    private bool _closeReminderPending;

    public Task ShowStartupAsync()
    {
        if (_startupShown || _closed.IsCancellationRequested || root.XamlRoot is not { } xamlRoot)
            return Task.CompletedTask;

        _startupShown = true;
        return ShowAsync(xamlRoot, "磁带文件操作提醒",
            "向磁带盘写入文件时，请使用“复制”，避免直接移动或剪切文件。\n\n" +
            "复制完成后，请正常卸载磁带并核验数据，再决定是否删除源文件。",
            "知道了");
    }

    public void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (!HasActiveMounts()) return;

        // Cancel the system close request before waiting for any dialog.
        args.Cancel = true;
        if (_closeReminderPending || _closed.IsCancellationRequested || root.XamlRoot is not { } xamlRoot)
            return;

        _closeReminderPending = true;
        _ = ShowCloseReminderAsync(xamlRoot);
    }

    private bool HasActiveMounts() => drives.Any(drive => drive.Phase != MountPhase.Idle);

    private async Task ShowCloseReminderAsync(XamlRoot xamlRoot)
    {
        try
        {
            await ShowAsync(xamlRoot, "请先卸载磁带",
                "请返回“磁带机”页面，等待当前挂载或卸载操作完成，并卸载所有已挂载的磁带。\n\n" +
                "全部卸载后，再关闭 LTOG。", "返回 LTOG", HasActiveMounts);
        }
        finally { _closeReminderPending = false; }
    }

    private async Task ShowAsync(XamlRoot xamlRoot, string title, string message, string button,
        Func<bool>? shouldShow = null)
    {
        try
        {
            await UiDialogs.ShowAsync(new ContentDialog
            {
                XamlRoot = xamlRoot,
                Title = title,
                Content = message,
                CloseButtonText = button,
                DefaultButton = ContentDialogButton.Close,
            }, _closed.Token, shouldShow);
        }
        catch (OperationCanceledException) when (_closed.IsCancellationRequested) { }
        catch (Exception ex) { Debug.WriteLine($"Tape safety dialog could not be shown: {ex}"); }
    }

    public void Dispose()
    {
        if (_closed.IsCancellationRequested) return;
        _closed.Cancel();
        _closed.Dispose();
    }
}
