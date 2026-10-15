using System.Text.Json.Serialization;

namespace EasyDiskManager.Models;

public sealed class DiskInfo
{
    public int Number { get; set; }
    public string FriendlyName { get; set; } = "Disco";
    public string SerialNumber { get; set; } = string.Empty;
    public string BusType { get; set; } = string.Empty;
    public string PartitionStyle { get; set; } = string.Empty;
    public string OperationalStatus { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = string.Empty;
    public long Size { get; set; }
    public long AllocatedSize { get; set; }
    public bool IsBoot { get; set; }
    public bool IsSystem { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsOffline { get; set; }
    public List<PartitionInfo> Partitions { get; set; } = [];

    [JsonIgnore] public string DisplayName => $"Disco {Number} · {FriendlyName}";
    [JsonIgnore] public string SizeText => SizeFormatter.Format(Size);
    [JsonIgnore] public string Description => $"{BusType}  ·  {PartitionStyle}  ·  {SizeText}";
    [JsonIgnore] public string HealthText => IsOffline ? "Sin conexión" : HealthStatus is "Healthy" or "Saludable" ? "Correcto" : HealthStatus;
    [JsonIgnore] public long UnallocatedSize => Math.Max(0, Size - Partitions.Sum(p => p.Size));
    [JsonIgnore] public string UnallocatedText => SizeFormatter.Format(UnallocatedSize);
    [JsonIgnore] public IReadOnlyList<DiskSegment> Segments
    {
        get
        {
            var result = new List<DiskSegment>();
            long position = 0;
            foreach (var partition in Partitions.OrderBy(p => p.Offset))
            {
                if (partition.Offset > position)
                    result.Add(new DiskSegment(partition.Offset - position, position, null));
                result.Add(new DiskSegment(partition.Size, partition.Offset, partition));
                position = Math.Max(position, partition.Offset + partition.Size);
            }
            if (Size > position) result.Add(new DiskSegment(Size - position, position, null));
            return result;
        }
    }
}

public static class SizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

    public static string Format(long bytes)
    {
        double value = Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.##} {Units[unit]}";
    }
}
