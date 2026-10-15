namespace EasyDiskManager.Models;

public sealed record DiskSegment(long Size, long Offset, PartitionInfo? Partition)
{
    public bool IsUnallocated => Partition is null;
    public string Title => Partition is null
        ? "Sin asignar"
        : string.IsNullOrWhiteSpace(Partition.DriveLetter) ? Partition.VolumeName : $"{Partition.DriveLetter}:  {Partition.VolumeName}";
    public string Subtitle => $"{SizeFormatter.Format(Size)}  ·  {(Partition?.FileSystemText ?? "Disponible")}";
}
