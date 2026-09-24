# Manual Técnico - Checador SSSD

**Versión:** 1.0  
**Fecha:** Junio 2026  
**Plataforma:** .NET 8.0 - Avalonia UI  

---

## Tabla de Contenidos

1. [Resumen General](#1-resumen-general)
2. [Requisitos del Sistema](#2-requisitos-del-sistema)
3. [Arquitectura del Sistema](#3-arquitectura-del-sistema)
4. [Configuración del Proyecto](#4-configuración-del-proyecto)
5. [Modelos de Datos](#5-modelos-de-datos)
6. [Servicios (Services)](#6-servicios-services)
7. [ViewModels](#7-viewmodels)
8. [Vistas (Views)](#8-vistas-views)
9. [Navegación y Flujo de la Aplicación](#9-navegación-y-flujo-de-la-aplicación)
10. [Integración con Lector de Huella](#10-integración-con-lector-de-huella)
11. [Base de Datos](#11-base-de-datos)
12. [Estilos y Recursos](#12-estilos-y-recursos)
13. [Dependencias y Paquetes](#13-dependencias-y-paquetes)

---

## 1. Resumen General

**Checador SSSD** es una aplicación de escritorio desarrollada en **.NET 8.0** con **Avalonia UI** para Windows. Su propósito es gestionar el control de asistencia (entrada/salida) de personal mediante lectura de huella dactilar y número de matrícula, generando reportes detallados de horas trabajadas.

### Características principales:
- Chequeo de entrada/salida por huella dactilar o matrícula manual
- Gestión de personas (CRUD)
- Generación de reportes mensuales y por fecha
- Administración de imágenes de carrusel y logotipos
- Soporte multi-vistas con navegación por sidebar
- Base de datos MySQL con EF Core y Dapper

---

## 2. Requisitos del Sistema

| Requisito | Especificación |
|-----------|---------------|
| Framework | .NET 8.0 |
| Sistema Operativo | Windows 7/10/11 (64-bit) |
| Base de Datos | MySQL 8.0+ |
| Lector de Huella | DigitalPersona U.are.U compatible |
| RAM Mínima | 4 GB |
| Almacenamiento | 500 MB |

---

## 3. Arquitectura del Sistema

El proyecto sigue el patrón **MVVM (Model-View-ViewModel)** con inyección de dependencias (DI).

```
┌─────────────────────────────────────┐
│           MainWindow.axaml          │
│       (Shell con navegación)        │
└──────────────┬──────────────────────┘
               │
       ┌───────┴───────┐
       ▼               ▼
┌──────────────┐  ┌──────────────┐
│   Services   │  │  ViewModels  │
│  (Lógica)    │  │ (Presentación)│
└──────────────┘  └──────────────┘
       │               │
       └───────┬───────┘
               ▼
       ┌──────────────┐
       │  AppDbContext │
       │  (EF Core /   │
       │ gleiche Dapper)│
       └───────┬───────┘
               ▼
       ┌──────────────┐
       │    MySQL     │
       └──────────────┘
```

### Capas del sistema:
1. **Models**: Entidades que representan tablas de la base de datos
2. **Services**: Lógica de negocio, acceso a datos, integraciones externas
3. **ViewModels**: Lógica de presentación y bindings para las vistas
4. **Views**: Interfaces de usuario en XAML (Avalonia)
5. **Converters**: Conversores de datos para XAML

---

## 4. Configuración del Proyecto

### Archivo: `ChecadorSSSD.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>
```

### Configuración importante:
- **OutputType**: `WinExe` - Aplicación de escritorio Windows
- **TargetFramework**: `net8.0`
- **AvaloniaUseCompiledBindingsByDefault**: `true` - Compilación optimizada de bindings

### Archivos nativos copiados al output:
- `dpfpdd.dll` - Driver del lector de huella
- `dpfj.dll` - Librería de procesamiento de huella (SDK DigitalPersona)
- Las DLLs del paquete NuGet `DigitalPersona.dpUruNet`

---

## 5. Modelos de Datos

### 5.1 Personas

```csharp
public class Personas
{
    [Key] [Column("id_personas")]
    public int IdPersona { get; set; }
    
    [Column("Nombre")] 
    public string Nombre { get; set; } = "Persona sin nombre";
    
    [Column("ApellidoP")]
    public string? ApellidoPaterno { get; set; }
    
    [Column("ApellidoM")]
    public string? ApellidoMaterno { get; set; }
    
    [Column("Matricula")] 
    [StringLength(15, ErrorMessage = "La matrícula no puede tener más de 15 caracteres.")]
    public string Matricula { get; set; } = "0";
    
    [Column("TipoPersona")]
    public TipoPersona? TipoPersona { get; set; }
    
    [Column("Imagen")]
    public string? Imagen { get; set; }
    
    [Column("Huella")]
    [StringLength(50)]
    public string? Huella { get; set; }
}
```

**Propósito:** Representa a una persona registrada en el sistema (alumnos, docentes, administrativos).

### 5.2 AsistenciaChecador

```csharp
public class AsistenciaChecador
{
    [Key] [Column("id_asistencias")]
    public int IdAsistencia { get; set; }
    
    [Column("fecha")]
    public DateTime Fecha { get; set; }
    
    [Column("id_personas")]
    public int IdPersonas { get; set; }
    
    [Column("hora_entrada")]
    public TimeSpan HoraEntrada { get; set; }
    
    [Column("hora_salida")]
    public TimeSpan? HoraSalida { get; set; }
    
    [ForeignKey(nameof(IdPersonas))]
    public virtual Personas Persona { get; set; } = null!;
}
```

**Propósito:** Registra las asistencias (entradas/salidas) de cada persona.

### 5.3 Administrador

```csharp
public class Administrador
{
    [Key]
    public int Id { get; set; }
    public string Usuario { get; set; } = null!;
    public string Contrasenia { get; set; } = null!;
}
```

**Propósito:** Credenciales de administrador para acceso al módulo de administración.

### 5.4 Checadores (tabla legada)

```csharp
public class Checadores
{
    [Key] [Column("idchecadas")]
    public int Id { get; set; }
    // ... mapeo a tabla legacy 'checador'
}
```

**Propósito:** Compatibilidad con sistema de checador anterior.

---

## 6. Servicios (Services)

### 6.1 ChecadorService

**Responsabilidad:** Lógica principal de registro de asistencia (entrada/salida).

```csharp
public async Task<AsistenciaResult> MarcarAsistenciaAsync(string matricula)
```

**Funcionalidades clave:**
- Busca a la persona por matrícula
- Verifica si tiene entrada sin salida (turno abierto < 16 horas)
- Si hay turno abierto, registra la salida
- Si no, registra nueva entrada
- Verifica cooldown de 5 minutos entre registros
- Previene registros dobles del mismo tipo

**Lógica del servicio:**
```
1. Obtener persona por matrícula
2. Verificar última asistencia del día
3. Si existe entrada sin salida (y es turno válido):
   - Registrar hora de salida
4. Si no existe o tiene salida:
   - Registrar nueva entrada
5. Validar cooldown de 5 minutos
```

### 6.2 PersonasService

**Responsabilidad:** CRUD completo de personas.

**Métodos principales:**
- `ObtenerTodasAsync()` - Obtiene todas las personas
- `ObtenerPorIdAsync(id)` - Obtiene una persona por ID
- `ObtenerPorMatriculaAsync(matricula)` - Busca por matrícula
- `CrearAsync(persona)` - Crea nueva persona
- `ActualizarAsync(persona)` - Actualiza persona existente
- `EliminarAsync(id)` - Elimina persona
- `ObtenerPersonasConHuellaAsync()` - Obtiene personas con huella registrada

### 6.3 ReportesService

**Responsabilidad:** Generación de reportes usando Dapper (SQL directo).

**Métodos principales:**
- `GenerarReporteAsync(fechaInicio, fechaFin, tipo)` - Reporte de horas por persona
- `GenerarReporteUsuariosAsync(...)` - Reporte detallado de usuarios

**Tipos de asistencia:**
- `Insuficiente`: < 4 horas
- `Parcial`: 4-7 horas
- `Completa`: 8 horas exactas
- `Extra`: > 8 horas

### 6.4 AdministradorService

**Responsabilidad:** Gestión de administradores y autenticación.

**Métodos principales:**
- `ObtenerTodosAsync()` - Lista todos los administradores
- `CrearAsync(admin)` - Crea nuevo administrador
- `ActualizarAsync(admin)` - Actualiza administrador
- `EliminarAsync(id)` - Elimina administrador
- `ValidarCredencialesAsync(user, pass)` - Valida login (contraseñas hasheadas con SHA256)
- `HashPassword(password)` - Genera hash SHA256

### 6.5 FingerprintReaderService

**Responsabilidad:** Interfaz con el hardware lector de huella DigitalPersona.

**Métodos principales:**
- `GetReader()` - Obtiene el primer lector disponible
- `OpenReader()` - Abre el lector
- `CloseReader()` - Cierra el lector
- `GetStatus()` - Verifica estado del lector
- `StartCapture()` / `StopCapture()` - Inicia/detienen captura continua
- `CaptureFingerprint()` - Captura única de huella
- `CreateFmdFromFid(fid)` - Convierte imagen a template FMD
- `IsFingerOnReader()` / `WaitForFingerOn()` - Detecta si hay un dedo
- `CompareFmds(fmd1, fmd2, threshold)` - Compara dos templates

**Umbral de coincidencia:** `70` (threshold ajustable)

### 6.6 FingerPrintService

**Responsabilidad:** Persistencia de huellas dactilares en base de datos.

**Métodos principales:**
- `getFingerPrint(string matricula)` - Obtiene huella de una persona
- `hasFingerPrint(string matricula)` - Verifica si tiene huella
- `saveFingerPrint(matricula, huella)` - Guarda huella
- `getAllFingerPrint()` - Obtiene todas las huellas registradas

### 6.7 ImageService

**Responsabilidad:** Gestión de imágenes (logotipos, carrusel).

**Métodos principales:**
- `LoadImage(path)` - Carga imagen en Bitmap
- `SetLogoChecador(path)` / `GetLogoChecador()` - Logo del checador
- `SetImagenLugar(path)` / `GetImagenLugar()` - Imagen del lugar
- `SetImagenUniversidad(path)` / `GetImagenUniversidad()` - Logo universidad
- `GuardarImagenesCarrusel(lista)` / `ObtenerImagenesCarrusel()` - Carrusel
- `AgregarImagenCarrusel(path)` / `EliminarImagenCarrusel(id)`

### 6.8 ExportService

**Responsabilidad:** Exportación de datos a CSV.

**Métodos principales:**
- `ExportarPersonasCsvAsync(List<Personas>)` - Exporta lista a CSV
- `ExportarDatosCsvAsync(datos, ruta)` - Exporta datos genéricos

### 6.9 SettingsService

**Responsabilidad:** Persistencia de configuraciones en JSON (`appconfig.json`).

**Métodos principales:**
- `Get<T>(clave, defaultValue)` - Obtiene valor de configuración
- `Set<T>(clave, valor)` - Guarda valor de configuración

### 6.10 HashService

**Responsabilidad:** Generación de hashes SHA256.

**Métodos principales:**
- `Hash(input)` - Genera hash SHA256
- `Verify(input, hash)` - Verifica que input coincida con hash

### 6.11 ClipboardHelper

**Responsabilidad:** Copiar texto al portapapeles de Windows.

**Métodos principales:**
- `SetTextAsync(texto)` - Copia texto usando P/Invoke (Win32 API)

### 6.12 AuthService

**Responsabilidad:** Gestión del estado de autenticación del administrador.

**Propiedades:**
- `IsAuthenticated` - Indica si hay sesión activa
- `CurrentAdmin` - Administrador logueado actual

### 6.13 AppMessenger

**Responsabilidad:** Patrón publicador/suscriptor para eventos globales.

**Eventos:**
- `ImagesChanged` - Notifica cambios en imágenes

---

## 7. ViewModels

### 7.1 MainWindowViewModel

**Responsabilidad:** Vista principal - navegación entre módulos.

**Propiedades y comandos:**
- `SelectedViewModel` - ViewModel actual visible
- `SelectedIndex` - Índice del módulo seleccionado (0-3)
- `CarruselImagenActual` / `CarruselTituloActual` - Imagen actual del carrusel
- `AbrirCarruselCommand` - Comando para abrir imagen en grande
- `NavigateToChecadorCommand` ... - Navegación entre vistas

**Vistas disponibles (índices):**
0. ChecadorViewModel
1. PersonasViewModel
2. ReportesViewModel
3. AdministracionViewModel

**Eventos del ciclo de vida:**
- En cambio de índice (0): Llama `Activar()` del ChecadorViewModel (inicia lector)
- En cambio de índice (desde 0): Llama `Desactivar()` del ChecadorViewModel (detiene lector)

### 7.2 ChecadorViewModel

**Responsabilidad:** Lógica de la vista de checador principal.

**Propiedades clave:**
- `Mensaje` / `TipoMensaje` - Estado del mensaje (éxito/error)
- `EstadoLector` - Estado del lector de huella
- `HoraActual` / `FechaActual` - Reloj en tiempo real
- `Matricula` - Matrícula ingresada manualmente

**Comandos:**
- `RegistrarCommand` - Registrar entrada/salida manual
- `InicializarLectorHuellaAsync` - Inicializa el lector

**Flujo de trabajo del lector:**
1. Inicializa lector y verifica conexión
2. Inicia captura continua en bucle
3戴维斯3. Al capturar huella:
   - Convierte imagen a template FMD
   - Compara con todas las huellas registradas
   - Si coincide (> 70%): registra asistencia
   - Si no: muestra error

### 7.3 PersonasViewModel

**Responsabilidad:** Lista filtrada y paginada de personas.

**Propiedades clave:**
- `Personas` - Lista paginada actual
- `Busqueda` - Texto de búsqueda
- `PaginaActual` / `TotalPaginas` - Paginación
- `OrdenActual` / `OrdenDescendente` - Ordenamiento

**Comandos:**
- `BuscarCommand` - Filtra personas por texto
- `PaginarCommand` - Cambia página
- `OrdenarCommand` - Cambia ordenamientrigger
- `VerDetalleCommand` - Abre ventana de detalle
- `CopiarMatriculaCommand` - Copia matrícula al portapapeles

### 7.4 ReportesViewModel

**Responsabilidad:** Configuración y generación de reportes.

**Propiedades clave:**
- `FechaInicio` / `FechaFin` - Rango de fechas
- `TipoReporte` - Tipo de reporte (Horas/Usuarios)
- `TipoPersona` - Filtro por tipo de persona
- `ReporteGenerado` - Lista de resultados

**Comandos:**
- `GenerarCommand` - Genera reporte
- `ExportarCommand` - Exporta a CSV
- `PaginarCommand` - Paginación

### 7.5 AdministracionViewModel

**Responsabilidad:** Panel de administración (CRUD, configuraciones).

**Propiedades clave:**
- `Autenticado` - Si el admin está logueado
- `AdministradorActual` - Admin actual
- `PersonaEnEdicion` - Persona en modo edición
- `ImagenesCarrusel` - Lista de imágenes del carrusel
- `VistaPreviaLogo` / `VistaPreviaLugar` / `VistaPreviaUniversidad` - Previsualizaciones

**Comandos:**
- `LoginCommand` / `LogoutCommand` - Login/logout
- `GuardarPersonaCommand` - Guarda persona
- `EliminarPersonaCommand` - Elimina persona
- `SeleccionarImagenCommand` - Selecciona imagen
- `GuardarImagenesCommand` - Guarda imagen del carrusel
- `CapturarHuellaCommand` - Abre diálogo de captura de huella
- `AbrirCarruselCommand` - Abre imagen del carrusel en grande

### 7.6 FingerprintCaptureViewModel

**Responsabilidad:** Lógica del diálogo de captura de huella.

**Estados:**
- `IsCapturing` - Capturando
- `IsSuccess` - Éxito
- `IsError` - Error

**Eventos:**
- `CaptureCompleted` - Huella capturada con éxito
- `CaptureCancelled` - Captura cancelada

### 7.7 RelayCommand

**Implementación genérica de ICommand para MVVM.**

```csharp
public class RelayCommand : ICommand
{
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    public void Execute(object? parameter)
    public bool CanExecute(object? parameter)
    public void RaiseCanExecuteChanged()
}
```

### 7.8 ViewModelBase

**Clase base para todos los ViewModels.**

```csharp
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "");
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "");
}
```

---

## 8. Vistas (Views)

### 8.1 MainWindow.axaml

**Estructura:**
- Grid de 2 columnas: Sidebar (250px) + Contenido
- Sidebar: Logo, navegación, carrusel de imágenes
- Contenido: `ContentControl` con `ContentTemplate` dinámico
- DataTemplates definen el mapeo ViewModel -> Vista

### 8.2 ChecadorView.axaml

**Elementos:**
- Panel superior: Reloj grande, fecha, estado del lector
- Centro: Imagen preview y mensaje de estado
- Inferior: TextBox para matrícula + botón Marcar
- Indicador de lector de huella

**Eventos:**
- `TextBox.KeyDown` = Enter (registrar)
- Timer para actualización de reloj (cada segundo)

### 8.3 PersonasView.axaml

**Elementos:**
- Barra de búsqueda superior
- Tabla/Lista de personas con columnas
- Encabezados clickeables para ordenar
- Botones: Ver detalle, Copiar matrícula
- Paginación inferior

### 8.4 ReportesView.axaml

**Elementos:**
- Filtros: Fecha inicio/fin, Tipo persona, Tipo reporte
- Botones: Generar, Exportar CSV
- Tabla de resultados con paginación
- Formato: Horas mensual o Detalle de usuarios

### 8.5 AdministracionView.axaml

**Estructura de tabs:**
- **Personas**: CRUD de personas, tabla con edición inline
- **Reportes**: Lista de asistencias, filtros
- **Imágenes**: Configuración de logotipos, carrusel
- **Administradores**: CRUD de administradores

**Configuración de imágenes:**
- Logo del checador
- Imagen del lugar
- Logo de la universidad
- Carrusel de imágenes (agregar/eliminar)

### 8.6 FingerprintCaptureDialog.axaml

**Elementos:**
- Vista previa de la huella
- Estado de captura (capturando/éxito/error)
- Botones: Cancelar, Aceptar, Reintentar
- Timer de timeout (60 segundos)

### 8.7 ImagePreviewDialog.axaml

**Elementos:**
- Imagen a pantalla completa
- Botón cerrar
- Fondo oscuro para resaltar imagen

### 8.8 VerPersonaView.axaml

**Elementos:**
- Foto de la persona
- Datos personales (nombre, apellidos, matrícula, tipo)
- Botón cerrar

---

## 9. Navegación y Flujo de la Aplicación

### Flujo principal:

```
┌─────────────────────────────────────────┐
│             MainWindow                  │
│  ┌──────────┐  ┌──────────────────────┐  │
│  │ Sidebar  │  │   ContentControl     │  │
│  │          │  │   (ViewModel actual) │  │
│  │ [Logo]   │  │                      │  │
│  │ Checador │  │  ┌──────────────┐   │  │
│  │ Personas │  │  │   Checador   │   │  │
│  │ Reportes │  │  │   Personas   │   │  │
│  │ Admin.   │  │  │   Reportes   │   │  │
│  │ [Carousel]│ │  │   Admin.     │   │  │
│  └──────────┘  │  └──────────────┘   │  │
│                 └──────────────────────┘  │
└─────────────────────────────────────────┘
```

### Navegación por índice:
| Índice | ViewModel | Función |
|--------|-----------|---------|
| 0 | ChecadorViewModel | Chequeo de asistencia |
| 1 | PersonasViewModel | Lista de personas |
| 2 | ReportesViewModel | Reportes |
| 3 | AdministracionViewModel | Panel de admin |

### Ciclo de vida:
1. **Inicio**: Selecciona índice 0 (Checador), llama `Activar()`
2. **Cambio a Personas**: `Desactivar()` del checador (cierra lector), muestra Personas
3. **Regreso a Checador**: Llama `Activar()` (reinicia lector)

---

## 10. Integración con Lector de Huella

### Flujo completo:

```
┌──────────────────────┐
│  FingerprintReader   │
│       Service        │
└──────────┬───────────┘
           │
     ┌─────┴─────┐
     ▼          ▼
┌────────┐ ┌──────────┐
│GetReader│ │OpenReader│
└────┬───┘ └────┬─────┘
     │          │
     ▼          ▼
┌───────────────────────┐
│  StartContinuous      │
│   Capture             │
└─────────┬─────────────┘
          │
          ▼
┌──────────────────┐
│ OnHuellaCapturada │
│ callback         │
└────────┬─────────┘
         │
         ▼
┌────────────────────┐
│ CreateFMDFromFid   │
│ (convertir foto →  │
│  template)         │
└────────┬───────────┘
         │
         ▼
┌────────────────────┐
│ Compara con todas │
│ huellas:           │
│ - getAllFingerPrint│
│ - CompareFmds      │
└────────┬───────────┘
         │
         ▼
┌────────────────────┐
│ Si match > 70%:    │
│ MarcarAsistencia()  │
└────────────────────┘
```

### Requisitos del hardware:
- Lector DigitalPersona U.are.U compatible
- Driver instalado correctamente
- DLLs (`dpfj.dll`, `dpfpdd.dll`) en el directorio del ejecutable

---

## 11. Base de Datos

### Configuración (Data/AppDbContext.cs):
```csharp
optionsBuilder.UseMySql(
    "server=localhost;database=checador_sssd;user=root;password=",
    new MySqlServerVersion(new Version(8, 0, 21))
);
```

### Tablas principales:
| Tabla | Descripción |
|-------|------------|
| `personas` | Datos de personas (alumnos, docentes, etc.) |
| `asistencias_checador` | Registros de entrada/salida |
| `administradores` | Credenciales de administradores |
| `checador` (legacy) | Tabla antigua de checador |

### Dual ORM Strategy:
| Componente | Tecnología | Uso |
|------------|-----------|-----|
| CRUD general | EF Core | Personas, Administradores |
| Reportes complejos | Dapper | Reportes de asistencia (SQL complejo) |

---

## 12. Estilos y Recursos

### Paleta de Colores (App.axaml):
| Color | Hex | Uso |
|-------|-----|-----|
| AzulMarino | #0D1B2A | Fondo principal |
| AzulMarinoMedio | #162D52 | Fondos secundarios |
| DoradoUniversidad | #FFDBA0 | Textos destacados |
| GrisTextoClaro | #8A8AA0 | Textos secundarios |
| RojoError | #A52C21 | Errores |
| VerdeExito | #266B29 | Éxito |
| Cappuccino | #473e31 | Bordes/accentos |

### Archivos de estilo (Styles/*.axaml):
- **ButtonStyles.axaml**: Botones primarios, secundarios, iconos
- **TextBoxStyles.axaml**: TextBox con clase `.custom`
- **ComboBoxStyles.axaml**: ComboBox con clase `.custom`
- **DataGridStyles.axaml**: Tablas (ItemsControl transparente)
- **TabControlStyles.axaml**: Tabs personalizados
- **DatePickerStyles.axaml**: Selector de fecha oscuro

### Converters:
- **InverseBoolConverter**: `true` ↔ `false` (para visibilidad)
- **ImagePathToBitmapConverter**: Ruta de archivo → Bitmap de Avalonia
- **BinaryToImage**: Placeholder para conversión de datos binarios a imagen

---

## 13. Dependencias y Paquetes

### Paquetes NuGet principales:
| Paquete | Versión | Propósito |
|---------|---------|-----------|
| Avalonia | 12.0.3 | Framework UI multiplataforma |
| Avalonia.Desktop | 12.0.3 | Soporte para escritorio |
| Avalonia.Themes.Fluent | 12.0.3 | Tema Fluent Design |
| Avalonia.Fonts.Inter | 12.0.3 | Fuente Inter |
| Dapper | 2.1.79 | ORM ligero para SQL |
| DigitalPersona.dpUruNet | 3.4.4.127 | SDK lector de huella |
| Pomelo.EntityFrameworkCore.MySql | 8.0.2 | Proveedor MySQL EF Core |
| Microsoft.EntityFrameworkCore.Design | 8.0.x | Herramientas EF Core |

### Librerías nativas:
- `dpfj.dll` - Procesamiento de huella (Fingerprint jet)
- `dpfpdd.dll` - Driver del dispositivo
- `DPFPApi.dll` - API de DigitalPersona

---

## Notas Finales

### Patrones de diseño utilizados:
- **MVVM**: Separación Model-View-ViewModel
- **DI (Dependency Injection)**: Inyección de dependencias en App.axaml.cs
- **Repository**: Services actúan como repositorios
- **Observer**: INotifyPropertyChanged, eventos

### Convenciones:
- Nombres de métodos en español (dominio del negocio)
- Clases y propiedades en español
- Nombres en PascalCase
- Async/await para operaciones I/O

### Consideraciones:
- La app es **Windows-only** por el uso de P/Invoke con Win32
- Las huellas se almacenan como strings base64 en la base de datos
- Las imágenes del carrusel se guardan en `appconfig.json`
- El lector de huella se desactiva al cambiar de vista para evitar conflictos

---

*Fin del Manual Técnico - Checador SSSD*