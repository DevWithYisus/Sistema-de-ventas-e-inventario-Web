# Despliegue de la API (Minino's Pet Shop)

## 1. Variables de entorno

En producción no hay User Secrets: todo va en variables de entorno del hosting.
El doble guion bajo `__` equivale a `:` en la configuración de .NET.

| Variable | Ejemplo | Obligatoria |
|---|---|---|
| `ConnectionStrings__Default` | `Host=...;Port=5432;Database=sistema_ventas;Username=ventas_app;Password=...;SSL Mode=Require` | Sí |
| `Jwt__Issuer` | `mininos-api` | Sí |
| `Jwt__Audience` | `mininos-web` | Sí |
| `Jwt__Key` | 32+ caracteres aleatorios | Sí |
| `Jwt__MinutosExpiracion` | `60` | No (60) |
| `Plataforma__Clave` | 16+ caracteres aleatorios | Sí, para crear negocios |
| `Cors__OrigenesPermitidos__0` | `https://app.mininos.com` (dominio del frontend) | Si el frontend está en otro dominio |
| `ProxyInverso__Habilitado` | `true` | Sí en Render, Railway, Azure, Fly, Nginx… |
| `Negocio__ZonaHoraria` | `America/Bogota` | No (Bogotá) |

> **Importante:** la clave de plataforma que aparece en `identidad.http` ya está en GitHub.
> En producción usa una distinta. Genera claves con PowerShell:
> `[Convert]::ToBase64String((1..48 | % { Get-Random -Max 256 }))`

La API usa siempre `ventas_app` (sin permisos para crear tablas, con Row Level Security).
Las migraciones se aplican con `ventas_owner`.

## 2. Base de datos (primera vez y en cada versión nueva)

Genera scripts SQL idempotentes (se pueden ejecutar varias veces sin romper nada), desde la raíz del repo:

```powershell
$api = "src/Api/SistemaVentas.Api"
dotnet ef migrations script --idempotent -p src/Modules/Identidad/Identidad.Infrastructure  -s $api -c IdentidadDbContext  -o sql/deploy/1_identidad.sql
dotnet ef migrations script --idempotent -p src/Modules/Catalogo/Catalogo.Infrastructure    -s $api -c CatalogoDbContext   -o sql/deploy/2_catalogo.sql
dotnet ef migrations script --idempotent -p src/Modules/Inventario/Inventario.Infrastructure -s $api -c InventarioDbContext -o sql/deploy/3_inventario.sql
dotnet ef migrations script --idempotent -p src/Modules/Ventas/Ventas.Infrastructure        -s $api -c VentasDbContext     -o sql/deploy/4_ventas.sql
```

Ejecútalos **como `ventas_owner`** en este orden:

1. `sql/deploy/1_identidad.sql` … `4_ventas.sql`
2. `sql/02_seguridad_tenant.sql` (permisos de `ventas_app` + Row Level Security)
3. `sql/03_permiso_reportes.sql` y `sql/04_permisos_clientes.sql` (solo si ya había negocios creados antes)
4. Con un administrador: `POST /api/ventas/inicializar` (solo negocios creados antes del módulo de clientes)

## 3. Imagen Docker

```bash
docker build -t mininos-api .
```

Escucha en el puerto **8080**. Probar todo localmente igual que en producción:

```bash
cp .env.example .env          # y cambia los valores
docker compose --profile api up --build
```

## 4. Verificación

- `GET /health` → `Healthy` (sin token). Configúralo como *health check* en el hosting.
- `POST /api/auth/login` → token.
- `GET /api/dashboard` con el token de un administrador → métricas.

Scalar (`/scalar`) solo está disponible en Desarrollo.
