// Small boundary fakes allow UI notification tests to run without Windows or tape hardware.
// These do not emulate WinUI rendering, the engine or device I/O.
using System.ComponentModel;

namespace LTOG
{
    public enum MountPhase { Idle, Mounting, Mounted, Unmounting }
    public sealed class DriveViewModel
    {
        private MountPhase _phase;
        public MountPhase Phase
        {
            get => _phase;
            set { _phase = value; Refresh(); }
        }
        public Core.Mapping? Mapping { get; set; }
        public TapeDrive Drive { get; } = new();
        public event PropertyChangedEventHandler? PropertyChanged;
        public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
    }
    public sealed class TapeDrive { public string Device { get; set; } = "TAPE0"; }
}
namespace LTOG.Core
{
    public sealed class Mapping
    {
        public string Letter { get; set; } = "T:";
        public bool ReadOnly { get; set; }
        public FakeProcess? Proc { get; set; } = new();
    }
    public sealed class FakeProcess
    {
        public bool HasExited { get; set; }
        public int ExitCode { get; set; }
    }
}
namespace Microsoft.UI.Xaml
{
    public sealed class XamlRoot { }
    public sealed class FrameworkElement { public XamlRoot XamlRoot { get; set; } = new(); }
}
namespace Microsoft.UI.Xaml.Controls
{
    public enum ContentDialogResult { None, Primary }
    public enum ContentDialogButton { Close }
    public sealed class FakeDispatcher
    {
        public bool TryEnqueue(Action action) { action(); return true; }
    }
    public sealed class ContentDialog
    {
        public static List<ContentDialog> Shown { get; } = new();
        private static readonly HashSet<XamlRoot> Active = new();
        private readonly TaskCompletionSource<ContentDialogResult> _dismissed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public XamlRoot XamlRoot { get; set; } = null!;
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string CloseButtonText { get; set; } = "";
        public ContentDialogButton DefaultButton { get; set; }
        public FakeDispatcher DispatcherQueue { get; } = new();
        public async Task<ContentDialogResult> ShowAsync()
        {
            if (!Active.Add(XamlRoot)) throw new InvalidOperationException("Two dialogs on one XamlRoot");
            Shown.Add(this);
            try { return await _dismissed.Task; }
            finally { Active.Remove(XamlRoot); }
        }
        public void Hide() => _dismissed.TrySetResult(ContentDialogResult.None);
    }
}
