using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BorderlessTool.Monitors;

/// <summary>
/// Provides functionality to enumerate display monitors and change their settings
/// using Win32 display configuration APIs.
/// </summary>
public static class MonitorManager
{
    /// <summary>
    /// Enumerates all active display monitors on the system.
    /// Uses <c>EnumDisplayDevicesW</c> to discover devices and <c>EnumDisplaySettingsW</c>
    /// to retrieve each monitor's current resolution and position.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="MonitorInfo"/> records representing all active monitors.
    /// Width and Height will be -1 if the display settings could not be retrieved.
    /// </returns>
    public static IReadOnlyList<MonitorInfo> EnumerateAllMonitors()
    {
        var monitors = new List<MonitorInfo>();

        uint deviceNum = 0;
        while (true)
        {
            var dd = DISPLAY_DEVICEW.Create();
            if (!EnumDisplayDevicesW(null, deviceNum, ref dd, 0))
                break;

            if ((dd.StateFlags & DISPLAY_DEVICE_ACTIVE) != 0)
            {
                bool isPrimary = (dd.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0;

                var dm = DEVMODEW.Create();
                int width = -1, height = -1;

                if (EnumDisplaySettingsW(dd.DeviceName, ENUM_CURRENT_SETTINGS, ref dm))
                {
                    width = (int)dm.dmPelsWidth;
                    height = (int)dm.dmPelsHeight;
                }

                monitors.Add(new MonitorInfo(
                    DeviceName: dd.DeviceName,
                    Width: width,
                    Height: height,
                    Y: dm.dmPositionY,
                    X: dm.dmPositionX,
                    IsPrimary: isPrimary,
                    StateFlags: dd.StateFlags
                ));
            }

            deviceNum++;
        }

        return monitors;
    }

    /// <summary>
    /// Changes the resolution of the specified monitor.
    /// Applies the change globally so it affects all users on the system (<c>CDS_GLOBAL</c>).
    /// </summary>
    /// <param name="deviceName">The Win32 device name of the monitor, e.g. <c>\\.\DISPLAY1</c>.</param>
    /// <param name="width">The desired horizontal resolution in pixels.</param>
    /// <param name="height">The desired vertical resolution in pixels.</param>
    /// <returns>A <see cref="MonitorStatus"/> indicating the result of the operation.</returns>
    public static MonitorStatus ChangeResolution(string deviceName, int width, int height)
    {
        var dm = DEVMODEW.Create();
        if (!EnumDisplaySettingsW(deviceName, ENUM_CURRENT_SETTINGS, ref dm))
            return MonitorStatus.Failed;

        dm.dmPelsWidth = (uint)width;
        dm.dmPelsHeight = (uint)height;
        dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;

        int result = ChangeDisplaySettingsExW(deviceName, ref dm, IntPtr.Zero, CDS_GLOBAL, IntPtr.Zero);

        return result switch
        {
            DISP_CHANGE_SUCCESSFUL => MonitorStatus.Success,
            DISP_CHANGE_BADMODE => MonitorStatus.BadMode,
            DISP_CHANGE_RESTART => MonitorStatus.RestartRequired,
            _ => MonitorStatus.Failed
        };
    }

    /// <summary>
    /// Sets the specified monitor as the primary display.
    /// Moves the target monitor's position to virtual coordinate (0, 0) and persists
    /// the change to the registry via <c>CDS_UPDATEREGISTRY | CDS_SET_PRIMARY</c>.
    /// <para>
    /// After applying the change, Explorer is restarted to ensure the taskbar
    /// and shell correctly reflect the new primary monitor.
    /// </para>
    /// </summary>
    /// <param name="deviceName">The Win32 device name of the monitor to promote, e.g. <c>\\.\DISPLAY2</c>.</param>
    /// <returns>A <see cref="MonitorStatus"/> indicating the result of the operation.</returns>
    public static MonitorStatus SetPrimaryMonitor(string deviceName)
    {
        var dm = DEVMODEW.Create();
        if (!EnumDisplaySettingsW(deviceName, ENUM_CURRENT_SETTINGS, ref dm))
            return MonitorStatus.MonitorNotFound;

        // The primary monitor must be positioned at virtual coordinate (0, 0)
        dm.dmPositionX = 0;
        dm.dmPositionY = 0;
        dm.dmFields = DM_POSITION;

        int result = ChangeDisplaySettingsExW(deviceName, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_SET_PRIMARY, IntPtr.Zero);
        if (result != DISP_CHANGE_SUCCESSFUL)
            return MonitorStatus.Failed;

        try
        {
            // Restart Explorer so the taskbar moves to the new primary monitor
            using var p1 = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = "/f /im explorer.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            p1?.WaitForExit();

            _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                CreateNoWindow = true,
                UseShellExecute = true
            });
        }
        catch
        {
            // Explorer restart is best-effort; failure does not affect the display change
        }

