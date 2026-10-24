# HobbitCoder.TMTBucaramanga.Identity

Servicio de identidad de la plataforma TMT Bucaramanga: gestiona los usuarios y sus sesiones, y emite los JWT. Es el
único que tiene claves para firmarlos: las APIs consumidoras (la primera es **HobbitCoder.TMTBucaramanga.Sheepfold**)
solo los validan con la clave pública que descargan del documento de discovery de Identity.

Todo lo de identidad vive aquí y en su frontend (**hobbitcoder-tmtbucaramanga-identity**): inicio de sesión, primer
ingreso, recuperación del acceso, perfil y administración de cuentas. Las aplicaciones de negocio, como Sheepfold, no piden credenciales: envían al usuario al frontend de
Identity y reciben un código de autorización que cambian por tokens (OAuth 2.0 con PKCE). Ver
[Inicio de sesión de las aplicaciones](#inicio-de-sesión-de-las-aplicaciones).

## Configuración

| Clave | Descripción |
| --- | --- |
| `Persistence:Provider` | `PostgreSQL` (por defecto) o `SqlServer`. |
| `ConnectionStrings:Default` | Cadena de conexión del proveedor elegido. |
| `Jwt:Issuer` | URL base pública de Identity, sin barra final. Va tal cual en el claim `iss` y en el `issuer` del discovery. |
| `Jwt:Audiences` | APIs registradas: valores admitidos para el claim `aud`. |
| `Jwt:Clients` | Aplicaciones que inician sesión con Identity: `ClientId`, `Audience` y `RedirectUris`. Ver [Inicio de sesión de las aplicaciones](#inicio-de-sesión-de-las-aplicaciones). |
| `Jwt:AccessTokenLifetimeMinutes` | Vida de los tokens de acceso. Por defecto `60`. |
| `Jwt:RefreshTokenLifetimeDays` | Vida de cada refresh token. Por defecto `14`. Ver [Refresh tokens](#refresh-tokens). |
| `Jwt:SigningKey:Pem` / `PemPath` | Clave RSA privada (PEM) que firma los tokens, o la ruta a su archivo. Uno de los dos, no ambos. |
| `Jwt:SigningKey:KeyId` | Opcional. `kid` de la clave; por defecto, su thumbprint (RFC 7638). |
| `Jwt:PreviousSigningKeys` | Claves de rotaciones anteriores que se siguen publicando en el JWKS. Ver [Rotación de claves](#rotación-de-claves). |
| `Jwt:ServiceClients` | Aplicaciones de servidor que consultan el directorio (`ClientId`, `Secret` de 32+ caracteres). Ver [Directorio](#directorio-para-sheepfold). |
| `Bootstrap:Admin:UserName` / `PhoneNumber` / `Password` | Administrador que se crea al arrancar si no hay ninguno (user-secrets o variables de entorno, nunca `appsettings`). Ver [Primer administrador](#primer-administrador). |
| `Seeding:Enabled` | `true` crea al arrancar las cuentas de la carga inicial 2026 (directores, coordinadores y líderes). Ver [Carga inicial](#carga-inicial). |
| `Verification:CodeLength` / `LifetimeMinutes` / `MaxAttempts` | Política de los códigos de seguridad. Por defecto `6`, `15` y `5`: el requerimiento no los fija (contrato pendiente). |
| `Messaging:Provider` / `Evolution:*` / `Meta:*` / `TimeoutSeconds` | WhatsApp para los códigos: Evolution API autoalojado (por defecto) o WhatsApp Cloud API de Meta. Ver [Mensajes de seguridad](#mensajes-de-seguridad-whatsapp). |
| `CorsSettings:*` | Política CORS por ambiente, en los `appsettings`. Ver [CORS](#cors). |

Como variable de entorno, `:` se escribe `__` (`Jwt__Issuer`, `Jwt__SigningKey__Pem`, `Jwt__Audiences__0`, ...).

La sección `Jwt` se valida al arrancar: **la aplicación no inicia** si falta el issuer o no es válido, o si la clave de
firma falta, no es una clave RSA privada en PEM o tiene menos de 2048 bits. El mensaje de error dice qué valor corregir.

### Issuer por ambiente

`Jwt:Issuer` es la URL base pública de Identity, la misma que las APIs consumidoras configuran como `Authority`:

| Ambiente | `Jwt:Issuer` | Dónde se configura |
| --- | --- | --- |
| Desarrollo | `https://localhost:7101` | `appsettings.Development.json` (perfil `https` de `launchSettings.json`) |
| Producción | `https://identity.tmtbucaramanga.com` | `appsettings.Production.json` |

- Debe ser una URL absoluta, sin barra final y sin query. Fuera de `Development` debe usar `https`.
- Se usa siempre el valor configurado, nunca el `Host` de la petición: detrás de un proxy o balanceador el `Host` puede
  cambiar, el issuer no.
- `appsettings.json` lo deja vacío a propósito: cada ambiente debe declararlo o la aplicación no arranca. Otro
  ambiente (por ejemplo, pruebas) lo indica con la variable `Jwt__Issuer`, que tiene prioridad sobre los archivos.

### Clave de firma

La clave privada **nunca** va en el repositorio ni en `appsettings*.json` (`*.pem` está en `.gitignore`).

**Desarrollo.** Genera una clave RSA de 2048 bits fuera del repositorio y referénciala con `dotnet user-secrets`:

```powershell
$project = 'HobbitCoder.TMTBucaramanga.Identity.WebAPI'
$keyPath = "$HOME\.tmt-bucaramanga\identity-dev-signing-key.pem"

New-Item -ItemType Directory -Force (Split-Path $keyPath) | Out-Null
[System.Security.Cryptography.RSA]::Create(2048).ExportPkcs8PrivateKeyPem() | Set-Content -NoNewline $keyPath
# Alternativa con OpenSSL: openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out $keyPath

dotnet user-secrets set 'Jwt:SigningKey:PemPath' $keyPath --project $project
dotnet user-secrets set 'ConnectionStrings:Default' 'Server=localhost;Port=5432;Database=TmtBucaramangaIdentity;User Id=<usuario>;Password=<clave>' --project $project
```

**Demás ambientes.** La clave sale de una variable de entorno o de un almacén de secretos:

- `Jwt__SigningKey__Pem` con el contenido del PEM, o
- `Jwt__SigningKey__PemPath` con la ruta del archivo que monta el almacén de secretos (por ejemplo, un secret de
  Kubernetes o de Docker).

### Rotación de claves

El JWKS publica varias claves a la vez, cada una con su `kid`. Para rotar:

1. Genera la clave nueva y configúrala en `Jwt:SigningKey`. Desde ese momento firma los tokens nuevos.
2. Mueve la clave anterior a `Jwt:PreviousSigningKeys:0` (basta con su clave pública:
   `openssl pkey -in anterior.pem -pubout`). Si no tenía `KeyId` configurado, no se lo pongas ahora: su `kid` es su
   thumbprint y debe seguir siendo el mismo.
3. Cuando pasen `Jwt:AccessTokenLifetimeMinutes` más el margen de reloj de las APIs (5 minutos por defecto), los
   tokens que firmó la clave anterior ya expiraron: quítala de `PreviousSigningKeys`.

Las APIs consumidoras vuelven a descargar el JWKS cuando reciben un `kid` que no conocen, así que aceptan la clave nueva
sin reiniciarse.

### CORS

El frontend de Identity comparte dominio con esta API (`https://identity.tmtbucaramanga.com`; en desarrollo su servidor
reenvía `/api` a ella), así que no necesita CORS. El que sí lo necesita es el frontend de Sheepfold, que llama a
`token`, `refresh`, `logout` y `me` desde `https://sheepfold.tmtbucaramanga.com` (en desarrollo,
`http://localhost:4200`). Cada ambiente declara su política en su `appsettings`:

| Ambiente | `AllowedOrigins` |
| --- | --- |
| Development | `http://localhost:4200` |
| Production | `https://sheepfold.tmtbucaramanga.com` |
| Base (cualquier otro) | ninguno |

En todos: métodos `GET`, `POST`, `PUT`, `DELETE`; cabeceras `Authorization` y `Content-Type`; se exponen `Location`
y `WWW-Authenticate`; preflight en caché 10 minutos y **sin credenciales** (los tokens viajan en la cabecera
`Authorization` y el refresh token en el cuerpo, no en cookies).

Cada origen se valida al arrancar: debe ser exactamente `esquema://host[:puerto]`, sin barra final ni ruta (así lo
envía el navegador en `Origin`), `https` fuera de desarrollo y nunca `*`. Un origen mal escrito impide arrancar en vez
de bloquear al frontend sin ningún error. Si el frontend cambia de dominio, actualiza `appsettings.Production.json` o
usa `CorsSettings__AllowedOrigins__0`.

### Primer administrador

Las cuentas las registra la administración y una base de datos nueva no tiene a nadie.

- **Desarrollo: `Bootstrap:Admin`.** Al arrancar, `AdminBootstrapper` crea el administrador ya activado si
  `Bootstrap:Admin:UserName` está configurado y todavía no hay ninguna cuenta `Admin`; es idempotente. Los datos
  van en user-secrets, nunca en `appsettings*.json`:

  ```powershell
  $project = 'HobbitCoder.TMTBucaramanga.Identity.WebAPI'
  dotnet user-secrets set 'Bootstrap:Admin:UserName' 'admin' --project $project
  dotnet user-secrets set 'Bootstrap:Admin:PhoneNumber' '+570000000000' --project $project
  dotnet user-secrets set 'Bootstrap:Admin:Password' '<contraseña de prueba>' --project $project
  ```

  Las pruebas de integración usan el mismo mecanismo con su propia configuración (`DatabaseApiFixture`).
- **Producción: primer administrador sin contraseñas en la configuración.** Se inserta una cuenta `Admin`
  **pendiente de activación** con el teléfono real de su titular, y esa persona la activa en "Es mi primer
  ingreso" con el código de seguridad (requiere la pasarela de mensajería). El teléfono va normalizado
  (`+57` y 10 dígitos). Solo inserta si todavía no hay ningún administrador:

```sql
-- psql -v usuario=admin.tmt -v telefono=+573001234567 -v nombres='Nombre' -v apellidos='Apellido' -f primer-admin.sql
INSERT INTO "Identity"."AspNetUsers"
    ("Id", "FirstName", "LastName", "IsActive", "IsDeleted", "CreatedBy", "CreatedDate", "UserName", "NormalizedUserName",
     "EmailConfirmed", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled",
     "LockoutEnabled", "AccessFailedCount", "CanRegisterAttendees", "Role")
SELECT gen_random_uuid(), :'nombres', :'apellidos', TRUE, FALSE, 'manual:primer-administrador', now(), :'usuario', upper(:'usuario'),
       FALSE, replace(gen_random_uuid()::text, '-', ''), gen_random_uuid()::text, :'telefono', FALSE, FALSE, TRUE, 0, FALSE, 'Admin'
WHERE NOT EXISTS (SELECT 1 FROM "Identity"."AspNetUsers" WHERE "Role" = 'Admin');
```

### Carga inicial

Con `Seeding:Enabled=true` (variable `Seeding__Enabled` o user-secrets), `IdentitySeeder` crea al arrancar las 28 cuentas
de `Infrastructure/Persistence/Seeding/identity-seed.json`: 2 directores, 4 coordinadores y 22 líderes, sin contraseña;
cada titular activa la suya en "Es mi primer ingreso". Corre después del primer administrador y solo una vez: si
alguna de esas cuentas ya existe (aunque esté eliminada) no hace nada. Una cuenta cuyo teléfono o usuario ya está en
uso no se crea y queda advertida en el log. Sus Id fijos son los que usa la carga inicial de Sheepfold, así que
**Identity se siembra primero**. El JSON se genera con `docs/resources/bases-grupos/generar-semillas.py`.

### Junto con Sheepfold y los frontends

Cada proyecto tiene sus propios puertos, user-secrets y base de datos, así que pueden correr a la vez:

| Proyecto | URL de desarrollo | Base de datos |
| --- | --- | --- |
| HobbitCoder.TMTBucaramanga.Identity | `https://localhost:7101` / `http://localhost:5101` | `TmtBucaramangaIdentity` |
| hobbitcoder-tmtbucaramanga-identity (frontend) | `http://localhost:4300` (`ng serve`, reenvía `/api` a Identity) | — |
| HobbitCoder.TMTBucaramanga.Sheepfold | `https://localhost:7291` / `http://localhost:5280` | `TmtBucaramangaSheepfold` |
| hobbitcoder-tmtbucaramanga-sheepfold (frontend) | `http://localhost:4200` (`ng serve`, reenvía `/api` a Sheepfold) | — |

Al abrir Sheepfold sin sesión, el navegador va al frontend de Identity para iniciar sesión y vuelve a
Sheepfold con un código de autorización. `appsettings.Development.json` ya registra a Sheepfold como cliente.

Arranca Identity con el perfil `https`: su issuer de desarrollo es `https://localhost:7101`, que es el `Authority` de
Sheepfold. El certificado de desarrollo debe ser de confianza (`dotnet dev-certs https --trust`). Antes de la primera
ejecución, crea la base con `dotnet ef database update` (ver [Migraciones](#migraciones)).

## Cuentas, roles e identificadores

Cada cuenta tiene **un rol** (§3.1): `Admin`, `Director`, `Coordinator`, `Leader`, `Apprentice` o `Sniper`, guardado en la
propia cuenta (no hay tablas de roles). Se emite en el claim `role`; si la cuenta está designada para registrar
asistentes (§7) el token lleva además `permission: RegisterAttendees`.

Se ingresa con **teléfono, nombre de usuario o correo** (si se registró). El teléfono se guarda normalizado
(`+573001234567`; un número de 10 dígitos se completa con `+57`). Los tres identificadores son únicos, también frente
a cuentas retiradas. Las cuentas se crean **sin contraseña**: solo su titular la crea (§6).

| Estado de acceso (`accessStatus`) | Significado |
| --- | --- |
| `PendingActivation` | Creada por la administración; el titular no ha completado su primer ingreso. |
| `Ready` | Puede ingresar con su contraseña. |
| `PasswordResetRequested` | La administración pidió que cree una contraseña nueva; la anterior ya no sirve. |

Los enums viajan como texto en JSON (`"Leader"`, `"WhatsApp"`).

## Autenticación (`/api/auth`)

| Endpoint | Acceso | Descripción |
| --- | --- | --- |
| `POST /api/auth/login` | Público | `{ identifier, password, audience }`. Teléfono, usuario o correo registrado. Devuelve el par de tokens. |
| `POST /api/auth/refresh` | Público | `{ refreshToken, audience? }`. Rota el refresh token y devuelve un par nuevo. |
| `POST /api/auth/logout` | Público | `{ refreshToken }`. Cierra esa sesión, también en las aplicaciones que entraron con ella. Responde 204 aunque el token no exista. |
| `POST /api/auth/authorize` | Autenticado | `{ clientId, redirectUri, codeChallenge, codeChallengeMethod, state }`. Emite un código de autorización para la aplicación cliente y devuelve `{ redirectUri }` con `code` y `state`. |
| `POST /api/auth/token` | Público | `{ code, codeVerifier, clientId, redirectUri }`. Cambia el código por el par de tokens del cliente, en la misma sesión. |
| `POST /api/auth/service-token` | Público | `{ clientId, clientSecret }`. Token de servicio (rol `Service`, sin sesión) para el directorio. |
| `GET /api/auth/me` | Autenticado | Mi perfil (`UserResponse`), con rol, estado de acceso y edad calculada. |
| `PUT /api/auth/me` | Autenticado | Nombres, apellidos, género, teléfono, correo (opcional), usuario, Instagram y nacimiento. Rol, estado y designación solo los cambia la administración. |
| `POST /api/auth/change-password` | Autenticado | `{ currentPassword, newPassword }`. Cierra **todas** las sesiones del usuario. |

No hay registro público: las cuentas las crea la administración (§4).

## Primer ingreso y recuperación (`/api/account`)

Públicos: el titular aún no tiene sesión. El código llega **solo al teléfono registrado de la cuenta**, por
WhatsApp; nunca se envían contraseñas.

| Endpoint | Descripción |
| --- | --- |
| `POST /api/account/verification` | `{ identifier, channel, purpose }` con `channel` `Sms` o `WhatsApp` y `purpose` `Activation` o `Recovery`. Teléfono o usuario. Responde `{ verificationId }` **exista o no la cuenta** (no revela cuáles existen); solo envía el código si la cuenta corresponde al proceso. 503 si WhatsApp no está configurado o si se pide `Sms` (el proveedor no envía SMS; el frontend solo ofrece WhatsApp). |
| `POST /api/account/verification/confirm` | `{ verificationId, code }` → `{ verificationToken }`. Un código inválido no cambia la cuenta; tras `MaxAttempts` fallos la solicitud deja de servir. |
| `POST /api/account/activation/profile` | `{ verificationToken }` → datos ya registrados, para completar solo lo que falta. |
| `POST /api/account/activation` | `{ verificationToken, password, firstName, lastName, gender, phoneNumber, email?, userName, instagram, birthDate }`. Contraseña y perfil se guardan **juntos** y la cuenta queda activada. Todos los datos del §5 son obligatorios menos el correo. |
| `POST /api/account/password` | `{ verificationToken, password }`. Recuperación, o contraseña nueva tras un reinicio administrativo; cierra las sesiones anteriores. |

El token de verificación es de un solo uso y vence con la solicitud (`Verification:LifetimeMinutes`).

Los endpoints autenticados de Identity solo aceptan tokens con `aud = HobbitCoder.TMTBucaramanga.Identity`: para usarlos
inicia sesión (o renueva) con esa audiencia.

Respuesta de `login` y `refresh`:

```json
{
  "accessToken": "<JWT RS256>",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "accessTokenExpiresAt": "2026-10-02T17:00:00+00:00",
  "refreshToken": "<token opaco>",
  "refreshTokenExpiresAt": "2026-10-16T16:00:00+00:00"
}
```

- **Credenciales.** Contraseña: mínimo 8 caracteres con mayúscula, minúscula, número y símbolo. Un login fallido
  responde 401 con el mismo mensaje sea cual sea el motivo (identificador inexistente, contraseña incorrecta, cuenta
  sin activar, sin contraseña o bloqueada), para no revelar qué cuentas existen; el motivo real queda en el log. La
  única excepción es una cuenta **desactivada con la contraseña correcta**: responde 403 ("acceso no disponible",
  HU-02), algo que solo puede saber quien ya conoce la contraseña.
- **Bloqueo.** 5 contraseñas incorrectas seguidas bloquean la cuenta 15 minutos. Crear una contraseña nueva con la
  recuperación la desbloquea.

### Refresh tokens

Son responsabilidad exclusiva de Identity: las APIs consumidoras nunca los reciben ni los validan.

- **Opacos.** 256 bits aleatorios en base64url. La base de datos guarda solo su hash SHA-256 (tabla `RefreshTokens`).
- **Rotación.** Cada uso revoca el token y entrega uno nuevo con la vida completa (`Jwt:RefreshTokenLifetimeDays`): una
  sesión sigue abierta mientras se renueve al menos una vez en ese plazo. Si llegan dos renovaciones simultáneas con el
  mismo token, solo una tiene éxito.
- **Detección de robo.** Presentar un token que ya se rotó revoca la sesión completa (todos los tokens que descienden
  del mismo login), porque significa que alguien más tiene una copia. Usuario y atacante deben volver a iniciar sesión.
- **Varias APIs, una sesión.** `refresh` acepta otra `audience` registrada para obtener un token de acceso para esa API.
  La sesión conserva su audiencia original: un `refresh` sin `audience` vuelve a emitir para ella.
- **Revocación.** `logout` revoca la sesión; cambiar o recuperar la contraseña, el reinicio administrativo, desactivar
  o retirar al usuario revocan todas. El rol se lee en cada emisión: un cambio llega en el siguiente `refresh`.
- Los **tokens de acceso** ya emitidos no se pueden revocar: siguen siendo válidos hasta su `exp` (60 minutos por
  defecto). Por eso su vida es corta.

## Inicio de sesión de las aplicaciones

Las aplicaciones cliente (hoy, el frontend de Sheepfold) usan Identity como proveedor de identidad con el flujo de
código de autorización de OAuth 2.0 y PKCE (S256). Nunca reciben la contraseña del usuario:

1. El cliente genera un `code_verifier` aleatorio y su `code_challenge`, y lleva el navegador al frontend de Identity:
   `{frontend de Identity}/auth/authorize?client_id&redirect_uri&code_challenge&code_challenge_method=S256&state`.
2. Si no hay sesión, el frontend de Identity pide iniciar sesión o registrarse y luego llama a `POST /api/auth/authorize`.
   Identity valida que el cliente exista y que `redirect_uri` sea **exactamente** una de sus `RedirectUris`; si no, no
   emite nada y el frontend muestra el error en lugar de redirigir.
3. El navegador vuelve a `redirect_uri?code&state`. El cliente comprueba `state` y llama a `POST /api/auth/token` con
   el `code_verifier`. Recibe un par de tokens con la `Audience` del cliente.

- **Código.** Opaco, de un solo uso, vence en un minuto y se guarda solo su hash (tabla `AuthorizationCodes`). Se marca
  como usado en el primer intento de canje, aunque falle. Presentar un código ya usado revoca la sesión completa.
- **Una sesión para todos.** El par de tokens del cliente pertenece a la misma sesión de Identity (misma familia de
  refresh tokens), así que `logout` desde cualquier aplicación la cierra en todas. Cada token de acceso lleva el claim
  `sid` con esa sesión; `authorize` comprueba que siga abierta.
- **Registro de clientes.** En `Jwt:Clients`. Cada `Audience` debe estar en `Jwt:Audiences`; cada `RedirectUri` debe
  ser absoluta, sin query ni fragmento y `https` fuera de desarrollo. Se valida al arrancar.

```json
"Clients": [
  {
    "ClientId": "sheepfold-web",
    "Audience": "HobbitCoder.TMTBucaramanga.Sheepfold",
    "RedirectUris": [ "https://sheepfold.tmtbucaramanga.com/auth/callback" ]
  }
]
```

## Usuarios (`/api/users`)

Administración de cuentas (§4.1, §4.5, HU-03). Exige el rol `Admin` o `Director` y un token con la audiencia de
Identity. Las cuentas con rol `Admin` solo las crea o modifica otro administrador. Nadie define contraseñas ajenas.

| Endpoint | Descripción |
| --- | --- |
| `GET /api/users` | Paginado (`pageNumber`, `pageSize` hasta 100). Filtros: `search` (nombre, apellido, usuario, correo, teléfono), `isActive`, `role`. |
| `GET /api/users/retired` | Cuentas retiradas o desactivadas (registros retirados, §26). |
| `GET /api/users/{id}` | Detalle. |
| `POST /api/users` | Alta **sin contraseña**: nombres, apellidos, teléfono, usuario y rol obligatorios; género, correo, Instagram, nacimiento, `isActive` y `canRegisterAttendees` opcionales. |
| `PUT /api/users/{id}` | Actualiza datos, rol, estado y designación. Desactivar cierra sus sesiones. |
| `POST /api/users/{id}/password-reset` | Pide que el titular cree una contraseña nueva: invalida la actual y cierra sus sesiones. |
| `DELETE /api/users/{id}` | Retiro (borrado lógico); cierra sus sesiones. Sus identificadores siguen reservados. |
| `POST /api/users/{id}/restore` | Restaura una cuenta retirada o desactivada (queda activa). |
| `DELETE /api/users/{id}/permanent` | Eliminación definitiva de una cuenta retirada o desactivada. |

Nadie puede desactivarse, retirarse, eliminarse ni cambiar su propio rol (409). Cada cambio queda en la auditoría de
cuentas.

## Auditoría de cuentas (`/api/audit/accounts`)

Solo directores (§25). Paginada, con filtros `from`, `to`, `action` y `actorUserId`. Cada entrada guarda actor,
momento, acción, cuenta afectada (nombre de ese momento) y datos anteriores/resultantes en JSON, nunca contraseñas
ni hashes. No referencia la cuenta con clave foránea: sobrevive a su eliminación definitiva.

## Directorio para Sheepfold

`GET /api/directory/users` devuelve las cuentas vigentes (id, nombre, género, teléfono, nacimiento, rol, estado,
activación y designación). Solo acepta tokens de servicio: la API de Sheepfold pide uno con
`POST /api/auth/service-token` usando su cliente de `Jwt:ServiceClients` y decide qué ve cada usuario según su alcance
ministerial. El teléfono solo se usa para notificaciones por WhatsApp (§23).

```powershell
dotnet user-secrets set 'Jwt:ServiceClients:0:ClientId' 'sheepfold-api' --project $project
dotnet user-secrets set 'Jwt:ServiceClients:0:Secret' '<secreto de 32+ caracteres>' --project $project
```

## Mensajes de seguridad (WhatsApp)

El proveedor se elige con `Messaging:Provider` (`Messaging/SecurityMessageSenders.cs`):

- **`Evolution`** (por defecto): Evolution API v2 autoalojado. Confirma con
  `POST {Evolution:Url}/chat/whatsappNumbers/{Evolution:Instance}` que el teléfono tiene WhatsApp y envía el código
  como texto con `POST {Evolution:Url}/message/sendText/{Evolution:Instance}` y la cabecera
  `apikey: {Evolution:ApiKey}`. Instancia, número y secretos en `docs/requerimiento/whatsapp-evolution.md`.
- **`Meta`**: WhatsApp Cloud API con la plantilla de autenticación `Meta:CodeTemplate` (requiere un negocio
  verificado). Ver `docs/requerimiento/whatsapp-cloud-api.md`.

Comportamiento común:

- Sin las credenciales del proveedor en **Development**, el código se escribe en el log (nivel Warning) para probar
  los flujos sin enviar mensajes reales.
- Sin ellas en otro ambiente, pedir un código responde **503**: no se finge el envío.
- El canal `Sms` responde 503 (ningún proveedor envía SMS).
- Si el proveedor rechaza el envío o el teléfono no tiene WhatsApp, se registra el estado y el mensaje de error (sin
  el cuerpo completo) y la API responde 503. No hay reintentos ni estados de entrega.

## Discovery y JWKS

Ambos endpoints son públicos (no requieren autenticación) y fuera de desarrollo se sirven por HTTPS:

| Endpoint | Contenido |
| --- | --- |
| `GET {issuer}/.well-known/openid-configuration` | `issuer` (idéntico al claim `iss`), `jwks_uri` (`{issuer}/.well-known/jwks.json`) e `id_token_signing_alg_values_supported` (`["RS256"]`). |
| `GET {issuer}/.well-known/jwks.json` | Claves públicas de firma en formato JWK: `kty`, `use`, `alg`, `kid`, `n`, `e`. Nunca los parámetros privados. |

## Tokens de acceso

Se firman con **RS256** (RSA de al menos 2048 bits). El header lleva `alg: "RS256"`, `typ: "JWT"` y el `kid` de la
clave que firmó.

| Claim | Valor |
| --- | --- |
| `iss` | `Jwt:Issuer`, idéntico al `issuer` del discovery. |
| `aud` | La API destino, que debe estar en `Jwt:Audiences`. Para Sheepfold: `HobbitCoder.TMTBucaramanga.Sheepfold`. |
| `sub` | Id único y estable del usuario. Las APIs lo usan para auditoría (`CreatedBy`, `UpdatedBy`, ...). |
| `exp` | Emisión + `Jwt:AccessTokenLifetimeMinutes`. |
| `iat`, `nbf` | Momento de emisión. |
| `jti` | Id único del token. |
| `role` | Opcional, un valor por rol. Las APIs lo leen como `ClaimTypes.Role`. |
| `sid` | Sesión de Identity que emitió el token. Las APIs lo reciben con ese mismo nombre. |

El payload va firmado, no cifrado: no incluyas datos sensibles. Los tokens se emiten con `IAccessTokenIssuer`
(capa Application).

Los endpoints propios de Identity que declaren `[Authorize]` validan los tokens con las mismas claves y solo aceptan
la audiencia `HobbitCoder.TMTBucaramanga.Identity`, así que un token emitido para otra API no sirve aquí.

### Configuración de una API consumidora

```csharp
options.Authority = "<Jwt:Issuer de Identity>";
options.RequireHttpsMetadata = !isDevelopment;
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,          // sin ValidIssuer: se usa el "issuer" del discovery
    ValidateAudience = true,
    ValidAudience = "HobbitCoder.TMTBucaramanga.Sheepfold",
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
};
```

## Producción (Docker + Dokploy)

El `Dockerfile` de la raíz construye la imagen de la WebAPI. El contenedor escucha **HTTP en el puerto 8080**, corre
con un usuario sin privilegios (uid `1654`) y arranca en `Production`. El HTTPS de `identity.tmtbucaramanga.com` lo
termina el Traefik de Dokploy con Let's Encrypt; la imagen respeta sus encabezados `X-Forwarded-*`.

**Aplicación en Dokploy**

- Origen: este repositorio. Tipo de build: `Dockerfile`, ruta `Dockerfile`.
- Dominio: `identity.tmtbucaramanga.com` con las rutas **`/api`** y **`/.well-known`** (sin quitar el prefijo), puerto
  del contenedor `8080`, HTTPS con Let's Encrypt. El frontend de Identity es otra aplicación de Dokploy con el mismo
  dominio y ruta `/`, así que ambos comparten origen.
- Health check: `GET /health` en el puerto 8080 del contenedor (incluye la conexión a PostgreSQL). Por el dominio
  público esa ruta la sirve el frontend.

**Variables de entorno**

| Variable | Valor |
| --- | --- |
| `ConnectionStrings__Default` | Cadena de conexión al PostgreSQL de producción (base `TmtBucaramangaIdentity`). |
| `Jwt__SigningKey__PemPath` | `/run/secrets/identity-signing-key.pem` (ver abajo). |
| `Jwt__ServiceClients__0__ClientId` / `Jwt__ServiceClients__0__Secret` | Cliente de servicio de la API de Sheepfold (el mismo secreto se configura en Sheepfold). |
| `Messaging__Evolution__Url` / `Messaging__Evolution__Instance` / `Messaging__Evolution__ApiKey` | Evolution API (`docs/requerimiento/whatsapp-evolution.md`). Sin ellos, la verificación responde 503. |

`Jwt:Issuer` (`https://identity.tmtbucaramanga.com`), el cliente de Sheepfold (`Jwt:Clients`) y el origen CORS de su
frontend (`https://sheepfold.tmtbucaramanga.com`) ya vienen en `appsettings.Production.json`; no hace falta
configurarlos salvo en otro ambiente.

**Clave de firma.** Genérala una sola vez en un equipo seguro (`openssl genpkey -algorithm RSA -pkeyopt
rsa_keygen_bits:2048 -out identity-signing-key.pem`) y súbela en Dokploy como *File Mount* en
`/run/secrets/identity-signing-key.pem`. El archivo debe poder leerlo el uid `1654` (por ejemplo, permisos `644`).
Guarda una copia de respaldo en tu almacén de secretos: si se pierde, todos los tokens emitidos dejan de validar y
hay que [rotarla](#rotación-de-claves). Sin una clave válida el contenedor no arranca y el log dice por qué.

**Base de datos.** El contenedor no aplica migraciones. Antes de desplegar una versión con cambios de modelo, genera
el script idempotente y ejecútalo en el PostgreSQL de producción:

```powershell
$env:Persistence__Provider = 'PostgreSQL'
dotnet ef migrations script --idempotent --output identity.sql --project HobbitCoder.TMTBucaramanga.Identity.Persistence.PostgreSQL --startup-project HobbitCoder.TMTBucaramanga.Identity.WebAPI
```

Despliega Identity antes que Sheepfold: Sheepfold descarga el discovery de `https://identity.tmtbucaramanga.com`.

## Pruebas

```powershell
dotnet run --project HobbitCoder.TMTBucaramanga.Identity.IntegrationTests
```

Requieren **Docker**: las pruebas de autenticación, primer ingreso y usuarios usan un PostgreSQL real
(Testcontainers) cuyo esquema se crea desde el modelo de EF. El envío por WhatsApp se reemplaza por un doble de
prueba que captura los códigos; nunca se envían mensajes reales. Las de tokens levantan además una API con exactamente la configuración de arriba contra
Identity en memoria.

## Migraciones

Cada proveedor tiene sus migraciones en su proyecto `Persistence.*`. El proveedor se elige con `Persistence__Provider`:

```powershell
$env:Persistence__Provider = 'PostgreSQL'   # o 'SqlServer'
dotnet ef migrations add <Nombre> --project HobbitCoder.TMTBucaramanga.Identity.Persistence.PostgreSQL --startup-project HobbitCoder.TMTBucaramanga.Identity.WebAPI
dotnet ef database update --project HobbitCoder.TMTBucaramanga.Identity.Persistence.PostgreSQL --startup-project HobbitCoder.TMTBucaramanga.Identity.WebAPI
```

Un cambio de modelo requiere una migración para cada proveedor.
