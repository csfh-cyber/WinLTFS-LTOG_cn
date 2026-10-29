using System.Collections.ObjectModel;
using LTOG;
using LTOG.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

int assertions = 0;
void Check(bool ok, string message)
{
    if (!ok) throw new Exception(message);
    assertions++;
}
void Transition(string scenario, MountPhase initial, (MountPhase Phase, bool Running, int? Exit, MountCompletion Expected)[] steps)
{
    var state = new MountCompletionState(initial);
    foreach (var step in steps)
        Check(state.Observe(step.Phase, step.Running, step.Exit) == step.Expected, scenario);
}
Transition("successful mount, refresh and unmount", MountPhase.Idle,
[
    (MountPhase.Mounting, true, null, MountCompletion.None),
    (MountPhase.Mounted, true, null, MountCompletion.Mounted),
    (MountPhase.Mounted, true, null, MountCompletion.None),
    (MountPhase.Unmounting, true, null, MountCompletion.None),
    (MountPhase.Idle, false, 0, MountCompletion.Unmounted),
    (MountPhase.Idle, false, 0, MountCompletion.None),
]);
Transition("failed mount", MountPhase.Idle,
[
    (MountPhase.Mounting, true, null, MountCompletion.None),
    (MountPhase.Idle, false, 1, MountCompletion.None),
]);
Transition("cancelled mount is not a successful unmount", MountPhase.Mounting,
[
    (MountPhase.Unmounting, true, null, MountCompletion.None),
    (MountPhase.Idle, false, 0, MountCompletion.None),
]);
foreach (int? exit in new int?[] { null, 1, -1 })
    Transition("unmount error or unknown result", MountPhase.Mounted,
    [ (MountPhase.Unmounting, true, null, MountCompletion.None), (MountPhase.Idle, false, exit, MountCompletion.None) ]);
Transition("unexpected process exit", MountPhase.Mounted,
    [ (MountPhase.Idle, false, 1, MountCompletion.None) ]);
Transition("adopted mount has no startup success popup", MountPhase.Idle,
    [ (MountPhase.Mounted, true, null, MountCompletion.None) ]);
Transition("already exited at mount completion", MountPhase.Mounting,
    [ (MountPhase.Mounted, false, 1, MountCompletion.None) ]);

// Actual observer, with controllable drive/process boundaries and a fake modal surface.
var root = new FrameworkElement();
var drives = new ObservableCollection<DriveViewModel>();
using var notifications = new MountCompletionNotifications(root, drives);
var vm = new DriveViewModel();
drives.Add(vm);
vm.Mapping = new Mapping { ReadOnly = true };
vm.Phase = MountPhase.Mounting;
vm.Phase = MountPhase.Mounted;
Check(ContentDialog.Shown.Count == 1, "mount popup missing");
Check(ContentDialog.Shown[0].Content.Contains("T:") && ContentDialog.Shown[0].Content.Contains("只读"), "missing letter/mode");
vm.Refresh();
Check(ContentDialog.Shown.Count == 1, "refresh duplicated popup");
vm.Phase = MountPhase.Unmounting;
vm.Mapping.Proc!.HasExited = true;
vm.Mapping = null; // matches DriveStore.Release ordering
vm.Phase = MountPhase.Idle;
Check(ContentDialog.Shown.Count == 1, "overlapping dialogs");
ContentDialog.Shown[0].Hide();
await WaitForCount(2);
Check(ContentDialog.Shown[1].Title == "磁带卸载完成", "lost mapping before release notification");
ContentDialog.Shown[1].Hide();
await Task.Delay(20);

// A utility confirmation and notifications share the same queue.
var utility = new ContentDialog { XamlRoot = root.XamlRoot, Title = "格式化确认" };
var utilityTask = UiDialogs.ShowAsync(utility);
var vm2 = new DriveViewModel();
vm2.Drive.Device = "TAPE1";
drives.Add(vm2);
vm2.Mapping = new Mapping { Letter = "U:" };
vm2.Phase = MountPhase.Mounting;
vm2.Phase = MountPhase.Mounted;
Check(ContentDialog.Shown.Count == 3, "notification overlaps utility dialog");
utility.Hide();
await utilityTask;
await WaitForCount(4);
Check(ContentDialog.Shown[3].Content.Contains("TAPE1") && ContentDialog.Shown[3].Content.Contains("U:"), "wrong drive in queued dialog");

// Closing cancels an active notification and any queued ones; removed drives detach.
vm.Mapping = new Mapping();
vm.Phase = MountPhase.Mounting;
vm.Phase = MountPhase.Mounted;
notifications.Dispose();
await Task.Delay(30);
Check(ContentDialog.Shown.Count == 4, "queued notification survived close");
var removed = new DriveViewModel();
using (var other = new MountCompletionNotifications(new FrameworkElement(), drives))
{
    drives.Add(removed);
    drives.Remove(removed);
    removed.Mapping = new Mapping();
    removed.Phase = MountPhase.Mounting;
    removed.Phase = MountPhase.Mounted;
    Check(ContentDialog.Shown.Count == 4, "removed drive still subscribed");
}

Check(SchemaDisplay.Policy("Not permitted") == "不允许", "policy display");
Check(SchemaDisplay.Location("Partition a starting from block 12") == "分区 a，起始块 12", "location display");
Check(SchemaDisplay.Location("Not specified") == "未指定", "missing location display");
Check(SchemaDisplay.Time("2026-10-29 at 12:30:00 (UTC)") == "2026-10-29 12:30:00 (UTC)", "timestamp display");
Console.WriteLine($"Passed {assertions} UI notification and display checks (boundary fakes; no native rendering or tape I/O).");

static async Task WaitForCount(int count)
{
    for (int i = 0; i < 100 && ContentDialog.Shown.Count < count; i++) await Task.Delay(10);
    if (ContentDialog.Shown.Count != count) throw new Exception($"Expected {count} dialogs, got {ContentDialog.Shown.Count}");
}
