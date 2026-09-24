# Cambios Aplicados - ReportesService.cs

## Fecha: 2026-05-20

---

## 1. Problema: "Unable to cast object type 'System.DBNull' to type 'System.String'"

### Causa
El `IQueryable` intentaba traducir `c.Hora` a SQL, pero `Hora` tiene `[NotMapped]` en el modelo.

### Fix
Se cambió el patrón a **siempre cargar a memoria primero** y luego filtrar/ordenar:

```csharp
// ANTES (provocaba error):
var registros = await query
    .OrderBy(c => c.Matricula)
    .ThenBy(c => c.Fecha)
    .ThenBy(c => c.Hora)  // ❌ [NotMapped] no traduce a SQL
    .ToListAsync();

// DESPUÉS (correcto):
var registros = await _context.Checadores
    .Where(c => c.Fecha >= fechaInicio.Date && c.Fecha <= fechaFin.Date)
    .ToListAsync();  // ✅ Cargar todo primero

// Luego filtrar en memoria:
registros = registros
    .Where(c => !string.IsNullOrEmpty(c.TipoPersona))
    .OrderBy(c => c.Matricula)
    .ThenBy(c => c.Fecha)
    .ThenBy(c => c.Hora)
    .ToList();
```

---

## 2. Problema: Tabla de horas no se muestra

### Causa
El `ViewModel` intentaba usar `MostrarResultados` pero no se notificaba correctamente la propiedad visibilidad.

### Fix
En `GenerarReporteAsync` se asegura que siempre retorne datos con `return (reporte, true);` cuando hay registros, y el `ViewModel` maneja correctamente `MostrarResultados = true`.

---

## 3. Problema: Buscador superpuesto con filtros

### Fix
En `ReportesView.axaml` se separó el buscador en su propia fila (`Row 2`) separada del contenedor de filtros (`Row 1`), evitando la superposición.

---

## 4. Cambios en ReportesService.cs

### GenerarReporteAsync
- Carga todo a memoria primero con `.ToListAsync()`
- Filtra por tipo de persona en memoria (no en SQL)
- Agrupa por matrícula
- Calcula parejas entrada-salida
- Retorna asistencias = parejas válidas

### GenerarReporteUsuariosAsync
- Carga todo a memoria primero
- Filtra por tipo de persona en memoria
- Agrupa por Matrícula y Fecha
- Empareja entradas con salidas por orden cronológico
- Calcula horas diarias con `FLOOR` (entero)
- Clasifica: Unidades / Decenas / Centenas / Mas de mil horas
- Ordena por Matricula, luego Fecha

### ReporteDetalleItem
- `HorasDiarias`: `int` (antes `double`)
- `HorasTrabajadas`: `int` (antes `double`)

---

## 5. Prueba Recomendada

1. Ejecutar con fechas largas (ej: 2024-01-01 a 2026-05-20)
2. Seleccionar "Horas Mensual" -> Verificar tabla con datos
3. Seleccionar "Usuarios Mensual" -> Verificar tabla con parejas entrada-salida
4. Filtrar por tipo de persona -> Verificar que filtra correctamente sin error de NULL
