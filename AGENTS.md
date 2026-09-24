# AGENTS.md - ChecadorSSSD

> Guía para agentes y para la siguiente persona que dé soporte. Revisada contra el código de la copia de trabajo el 2026-09-24; corresponde a la versión con carrusel en la barra lateral. Las rutas siguientes son relativas a la raíz del repositorio.

## Proyecto y comandos

- Aplicación de escritorio Windows: .NET 8, WinExe, Avalonia UI 12.0.3, MVVM y Microsoft.Extensions.DependencyInjection.
- Control de asistencia y personas con lector DigitalPersona. MySQL mediante Pomelo.EntityFrameworkCore.MySql 8.0.2; reportes mediante Dapper.
- Solución XML `.slnx`, no `.sln`. Usar un SDK de .NET que reconozca este formato.

```powershell
# Compilar
dotnet build ChecadorSSSD.slnx
# Ejecutar en desarrollo
dotnet run --project ChecadorSSSD/ChecadorSSSD.csproj
# Publicar: Release configura win-x64, SelfContained=true y PublishSingleFile=false
dotnet publish ChecadorSSSD/ChecadorSSSD.csproj -c Release -o ./publish
```

La publicación genera una carpeta con dependencias: distribuirla completa. El runtime de .NET incluido no sustituye al controlador/runtime DigitalPersona.

## Mapa de código y ciclo de vida

- `ChecadorSSSD/Program.cs`: entrada, comprobación del servicio Windows `DpHost` y arranque de Avalonia. Solo informa mediante `Trace`; no instala ni inicia el servicio.
- `ChecadorSSSD/App.axaml.cs`: contenedor DI, ventana principal y comprobación inicial de MySQL. Registra `AppDbContext` y servicios de datos como Scoped; `FingerprintService`, ViewModels y vistas como Singleton.
- `ChecadorSSSD/ViewModels/MainWindowViewModel.cs`: navegación por `SelectedIndex`: 0 Checador, 1 Personas, 2 Reportes, 3 Administración. También maneja el carrusel.
- `ChecadorSSSD/ViewModels/ChecadorViewModel.cs`: `Activar()` inicializa el lector si hace falta, espera la carga de galería e inicia verificación; `Desactivar()` detiene verificación y cancela captura.
- Detalle de navegación a revisar al diagnosticar capturas fuera de Checador: `_previousIndex` inicia en -1, aunque el constructor activa Checador; la desactivación al navegar solo ocurre si `_previousIndex == 0`. No asumir que la primera salida siempre detiene el lector.
- `ChecadorSSSD/Services/FingerprintService.cs`: lector compartido y un único manejador `OnCaptured`, conectado al evento SDK `On_Captured`; despacha a inscripción o verificación. `EnrollAsync()` detiene la verificación antes de capturar.
- Los callbacks del SDK ocurren fuera del hilo de UI: usar `Dispatcher.UIThread.Post()`/`InvokeAsync()` para actualizarla. Preservar locks, cancelaciones y la exclusión de verificaciones mediante `Interlocked`.
- `FingerprintService` crea scopes mediante `IServiceScopeFactory` para cada operación de BD. No compartir un `DbContext` entre callbacks concurrentes ni capturarlo en un Singleton.

## Base de datos y configuración

- El motor actual es **MySQL**, aunque documentos antiguos mencionen SQLite.
- `ChecadorSSSD/Services/DatabaseConnectionService.cs` lee `MySqlConnection` de `appconfig.json`. Existe un fallback en código a localhost, base `subdireccion_ss_urn`, con credenciales predeterminadas; revisar este servicio si se conecta a una base inesperada.
- `SettingsService` guarda `appconfig.json` junto al ejecutable. El JSON es un diccionario de **valores string**, incluso para booleanos. Si falta o no puede leerse, `Get()` devuelve el valor predeterminado. La carpeta debe permitir escritura para guardar cambios.
- Al abrir la ventana, la app prueba conexión con `SELECT 1`; si falla, ofrece `DatabaseConnectionDialog`. Al confirmar el reinicio, lanza de nuevo el ejecutable. La conexión de EF y Dapper se fija al construir DI: los cambios requieren reiniciar.
- `SELECT 1` prueba conectividad, no que existan las tablas/columnas necesarias.
- `ChecadorSSSD/Data/AppDbContext.cs` mapea `personas`, `administradores` y `asistencias_checador`, con columnas como `id_persona`, `apellido_paterno`, `matricula` y `huella`; declara índice único para matrícula.
- **`CSSSD.sql` es un dump antiguo**: su cabecera indica `checador_curn` y usa estructuras como `checadores` y `personas.Id`, distintas del modelo actual. Contiene `DROP TABLE`; no usarlo como migración ni restaurarlo sobre una BD en uso. Comparar el esquema real con el modelo antes de preparar una instalación.
- `Personas.Huella` es `byte[]?`. La columna `personas.huella` debe ser **LONGBLOB nullable** para almacenar el XML completo sin limitarlo a un `BINARY(n)` insuficiente. El mapeo actual solo declara el nombre, sin `HasColumnType("longblob")`: verificar el tipo real en MySQL. El tipo de almacenamiento no evita errores de serialización ni garantiza por sí solo un match.

