using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using LTOG.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LTOG;

/// <summary>Observes the existing drive store from the window, including when another page is selected.</summary>
internal sealed class MountCompletionNotifications : IDisposable
{
    private sealed class Observation(DriveViewModel vm)
    {
        public MountCompletionState State { get; } = new(vm.Phase);
        public Mapping? Mapping { get; set; } = vm.Mapping;
    }

    private readonly FrameworkElement _root;
    private readonly ObservableCollection<DriveViewModel> _drives;
    private readonly Dictionary<DriveViewModel, Observation> _observations = new();
    private readonly CancellationTokenSource _closed = new();

    public MountCompletionNotifications(FrameworkElement root, ObservableCollection<DriveViewModel> drives)
    {
        _root = root;
        _drives = drives;
        _drives.CollectionChanged += DrivesChanged;
        SyncDrives();
    }

    private void DrivesChanged(object? sender, NotifyCollectionChangedEventArgs e) => SyncDrives();

    private void SyncDrives()
    {
        foreach (var vm in _observations.Keys.Where(vm => !_drives.Contains(vm)).ToList())
        {
            vm.PropertyChanged -= DriveChanged;
            _observations.Remove(vm);
        }
        foreach (var vm in _drives.Where(vm => !_observations.ContainsKey(vm)))
        {
            _observations.Add(vm, new Observation(vm));
            vm.PropertyChanged += DriveChanged;
        }
    }

    private void DriveChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not DriveViewModel vm || !_observations.TryGetValue(vm, out var observation)) return;
        // Release clears vm.Mapping before publishing Idle; retain it to check the actual exit result.
        observation.Mapping = vm.Mapping ?? observation.Mapping;
        var mapping = observation.Mapping;
        bool running = false;
        int? exitCode = null;
        try
        {
            if (mapping?.Proc is { } process)
            {
                running = !process.HasExited;
                if (!running) exitCode = process.ExitCode;
            }
        }
        catch (InvalidOperationException) { /* unavailable process: never claim success */ }
        catch (System.ComponentModel.Win32Exception) { }

        var completion = observation.State.Observe(vm.Phase, running, exitCode);
        if (vm.Phase == MountPhase.Idle) observation.Mapping = null;
        if (completion != MountCompletion.None && mapping != null)
            _ = NotifyAsync(completion, vm.Drive.Device, mapping.Letter, mapping.ReadOnly);
    }

    private async Task NotifyAsync(MountCompletion completion, string device, string letter, bool readOnly)
    {
        try
        {
            if (_closed.IsCancellationRequested || _root.XamlRoot == null) return;
            bool mounted = completion == MountCompletion.Mounted;
            await UiDialogs.ShowAsync(new ContentDialog
            {
                XamlRoot = _root.XamlRoot,
                Title = mounted ? "磁带挂载完成" : "磁带卸载完成",
                Content = mounted
                    ? $"{device} 中的磁带已挂载到 {letter}（{(readOnly ? "只读" : "读写")}）。\n可在文件资源管理器中访问该盘符。"
                    : $"{device} 的磁带盘 {letter} 已完成卸载。",
                CloseButtonText = "确定",
                DefaultButton = ContentDialogButton.Close,
            }, _closed.Token);
        }
        catch (OperationCanceledException) { /* window closed while queued */ }
        catch (Exception ex) { Debug.WriteLine($"Completion dialog could not be shown: {ex}"); }
    }

    public void Dispose()
    {
        _closed.Cancel();
        _drives.CollectionChanged -= DrivesChanged;
        foreach (var vm in _observations.Keys) vm.PropertyChanged -= DriveChanged;
        _observations.Clear();
    }
}
