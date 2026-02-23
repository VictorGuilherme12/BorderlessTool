namespace MonitorTool;
public readonly record struct MonitorInfo (
    string DeviceName,
    int Width,
    int Height,
    bool IsPrimary,
    uint StateFlags
);

public enum MonitorStatus
{
    Success,
    Failed,
    BadMode,
    RestartRequired,
    MonitorNotFound

}
