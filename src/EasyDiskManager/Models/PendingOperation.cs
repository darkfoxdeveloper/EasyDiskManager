namespace EasyDiskManager.Models;

public enum DiskOperationKind
{
    Rename,
    ChangeLetter,
    Resize,
    Format,
    Delete,
    Create
}

public sealed record PendingOperation(
    DiskOperationKind Kind,
    int DiskNumber,
    int? PartitionNumber,
    string Description,
    string PowerShell,
    bool IsDestructive)
{
    public string Icon => Kind switch
    {
        DiskOperationKind.Create => "+",
        DiskOperationKind.Delete => "×",
        DiskOperationKind.Format => "↻",
        DiskOperationKind.Resize => "↔",
        DiskOperationKind.ChangeLetter => "A:",
        _ => "✎"
    };
}
