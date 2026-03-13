<#
.SYNOPSIS
    Windows 11 audio device manager - enable/disable microphones and speakers, set volumes.

.DESCRIPTION
    Controls audio devices (microphones, speakers, headsets) via command-line parameters.
    Uses Windows Core Audio API for volume control and PnP Device API for enable/disable.
    Requires elevation for enable/disable operations.

.PARAMETER List
    List all audio devices with their status and current volume.

.PARAMETER Enable
    Enable an audio device by name (partial match) or Device ID.

.PARAMETER Disable
    Disable an audio device by name (partial match) or Device ID.

.PARAMETER SetVolume
    Set the volume (0-100) for an audio device by name (partial match) or Device ID.
    Requires -Volume parameter.

.PARAMETER Volume
    Volume level 0-100. Used with -SetVolume.

.PARAMETER Mute
    Mute an audio device by name (partial match) or Device ID.

.PARAMETER Unmute
    Unmute an audio device by name (partial match) or Device ID.

.PARAMETER Type
    Filter by device type: 'Playback', 'Recording', or 'All' (default: All).

.EXAMPLE
    .\AudioControl.ps1 -List
    .\AudioControl.ps1 -List -Type Playback
    .\AudioControl.ps1 -SetVolume "Speakers" -Volume 75
    .\AudioControl.ps1 -SetVolume "Microphone" -Volume 50
    .\AudioControl.ps1 -Mute "Microphone Array"
    .\AudioControl.ps1 -Unmute "Realtek"
    .\AudioControl.ps1 -Disable "Microphone" -Confirm
    .\AudioControl.ps1 -Enable "Headset"
#>

[CmdletBinding(DefaultParameterSetName = 'List', SupportsShouldProcess)]
param(
    [Parameter(ParameterSetName = 'List')]
    [switch]$List,

    [Parameter(ParameterSetName = 'Enable', Mandatory)]
    [string]$Enable,

    [Parameter(ParameterSetName = 'Disable', Mandatory)]
    [string]$Disable,

    [Parameter(ParameterSetName = 'SetVolume', Mandatory)]
    [string]$SetVolume,

    [Parameter(ParameterSetName = 'SetVolume', Mandatory)]
    [ValidateRange(0, 100)]
    [int]$Volume,

    [Parameter(ParameterSetName = 'Mute', Mandatory)]
    [string]$Mute,

    [Parameter(ParameterSetName = 'Unmute', Mandatory)]
    [string]$Unmute,

    [ValidateSet('Playback', 'Recording', 'All')]
    [string]$Type = 'All'
)