        return MonitorStatus.Success;
    }

    // -------------------------------------------------------------------------
    // Win32 constants
    // -------------------------------------------------------------------------

    /// <summary>StateFlags bit indicating the display device is active (connected and enabled).</summary>
    private const uint DISPLAY_DEVICE_ACTIVE = 0x00000001;

    /// <summary>StateFlags bit indicating this is the primary display device.</summary>
    private const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;

    /// <summary>Passed to EnumDisplaySettingsW to retrieve the current display mode.</summary>
    private const int ENUM_CURRENT_SETTINGS = -1;

    /// <summary>DEVMODE dmFields bit indicating dmPelsWidth is being set.</summary>
    private const uint DM_PELSWIDTH = 0x00080000;

    /// <summary>DEVMODE dmFields bit indicating dmPelsHeight is being set.</summary>
    private const uint DM_PELSHEIGHT = 0x00100000;

    /// <summary>DEVMODE dmFields bit indicating dmPosition (X/Y) is being set.</summary>
    private const uint DM_POSITION = 0x00000020;

    /// <summary>ChangeDisplaySettingsEx flag: persist the change to the registry.</summary>
    private const uint CDS_UPDATEREGISTRY = 0x00000001;

    /// <summary>ChangeDisplaySettingsEx flag: apply the change globally for all users.</summary>
    private const uint CDS_GLOBAL = 0x00000008;

    /// <summary>ChangeDisplaySettingsEx flag: set this device as the primary monitor.</summary>
    private const uint CDS_SET_PRIMARY = 0x00000010;

    /// <summary>ChangeDisplaySettingsEx return value: the change was successful.</summary>
    private const int DISP_CHANGE_SUCCESSFUL = 0;

    /// <summary>ChangeDisplaySettingsEx return value: a restart is required to apply the change.</summary>
    private const int DISP_CHANGE_RESTART = 1;

    /// <summary>ChangeDisplaySettingsEx return value: the requested mode is not supported.</summary>
    private const int DISP_CHANGE_BADMODE = -2;

    // -------------------------------------------------------------------------
    // Win32 P/Invoke declarations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enumerates display devices on the system. Pass <c>null</c> as lpDevice to enumerate
    /// all display adapters; pass a device name to enumerate monitors attached to that adapter.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevicesW(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICEW lpDisplayDevice, uint dwFlags);

    /// <summary>
    /// Retrieves display settings for the specified device into a <see cref="DEVMODEW"/> struct.
    /// Pass <see cref="ENUM_CURRENT_SETTINGS"/> as iModeNum to get the active configuration.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettingsW(string lpszDeviceName, int iModeNum, ref DEVMODEW lpDevMode);

    /// <summary>
    /// Changes the display settings for the specified device. Supports resolution changes,
    /// position changes, and primary monitor assignment depending on the flags passed.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsExW(string lpszDeviceName, ref DEVMODEW lpDevMode, IntPtr hwnd, uint dwFlags, IntPtr lParam);

    // -------------------------------------------------------------------------
    // Win32 structs
    // -------------------------------------------------------------------------

    /// <summary>
    /// Mirrors the Win32 <c>DISPLAY_DEVICE</c> structure. Contains identifying information
    /// about a display device including its name, description, and state flags.
    /// The <c>cb</c> field must be set to the struct size before use.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICEW
    {
        /// <summary>Size of this structure in bytes. Must be initialized before calling EnumDisplayDevicesW.</summary>
        public int cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        /// <summary>Combination of DISPLAY_DEVICE_* flags describing the device state.</summary>
        public uint StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;

        /// <summary>
        /// Creates a properly initialized <see cref="DISPLAY_DEVICEW"/> instance
        /// with <c>cb</c> set and string fields initialized to empty.
        /// </summary>
        public static DISPLAY_DEVICEW Create()
        {
            var dd = new DISPLAY_DEVICEW
            {
                cb = Marshal.SizeOf<DISPLAY_DEVICEW>(),
                DeviceName = string.Empty,
                DeviceString = string.Empty,
                DeviceID = string.Empty,
                DeviceKey = string.Empty
            };
            return dd;
        }
    }

    /// <summary>
    /// Mirrors the Win32 <c>DEVMODE</c> structure. Contains the display configuration
    /// for a device including resolution, position, color depth, and refresh rate.
    /// Only the fields relevant to monitor management are actively used here.
    /// The <c>dmSize</c> field must be set to the struct size before use.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODEW
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;

        public ushort dmSpecVersion;
        public ushort dmDriverVersion;

        /// <summary>Size of this structure in bytes. Must be initialized before use.</summary>
        public ushort dmSize;
        public ushort dmDriverExtra;

        /// <summary>
        /// Bitmask indicating which fields in this struct contain valid values.
        /// Set the corresponding DM_* flag before passing to ChangeDisplaySettingsExW.
        /// </summary>
        public uint dmFields;

        /// <summary>Horizontal position of the monitor in virtual screen coordinates.</summary>
        public int dmPositionX;

        /// <summary>Vertical position of the monitor in virtual screen coordinates.</summary>
        public int dmPositionY;

        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;

        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;

        public ushort dmLogPixels;
        public uint dmBitsPerPel;

        /// <summary>Horizontal resolution in pixels.</summary>
        public uint dmPelsWidth;

        /// <summary>Vertical resolution in pixels.</summary>
        public uint dmPelsHeight;

        public uint dmDisplayFlags;
        public uint dmDisplayFrequency;

        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;

        /// <summary>
        /// Creates a properly initialized <see cref="DEVMODEW"/> instance
        /// with <c>dmSize</c> set and string fields initialized to empty.
        /// </summary>
        public static DEVMODEW Create()
        {
            var dm = new DEVMODEW
            {
                dmDeviceName = string.Empty,
                dmFormName = string.Empty
            };
            dm.dmSize = (ushort)Marshal.SizeOf<DEVMODEW>();
            return dm;
        }
    }
}