## Huellas DigitalPersona: contrato de persistencia

- Paquete NuGet **`DigitalPersona.dpUruNet` 3.4.4.127**, familia DPUruNet 3.4.4, namespace `DPUruNet`.
- `Fid` representa la captura de imagen; `Fmd` es el objeto del SDK con características biométricas. Se toman **4 FMD individuales del mismo dedo** y `Enrollment.CreateEnrollmentFmd(Constants.Formats.Fmd.ANSI, ...)` los consolida en una plantilla ANSI.
- Si el SDK devuelve `DP_ENROLLMENT_INVALID_SET`, hay que repetir la inscripción con muestras consistentes del mismo dedo. No mezclar dedos ni guardar las capturas individuales como si fueran la plantilla consolidada.
- Para persistir: **`Fmd.SerializeXml(fmd)` ya devuelve el string XML**; después `Encoding.UTF8.GetBytes(xml)` produce el `byte[]` que `GuardarHuellaAsync()` guarda en `personas.huella`.
- Para leer: `Encoding.UTF8.GetString(bytes)` recupera el XML y **`Fmd.DeserializeXml(xml)`** reconstruye el FMD. Mantener UTF-8 en ambos sentidos; no sustituirlo por la codificación predeterminada del equipo ni alterar los bytes.
- Aunque la plantilla se genere como ANSI, el contrato persistido de esta aplicación es **XML del SDK codificado en UTF-8**, no un bloque ANSI/ISO crudo. La reconstrucción compatible utilizada con DPUruNet 3.4.4 es `DeserializeXml`; no reemplazarla por una supuesta deserialización ANSI/ISO.
- Otro checador necesita poder leer este XML con un SDK compatible. No asumir compatibilidad al cambiar DLL, versión o proveedor por el hecho de generar ANSI. Según la experiencia de integración aportada por mantenimiento, puede ser necesaria la recaptura si el nuevo sistema no puede reconstruir estas plantillas; validar compatibilidad antes de migrar.
- **No hay cifrado de huellas en la aplicación**: el LONGBLOB contiene XML sin cifrar con la representación de la plantilla biométrica. XML, UTF-8 y cualquier codificación interna del SDK no son cifrado.

### Inscripción y guardado

```text
Dedo -> Reader.CaptureAsync -> Fid (captura de imagen)
     -> FeatureExtraction.CreateFmdFromFid(..., Fmd.ANSI)
     -> Fmd individual (x4, mismo dedo)
     -> Enrollment.CreateEnrollmentFmd(Fmd.ANSI, 4 FMDs)
     -> Fmd consolidado -> Fmd.SerializeXml -> string XML
     -> Encoding.UTF8.GetBytes -> byte[]
     -> GuardarHuellaAsync -> AppDbContext.SaveChangesAsync
     -> MySQL: personas.huella (LONGBLOB)
```

`GuardarHuellaAsync()` busca por matrícula, persiste la plantilla y, si logra reconstruirla, reemplaza la entrada de esa matrícula en `_gallery`. Un guardado exitoso en BD no demuestra por sí solo que la reconstrucción y el matching funcionen.

### Carga de galería al activar Checador

```text
MySQL: personas con Huella != null y Huella.Length > 0
     -> byte[] -> Encoding.UTF8.GetString -> string XML
     -> comprobación TrimStart().StartsWith("<")
     -> Fmd.DeserializeXml -> Fmd
     -> galería temporal (matricula, Fmd) -> reemplazo de _gallery bajo lock
```

`RecargarGaleriaAsync()` usa `AsNoTracking()`. El chequeo de `<` es solo un filtro inicial, no validación completa de XML. Las plantillas que no se reconstruyen se registran en log y se omiten. Si falla la consulta, el método captura la excepción y conserva la galería anterior: revisar los logs, no solo el mensaje de la UI. Cambios directos en BD u otra instancia requieren recargar la galería local.

### Verificación en vivo

```text
Dedo -> Fid -> Fmd capturado
     -> Comparison.Compare(fmdCaptura, 0, gallery[i].fmd, 0)
     -> resultado DP_SUCCESS y score >= 0
     -> elegir el menor score que sea < DEFAULT_THRESHOLD
     -> onMatch(matricula) en el hilo de UI
```

