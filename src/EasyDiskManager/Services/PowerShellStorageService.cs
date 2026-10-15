using System.Diagnostics;
using System.Text;
using System.Text.Json;
using EasyDiskManager.Models;

namespace EasyDiskManager.Services;

public sealed class PowerShellStorageService
{
    private const string PowerShellPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";

    public async Task<IReadOnlyList<DiskInfo>> GetDisksAsync(CancellationToken cancellationToken = default)
    {
        const string script = """
            $ErrorActionPreference = 'Stop'
            $result = @(Get-Disk | Sort-Object Number | ForEach-Object {
                $d = $_
                $parts = @(Get-Partition -DiskNumber $d.Number -ErrorAction SilentlyContinue | Sort-Object Offset | ForEach-Object {
                    $p = $_
                    $v = $null
                    if ($p.DriveLetter) { $v = Get-Volume -DriveLetter $p.DriveLetter -ErrorAction SilentlyContinue }
                    [PSCustomObject]@{
                        DiskNumber = [int]$d.Number
                        PartitionNumber = [int]$p.PartitionNumber
                        DriveLetter = if ($p.DriveLetter) { [string]$p.DriveLetter } else { '' }
                        Type = [string]$p.Type
                        GptType = [string]$p.GptType
                        Size = [long]$p.Size
                        Offset = [long]$p.Offset
                        IsBoot = [bool]$p.IsBoot
                        IsSystem = [bool]$p.IsSystem
                        IsHidden = [bool]$p.IsHidden
                        IsReadOnly = [bool]$p.IsReadOnly
                        FileSystem = if ($v) { [string]$v.FileSystem } else { '' }
                        FileSystemLabel = if ($v) { [string]$v.FileSystemLabel } else { '' }
                        HealthStatus = if ($v) { [string]$v.HealthStatus } else { [string]$d.HealthStatus }
                        SizeRemaining = if ($v) { [long]$v.SizeRemaining } else { 0L }
                    }
                })
                [PSCustomObject]@{
                    Number = [int]$d.Number
                    FriendlyName = [string]$d.FriendlyName
                    SerialNumber = [string]$d.SerialNumber
                    BusType = [string]$d.BusType
                    PartitionStyle = [string]$d.PartitionStyle
                    OperationalStatus = [string]($d.OperationalStatus -join ', ')
                    HealthStatus = [string]$d.HealthStatus
                    Size = [long]$d.Size
                    AllocatedSize = [long]$d.AllocatedSize
                    IsBoot = [bool]$d.IsBoot
                    IsSystem = [bool]$d.IsSystem
                    IsReadOnly = [bool]$d.IsReadOnly
                    IsOffline = [bool]$d.IsOffline
                    Partitions = $parts
                }
            })
            ConvertTo-Json -InputObject $result -Depth 6 -Compress
            """;

        var json = await RunCapturedAsync(script, cancellationToken);
        if (string.IsNullOrWhiteSpace(json)) return [];
        return JsonSerializer.Deserialize<List<DiskInfo>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
    }

    public async Task ExecuteElevatedAsync(IEnumerable<PendingOperation> operations, CancellationToken cancellationToken = default)
    {
        var commands = string.Join(Environment.NewLine, operations.Select(o => o.PowerShell));
        var script = $$"""
            $ErrorActionPreference = 'Stop'
            try {
                Import-Module Storage -ErrorAction Stop
                {{commands}}
                exit 0
            } catch {
                Write-Error $_
                exit 1
            }
            """;

        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var startInfo = new ProcessStartInfo
        {
            FileName = PowerShellPath,
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}",
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("No se pudo iniciar el proceso administrativo.");
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
            throw new InvalidOperationException("Windows no pudo completar una o más operaciones. No se aplicaron las operaciones posteriores.");
    }

    private static async Task<string> RunCapturedAsync(string script, CancellationToken cancellationToken)
    {
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var startInfo = new ProcessStartInfo
        {
            FileName = PowerShellPath,
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("PowerShell no está disponible.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "No se pudo leer la configuración de discos." : error.Trim());
        return output.Trim();
    }
}
