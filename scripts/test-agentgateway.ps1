param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [int]$CustomerId = 1,

    [string]$ApiKey = "dev-change-me",

    [switch]$SkipCheckout
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$normalizedBaseUrl = $BaseUrl.TrimEnd('/')

$headers = @{
    "X-Dev-Api-Key" = $ApiKey
}

function Invoke-RestWithRetry {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Get", "Post", "Put", "Delete")]
        [string]$Method,

        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [hashtable]$Headers = @{},

        [string]$ContentType,

        [string]$Body,

        [int]$MaxAttempts = 3
    )

    $lastError = $null
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            $invokeParams = @{
                Method = $Method
                Uri    = $Uri
            }

            if ($Headers.Count -gt 0) {
                $invokeParams["Headers"] = $Headers
            }

            if (-not [string]::IsNullOrWhiteSpace($ContentType)) {
                $invokeParams["ContentType"] = $ContentType
            }

            if (-not [string]::IsNullOrWhiteSpace($Body)) {
                $invokeParams["Body"] = $Body
            }

            return Invoke-RestMethod @invokeParams
        }
        catch {
            $lastError = $_
            if ($attempt -lt $MaxAttempts) {
                Write-Host "Attempt $attempt failed for $Method $Uri. Retrying..." -ForegroundColor DarkYellow
                Start-Sleep -Milliseconds 500
            }
        }
    }

    throw $lastError
}

function Resolve-WorkingBaseUrl {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CandidateBaseUrl
    )

    $candidates = New-Object System.Collections.Generic.List[string]
    $candidates.Add($CandidateBaseUrl)

    if ($CandidateBaseUrl.StartsWith("http://localhost", [System.StringComparison]::OrdinalIgnoreCase)) {
        $httpsCandidate = "https://" + $CandidateBaseUrl.Substring("http://".Length)
        if (-not $candidates.Contains($httpsCandidate)) {
            $candidates.Add($httpsCandidate)
        }
    }

    foreach ($candidate in $candidates) {
        try {
            Write-Host "Probing agent-card at $candidate/agent-card" -ForegroundColor DarkCyan
            $null = Invoke-RestWithRetry -Method Get -Uri "$candidate/agent-card" -MaxAttempts 2
            return $candidate
        }
        catch {
            Write-Host "Probe failed for ${candidate}: $($_.Exception.Message)" -ForegroundColor DarkYellow
        }
    }

    throw "Unable to reach AgentGateway at '$CandidateBaseUrl'. Ensure AppHost is running and the port/path are correct."
}

$resolvedBaseUrl = Resolve-WorkingBaseUrl -CandidateBaseUrl $normalizedBaseUrl

function Invoke-AgentTask {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Operation,

        [hashtable]$Extra = @{}
    )

    $uri = "$resolvedBaseUrl/tasks"
    $body = @{
        operation  = $Operation
        customerId = $CustomerId
    }

    foreach ($key in $Extra.Keys) {
        $body[$key] = $Extra[$key]
    }

    $json = $body | ConvertTo-Json -Depth 8

    Write-Host ">>> $Operation" -ForegroundColor Cyan
    return Invoke-RestWithRetry -Method Post -Uri $uri -Headers $headers -ContentType "application/json" -Body $json
}

Write-Host "Testing AgentGateway at $resolvedBaseUrl" -ForegroundColor Yellow

Write-Host ">>> agent-card" -ForegroundColor Cyan
$card = Invoke-RestWithRetry -Method Get -Uri "$resolvedBaseUrl/agent-card"

if ($card -is [string] -and $card.TrimStart().StartsWith("<!DOCTYPE html>", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "The URL '$resolvedBaseUrl' returned HTML instead of AgentGateway JSON. This usually means the port points to the Aspire dashboard or another web UI. Use the AgentGateway endpoint URL from AppHost resources."
}

if (-not ($card.PSObject.Properties.Name -contains "skills")) {
    throw "Unexpected agent-card response from '$resolvedBaseUrl'. Ensure BaseUrl ends with '/api/agent-gateway' and points to AgentGateway."
}

$card | ConvertTo-Json -Depth 8

$products = Invoke-AgentTask -Operation "list_products" -Extra @{ page = 1; size = 5 }
$products | ConvertTo-Json -Depth 8

if ($products -is [string] -and $products.TrimStart().StartsWith("<!DOCTYPE html>", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "list_products returned HTML instead of JSON. Verify AgentGateway BaseUrl and port."
}

$productList = @($products)
if ($productList.Count -eq 0) {
    throw "No products returned from list_products."
}

$productId = $productList[0].id
Write-Host "Selected productId: $productId" -ForegroundColor Green

$product = Invoke-AgentTask -Operation "get_product" -Extra @{ productId = $productId }
$product | ConvertTo-Json -Depth 8

$cart = Invoke-AgentTask -Operation "create_cart" -Extra @{ ttlMinutes = 30 }
$cart | ConvertTo-Json -Depth 8

$cartId = $cart.cartId
if (-not $cartId) {
    throw "create_cart did not return a cartId."
}
Write-Host "Created cartId: $cartId" -ForegroundColor Green

$updatedCart = Invoke-AgentTask -Operation "add_or_update_item" -Extra @{ cartId = $cartId; productId = $productId; quantity = 2 }
$updatedCart | ConvertTo-Json -Depth 8

$fetchedCart = Invoke-AgentTask -Operation "get_cart" -Extra @{ cartId = $cartId }
$fetchedCart | ConvertTo-Json -Depth 8

if (-not $SkipCheckout) {
    $order = Invoke-AgentTask -Operation "checkout_cart" -Extra @{ cartId = $cartId }
    $order | ConvertTo-Json -Depth 8
}

$orders = Invoke-AgentTask -Operation "list_orders"
$orders | ConvertTo-Json -Depth 8

Write-Host "AgentGateway smoke test finished successfully." -ForegroundColor Green
