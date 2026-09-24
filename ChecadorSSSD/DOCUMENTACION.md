# Documentacion Tecnica - Checador SSSD

## 1. Descripcion General

**Checador SSSD** es una aplicacion de escritorio multiplataforma desarrollada con **Avalonia UI** y **.NET 8**. Su proposito es gestionar el control de asistencia y registro de horas laboradas del personal de una institucion educativa (u otra organizacion). Permite administrar personas (empleados, brigadistas, asesores), generar reportes de horas, y gestionar el checado de entrada y salida mediante un lector de huellas o registro manual.

Los usuarios se dividen en:
- **Usuarios publicos**: Acceden al modulo de reportes generales y consulta de datos generales.
- **Administradores**: Acceso restringido a traves de login, permitiendo CRUD de personas, reportes individuales, gestion de imagenes institucionales, y administracion de cuentas de administrador.

## 2. Arquitectura y Tecnologias

| Tecnologia | Version | Uso |
|---|---|---|
| .NET | 8.0 | Runtime y base del proyecto |
| Avalonia UI | 11.x | Framework de UI multiplataforma |
| Entity Framework Core | 8.x | ORM para acceso a base de datos |
| Dapper | 2.x | Consultas SQL crudas (reportes) |
| SQLite / MySQL | - | Base de datos persistente (configurable en AppDbContext) |
| MVVM | - | Patron de arquitectura (Model-View-ViewModel) |
| Dependecy Injection | - | Inyeccion de servicios en ViewModels a traves de DI container |

### Estructura de Proyecto

```
ChecadorSSSD/
├── App.axaml              # Recursos globales, paleta de colores, tema
├── App.axaml.cs           # Configuracion de servicios, ventana principal
├── Views/                 # Vistas de la UI (AXAML + code-behind .cs)
│   ├── MainWindow.axaml           # Ventana contenedor con navegacion lateral
│   ├── MainWindow.axaml.cs
│   ├── ReportesView.axaml         # Reportes publicos (tipos, fechas, busqueda)
│   ├── AdministracionView.axaml   # Login + Tabs: Personas, Reportes, Imagenes, Admin
│   └── ... (otras vistas)
├── ViewModels/            # ViewModels de la app (logica de presentacion)
│   ├── ReportesViewModel.cs
│   ├── AdministracionViewModel.cs
│   └── ...
├── Services/              # Logica de negocio (servicios que acceden a DB)
│   ├── ReportesService.cs
│   ├── PersonasService.cs
│   ├── AdministradorService.cs
│   ├── ImageService.cs
│   ├── AuthService.cs
│   └── ...
├── Models/                # Entidades de dominio (EF Core)
│   ├── Personas.cs
│   ├── Administrador.cs
│   ├── Checador.cs
│   └── ...
├── Data/                  # AppDbContext (configuracion de EF Core)
└── Assets/                # Iconos, imagenes predeterminadas
```

## 3. Flujo de Trabajo Principal

### 3.1 Vista Publica (Reportes)
1. El usuario selecciona un **tipo de reporte** ("Horas Mensual" o "Usuarios Mensual").
2. Define un **rango de fechas** (Desde - Hasta).
3. Opcionalmente filtra por **tipo de usuario** (Todos, Brigadista, Asesor, etc.).
4. Presiona **Generar** y el sistema consulta la base de datos, agrupando entradas y salidas para mostrar horas acumuladas por persona o detalle individual.
5. El resultado se muestra en una tabla con paginacion (`50` registros por pagina). El usuario puede buscar por nombre/matricula y paginar.

### 3.2 Modulo Administracion

#### Login
- Se muestra al iniciar desde la tab de Administracion.
- Valida credenciales contra una tabla de `Administrador` en la base de datos.
- Si se autentica correctamente, se muestran las pestanas internas.

#### Personas (CRUD)
- **Tabla**: Listado de todas las personas registradas (Nombre, Matricula, Rol, Acciones).
- **Formulario**: Permite Agregar o Editar una persona (Nombre, Apellidos, Matricula, Tipo).
- **Buscar**: Filtro en tiempo real por nombre, apellido o matricula.

