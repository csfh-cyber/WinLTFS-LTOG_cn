using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;

namespace LTOG;

internal sealed class TapeCloseProtection(InfoBar notice, IReadOnlyCollection<DriveViewModel> drives)
{
    public void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (!drives.Any(drive => drive.Phase != MountPhase.Idle)) return;

        args.Cancel = true;
        notice.IsOpen = true;
    }
}
