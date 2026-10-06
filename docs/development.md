# Desarrollo local

La API usa **PostgreSQL** (Npgsql + EF Core 9, nombres en `snake_case`). En producción corre
sobre Neon; en local, sobre un contenedor `postgres:16`.

## Requisitos

- .NET SDK 8 o superior (los proyectos apuntan a `net8.0`, así que hace falta el runtime 8).
- Docker (Docker Desktop en Windows), para la base local y para los tests de integración.
- Herramienta `dotnet-ef`: `dotnet tool install -g dotnet-ef`.

## 1. Levantar PostgreSQL local

Elegí una contraseña local (no se commitea en ningún lado):

```bash
docker run -d --name lvbuild-postgres \
  -e POSTGRES_USER=lvbuild \
  -e POSTGRES_PASSWORD=<tu-password-local> \
  -e POSTGRES_DB=lvbuild \
  -p 5432:5432 \
  -v lvbuild-pgdata:/var/lib/postgresql/data \
  postgres:16
```

Para pararlo o volver a arrancarlo: `docker stop lvbuild-postgres` / `docker start lvbuild-postgres`.

## 2. Configurar secretos (user-secrets)

`appsettings*.json` no lleva contraseñas: `ConnectionStrings:DefaultConnection` y `Jwt:Key`
están vacíos a propósito y se cargan desde user-secrets (en Development) o desde variables
de entorno (en cualquier entorno).

```bash
cd LvApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=lvbuild;Username=lvbuild;Password=<tu-password-local>"
dotnet user-secrets set "Jwt:Key" "<una-clave-de-al-menos-32-caracteres>"
```

Equivalente con variables de entorno: `ConnectionStrings__DefaultConnection` y `Jwt__Key`.

## 3. Crear el esquema

Hay una sola migración (`InitialCreate`), con el usuario administrador y los roles sembrados.

```bash
dotnet ef database update -p LvInfrastructure -s LvApi
```

Después de cambiar el modelo:

```bash
dotnet ef migrations add <Nombre> -p LvInfrastructure -s LvApi -o Migrations
dotnet ef migrations has-pending-model-changes -p LvInfrastructure -s LvApi
```

## 4. Correr la API

```bash
dotnet run --project LvApi
```

## 5. Tests

```bash
dotnet test
```

- **Unitarios** (`LvTest/Services`): EF Core InMemory, no necesitan nada.
- **HTTP** (`LvTest/Http`): `WebApplicationFactory` con InMemory, no necesitan nada.
- **Integración** (`LvTest/Integration`): levantan un contenedor `postgres:16-alpine` con
  Testcontainers (uno por colección), aplican las migraciones reales y limpian los datos
  con Respawn antes de cada test. **Necesitan Docker corriendo.**

Sin Docker, se pueden correr solo los demás:

```bash
dotnet test --filter "FullyQualifiedName!~LvTest.Integration"
```

Formato: `csharpier check .` (o `csharpier format .`).

## Convenciones de PostgreSQL

- **Fechas.** Las marcas de tiempo (`CreatedAt`, `PaidAt`, `ClosingDate`, …) se guardan en
  `timestamptz` y siempre con `DateTime.UtcNow`: Npgsql rechaza `DateTime` con
  `Kind` distinto de `Utc` en esas columnas. Las fechas de calendario sin hora (semanas de
  bitácora y planilla, fechas de oferta y proyecto, incidentes, cumpleaños) son columnas
  `date`. Si se compara una fecha de Costa Rica con un `timestamptz`, convertir los límites
  del día con `CostaRicaTime.StartOfDayUtc` (`LvApplication/Common`). No usar
  `Npgsql.EnableLegacyTimestampBehavior`.
- **Mayúsculas.** PostgreSQL compara texto distinguiendo mayúsculas. Los emails se guardan y
  buscan normalizados (`EmailNormalizer`) y el chequeo de SKU duplicado compara con `lower()`.
- **Nombres.** Tablas, columnas, índices y constraints en `snake_case`. Los nombres explícitos
  (`ToTable`, `HasCheckConstraint`, `HasFilter`) se escriben ya en `snake_case`, porque
  `UseSnakeCaseNamingConvention()` no reescribe los nombres puestos a mano.
- **Datos sembrados.** `HasData` con Ids explícitos no avanza la secuencia de identidad; las
  tablas sembradas (`users`, `roles`) declaran `HasIdentityOptions(startValue: …)` por encima
  del Id sembrado más alto. Si se agregan semillas, ajustar ese valor.

## Neon (producción)

- Connection string: `Host=<endpoint>.neon.tech;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require`,
  en la variable de entorno `ConnectionStrings__DefaultConnection` del hosting (nunca en el repo).
- Para `dotnet ef database update` usar el endpoint **directo** (sin `-pooler`); la app puede
  usar el endpoint con pooler.

### Re-sembrar la demo en producción

El seeder demo es idempotente: si ya existe `gerencia@lvbuild.test` no hace nada. Las fechas
de la demo se calculan relativas al día en que corre el seed (el proyecto arranca el lunes de
cinco semanas antes), así que para "rejuvenecer" la demo o cargar cambios del seeder hay que
vaciar la base y dejar que el siguiente arranque la vuelva a crear:

1. En la consola de Neon (**SQL Editor**, base de la app, con el rol dueño, p. ej.
   `neondb_owner`) ejecutar:

   ```sql
   -- Borra TODAS las tablas de la app, incluido __EFMigrationsHistory.
   DROP SCHEMA public CASCADE;
   CREATE SCHEMA public;
   GRANT ALL ON SCHEMA public TO public;
   ```

   Alternativa sin SQL: en Neon, **Branches → (rama de producción) → Reset from parent** o
   restaurar la rama a un punto anterior al primer deploy.
2. En Render, **Manual Deploy → Restart service** (o cualquier deploy). Con
   `Database__MigrateOnStartup=true` y `Seed__Demo=true` el arranque aplica las migraciones y
   vuelve a sembrar la empresa demo con fechas relativas a ese día.
3. Verificar `GET /health/ready` y entrar con `gerencia@lvbuild.test` / `LvBuild#2026`.

> Esto borra cualquier dato creado a mano en la demo. Nunca hacerlo sobre una base con datos
> reales: ahí `Seed__Demo` debe estar en `false`.
