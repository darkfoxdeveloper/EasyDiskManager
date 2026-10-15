using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using EasyDiskManager.Infrastructure;
using EasyDiskManager.Models;
using EasyDiskManager.Services;

namespace EasyDiskManager.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly PowerShellStorageService _storage = new();
    private DiskInfo? _selectedDisk;
    private PartitionInfo? _selectedPartition;
    private bool _isBusy;
    private string _statusText = "Preparando el administrador de discos…";

    public MainViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        ApplyCommand = new AsyncRelayCommand(ApplyAsync, () => !IsBusy && PendingOperations.Count > 0);
        UndoCommand = new RelayCommand(Undo, () => PendingOperations.Count > 0 && !IsBusy);
        ClearCommand = new RelayCommand(Clear, () => PendingOperations.Count > 0 && !IsBusy);
        RenameCommand = new RelayCommand(QueueRename, CanEditVolume);
        ChangeLetterCommand = new RelayCommand(QueueChangeLetter, CanModifyPartition);
        ResizeCommand = new RelayCommand(QueueResize, CanResize);
        FormatCommand = new RelayCommand(QueueFormat, CanModifyPartition);
        DeleteCommand = new RelayCommand(QueueDelete, CanModifyPartition);
        CreateCommand = new RelayCommand(QueueCreate, CanCreate);
        PendingOperations.CollectionChanged += (_, _) => RaiseCommandStates();
    }

    public ObservableCollection<DiskInfo> Disks { get; } = [];
    public ObservableCollection<PendingOperation> PendingOperations { get; } = [];

    public DiskInfo? SelectedDisk
    {
        get => _selectedDisk;
        set
        {
            if (!SetProperty(ref _selectedDisk, value)) return;
            SelectedPartition = value?.Partitions.FirstOrDefault();
            OnPropertyChanged(nameof(Partitions));
            RaiseCommandStates();
        }
    }

    public PartitionInfo? SelectedPartition
    {
        get => _selectedPartition;
        set
        {
            if (!SetProperty(ref _selectedPartition, value)) return;
            OnPropertyChanged(nameof(SelectionTitle));
            OnPropertyChanged(nameof(SelectionDetails));
            RaiseCommandStates();
        }
    }

    public IReadOnlyList<PartitionInfo> Partitions => SelectedDisk?.Partitions ?? [];
    public string SelectionTitle => SelectedPartition?.VolumeName ?? "Selecciona una partición";
    public string SelectionDetails => SelectedPartition is null ? "Elige una fila para ver y gestionar el volumen."
        : $"{SelectedPartition.Identity}  ·  {SelectedPartition.SizeText}  ·  {SelectedPartition.FileSystemText}";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(IsReady));
            RaiseCommandStates();
        }
    }
    public bool IsReady => !IsBusy;
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public int PendingCount => PendingOperations.Count;

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand ApplyCommand { get; }
    public RelayCommand UndoCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand RenameCommand { get; }
    public RelayCommand ChangeLetterCommand { get; }
    public RelayCommand ResizeCommand { get; }
    public RelayCommand FormatCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand CreateCommand { get; }

    public async Task RefreshAsync()
    {
        IsBusy = true;
        StatusText = "Leyendo discos y volúmenes…";
        try
        {
            var selectedNumber = SelectedDisk?.Number;
            var disks = await _storage.GetDisksAsync();
            Disks.Clear();
            foreach (var disk in disks) Disks.Add(disk);
            SelectedDisk = Disks.FirstOrDefault(d => d.Number == selectedNumber) ?? Disks.FirstOrDefault();
            StatusText = disks.Count == 0 ? "Windows no ha devuelto ningún disco." : $"{disks.Count} disco(s) detectado(s) · Actualizado {DateTime.Now:HH:mm}";
        }
        catch (Exception ex)
        {
            StatusText = "No se pudo leer la información de almacenamiento.";
            MessageBox.Show(ex.Message, "Error al leer los discos", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    private void QueueRename()
    {
        var p = SelectedPartition!;
        var label = DialogService.Prompt("Cambiar etiqueta", "Escribe la nueva etiqueta del volumen:", p.FileSystemLabel);
        if (label is null) return;
        var escaped = Escape(label);
        Add(new(DiskOperationKind.Rename, p.DiskNumber, p.PartitionNumber,
            $"Cambiar la etiqueta de {p.LetterText} a “{label}”",
            $"Get-Partition -DiskNumber {p.DiskNumber} -PartitionNumber {p.PartitionNumber} | Get-Volume | Set-Volume -NewFileSystemLabel '{escaped}'", false));
    }

    private void QueueChangeLetter()
    {
        var p = SelectedPartition!;
        var value = DialogService.Prompt("Cambiar letra", "Indica una letra libre entre D y Z:", p.DriveLetter);
        if (value is null) return;
        var letter = value.Trim().TrimEnd(':').ToUpperInvariant();
        if (letter.Length != 1 || letter[0] is < 'D' or > 'Z')
        {
            MessageBox.Show("Usa una sola letra entre D y Z.", "Letra no válida", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Add(new(DiskOperationKind.ChangeLetter, p.DiskNumber, p.PartitionNumber,
            $"Cambiar {p.Identity} a la unidad {letter}:",
            $"Set-Partition -DiskNumber {p.DiskNumber} -PartitionNumber {p.PartitionNumber} -NewDriveLetter '{letter}'", false));
    }

    private void QueueResize()
    {
        var p = SelectedPartition!;
        var currentGb = p.Size / Math.Pow(1024, 3);
        var value = DialogService.Prompt("Redimensionar partición",
            $"Tamaño actual: {p.SizeText}. Escribe el nuevo tamaño en GB. Windows comprobará el mínimo y máximo compatibles.",
            currentGb.ToString("0.##"));
        if (value is null) return;
        if (!double.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var gb) || gb <= 0)
        {
            MessageBox.Show("Introduce un tamaño válido en GB.", "Tamaño no válido", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var bytes = checked((long)(gb * 1024 * 1024 * 1024));
        var command = $"$r=Get-PartitionSupportedSize -DiskNumber {p.DiskNumber} -PartitionNumber {p.PartitionNumber}; " +
                      $"if ({bytes} -lt $r.SizeMin -or {bytes} -gt $r.SizeMax) {{ throw 'Tamaño fuera del rango permitido por Windows.' }}; " +
                      $"Resize-Partition -DiskNumber {p.DiskNumber} -PartitionNumber {p.PartitionNumber} -Size {bytes}";
        Add(new(DiskOperationKind.Resize, p.DiskNumber, p.PartitionNumber,
            $"Redimensionar {p.LetterText} a {gb:0.##} GB", command, true));
    }

    private void QueueFormat()
    {
        var p = SelectedPartition!;
        var fileSystem = DialogService.Choose("Formatear volumen", "Elige el sistema de archivos:", ["NTFS", "exFAT", "ReFS"], "NTFS");
        if (fileSystem is null) return;
        var label = DialogService.Prompt("Etiqueta del volumen", "Etiqueta para el volumen (puede quedar vacía):", p.FileSystemLabel);
        if (label is null) return;
        Add(new(DiskOperationKind.Format, p.DiskNumber, p.PartitionNumber,
            $"Formatear {p.Identity} como {fileSystem} (BORRA TODOS LOS DATOS)",
            $"Get-Partition -DiskNumber {p.DiskNumber} -PartitionNumber {p.PartitionNumber} | Format-Volume -FileSystem '{fileSystem}' -NewFileSystemLabel '{Escape(label)}' -Confirm:$false -Force", true));
    }

    private void QueueDelete()
    {
        var p = SelectedPartition!;
        foreach (var existing in PendingOperations.Where(o => o.DiskNumber == p.DiskNumber && o.PartitionNumber == p.PartitionNumber).ToArray())
            PendingOperations.Remove(existing);
        Add(new(DiskOperationKind.Delete, p.DiskNumber, p.PartitionNumber,
            $"Eliminar {p.Identity} (BORRA TODOS LOS DATOS)",
            $"Remove-Partition -DiskNumber {p.DiskNumber} -PartitionNumber {p.PartitionNumber} -Confirm:$false", true));
    }

    private void QueueCreate()
    {
        var d = SelectedDisk!;
        if (PendingOperations.Any(o => o.Kind == DiskOperationKind.Create && o.DiskNumber == d.Number))
        {
            MessageBox.Show("Ya hay una creación de volumen pendiente para este disco.", "Operación ya añadida", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var fileSystem = DialogService.Choose("Nuevo volumen", $"Se usará el espacio disponible de {d.DisplayName}.", ["NTFS", "exFAT", "ReFS"], "NTFS");
        if (fileSystem is null) return;
        var label = DialogService.Prompt("Nuevo volumen", "Etiqueta del volumen:", "Nuevo volumen");
        if (label is null) return;
        Add(new(DiskOperationKind.Create, d.Number, null,
            $"Crear volumen {fileSystem} usando el espacio sin asignar de Disco {d.Number}",
            $"New-Partition -DiskNumber {d.Number} -UseMaximumSize -AssignDriveLetter | Format-Volume -FileSystem '{fileSystem}' -NewFileSystemLabel '{Escape(label)}' -Confirm:$false", false));
    }

    private async Task ApplyAsync()
    {
        var destructive = PendingOperations.Count(o => o.IsDestructive);
        var warning = destructive > 0
            ? $"La cola contiene {destructive} operación(es) destructiva(s). Los datos afectados no se podrán recuperar."
            : "Windows solicitará permisos de administrador para aplicar los cambios.";
        var answer = MessageBox.Show($"{warning}\n\n¿Quieres aplicar ahora las {PendingOperations.Count} operaciones?",
            "Confirmar cambios", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;

        IsBusy = true;
        StatusText = "Aplicando operaciones… No apagues el equipo.";
        try
        {
            await _storage.ExecuteElevatedAsync(PendingOperations.ToArray());
            PendingOperations.Clear();
            MessageBox.Show("Las operaciones finalizaron correctamente.", "Cambios aplicados", MessageBoxButton.OK, MessageBoxImage.Information);
            await RefreshAsync();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            StatusText = "Operación cancelada por el usuario.";
        }
        catch (Exception ex)
        {
            StatusText = "La ejecución se detuvo por un error.";
            MessageBox.Show(ex.Message + "\n\nActualiza la vista antes de volver a intentarlo.", "No se pudieron aplicar los cambios", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    private bool CanEditVolume() => !IsBusy && SelectedPartition is { FileSystem.Length: > 0 } p && !p.IsReadOnly;
    private bool CanModifyPartition() => !IsBusy && SelectedDisk is { IsOffline: false, IsReadOnly: false } && SelectedPartition is { IsProtected: false, IsReadOnly: false };
    private bool CanResize() => CanModifyPartition() && !string.IsNullOrWhiteSpace(SelectedPartition?.FileSystem);
    private bool CanCreate() => !IsBusy && SelectedDisk is { IsOffline: false, IsReadOnly: false, UnallocatedSize: > 16777216 };
    private static string Escape(string value) => value.Replace("'", "''");

    private void Add(PendingOperation operation)
    {
        if (operation.PartitionNumber is not null && operation.Kind != DiskOperationKind.Delete &&
            PendingOperations.Any(o => o.Kind == DiskOperationKind.Delete && o.DiskNumber == operation.DiskNumber && o.PartitionNumber == operation.PartitionNumber))
        {
            MessageBox.Show("Esa partición ya está marcada para eliminar. Deshaz esa operación antes de añadir otra.",
                "Operaciones incompatibles", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        PendingOperations.Add(operation);
        StatusText = $"{PendingOperations.Count} operación(es) pendiente(s). Revisa la cola antes de aplicar.";
    }
    private void Undo() { if (PendingOperations.Count > 0) PendingOperations.RemoveAt(PendingOperations.Count - 1); }
    private void Clear() => PendingOperations.Clear();

    private void RaiseCommandStates()
    {
        OnPropertyChanged(nameof(PendingCount));
        RefreshCommand.RaiseCanExecuteChanged(); ApplyCommand.RaiseCanExecuteChanged(); UndoCommand.RaiseCanExecuteChanged(); ClearCommand.RaiseCanExecuteChanged();
        RenameCommand.RaiseCanExecuteChanged(); ChangeLetterCommand.RaiseCanExecuteChanged(); ResizeCommand.RaiseCanExecuteChanged(); FormatCommand.RaiseCanExecuteChanged(); DeleteCommand.RaiseCanExecuteChanged(); CreateCommand.RaiseCanExecuteChanged();
    }
}
