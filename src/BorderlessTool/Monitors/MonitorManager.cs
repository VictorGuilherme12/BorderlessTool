using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BorderlessTool.Monitors;

public static class MonitorManager
{
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
                    IsPrimary: isPrimary,
                    StateFlags: dd.StateFlags
                ));
            }

            deviceNum++;
        }

        return monitors;
    }

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

    public static MonitorStatus SetPrimaryMonitor(string deviceName)
    {
        var dm = DEVMODEW.Create();
        if (!EnumDisplaySettingsW(deviceName, ENUM_CURRENT_SETTINGS, ref dm))
            return MonitorStatus.MonitorNotFound;

        dm.dmPositionX = 0;
        dm.dmPositionY = 0;
        dm.dmFields = DM_POSITION;

        int result = ChangeDisplaySettingsExW(deviceName, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_SET_PRIMARY, IntPtr.Zero);
        if (result != DISP_CHANGE_SUCCESSFUL)
            return MonitorStatus.Failed;

        try
        {
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

        }

        return MonitorStatus.Success;
    }

    // ---------------- Win32 interop ----------------

    private const uint DISPLAY_DEVICE_ACTIVE = 0x00000001;
    private const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;

    private const int ENUM_CURRENT_SETTINGS = -1;

    private const uint DM_PELSWIDTH = 0x00080000;
    private const uint DM_PELSHEIGHT = 0x00100000;
    private const uint DM_POSITION = 0x00000020;

    private const uint CDS_UPDATEREGISTRY = 0x00000001;
    private const uint CDS_GLOBAL = 0x00000008;
    private const uint CDS_SET_PRIMARY = 0x00000010;

    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const int DISP_CHANGE_RESTART = 1;
    private const int DISP_CHANGE_BADMODE = -2;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevicesW(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICEW lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettingsW(string lpszDeviceName, int iModeNum, ref DEVMODEW lpDevMode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsExW(string lpszDeviceName, ref DEVMODEW lpDevMode, IntPtr hwnd, uint dwFlags, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICEW
    {
        public int cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public uint StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;

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

    // DEVMODE é grande. Aqui vai o mínimo com layout compatível.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODEW
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;

        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;

        public int dmPositionX;
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
        public uint dmPelsWidth;
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