#region Core Audio API via C#
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AudioControlV2
{
    // EDataFlow
    public enum DataFlow { Render = 0, Capture = 1, All = 2 }
    // ERole
    public enum Role { Console = 0, Multimedia = 1, Communications = 2 }
    // Device state flags
    [Flags]
    public enum DeviceState
    {
        Active     = 0x1,
        Disabled   = 0x2,
        NotPresent = 0x4,
        Unplugged  = 0x8,
        All        = 0xF
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(DataFlow dataFlow, DeviceState stateMask, out IMMDeviceCollection devices);
        int GetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out IMMDevice device);
        int GetDevice(string pwstrId, out IMMDevice device);
        int RegisterEndpointNotificationCallback(IntPtr pClient);
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceCollection
    {
        int GetCount(out uint count);
        int Item(uint index, out IMMDevice device);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, out IAudioEndpointVolume ppInterface);
        int OpenPropertyStore(int stgmAccess, out IPropertyStore propertyStore);
        int GetId(out string strId);
        int GetState(out DeviceState state);
    }

    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore
    {
        int GetCount(out uint count);
        int GetAt(uint index, out PropertyKey key);
        int GetValue(ref PropertyKey key, out PropVariant value);
        int SetValue(ref PropertyKey key, ref PropVariant value);
        int Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PropertyKey
    {
        public Guid fmtid;
        public uint pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PropVariant
    {
        public ushort vt;
        public ushort reserved1, reserved2, reserved3;
        public IntPtr data1;
        public IntPtr data2;
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr pNotify);
        int UnregisterControlChangeNotify(IntPtr pNotify);
        int GetChannelCount(out uint count);
        int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        int GetMasterVolumeLevel(out float fLevelDB);
        int GetMasterVolumeLevelScalar(out float fLevel);
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
        int GetChannelVolumeLevel(uint nChannel, out float fLevelDB);
        int GetChannelVolumeLevelScalar(uint nChannel, out float fLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool bMute);
        int GetVolumeStepInfo(out uint nStep, out uint nStepCount);
        int VolumeStepUp(ref Guid pguidEventContext);
        int VolumeStepDown(ref Guid pguidEventContext);
        int QueryHardwareSupport(out uint pdwHardwareSupportMask);
        int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    [ClassInterface(ClassInterfaceType.None)]
    public class MMDeviceEnumeratorClass {}

    public class AudioDeviceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DataFlow Flow { get; set; }
        public DeviceState State { get; set; }
        public float VolumeScalar { get; set; }
        public int VolumePercent { get { return (int)Math.Round(VolumeScalar * 100); } }
        public bool IsMuted { get; set; }
    }

    public static class CoreAudioHelper
    {
        private static readonly Guid CLSID_MMDeviceEnumerator = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
        private static readonly Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");

        // PKEY_Device_FriendlyName
        private static readonly PropertyKey FriendlyNameKey = new PropertyKey
        {
            fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
            pid = 14
        };

        public static IMMDeviceEnumerator CreateEnumerator()
        {
            var type = Type.GetTypeFromCLSID(CLSID_MMDeviceEnumerator, true);
            return (IMMDeviceEnumerator)Activator.CreateInstance(type);
        }

        public static string GetDeviceFriendlyName(IMMDevice device)
        {
            IPropertyStore store;
            device.OpenPropertyStore(0 /*STGM_READ*/, out store);
            var key = FriendlyNameKey;
            PropVariant pv;
            store.GetValue(ref key, out pv);
            try
            {
                if (pv.vt == 31 /*VT_LPWSTR*/)
                    return Marshal.PtrToStringUni(pv.data1) ?? string.Empty;
                return string.Empty;
            }
            finally
            {
                // Free LPWSTR if needed
                if (pv.vt == 31 && pv.data1 != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pv.data1);
            }
        }

        public static IAudioEndpointVolume GetEndpointVolume(IMMDevice device)
        {
            IAudioEndpointVolume vol;
            var iid = IID_IAudioEndpointVolume;
            int hr = device.Activate(ref iid, 23 /*CLSCTX_ALL*/, IntPtr.Zero, out vol);
            if (hr != 0) return null;
            return vol;
        }

        public static List<AudioDeviceInfo> EnumerateDevices(DataFlow flow)
        {
            var result = new List<AudioDeviceInfo>();
            var enumerator = CreateEnumerator();

            IMMDeviceCollection col;
            enumerator.EnumAudioEndpoints(flow, DeviceState.All, out col);

            uint count;
            col.GetCount(out count);

            for (uint i = 0; i < count; i++)
            {
                IMMDevice device;
                col.Item(i, out device);

                string id;
                device.GetId(out id);

                DeviceState state;
                device.GetState(out state);

                string name;
                try { name = GetDeviceFriendlyName(device); }
                catch { name = "(unknown)"; }

                float volScalar = 0f;
                bool muted = false;

                if (state == DeviceState.Active)
                {
                    var vol = GetEndpointVolume(device);
                    if (vol != null)
                    {
                        vol.GetMasterVolumeLevelScalar(out volScalar);
                        vol.GetMute(out muted);
                        Marshal.ReleaseComObject(vol);
                    }
                }

                result.Add(new AudioDeviceInfo
                {
                    Id = id,
                    Name = name,
                    Flow = flow == DataFlow.All ? DataFlow.All : flow,
                    State = state,
                    VolumeScalar = volScalar,
                    IsMuted = muted
                });

                Marshal.ReleaseComObject(device);
            }

            Marshal.ReleaseComObject(col);
            Marshal.ReleaseComObject(enumerator);
            return result;
        }

        public static bool SetVolume(string deviceId, float scalar)
        {
            var enumerator = CreateEnumerator();
            IMMDevice device;
            int hr = enumerator.GetDevice(deviceId, out device);
            Marshal.ReleaseComObject(enumerator);
            if (hr != 0) return false;

            var vol = GetEndpointVolume(device);
            Marshal.ReleaseComObject(device);
            if (vol == null) return false;

            var ctx = Guid.NewGuid();
            vol.SetMasterVolumeLevelScalar(scalar, ref ctx);
            Marshal.ReleaseComObject(vol);
            return true;
        }

        public static bool SetMute(string deviceId, bool mute)
        {
            var enumerator = CreateEnumerator();
            IMMDevice device;
            int hr = enumerator.GetDevice(deviceId, out device);
            Marshal.ReleaseComObject(enumerator);
            if (hr != 0) return false;

            var vol = GetEndpointVolume(device);
            Marshal.ReleaseComObject(device);
            if (vol == null) return false;

            var ctx = Guid.NewGuid();
            vol.SetMute(mute, ref ctx);
            Marshal.ReleaseComObject(vol);
            return true;
        }
    }
}
'@ -Language CSharp
#endregion

