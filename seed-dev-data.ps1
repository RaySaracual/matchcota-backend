# seed-dev-data.ps1
# Siembra datos de prueba en el backend de Matchcota para validar el flujo
# discovery -> swipe -> match.
# Uso: .\seed-dev-data.ps1 [-BaseUrl http://localhost:5269]

param(
    [string]$BaseUrl = "http://localhost:5269"
)

$ErrorActionPreference = "Stop"
$headers = @{ "Content-Type" = "application/json" }

function Invoke-Api {
    param([string]$Method, [string]$Path, [object]$Body, [string]$Token)
    $h = $headers.Clone()
    if ($Token) { $h["Authorization"] = "Bearer $Token" }
    $json = if ($Body) { $Body | ConvertTo-Json -Depth 5 } else { $null }
    try {
        $resp = Invoke-RestMethod -Uri "$BaseUrl$Path" -Method $Method -Headers $h -Body $json -ErrorAction Stop
        return $resp
    } catch {
        $status = $_.Exception.Response?.StatusCode.value__
        $detail = $_.ErrorDetails?.Message
        Write-Host "  [ERROR] $Method $Path -> HTTP $status : $detail" -ForegroundColor Red
        throw
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Matchcota — seed de datos de prueba" -ForegroundColor Cyan
Write-Host "  Backend: $BaseUrl" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ── Usuarios ──────────────────────────────────────────────────────────────────

$users = @(
    @{ email = "alice@matchcota.dev"; password = "Dev1234!"; displayName = "Alice" },
    @{ email = "bob@matchcota.dev";   password = "Dev1234!"; displayName = "Bob"   }
)

$tokens = @{}

foreach ($u in $users) {
    Write-Host "► Usuario: $($u.displayName) <$($u.email)>" -ForegroundColor Yellow

    # Intentar registro; si ya existe, hacer login
    try {
        $reg = Invoke-Api -Method POST -Path "/api/v1/auth/register" -Body $u
        $tokens[$u.email] = $reg.accessToken
        Write-Host "  [OK] Registrado  (userId: $($reg.userId))" -ForegroundColor Green
    } catch {
        Write-Host "  Ya existe — haciendo login..." -ForegroundColor DarkYellow
        $login = Invoke-Api -Method POST -Path "/api/v1/auth/login" -Body @{
            email    = $u.email
            password = $u.password
        }
        $tokens[$u.email] = $login.accessToken
        Write-Host "  [OK] Login exitoso (userId: $($login.userId))" -ForegroundColor Green
    }
}

# ── Perros (coordenadas cercanas en Santo Domingo, DO) ─────────────────────────

$dogs = @(
    @{
        owner      = "alice@matchcota.dev"
        name       = "Rocky"
        breed      = "Golden Retriever"
        bio        = "Me encantan los paseos largos y jugar en el parque."
        latitude   = 18.4861
        longitude  = -69.9312
    },
    @{
        owner      = "bob@matchcota.dev"
        name       = "Luna"
        breed      = "Labrador Retriever"
        bio        = "Soy muy juguetona y me llevo bien con todos."
        latitude   = 18.4872   # ~130 m de Rocky
        longitude  = -69.9301
    }
)

$dogIds = @{}

foreach ($d in $dogs) {
    $token = $tokens[$d.owner]
    Write-Host ""
    Write-Host "► Perro: $($d.name) (dueño: $($d.owner))" -ForegroundColor Yellow

    # Verificar si ya tiene perros
    $existing = Invoke-Api -Method GET -Path "/api/v1/dogs/mine" -Token $token
    if ($existing.Count -gt 0) {
        $dogIds[$d.owner] = $existing[0].dogId
        Write-Host "  Ya existe — usando dogId: $($dogIds[$d.owner])" -ForegroundColor DarkYellow
        continue
    }

    $body = @{
        name      = $d.name
        breed     = $d.breed
        bio       = $d.bio
        latitude  = $d.latitude
        longitude = $d.longitude
    }
    $created = Invoke-Api -Method POST -Path "/api/v1/dogs" -Body $body -Token $token
    $dogIds[$d.owner] = $created.dogId
    Write-Host "  [OK] Creado (dogId: $($created.dogId))" -ForegroundColor Green
}

# ── Swipe mutuo para generar match ────────────────────────────────────────────

$dogAlice = $dogIds["alice@matchcota.dev"]
$dogBob   = $dogIds["bob@matchcota.dev"]

Write-Host ""
Write-Host "► Swipe: Rocky (Alice) le da LIKE a Luna (Bob)" -ForegroundColor Yellow
$sw1 = Invoke-Api -Method POST -Path "/api/v1/discovery/swipes" -Body @{
    sourceDogId = $dogAlice
    targetDogId = $dogBob
    isLike      = $true
} -Token $tokens["alice@matchcota.dev"]
Write-Host "  [OK] SwipeId: $($sw1.swipeId)  MatchCreado: $($sw1.matchCreated)" -ForegroundColor Green

Write-Host ""
Write-Host "► Swipe: Luna (Bob) le da LIKE a Rocky (Alice)" -ForegroundColor Yellow
$sw2 = Invoke-Api -Method POST -Path "/api/v1/discovery/swipes" -Body @{
    sourceDogId = $dogBob
    targetDogId = $dogAlice
    isLike      = $true
} -Token $tokens["bob@matchcota.dev"]
Write-Host "  [OK] SwipeId: $($sw2.swipeId)  MatchCreado: $($sw2.matchCreated)  MatchId: $($sw2.matchId)" -ForegroundColor Green

# ── Resumen ───────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SEED COMPLETADO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Credenciales para probar en la app:" -ForegroundColor White
Write-Host "  alice@matchcota.dev  /  Dev1234!" -ForegroundColor White
Write-Host "  bob@matchcota.dev    /  Dev1234!" -ForegroundColor White
Write-Host ""
if ($sw2.matchCreated) {
    Write-Host "Match creado  MatchId: $($sw2.matchId)" -ForegroundColor Magenta
    Write-Host "  -> Ve a Discovery > Mis Matches en la app para verificarlo." -ForegroundColor Magenta
}
Write-Host ""
