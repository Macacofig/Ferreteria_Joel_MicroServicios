# Ferretería Joel — EC3 · Entregable Único (3ra Competencia)

Solución en **.NET 10** con arquitectura de microservicios, **ApiGateway (YARP)**,
**Keycloak** para la gestión de JWT y **frontend Razor** que consume el
microservicio de usuarios mediante el **patrón Adapter**.

Cada microservicio expone una **API** y un endpoint **`/index`** que puede
abrirse en el navegador para comprobar que está activo.

---

## Componentes (11)

| # | Componente | Tipo | Puerto host | URL de estado |
|---|------------|------|-------------|----------------|
| 1 | `MicroServicioUsuarios` | API .NET (hexagonal + Result + Factory Method) | 5072 | http://localhost:5072/index |
| 2 | `mysql-usuarios` | MySQL 8 — base `bdusuarios` | 3307 | — |
| 3 | `keycloak` | Keycloak 26 — realm `ferreteria` | 8081 | http://localhost:8081 |
| 4 | `MicroServicioProductos` | API .NET | 5071 | http://localhost:5071/index |
| 5 | `mysql-productos` | MySQL 8 — base `bdproductos` | 3308 | — |
| 6 | `MicroServicioClientes` | API .NET | 5073 | http://localhost:5073/index |
| 7 | `postgres-clientes` | PostgreSQL 16 — base `bdclientes` | 5433 | — |
| 8 | `MicroServicioProveedores` | API .NET | 5074 | http://localhost:5074/index |
| 9 | `mongo-proveedores` | MongoDB 7 — base `bdproveedores` | 27018 | — |
| 10 | `ApiGateway` | YARP | 5000 | http://localhost:5000/index |
| 11 | `ferreteria-web` | Frontend Razor (patrón Adapter) | 5267 | http://localhost:5267 |

---

## Arquitectura del microservicio de usuarios

Arquitectura **hexagonal** con el patrón **Result** en el dominio y
**Factory Method** para los repositorios.

```
MicroServicioUsuarios/
├── Dominio/            # No referencia nada. Entidades, puertos, Result.
│   ├── Entidades/      #   Empleado, RegistroBitacora
│   ├── Puertos/        #   IRepositoryEmpleado, IRepositoryBitacora,
│   │                   #   IProveedorIdentidad, IContextoUsuario, ...
│   ├── Resultados/     #   Result / Result<T>  (patrón Result)
│   ├── Constantes/     #   EstadoEmpleado, RolesSistema
│   └── Utilidades/     #   ICredencialesValidaciones, ...
├── Aplicacion/         # Referencia Dominio e Infraestructura.
│   └── Servicios/      #   ServicioInicioSesion, ServicioRegistroEmpleado, ...
├── Infraestructura/    # Referencia Dominio.
│   ├── Conexion/       #   MySqlConnectionFactory (MySql.Data)
│   ├── Repositorios/   #   MySqlEmpleadoRepository, MySqlBitacoraRepository
│   └── Factories/      #   RepositoryCreator<T> + creadores concretos
├── Servicios/          # Adaptadores: Keycloak, SMTP, UrlProtector, Argon2
└── Controllers/        # AuthController, UsuariosController, BitacoraController
```

**Factory Method**: `RepositoryCreator<T>` es el *Creator*; `EmpleadoRepositoryCreator`
y `BitacoraRepositoryCreator` son los *Concrete Creators*; los repositorios
`MySql*` son los *Concrete Products*; `IRepository*` es el *Product* (puerto del
dominio). Los servicios piden el repositorio al creador y nunca conocen las
clases `MySql*`.

**Persistencia**: solo `mysql.data`, y solo las tablas del microservicio de
usuarios: `empleado` y `bitacora_sesion`.

---

## Keycloak (JWT / JWKS)

- Keycloak **emite** los tokens y guarda las credenciales.
- `MicroServicioUsuarios` y `ApiGateway` **validan** los tokens contra el JWKS
  del realm (`realm_access.roles` → `ClaimTypes.Role`).
- El inicio de sesión usa el flujo *Resource Owner Password Credentials* del
  cliente público `ferreteria-web` (direct-access grants habilitados).
- La tabla `empleado` conserva los datos de negocio y de rol; la columna
  `Contrasena` guarda un hash Argon2id solo como respaldo.

### Usuarios de prueba