- `PROBABILITY_ONE = 0x7fffffff`; `DEFAULT_THRESHOLD = PROBABILITY_ONE / 100000 = 21474` por división entera.
- Menor score significa mayor similitud. El umbral es estricto: **score < 21474**, no `<=`. Si ninguna comparación válida cumple, se llama `onNoMatch`.
- Se compara contra una copia de la galería en memoria, no se consulta MySQL por cada comparación. Reconocer la huella y registrar asistencia son pasos distintos.

### Eliminación

`EliminarHuellaAsync()` asigna `persona.Huella = null`, guarda los cambios y elimina la matrícula de `_gallery`. Tras un guardado correcto, MySQL contiene **SQL NULL** en esa columna: el valor actual ya no contiene el byte[] XML. Esto no implica borrar copias de respaldo o registros históricos del servidor. Otras instancias mantienen su propia galería hasta recargarla.

## Instalación y diagnóstico del lector

- Verificar controlador/runtime DigitalPersona, conexión USB y servicio Windows `DpHost` en ejecución. La app selecciona el primer lector de `ReaderCollection.GetReaders()`.
- `dpfpdd.dll` y `dpfj.dll` deben acompañar al ejecutable; el `.csproj` declara su copia y la de DLL del paquete NuGet. Mantener compatibilidad de arquitectura con la publicación `win-x64`.
- Hay instaladores SDK y RTE x86/x64 en `Huellas_Driver_And_SDK/U.are.U_Windows_3.2.0.89/`. La versión del instalador es distinta de la del paquete NuGet: no confundirlas ni cambiar dependencias sin probar captura, inscripción y lectura de plantillas existentes.
- Según las notas del mantenedor, el SDK incluye ejemplos de Java, .NET, Visual Basic y JavaScript. Consultar los ejemplos/documentación de la instalación del SDK para integrar clases y funciones; no asumir que una API encontrada para otra versión existe en 3.4.4.
- Log principal: **`%LOCALAPPDATA%\ChecadorSSSD\logs\huellas.log`**, del usuario de Windows que ejecuta la aplicación (`ChecadorSSSD/Services/FingerprintLogger.cs`). Incluye categorías HARDWARE, ENROLL, DB, VERIFY, MATCH, NOMATCH y ERROR; también se escribe a `Trace`.
- El log acumula entradas sin rotación implementada y registra matrículas, nombres y scores. `App.axaml.cs` habilita `EnableSensitiveDataLogging()` de EF; revisar los datos antes de compartir diagnósticos.
- Si no detecta el lector: comprobar `DpHost`, runtime, DLL y arquitectura; buscar errores HARDWARE/Trace. Si captura pero no reconoce: revisar cantidad de huellas cargadas, errores de `DeserializeXml`, integridad de los bytes y scores de comparación antes de cambiar el umbral.

## Reglas de asistencia y recursos locales

- `ChecadorSSSD/Services/ChecadorService.cs` usa `DateTime.Now` del equipo; revisar reloj y zona horaria si los registros tienen horas incorrectas.
- Sin asistencia abierta, crea entrada. Con una abierta, marca salida salvo que haya pasado **menos de 1 minuto** desde la entrada. Un comentario menciona 10 minutos, pero la condición ejecutada es `TotalMinutes < 1`.
- Si la asistencia abierta supera **16 horas**, la cierra con salida igual a entrada (cero horas) y abre una nueva. Considerar esta regla al investigar turnos y reportes.
- `ImageService` guarda rutas de logos e imágenes y la lista `CarruselImages` en `appconfig.json`; las rutas relativas se resuelven respecto al ejecutable. Conservar los archivos referenciados al mover la instalación; copiar solo el JSON no copia las imágenes.
- El carrusel crea su temporizador de 5 segundos si hay imágenes al inicializarse. Si arrancó vacío y luego se agregaron, el manejador de cambios actualiza la imagen pero no crea ese temporizador; reiniciar puede ser necesario para que rote.

## Convenciones y comprobación de cambios

- Nullable habilitado; nombres de dominio en español (`Personas`, `Huella`, `Matricula`).
- Bindings compilados de Avalonia habilitados: los cambios de propiedades/XAML deben compilar para comprobar rutas de bindings.
- No se encontraron proyectos de pruebas automatizadas. Para cambios funcionales, compilar y hacer una prueba manual con MySQL y lector: inscribir el mismo dedo 4 veces, guardar, reiniciar para comprobar deserialización, reconocer, registrar asistencia y eliminar la huella comprobando que deja de reconocerse tras recargar.
- No editar a mano salidas generadas en `bin/`, `obj/`, `publish/`, `artifacts/` ni metadatos del IDE. `inspect_api.cs`/`InspectDP.cs` bajo directorios de compilación son artefactos, no fuentes de la aplicación.
- La copia de trabajo puede contener modificaciones locales previas: revisar `git status` y preservar cambios ajenos a la tarea.