#region Helpers

function Get-AudioDevices {
    param([string]$FlowFilter = 'All')

    $devices = [System.Collections.Generic.List[object]]::new()

    $flows = switch ($FlowFilter) {
        'Playback'  { @([AudioControlV2.DataFlow]::Render) }
        'Recording' { @([AudioControlV2.DataFlow]::Capture) }
        default     { @([AudioControlV2.DataFlow]::Render, [AudioControlV2.DataFlow]::Capture) }
    }

    foreach ($flow in $flows) {
        $list = [AudioControlV2.CoreAudioHelper]::EnumerateDevices($flow)
        foreach ($d in $list) {
            $d.Flow = $flow
            $devices.Add($d)
        }
    }

    return $devices
}

function Find-Device {
    param(
        [string]$Query,
        [string]$FlowFilter = 'All'
    )

    $all = Get-AudioDevices -FlowFilter $FlowFilter
    # Try exact ID match first
    $match = $all | Where-Object { $_.Id -eq $Query }
    if (-not $match) {
        # Case-insensitive partial name match
        $match = $all | Where-Object { $_.Name -ilike "*$Query*" }
    }

    if (-not $match) {
        Write-Host "ERROR: No audio device found matching '$Query'" -ForegroundColor Red
        Write-Host "Use -List to see available devices." -ForegroundColor Yellow
        return $null
    }
    if (@($match).Count -gt 1) {
        Write-Host "WARNING: Multiple devices match '$Query':" -ForegroundColor Yellow
        foreach ($m in $match) {
            Write-Host "  [$($m.Flow)] $($m.Name) ($($m.State))" -ForegroundColor Yellow
        }
        Write-Host "Using first match. Refine your query or use the device ID for precision." -ForegroundColor Yellow
        return @($match)[0]
    }
    return $match
}

function Format-DeviceRow {
    param($Device, [string]$FlowLabel)

    $stateColor = switch ($Device.State) {
        'Active'     { 'Green' }
        'Disabled'   { 'Red' }
        'Unplugged'  { 'DarkYellow' }
        default      { 'Gray' }
    }

    $volStr = if ($Device.State -eq [AudioControlV2.DeviceState]::Active) {
        $muteStr = if ($Device.IsMuted) { ' [MUTED]' } else { '' }
        "$($Device.VolumePercent)%$muteStr"
    } else {
        '-'
    }

    [PSCustomObject]@{
        Type    = $FlowLabel
        Name    = $Device.Name
        State   = $Device.State
        Volume  = $volStr
        'Device ID' = $Device.Id
    }
}

function Assert-Elevation {
    $identity  = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [System.Security.Principal.WindowsPrincipal]$identity
    if (-not $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "ERROR: Enable/Disable operations require Administrator privileges." -ForegroundColor Red
        Write-Host "Re-run this script as Administrator (Right-click -> Run as Administrator)." -ForegroundColor Yellow
        exit 1
    }
}

#endregion

#region Commands

