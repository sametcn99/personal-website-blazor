param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"
$base = $BaseUrl.TrimEnd('/')

function Assert-Status {
    param([string]$Path)

    $response = Invoke-WebRequest -Uri "$base$Path" -UseBasicParsing
    if ($response.StatusCode -ne 200) {
        throw "$Path returned HTTP $($response.StatusCode)"
    }

    Write-Host "PASS $Path ($($response.StatusCode))"
    return $response
}

function Get-Body {
    param([string]$Path)

    return (curl.exe --fail --show-error --silent "$base$Path" | Out-String)
}

$llms = Assert-Status "/llms.txt"
$full = Assert-Status "/llms-full.txt"
$profile = Assert-Status "/api/profile"
$timeline = Assert-Status "/api/timeline"
$skills = Assert-Status "/api/skills"
$github = Assert-Status "/api/profile/github"
$auth = Assert-Status "/auth.md"
$protectedResource = Assert-Status "/.well-known/oauth-protected-resource"
$authorizationServer = Assert-Status "/.well-known/oauth-authorization-server"
$webBotAuthDirectory = Assert-Status "/.well-known/http-message-signatures-directory"
$a2aCard = Assert-Status "/.well-known/agent-card.json"
$mcpCard = Assert-Status "/.well-known/mcp/server-card.json"
$openApi = Assert-Status "/openapi.json"
$sitemap = Assert-Status "/sitemap.xml"
$llmsBody = Get-Body "/llms.txt"
$fullBody = Get-Body "/llms-full.txt"
$profileBody = Get-Body "/api/profile"
$openApiBody = Get-Body "/openapi.json"
$sitemapBody = Get-Body "/sitemap.xml"
$authorizationServerBody = Get-Body "/.well-known/oauth-authorization-server"
$webBotAuthDirectoryBody = Get-Body "/.well-known/http-message-signatures-directory"
$a2aCardBody = Get-Body "/.well-known/agent-card.json"
$mcpCardBody = Get-Body "/.well-known/mcp/server-card.json"
$mcpInitializeBody = curl.exe --fail --show-error --silent --request POST --header "Content-Type: application/json" --data '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"validation","version":"1.0"}}}' "$base/mcp" | Out-String

foreach ($path in @("/index.md", "/about.md", "/cv.md", "/timeline.md", "/skills.md", "/blog.md", "/content.md", "/repo.md", "/blog/projeler-icin-ortak-developer-ve-ai-dokumantasyonu.md")) {
    $markdown = Invoke-WebRequest -Uri "$base$path" -Headers @{ Accept = "text/markdown" } -UseBasicParsing
    if ($markdown.StatusCode -ne 200 -or $markdown.Headers["Content-Type"] -notlike "text/markdown*") {
        throw "$path did not return Markdown"
    }

    Write-Output "PASS $path (Markdown)"
}

$openApiDocument = $openApiBody | ConvertFrom-Json
foreach ($path in @("/api/profile", "/api/timeline", "/api/skills", "/api/content/all", "/api/profile/github")) {
    if ($openApiDocument.paths.PSObject.Properties.Name -notcontains $path) {
        throw "OpenAPI is missing $path"
    }
}

$profileDocument = $profileBody | ConvertFrom-Json
foreach ($property in @("name", "jobTitle", "skills", "experience", "education", "lastUpdated")) {
    if ($null -eq $profileDocument.$property) {
        throw "Profile response is missing $property"
    }
}

$authorizationServerDocument = $authorizationServerBody | ConvertFrom-Json
if ($null -eq $authorizationServerDocument.agent_auth -or
    [string]::IsNullOrWhiteSpace($authorizationServerDocument.agent_auth.register_uri) -or
    $authorizationServerDocument.agent_auth.identity_types_supported.Count -eq 0) {
    throw "Authorization server metadata is missing agent_auth registration metadata"
}

$webBotAuthDirectoryDocument = $webBotAuthDirectoryBody | ConvertFrom-Json
if ($null -eq $webBotAuthDirectoryDocument.keys -or
    $webBotAuthDirectoryDocument.keys.Count -eq 0 -or
    $webBotAuthDirectoryDocument.keys[0].kty -ne "EC" -or
    [string]::IsNullOrWhiteSpace($webBotAuthDirectoryDocument.keys[0].kid)) {
    throw "Web Bot Auth directory is missing a valid public signing key"
}

$a2aCardDocument = $a2aCardBody | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($a2aCardDocument.name) -or
    [string]::IsNullOrWhiteSpace($a2aCardDocument.version) -or
    [string]::IsNullOrWhiteSpace($a2aCardDocument.description) -or
    $null -eq $a2aCardDocument.supportedInterfaces -or
    $a2aCardDocument.supportedInterfaces.Count -eq 0 -or
    [string]::IsNullOrWhiteSpace($a2aCardDocument.supportedInterfaces[0].url) -or
    [string]::IsNullOrWhiteSpace($a2aCardDocument.supportedInterfaces[0].protocolBinding) -or
    $null -eq $a2aCardDocument.capabilities -or
    $null -eq $a2aCardDocument.skills -or
    $a2aCardDocument.skills.Count -eq 0 -or
    [string]::IsNullOrWhiteSpace($a2aCardDocument.skills[0].id) -or
    [string]::IsNullOrWhiteSpace($a2aCardDocument.skills[0].name) -or
    [string]::IsNullOrWhiteSpace($a2aCardDocument.skills[0].description)) {
    throw "A2A Agent Card is missing required discovery metadata"
}

$mcpCardDocument = $mcpCardBody | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($mcpCardDocument.serverInfo.name) -or
    [string]::IsNullOrWhiteSpace($mcpCardDocument.serverInfo.version) -or
    [string]::IsNullOrWhiteSpace($mcpCardDocument.transport.endpoint)) {
    throw "MCP Server Card is missing serverInfo or transport endpoint"
}

$mcpInitializeDocument = $mcpInitializeBody | ConvertFrom-Json
if ($null -eq $mcpInitializeDocument.result.serverInfo -or
    $mcpInitializeDocument.result.serverInfo.name -ne "io.sametcc.personal-website") {
    throw "MCP initialize response is invalid"
}

foreach ($marker in @("About The Author", "Research Guidance For AI Agents", "Archive Overview", "Content API", "Agent Access")) {
    if ($llmsBody -notlike "*$marker*") {
        throw "/llms.txt is missing '$marker'"
    }
}

if ($fullBody.Length -le $llmsBody.Length) {
    throw "/llms-full.txt must contain more context than /llms.txt"
}

if ($sitemapBody -notlike "*$base/about*") {
    throw "Sitemap is missing /about"
}

Write-Output "All agent surface checks passed."
