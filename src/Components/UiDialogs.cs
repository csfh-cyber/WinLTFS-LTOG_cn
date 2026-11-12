using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LTOG;

/// <summary>WinUI allows one ContentDialog per window. Share the queue with utility confirmations.</summary>
internal static class UiDialogs
{
    private static readonly ConditionalWeakTable<XamlRoot, SemaphoreSlim> Gates = new();

    public static async Task<ContentDialogResult> ShowAsync(ContentDialog dialog, CancellationToken cancellationToken = default)
    {
        var gate = Gates.GetValue(dialog.XamlRoot, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var registration = cancellationToken.Register(() =>
                dialog.DispatcherQueue.TryEnqueue(() => dialog.Hide()));
            return await dialog.ShowAsync();
        }
        finally { gate.Release(); }
    }
}