#### Reportes (Individuales)
- Similar a la vista publica pero permite **seleccionar una persona especifica** del catalogo.
- Genera reportes **exclusivos** de esa persona para un periodo determinado.
- Muestra totales generales (Total asistencias, Total horas) y tabla detallada por dia.

#### Imagenes
- **Logos Institucionales**: Permite asignar imagenes para Logo del Modulo, Logo de la Universidad, e Imagen del Lugar. Se muestran en miniaturas clicables.
- **Carrusel Sidebar**: Administra un carrusel de imagenes que se visualiza en la barra lateral de la aplicacion (credenciales, anuncios, etc.). Las imagenes son clicables para ver en pantalla completa.

#### Administradores
- Listado, agregar, editar y eliminar cuentas de administrador.
- La tabla de administradores reutiliza el estilo de "Personas" (cabezeras doradas, bordes redondeados).

## 4. Servicios Principales

### 4.1 `ReportesService`
- **`GenerarReporteAsync(DateTime inicio, DateTime fin, TipoPersona? tipo)`**: Genera un resumen agrupado de horas por persona en un periodo. Empareja entradas y salidas del checador existente en la DB.
- **`GenerarReporteUsuariosAsync(...)`**: Genera un detalle individual por dia ( fecha, entrada, salida, horas diarias, clasificacion ).

### 4.2 `PersonasService`
- CRUD completo sobre la tabla `personas`.

### 4.3 `AuthService`
- Validacion de login comparando `Nombre` y `Contrasenia` (normalmente hasheada) contra la tabla `administrador`.

### 4.4 `ImageService`
- Gestiona rutas de imagenes institucionales y del carrusel.
- Las imagenes institucionales se almacenan en archivos de configuracion (Settings).
- El carrusel se serializa a JSON en la configuracion.

## 5. Modelo de Datos (Entidades Relevantes)

### Persona
```
IdPersonas (PK)
Nombre
ApellidoPaterno
ApellidoMaterno
Matricula
TipoPersona (Brigadista, Asesor, Personal Administrativo, Empleado)
Imagen (ruta de archivo opcional)
```

### Checador (Registro)
```
Id (PK)
Matricula (FK a Persona)
Nombre, Apellido_paterno, Apellido_materno, Tipo_persona
Fecha
Hora
Tipo_accion (Entrada/Salida)
```

### Administrador
```
IdAdmin (PK)
Nombre
Contrasenia (almacenada hasheada)
```

## 6. Consideraciones Técnicas Importantes

### 6.1 Imagenes en Avalonia
Avalonia no muestra rutas de archivos directas como strings en `Image.Source`. Para visualizar una imagen que reside en el disco duro, se debe cargar como `Avalonia.Media.Imaging.Bitmap`.

El proyecto incluye un converter universal `ImagePathToBitmapConverter` registrado en `App.axaml` para facilitar este proceso en el XAML:

```xml
<Image Source="{Binding RutaUniversidad, Converter={StaticResource ImagePathToBitmap}}" ... />
```

### 6.2 Reportes y Paginacion
- Los resultados se obtienen desde la base de datos una sola vez al hacer clic en **Generar**, y luego se paginan y filtran **en memoria** (client-side).
- Las propiedades `ResultadosPaginadosHoras`, `ResultadosPaginadosUsuarios`, `TotalPaginas`, etc., se calculan dinamicamente a partir de las listas cargadas.

### 6.3 Filtro de Tipo de Persona en SQL
- En los reportes, la columna `Tipo_persona` se filtra en la consulta SQL. Se requiere un alias de tabla expl&#195;&#173;cito (ej. `c.Tipo_persona = @tipoPersona`) para evitar errores de ambiguedad cuando se hace `JOIN` con la tabla `personas`.

### 6.4 Comunicacion entre ViewModels
- Existe un `AppMessenger` que notifica a los componentes de la UI cuando cambian las imagenes institucionales (para que se refresquen los controles que las muestran).

## 7. Consideraciones Legales / Compliance
- Las contrasenas de administrador son hasheadas antes de almacenarse.
- El control de asistencia registra entradas y salidas y las empareja automaticamente para calcular horas trabajadas.
- La gestion de imagenes permite personalizacion institucional (logos, carrusel) sin modificar el codigo fuente.
