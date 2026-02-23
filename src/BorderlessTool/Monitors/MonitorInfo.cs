namespace BorderlessTool.Monitors;
public readonly record struct MonitorInfo (
    string DeviceName,
    int Width,
    int Height,
    bool IsPrimary,
    uint StateFlags
);


