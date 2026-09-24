use subdireccion_ss_urn;

-- Query para reportes de usuario
SELECT
    p.Matricula,
    p.Nombre,
    p.Apellido_paterno,
    p.Apellido_materno,
    DATE(a.hora_entrada) AS Fecha,
    TIME(a.hora_entrada) AS Hora_entrada,
    TIME(a.hora_salida) AS Hora_salida,
    ROUND(TIMESTAMPDIFF(MINUTE, a.hora_entrada, a.hora_salida) / 60.0, 2) AS Horas_trabajadas
FROM
    personas p
        JOIN
    asistencias_checador a ON p.id_persona = a.id_persona
WHERE
    -- Cambiar el tipo de persona dependiendo del filtro
    -- Brigadista, Empleado y Asesor
    p.Tipo_persona = 'Asesor'
  -- Desde el inicio del primer dia hasta el final del ultimo
  AND a.hora_entrada >= '2020-05-01 00:00:00'
  AND a.hora_entrada <= '2026-05-22 23:59:59'
  -- se excluyen los que no tienen hora de salida o su estatus esta marcado como cerrado.
  AND a.hora_salida IS NOT NULL OR estatus <> 'cerrado'
ORDER BY
    p.Matricula,
    a.hora_entrada;

-- Script para reporte de horas mensuales

SELECT
    p.Matricula,
    p.Nombre,
    p.Apellido_paterno,
    p.Apellido_materno,
    COUNT(a.id_asistencia) AS Asistencias_totales,
    SUM(ROUND(TIMESTAMPDIFF(MINUTE, a.hora_entrada, a.hora_salida) / 60.0, 2)) AS Horas_totales
FROM
    personas p
        JOIN
    asistencias_checador a ON p.id_persona = a.id_persona
WHERE
    -- Filtro por tipo de usuario
    -- Brigadista, Empleado y Asesor

    p.Tipo_persona = 'Brigadista'
  AND a.hora_entrada >= '2026-06-10 00:00:00'
  AND a.hora_entrada <= '2026-06-11 23:59:59'
  AND a.hora_salida IS NOT NULL
GROUP BY

    p.id_persona,
    p.Matricula,
    p.Nombre,
    p.Apellido_paterno,
    p.Apellido_materno
ORDER BY
    p.Matricula;
