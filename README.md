# Easy Disk Manager

Administrador visual de discos y particiones para Windows, construido con C# y WPF sobre .NET 8.

La aplicación presenta los discos como un mapa fácil de entender, permite preparar varias acciones y solo modifica el sistema cuando el usuario revisa la cola y pulsa **Aplicar cambios**. Windows solicita elevación UAC únicamente en ese momento.

## Funciones

- Vista general de discos, particiones y espacio sin asignar.
- Capacidad, espacio usado/libre, formato, letra y estado de cada volumen.
- Cola de operaciones con deshacer y vaciar antes de aplicar.
- Crear un volumen usando espacio sin asignar.
- Cambiar etiqueta y letra de unidad.
- Redimensionar una partición dentro de los límites admitidos por Windows.
- Formatear en NTFS, exFAT o ReFS.
- Eliminar particiones.
- Protección de las particiones de sistema, arranque, recuperación y ocultas.
- Confirmación reforzada cuando hay operaciones destructivas.

## Seguridad

> Un gestor de particiones puede provocar pérdida de datos. Conserva siempre una copia de seguridad verificada antes de formatear, eliminar o redimensionar un volumen.

La aplicación nunca aplica una acción al seleccionarla: la añade a una cola visible. Las modificaciones requieren confirmación y permisos de administrador. El sistema vuelve a comprobar los límites de redimensionado justo antes de ejecutar la operación.

No se han implementado todavía el movimiento físico de particiones, la conversión MBR/GPT, la gestión de discos dinámicos ni flujos especiales para BitLocker/Storage Spaces. Para esos casos, la interfaz actúa únicamente como visor.

## Ejecutar

Requisitos: Windows 10/11 y .NET Desktop Runtime 8.

```powershell
dotnet run --project .\src\EasyDiskManager\EasyDiskManager.csproj
```

También se puede abrir `EasyDiskManager.sln` con Visual Studio 2022 y ejecutar el proyecto `EasyDiskManager`.

## Compilar y publicar

```powershell
dotnet build .\EasyDiskManager.sln -c Release
dotnet publish .\src\EasyDiskManager\EasyDiskManager.csproj `
  -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -o .\artifacts\win-x64
```

El ejecutable resultante estará en `artifacts\win-x64\EasyDiskManager.exe`.

## Diseño técnico

- `Models`: representación inmutable/serializable de discos, particiones y operaciones.
- `Services`: lectura del módulo Storage y ejecución elevada aislada.
- `ViewModels`: estado de la interfaz, validaciones y cola de operaciones.
- `MainWindow.xaml`: interfaz WPF adaptable con mapa visual y panel de acciones.

La lectura se realiza sin privilegios administrativos. Las operaciones se traducen a comandos del módulo oficial `Storage` de Windows y se lanzan en un proceso elevado independiente.
