namespace BorderlessTool.Monitors;

/// <summary>
/// Represents a physical display monitor detected on the system.
/// Populated by <see cref="MonitorManager.EnumerateAllMonitors"/> using Win32 display APIs.
/// </summary>
/// <param name="DeviceName">
/// The Win32 device name of the monitor, e.g. <c>\\.\DISPLAY1</c>.
/// Used to identify the monitor in calls such as <c>ChangeDisplaySettingsExW</c>.
/// </param>
/// <param name="Width">The horizontal resolution of the monitor in pixels.</param>
/// <param name="Height">The vertical resolution of the monitor in pixels.</param>
/// <param name="X">
/// The horizontal position of the monitor's top-left corner in virtual screen coordinates.
/// For the primary monitor this is typically 0; secondary monitors have an offset based on their arrangement.
/// </param>
/// <param name="Y">
/// The vertical position of the monitor's top-left corner in virtual screen coordinates.
/// For the primary monitor this is typically 0.
/// </param>
/// <param name="IsPrimary">
/// Indicates whether this is the primary monitor.
/// The primary monitor always has its top-left corner at virtual coordinate (0, 0).
/// </param>
/// <param name="StateFlags">
/// The raw Win32 state flags for the display device (<c>DISPLAY_DEVICE.StateFlags</c>).
/// Common values: <c>DISPLAY_DEVICE_ACTIVE (0x1)</c>, <c>DISPLAY_DEVICE_PRIMARY_DEVICE (0x4)</c>.
/// </param>
public readonly record struct MonitorInfo(
    string DeviceName,
    int Width,
    int Height,
    int X,
    int Y,
    bool IsPrimary,
    uint StateFlags
);