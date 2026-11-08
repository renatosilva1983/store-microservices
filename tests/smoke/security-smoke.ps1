<#
.SYNOPSIS
  Teste de fumaça das proteções de segurança, contra o sistema rodando (via Gateway).

.DESCRIPTION
  Confere, de ponta a ponta:
    1. Headers de segurança nas respostas e ausência dos headers que revelam a tecnologia.
    2. BOLA/IDOR: o cliente 2 não lê nem cancela o pedido do cliente 1 (404); o dono e o admin leem.
    3. Rate limiting no login: tentativas seguidas terminam em 429 com Retry-After.
  Pré-requisito: Gateway, Identity, Catalog e Ordering no ar (perfil "Back-end" do Visual Studio
  ou docker compose) e ao menos um produto com estoque no catálogo.

.EXAMPLE
  .\tests\smoke\security-smoke.ps1
  .\tests\smoke\security-smoke.ps1 -Gateway http://localhost:5000
#>
param(
    [string]$Gateway = 'http://localhost:5000',
    [string]$Password = 'Store@2026'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$http = New-Object System.Net.Http.HttpClient
$script:failures = 0

function Send([string]$Method, [string]$Path, [string]$Token, $Body) {
    $request = New-Object System.Net.Http.HttpRequestMessage ([System.Net.Http.HttpMethod]::new($Method)), "$Gateway$Path"
    if ($Token) { $request.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue 'Bearer', $Token }
    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 5 -Compress
        $request.Content = New-Object System.Net.Http.StringContent $json, ([System.Text.Encoding]::UTF8), 'application/json'
    }
    $response = $http.SendAsync($request).GetAwaiter().GetResult()
    $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $data = $null
    if ($text) { try { $data = $text | ConvertFrom-Json } catch { } }
    [pscustomobject]@{ Status = [int]$response.StatusCode; Response = $response; Data = $data }
}

function Check([string]$Name, [bool]$Ok, [string]$Detail = '') {
    if ($Ok) { Write-Host "  [OK]    $Name" -ForegroundColor Green }
    else { Write-Host "  [FALHA] $Name $Detail" -ForegroundColor Red; $script:failures++ }
}

function Header($Result, [string]$Name) {
    $values = $null
    if ($Result.Response.Headers.TryGetValues($Name, [ref]$values)) { return ($values -join ',') }
    if ($Result.Response.Content.Headers.TryGetValues($Name, [ref]$values)) { return ($values -join ',') }
    return $null
}

function Login([string]$Email) {
    $result = Send 'POST' '/api/auth/login' $null @{ email = $Email; password = $Password }
    if ($result.Status -ne 200) { throw "Login de $Email falhou com HTTP $($result.Status). Os serviços estão no ar?" }
    return $result.Data.accessToken
}

Write-Host "`n1) Headers de segurança" -ForegroundColor Cyan
$products = Send 'GET' '/api/catalog/products?page=1&pageSize=20'
Check 'Catálogo responde 200' ($products.Status -eq 200) "(HTTP $($products.Status))"
Check 'X-Content-Type-Options = nosniff' ((Header $products 'X-Content-Type-Options') -eq 'nosniff')
Check 'X-Frame-Options = DENY' ((Header $products 'X-Frame-Options') -eq 'DENY')
Check 'Referrer-Policy = no-referrer' ((Header $products 'Referrer-Policy') -eq 'no-referrer')
Check "Content-Security-Policy com frame-ancestors 'none'" ((Header $products 'Content-Security-Policy') -like "*frame-ancestors 'none'*")
Check 'Sem header Server' ($null -eq (Header $products 'Server'))
Check 'Sem header X-Powered-By' ($null -eq (Header $products 'X-Powered-By'))

Write-Host "`n2) BOLA/IDOR: pedido só do dono (e do admin)" -ForegroundColor Cyan
# Logins antes do teste de força bruta, que esgota o limite de tentativas por minuto.
$adminToken = Login 'admin@store.dev'
$ownerToken = Login 'cliente@store.dev'
$otherToken = Login 'cliente2@store.dev'

$product = $products.Data.items | Where-Object { $_.stockQuantity -gt 0 } | Select-Object -First 1
if (-not $product) { throw 'Nenhum produto com estoque no catálogo: cadastre um e rode de novo.' }

$created = Send 'POST' '/api/orders' $ownerToken @{ shippingMethod = 'Standard'; items = @(@{ productId = $product.id; quantity = 1 }) }
Check 'Cliente 1 cria o pedido (201)' ($created.Status -eq 201) "(HTTP $($created.Status))"
$orderId = $created.Data.id

$own = Send 'GET' "/api/orders/$orderId" $ownerToken
Check 'Dono lê o próprio pedido (200)' ($own.Status -eq 200) "(HTTP $($own.Status))"
Check 'Pedido gravado com o e-mail do token' ($own.Data.customerEmail -eq 'cliente@store.dev')

$other = Send 'GET' "/api/orders/$orderId" $otherToken
Check 'Cliente 2 NÃO lê o pedido do cliente 1 (404)' ($other.Status -eq 404) "(HTTP $($other.Status))"

$otherList = Send 'GET' '/api/orders?page=1&pageSize=100' $otherToken
Check 'Pedido do cliente 1 não aparece na lista do cliente 2' (-not ($otherList.Data.items | Where-Object { $_.id -eq $orderId }))

$otherCancel = Send 'POST' "/api/orders/$orderId/cancel" $otherToken
Check 'Cliente 2 NÃO cancela o pedido do cliente 1 (404)' ($otherCancel.Status -eq 404) "(HTTP $($otherCancel.Status))"

$admin = Send 'GET' "/api/orders/$orderId" $adminToken
Check 'Admin lê qualquer pedido (200)' ($admin.Status -eq 200) "(HTTP $($admin.Status))"

$anonymous = Send 'GET' "/api/orders/$orderId"
Check 'Sem token: 401' ($anonymous.Status -eq 401) "(HTTP $($anonymous.Status))"

Write-Host "`n3) Rate limiting no login (força bruta)" -ForegroundColor Cyan
$limited = $null
for ($i = 1; $i -le 10; $i++) {
    $attempt = Send 'POST' '/api/auth/login' $null @{ email = 'cliente@store.dev'; password = 'senha-errada' }
    if ($attempt.Status -eq 429) { $limited = $attempt; break }
}
Check 'Tentativas seguidas terminam em 429' ($null -ne $limited)
if ($limited) {
    Check "429 traz Retry-After (tentativa $i)" ($null -ne (Header $limited 'Retry-After'))
}

Write-Host ''
if ($script:failures -eq 0) {
    Write-Host 'Tudo certo: as proteções estão funcionando.' -ForegroundColor Green
    Write-Host 'O login fica bloqueado por até 1 minuto a partir deste IP.' -ForegroundColor DarkGray
    exit 0
}
Write-Host "$($script:failures) verificação(ões) falharam." -ForegroundColor Red
exit 1
