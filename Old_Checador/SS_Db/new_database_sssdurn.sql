create database subdireccion_ss_urn;
use subdireccion_ss_urn;
create table personas(
                         id_persona int auto_increment primary key,
                         nombre varchar(200) not null,
                         apellido_paterno varchar(200) null,
                         apellido_materno varchar(200) null,
                         tipo_persona varchar(50) not null default 'Brigadista',
                         matricula varchar(50) not null,
                         huella longblob null,
                         imagen varchar(200) null
);

create table asistencias_checador(
                                     id_asistencia int auto_increment primary key,
                                     id_persona int not null,
                                     hora_entrada datetime not null default now(),
                                     hora_salida datetime null,
                                     estatus varchar(30) null,
                                     foreign key (id_persona) references personas(id_persona) on delete cascade
);
create table administradores(
                                id_admin int primary key auto_increment,
                                id_persona int not null,
                                usuario varchar(200) not null,
                                contrasenia varchar(500) not null,
                                foreign key (id_persona) references personas(id_persona) on delete cascade
);
-- copiar los registros de persona a la nueva db
insert into personas(nombre,apellido_paterno, apellido_materno, tipo_persona, matricula, huella, imagen)
select
    distinct
    Nombre,
    Apellido_paterno,
    Apellido_materno,
    Tipo_persona,
    Matricula,
    huella,
    Imagen
from checador_sssurn.personas where Matricula is not null and Matricula != '';

-- insertar asistencias del primer checador a la nueva db
INSERT INTO asistencias_checador (id_persona, hora_entrada, hora_salida)
SELECT
    p.id_persona,
    CAST(CONCAT(ent.Fecha, ' ', ent.Hora) AS DATETIME) AS hora_entrada,
    CAST(CONCAT(sal.Fecha, ' ', sal.Hora) AS DATETIME) AS hora_salida
FROM
    checador_sssurn.checador ent
        INNER JOIN
    personas p ON ent.Matricula = p.Matricula

        LEFT JOIN
    checador_sssurn.checador sal ON ent.Matricula = sal.Matricula
        AND ent.Fecha = sal.Fecha
        AND sal.Tipo_accion = 'Salida'
WHERE
    ent.Tipo_accion = 'Entrada';



DELETE p1
FROM personas p1
         INNER JOIN personas p2 ON p1.Matricula = p2.Matricula
WHERE
   -- Si p1 no tiene huella pero p2 si tiene (Se elimina p1)
    ((p1.huella IS NULL OR LENGTH(p1.huella) = 0) AND p2.huella IS NOT NULL AND LENGTH(p2.huella) > 0)

   OR

   -- Si Ambos estan en la misma condicion de huella, pero p1 tiene un ID mayor/mas reciente (Se elimina p1)
    ((
         ((p1.huella IS NULL OR LENGTH(p1.huella) = 0) AND (p2.huella IS NULL OR LENGTH(p2.huella) = 0))
             OR
         (p1.huella IS NOT NULL AND LENGTH(p1.huella) > 0 AND p2.huella IS NOT NULL AND LENGTH(p2.huella) > 0)
         )
        AND p1.id_persona > p2.id_persona);

-- cerrar todas las entradas sin cerrar anteriores
UPDATE asistencias_checador
SET
    estatus = 'cerrado',
    hora_salida = hora_entrada
WHERE
    hora_salida IS NULL
    AND (estatus != 'cerrado' OR estatus IS NULL)
    AND hora_entrada <= NOW() - INTERVAL 1 DAY;
