namespace BorderlessTool.Monitors;
public readonly record struct MonitorInfo (
    string DeviceName,
    int Width,
    int Height,
    int X,
    int Y,
    bool IsPrimary,
    uint StateFlags
);