| Usuario | Contraseña | Rol |
|---------|-----------|-----|
| `jrios` | `admin123` | Administrador |
| `mlopez` | `clave123` | EmpleadoVenta |
| `cmendoza` … `erojas` | `clave123` | EmpleadoVenta |

> El login acepta correo **o** nombre de usuario.

---

## Frontend y patrón Adapter

`FERRETERIA_JOEL_Web/` es una **copia literal del frontend de la competencia
anterior**: mismas páginas, mismo HTML, mismos estilos, imágenes y scripts,
mismas restricciones por rol (`[Authorize(Roles = ...)]`) y el mismo control de
URL cifradas (`UrlProtector`, AES-GCM compartido con el microservicio).

Lo **único** que cambia es la forma de comunicarse: antes el front hablaba
directamente con `ServicioUsuarios`; ahora pasa por el **ApiGateway** y por el
**patrón Adapter de objetos**:

```
Páginas Razor ──► IServicioUsuarios / IServicioUsuariosClient   (target)
                          │
                          ▼
                 AdaptadorServicioUsuarios                       (adapter)
                          │ envuelve
                          ▼
                 UsuariosApiCliente                              (adaptee: HTTP + RespuestaApi<T>)
                          │
                          ▼
                 ApiGateway (YARP) ──► MicroServicioUsuarios
```

`AdaptadorServicioUsuarios` implementa los dos contratos que consumen las
páginas: delega en el adaptee para las páginas "clásicas" (`IServicioUsuarios`)
y traduce entre los DTOs del front (`FERRETERIA_Web.Models`) y el contrato HTTP
(`RespuestaApi<T>`) para `NuevoUsuario` y `CambiarPassword`.

Las páginas incluidas (idénticas a las del proyecto original) son: `Index`,
`Login`, `Logout`, `Perfil`, `Personal`, `Registro`, `NuevoUsuario`,
`Bitacora`, `CambiarContrasena`, `CambiarPassword`, `ActivarCuenta`,
`CatalogoProductos`, `Venta`, `Ventas`, `VentaComprobante` y `Reportes/*`,
además del layout, navegación y modales compartidos.

---

## Puesta en marcha

Requisitos: **Docker Desktop** (o Docker con Compose v2) y los puertos
5267, 5000, 8081, 5071-5074, 3307, 3308, 5433 y 27018 libres.

```bash
# 1) Clonar y entrar en la raíz del repositorio
git clone https://github.com/Macacofig/Ferreteria_Joel_MicroServicios.git
cd Ferreteria_Joel_MicroServicios

# 2) Crear el .env (NO está versionado; copiar desde la plantilla)
cp .env.example .env        # en Windows: Copy-Item .env.example .env

# 3) Construir y levantar los 11 contenedores
docker compose up -d --build
```

Luego:

- Frontend: <http://localhost:5267> (inicie sesión con `jrios` / `admin123`).
- ApiGateway: <http://localhost:5000/index>
- MicroServicioUsuarios: <http://localhost:5072/index> (Swagger en `/swagger`)
- Keycloak (admin console): <http://localhost:8081> (`admin` / `admin`)
- Productos / Clientes / Proveedores: puertos 5071 / 5073 / 5074 → `/index`

> `.env` **no se versiona** (está en `.gitignore`). Cada persona debe crearlo
> desde `.env.example`. Los valores de la plantilla ya son suficientes para
> arrancar: las bases se inicializan solas (scripts SQL, esquema de clientes e
> índices/semilla de MongoDB) y Keycloak importa el realm `ferreteria`.
>
> SMTP viene vacío a propósito: en modo desarrollo el enlace de activación se
> escribe en el log del microservicio de usuarios. Si se configura un servidor
> real, use credenciales propias y **nunca versionarlas ni compartirlas**.

---

## Bases de datos (`Scripts/`)

`Scripts/01-usuarios.sql` es el **script único y mergeado** del microservicio de
usuarios: crea la base `bdusuarios` y **solo** las tablas `empleado` y
`bitacora_sesion` (con las columnas de auditoría ya integradas desde
`migracion_auditoria.sql`) y los 10 empleados semilla.

No crea tablas de los demás microservicios: cada uno gestiona su propia base
(MySQL, PostgreSQL o MongoDB según corresponda).

---

## Validación

```bash
# Compilar la solución
dotnet build FERRETERIA_JOEL_Web.slnx

# Validar la orquestación
docker compose config --quiet

# Construir y levantar
docker compose up --build
```
