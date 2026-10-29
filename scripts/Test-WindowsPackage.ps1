<#
Validates a freshly built installer on an isolated Windows CI runner without tape hardware.
Installs to a temporary directory, omits the driver task, and checks GUI startup.
The installer still bundles the original checksum-verified WinFsp driver MSI.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$InstallerPath,
    [Parameter(Mandatory)][string]$LogDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Package validation requires Windows.' }
if (Get-Process LTOG -ErrorAction SilentlyContinue) { throw 'Close existing LTOG instances before validation.' }
if (Get-CimInstance Win32_TapeDrive) { throw 'Use an isolated build machine without connected tape hardware.' }

$installer = (Resolve-Path -LiteralPath $InstallerPath).Path
New-Item -ItemType Directory -Force $LogDirectory | Out-Null
$logs = (Resolve-Path -LiteralPath $LogDirectory).Path
$installDir = Join-Path ([IO.Path]::GetTempPath()) ('LTOG-cn-verify-' + [guid]::NewGuid().ToString('N'))
$installLog = Join-Path $logs 'install-windows.log'
$setupArguments = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-',
    '/TASKS="!deps_winfsp,!desktopicon"', "/DIR=`"$installDir`"", "/LOG=`"$installLog`"")
$setup = Start-Process -FilePath $installer -ArgumentList $setupArguments -Wait -PassThru
if ($setup.ExitCode -ne 0) { throw "Installer exited with code $($setup.ExitCode). See $installLog" }

foreach ($relative in @('LTOG.exe', 'LTOG.dll', 'coreclr.dll', 'Microsoft.UI.Xaml.dll',
    'winltfs/ltfs.exe', 'winltfs/mkltfs.exe', 'winltfs/unltfs.exe', 'winltfs/ltfsck.exe',
    'winltfs/winfsp-x64.dll', 'winltfs/ltfs.conf', 'LICENSE.txt', 'THIRD-PARTY-NOTICES.md')) {
    if (-not (Test-Path -LiteralPath (Join-Path $installDir $relative))) {
        throw "Installed payload is missing $relative"
    }
}

$app = $null
$startupLog = Join-Path $logs 'startup-windows.log'
try {
    $app = Start-Process -FilePath (Join-Path $installDir 'LTOG.exe') -WorkingDirectory $installDir -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(60)
    do {
        Start-Sleep -Milliseconds 500
        $app.Refresh()
        if ($app.HasExited) { throw "LTOG exited before opening its window (exit $($app.ExitCode))." }
    } while ($app.MainWindowHandle -eq [IntPtr]::Zero -and [DateTime]::UtcNow -lt $deadline)
    if ($app.MainWindowHandle -eq [IntPtr]::Zero) { throw 'LTOG did not create a visible main window within 60 seconds.' }
    Start-Sleep -Seconds 5
    $app.Refresh()
    if ($app.HasExited) { throw "LTOG exited after startup (exit $($app.ExitCode))." }
    "PASS: installer payload verified; LTOG main window opened and remained running. No tape hardware tested." |
        Tee-Object -FilePath $startupLog
} catch {
    $_ | Out-String | Add-Content $startupLog
    $crashLog = Join-Path $env:LOCALAPPDATA 'LTOG/crash.log'
    if (Test-Path $crashLog) { Copy-Item $crashLog (Join-Path $logs 'app-crash.log') }
    throw
} finally {
    if ($null -ne $app -and -not $app.HasExited) {
        $null = $app.CloseMainWindow()
        if (-not $app.WaitForExit(10000)) { Stop-Process -Id $app.Id -Force }
    }
}