switch ($PSCmdlet.ParameterSetName) {

    'List' {
        Write-Host ""
        Write-Host "  Audio Devices" -ForegroundColor Cyan
        Write-Host "  $('=' * 70)" -ForegroundColor DarkGray

        $typeMap = @{
            [AudioControlV2.DataFlow]::Render  = 'Playback'
            [AudioControlV2.DataFlow]::Capture = 'Recording'
        }

        $all = Get-AudioDevices -FlowFilter $Type

        if (-not $all -or @($all).Count -eq 0) {
            Write-Host "  No audio devices found." -ForegroundColor Yellow
        } else {
            $rows = foreach ($d in $all) {
                $label = $typeMap[$d.Flow]
                Format-DeviceRow -Device $d -FlowLabel $label
            }
            $rows | Format-Table -AutoSize -Property Type, Name, State, Volume, 'Device ID'
        }
    }

    'SetVolume' {
        $device = Find-Device -Query $SetVolume -FlowFilter $Type
        if (-not $device) { exit 1 }

        if ($device.State -ne [AudioControlV2.DeviceState]::Active) {
            Write-Host "ERROR: Device '$($device.Name)' is not active (State: $($device.State))." -ForegroundColor Red
            exit 1
        }

        $scalar = $Volume / 100.0
        $ok = [AudioControlV2.CoreAudioHelper]::SetVolume($device.Id, [float]$scalar)

        if ($ok) {
            Write-Host "OK  Volume set to $Volume% on '$($device.Name)'" -ForegroundColor Green
        } else {
            Write-Host "ERROR: Failed to set volume on '$($device.Name)'." -ForegroundColor Red
            exit 1
        }
    }

    'Mute' {
        $device = Find-Device -Query $Mute -FlowFilter $Type
        if (-not $device) { exit 1 }

        if ($device.State -ne [AudioControlV2.DeviceState]::Active) {
            Write-Host "ERROR: Device '$($device.Name)' is not active." -ForegroundColor Red
            exit 1
        }

        $ok = [AudioControlV2.CoreAudioHelper]::SetMute($device.Id, $true)
        if ($ok) {
            Write-Host "OK  Muted '$($device.Name)'" -ForegroundColor Green
        } else {
            Write-Host "ERROR: Failed to mute '$($device.Name)'." -ForegroundColor Red
            exit 1
        }
    }

    'Unmute' {
        $device = Find-Device -Query $Unmute -FlowFilter $Type
        if (-not $device) { exit 1 }

        if ($device.State -ne [AudioControlV2.DeviceState]::Active) {
            Write-Host "ERROR: Device '$($device.Name)' is not active." -ForegroundColor Red
            exit 1
        }

        $ok = [AudioControlV2.CoreAudioHelper]::SetMute($device.Id, $false)
        if ($ok) {
            Write-Host "OK  Unmuted '$($device.Name)'" -ForegroundColor Green
        } else {
            Write-Host "ERROR: Failed to unmute '$($device.Name)'." -ForegroundColor Red
            exit 1
        }
    }

    'Enable' {
        Assert-Elevation

        # Find via PnP (Enable/Disable requires PnP device, not audio endpoint)
        $pnpDevices = Get-PnpDevice -Class 'AudioEndpoint', 'Media' -ErrorAction SilentlyContinue |
            Where-Object { $_.FriendlyName -ilike "*$Enable*" -or $_.DeviceID -eq $Enable }

        if (-not $pnpDevices -or @($pnpDevices).Count -eq 0) {
            # Fallback: search all PnP devices for partial name match
            $pnpDevices = Get-PnpDevice -ErrorAction SilentlyContinue |
                Where-Object {
                    ($_.FriendlyName -ilike "*$Enable*" -or $_.DeviceID -eq $Enable) -and
                    ($_.Class -in @('AudioEndpoint', 'Media', 'SoundRecording'))
                }
        }

        if (-not $pnpDevices -or @($pnpDevices).Count -eq 0) {
            Write-Host "ERROR: No audio PnP device found matching '$Enable'." -ForegroundColor Red
            Write-Host "Use -List to see available devices and check the name." -ForegroundColor Yellow
            exit 1
        }

        foreach ($pnp in @($pnpDevices)) {
            Write-Host "Enabling: $($pnp.FriendlyName) [$($pnp.Status)]..." -NoNewline
            try {
                Enable-PnpDevice -InputObject $pnp -Confirm:$false -ErrorAction Stop
                Write-Host " OK" -ForegroundColor Green
            } catch {
                Write-Host " FAILED: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }

    'Disable' {
        Assert-Elevation

        $pnpDevices = Get-PnpDevice -Class 'AudioEndpoint', 'Media' -ErrorAction SilentlyContinue |
            Where-Object { $_.FriendlyName -ilike "*$Disable*" -or $_.DeviceID -eq $Disable }

        if (-not $pnpDevices -or @($pnpDevices).Count -eq 0) {
            $pnpDevices = Get-PnpDevice -ErrorAction SilentlyContinue |
                Where-Object {
                    ($_.FriendlyName -ilike "*$Disable*" -or $_.DeviceID -eq $Disable) -and
                    ($_.Class -in @('AudioEndpoint', 'Media', 'SoundRecording'))
                }
        }

        if (-not $pnpDevices -or @($pnpDevices).Count -eq 0) {
            Write-Host "ERROR: No audio PnP device found matching '$Disable'." -ForegroundColor Red
            Write-Host "Use -List to see available devices and check the name." -ForegroundColor Yellow
            exit 1
        }

        foreach ($pnp in @($pnpDevices)) {
            Write-Host "Disabling: $($pnp.FriendlyName) [$($pnp.Status)]..." -NoNewline
            try {
                Disable-PnpDevice -InputObject $pnp -Confirm:$false -ErrorAction Stop
                Write-Host " OK" -ForegroundColor Green
            } catch {
                Write-Host " FAILED: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}
