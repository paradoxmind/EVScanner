namespace EVScanner;

public class EnvironmentData
{
    public string? OSDescription { get; set; }
    public string? OSArchitecture { get; set; }
    public string? FrameworkDescription { get; set; }
    public int CPUCount { get; set; }
    public MemoryInfo? MemoryInfo { get; set; }
    public DiskInfo[]? DiskInfo { get; set; }
    public string[]? RunningProcesses { get; set; }
}