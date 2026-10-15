using System.Text.Json.Serialization;

namespace EasyDiskManager.Models;

public sealed class PartitionInfo
{
    public int DiskNumber { get; set; }
    public int PartitionNumber { get; set; }
    public string DriveLetter { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string GptType { get; set; } = string.Empty;
    public long Size { get; set; }
    public long Offset { get; set; }
    public bool IsBoot { get; set; }
    public bool IsSystem { get; set; }
    public bool IsHidden { get; set; }
    public bool IsReadOnly { get; set; }
    public string FileSystem { get; set; } = string.Empty;
    public string FileSystemLabel { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = string.Empty;
    public long SizeRemaining { get; set; }

    [JsonIgnore] public string VolumeName => !string.IsNullOrWhiteSpace(FileSystemLabel) ? FileSystemLabel : Type;
    [JsonIgnore] public string LetterText => string.IsNullOrWhiteSpace(DriveLetter) ? "—" : $"{DriveLetter}:";
    [JsonIgnore] public string SizeText => SizeFormatter.Format(Size);
    [JsonIgnore] public string UsedText => SizeFormatter.Format(Math.Max(0, Size - SizeRemaining));
    [JsonIgnore] public string FreeText => SizeFormatter.Format(SizeRemaining);
    [JsonIgnore] public double UsedPercent => Size <= 0 ? 0 : Math.Clamp((Size - SizeRemaining) * 100d / Size, 0, 100);
    [JsonIgnore] public string UsedPercentText => $"{UsedPercent:0}%";
    [JsonIgnore] public string FileSystemText => string.IsNullOrWhiteSpace(FileSystem) ? Type : FileSystem;
    [JsonIgnore] public string Identity => $"Disco {DiskNumber}, partición {PartitionNumber}";
    [JsonIgnore] public bool IsProtected => IsBoot || IsSystem || IsHidden || Type.Contains("Recovery", StringComparison.OrdinalIgnoreCase);
}
