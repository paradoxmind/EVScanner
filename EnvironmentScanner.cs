// ReSharper disable PossibleLossOfFraction

using System.Globalization;

namespace EVScanner;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

public static class EnvironmentScanner
{
    public static EnvironmentData Collect()
    {
        return new EnvironmentData
        {
            OSDescription = RuntimeInformation.OSDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            CPUCount = Environment.ProcessorCount,
            MemoryInfo = GetMemoryInfo(),
            DiskInfo = GetDiskInfo(),
            RunningProcesses = GetRunningProcesses()
        };
    }

    private static MemoryInfo GetMemoryInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var pc = new PerformanceCounter("Memory", "Available MBytes");

            return new MemoryInfo
            {
                AvailableMemoryMB = pc.NextValue(),
                TotalMemoryMB = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return ReadLinuxMemoryInfo();

        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) 
            ? ReadMacMemoryInfo() 
            : throw new PlatformNotSupportedException("Memory info not supported on this platform.");
    }

    private static DiskInfo[] GetDiskInfo()
    {
        return DriveInfo
            .GetDrives()
            .Where(d => d.IsReady)
            .Select(d => new DiskInfo
            {
                Name = d.Name,
                TotalSizeGB = d.TotalSize / (1024 * 1024 * 1024),
                FreeSpaceGB = d.TotalFreeSpace / (1024 * 1024 * 1024)
            })
            .ToArray();
    }

    private static string[] GetRunningProcesses()
    {
        return Process.GetProcesses()
            .Select(p => $"{p.ProcessName} (ID: {p.Id})")
            .ToArray();
    }

    private static MemoryInfo ReadLinuxMemoryInfo()
    {
        var memInfo = File.ReadAllLines("/proc/meminfo");
        float available = 0;
        float total = 0;

        foreach (var line in memInfo)
        {
            if (line.StartsWith("MemAvailable:", StringComparison.InvariantCultureIgnoreCase))
                available = float.Parse(line.Split(':')[1].Trim().Split(' ')[0], CultureInfo.InvariantCulture.NumberFormat) / 1024;
            else if (line.StartsWith("MemTotal:", StringComparison.InvariantCultureIgnoreCase))
                total = float.Parse(line.Split(':')[1].Trim().Split(' ')[0], CultureInfo.InvariantCulture.NumberFormat) / 1024;
        }

        return new MemoryInfo
        {
            AvailableMemoryMB = available,
            TotalMemoryMB = (long)total
        };
    }

    private static MemoryInfo ReadMacMemoryInfo()
    {
        var output = RunBashCommand("vm_stat");

        var lines = output.Split('\n');
        long pageSize = 4096;
        long freePages = 0;
        long totalPages = 0;

        foreach (var line in lines)
        {
            if (line.Contains("page size of"))
            {
                var parts = line.Split(' ');
                pageSize = long.Parse(parts.Last().Replace(".", ""), CultureInfo.InvariantCulture.NumberFormat);
            }

            if (line.StartsWith("Pages free", StringComparison.InvariantCultureIgnoreCase))
                freePages = long.Parse(line.Split(':')[1].Trim().Replace(".", ""), CultureInfo.InvariantCulture.NumberFormat);

            totalPages += line.Contains("Pages") 
                ? long.Parse(line.Split(':')[1].Trim().Replace(".", ""), CultureInfo.InvariantCulture.NumberFormat) 
                : 0;
        }

        return new MemoryInfo
        {
            AvailableMemoryMB = (freePages * pageSize) / 1024 / 1024,
            TotalMemoryMB = (totalPages * pageSize) / 1024 / 1024
        };
    }

    private static string RunBashCommand(string command)
    {
        var escapedArgs = command.Replace("\"", "\\\"");

        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return result;
    }

}