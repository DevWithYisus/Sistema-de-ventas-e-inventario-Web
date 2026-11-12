# Minino's Pet Shop: frontend web

React 19 + Vite + TypeScript + Tailwind CSS 4 + TanStack Query + Recharts.

## Requisitos
- Node.js 20 o superior (`node -v`)
- La API corriendo en `http://localhost:5248` (`dotnet run --project src/Api/SistemaVentas.Api`)

## Primera vez
```powershell
cd frontend
npm install
copy .env.example .env.local   # opcional: define VITE_CODIGO_NEGOCIO para no escribirlo en el login
npm run dev
```
Abre http://localhost:5173

## Comandos
| Comando | Qué hace |
|---|---|
| `npm run dev` | Servidor de desarrollo con recarga automática |
| `npm run typecheck` | Revisa los tipos de TypeScript |
| `npm run build` | Revisa tipos y genera la versión de producción en `dist/` |
| `npm run preview` | Sirve `dist/` para probar la versión de producción |

## Estructura
```
src/
  api/          cliente HTTP (token, errores) y tipos de las respuestas
  auth/         sesión, permisos, guardias de rutas
  layout/       menú lateral/hamburguesa y navegación por permisos
  features/     una carpeta por módulo (dashboard, luego ventas, inventario…)
  paginas/      login, 404, sin permiso, próximamente
  ui/           componentes reutilizables (botón, tarjeta, alerta…)
  utils/        formato de pesos, fechas y porcentajes
```

## Cómo se conecta con la API
- En desarrollo, Vite reenvía `/api` a `http://localhost:5248` (ver `vite.config.ts`): no hace falta CORS.
- En producción, define `VITE_API_URL` con la URL pública de la API y agrega el dominio del
  frontend en `Cors__OrigenesPermitidos__0` de la API.
- El menú y las pantallas se muestran según los permisos del token. Es solo experiencia de usuario:
  la seguridad real la aplica la API.
