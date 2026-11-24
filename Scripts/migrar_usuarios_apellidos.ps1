<#
.SYNOPSIS
    Limpieza de Keycloak y verificación de credenciales para la migración
    de usuarios de Ferretería Joel.

.DESCRIPTION
    Complemento de Scripts/04-usuarios-apellidos-migracion.sql.

    - Elimina del realm 'ferreteria' todos los usuarios excepto jrios y mlopez.
    - NO cambia contraseñas: jrios/admin123 y mlopez/clave123 se conservan.
    - Verifica que ambos puedan obtener un token (ROPC).

    Ejecutar:
        powershell -ExecutionPolicy Bypass -File Scripts\migrar_usuarios_apellidos.ps1
#>

[CmdletBinding()]
param(
    [string] $KeycloakUrl = 'http://localhost:8081',
    [string] $Realm = 'ferreteria',
    [string] $AdminUser = 'admin',
    [string] $AdminPassword = 'admin',
    [string] $WebClientId = 'ferreteria-web'
)

$ErrorActionPreference = 'Stop'

function Obtener-TokenAdmin {
    $body = @{
        client_id = 'admin-cli'
        username  = $AdminUser
        password  = $AdminPassword
        grant_type = 'password'
    }

    $respuesta = Invoke-RestMethod `
        -Uri "$KeycloakUrl/realms/master/protocol/openid-connect/token" `
        -Method Post -Body $body -ContentType 'application/x-www-form-urlencoded'

    return $respuesta.access_token
}

function Obtener-Usuarios {
    param([string] $Token)

    $headers = @{ Authorization = "Bearer $Token" }

    return @(Invoke-RestMethod `
        -Uri "$KeycloakUrl/admin/realms/$Realm/users?max=1000" `
        -Headers $headers)
}

function Probar-Login {
    param(
        [string] $Usuario,
        [string] $Contrasena
    )

    $body = @{
        client_id  = $WebClientId
        username   = $Usuario
        password   = $Contrasena
        grant_type = 'password'
    }

    try {
        $respuesta = Invoke-RestMethod `
            -Uri "$KeycloakUrl/realms/$Realm/protocol/openid-connect/token" `
            -Method Post -Body $body -ContentType 'application/x-www-form-urlencoded'

        return [pscustomobject]@{
            Usuario = $Usuario
            OK      = -not [string]::IsNullOrWhiteSpace($respuesta.access_token)
            Detalle = 'Token obtenido'
        }
    }
    catch {
        $codigo = $_.Exception.Response.StatusCode.value__
        return [pscustomobject]@{
            Usuario = $Usuario
            OK      = $false
            Detalle = "HTTP $codigo - $($_.ErrorDetails.Message)"
        }
    }
}

$tokenAdmin = Obtener-TokenAdmin
$headers = @{ Authorization = "Bearer $tokenAdmin" }

Write-Host "Realm: $Realm"

$usuarios = Obtener-Usuarios -Token $tokenAdmin
Write-Host "Usuarios antes de la migración: $($usuarios.Count)"

$conservar = @('jrios', 'mlopez')
$eliminados = 0
$errores = 0

foreach ($usuario in $usuarios) {
    if ($conservar -contains $usuario.username) {
        Write-Host "  Conservado: $($usuario.username) ($($usuario.email))"
        continue
    }

    try {
        Invoke-RestMethod `
            -Uri "$KeycloakUrl/admin/realms/$Realm/users/$($usuario.id)" `
            -Method Delete -Headers $headers | Out-Null

        $eliminados++
        Write-Host "  Eliminado: $($usuario.username) ($($usuario.email))"
    }
    catch {
        $errores++
        Write-Warning "  No se pudo eliminar $($usuario.username): $($_.Exception.Message)"
    }
}

$despues = Obtener-Usuarios -Token $tokenAdmin
Write-Host "Usuarios después de la migración: $($despues.Count)"
Write-Host "Eliminados: $eliminados | Errores: $errores"
$despues | Select-Object username, email, enabled | Format-Table -AutoSize | Out-String | Write-Host

Write-Host 'Verificación de credenciales (no se modifican):'
$credenciales = @(
    @{ Usuario = 'jrios';  Contrasena = 'admin123'  },
    @{ Usuario = 'mlopez'; Contrasena = 'clave123'  }
)

$resultados = foreach ($credencial in $credenciales) {
    Probar-Login -Usuario $credencial.Usuario -Contrasena $credencial.Contrasena
}

$resultados | Format-Table -AutoSize | Out-String | Write-Host

if ($errores -gt 0 -or $despues.Count -ne 2 -or ($resultados | Where-Object { -not $_.OK }).Count -gt 0) {
    throw 'La migración de Keycloak no terminó correctamente. Revise la salida.'
}

Write-Host 'Migración de Keycloak completada: solo jrios y mlopez, con sus contraseñas originales.'
