namespace BorderlessTool.Monitors;

/// <summary>
/// Represents the result of a monitor configuration operation
/// such as changing resolution or setting the primary display.
/// Returned by methods in <see cref="MonitorManager"/>.
/// </summary>
public enum MonitorStatus
{
    /// <summary>The operation completed successfully.</summary>
    Success,

    /// <summary>The operation failed for an unspecified reason.</summary>
    Failed,

    /// <summary>
    /// The requested display mode is not supported by the monitor or driver.
    /// Corresponds to the Win32 <c>DISP_CHANGE_BADMODE</c> return value.
    /// </summary>
    BadMode,

    /// <summary>
    /// The change was applied but requires a system restart to take effect.
    /// Corresponds to the Win32 <c>DISP_CHANGE_RESTART</c> return value.
    /// </summary>
    RestartRequired,

    /// <summary>The specified monitor device name was not found or its settings could not be read.</summary>
    MonitorNotFound
}