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
$aiCatalog = Assert-Status "/.well-known/ai-catalog.json"
$openApi = Assert-Status "/openapi.json"
$sitemap = Assert-Status "/sitemap.xml"
$ogImage = Assert-Status "/opengraph-image?title=Dynamic%20field%20note%20%26%20tests&description=Graphite%20and%20brass%20theme&type=gist&date=2026-08-19&path=%2Fgist%2Fdynamic-field-note"
$favicon = Assert-Status "/favicon.svg"
$faviconIco = Assert-Status "/favicon.ico"
$faviconPng = Assert-Status "/favicon.png"
$favicon16 = Assert-Status "/favicon-16x16.png"
$favicon32 = Assert-Status "/favicon-32x32.png"
$android192 = Assert-Status "/android-chrome-192x192.png"
$android512 = Assert-Status "/android-chrome-512x512.png"
$appleTouchIcon = Assert-Status "/apple-touch-icon.png"
$llmsBody = Get-Body "/llms.txt"
$fullBody = Get-Body "/llms-full.txt"
$profileBody = Get-Body "/api/profile"
$openApiBody = Get-Body "/openapi.json"
$sitemapBody = Get-Body "/sitemap.xml"
$authorizationServerBody = Get-Body "/.well-known/oauth-authorization-server"
$webBotAuthDirectoryBody = Get-Body "/.well-known/http-message-signatures-directory"
$a2aCardBody = Get-Body "/.well-known/agent-card.json"
$mcpCardBody = Get-Body "/.well-known/mcp/server-card.json"
$aiCatalogBody = Get-Body "/.well-known/ai-catalog.json"
$ogImageBody = Get-Body "/opengraph-image?title=Dynamic%20field%20note%20%26%20tests&description=Graphite%20and%20brass%20theme&type=gist&date=2026-08-19&path=%2Fgist%2Fdynamic-field-note"
$faviconBody = Get-Body "/favicon.svg"
$mcpInitializeBody = curl.exe --fail --show-error --silent --request POST --header "Content-Type: application/json" --data '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"validation","version":"1.0"}}}' "$base/mcp" | Out-String

function Invoke-Mcp {
    param([hashtable]$Payload)

    return Invoke-RestMethod -Uri "$base/mcp" -Method Post -ContentType "application/json" -Body ($Payload | ConvertTo-Json -Depth 20)
}

function Invoke-McpTool {
    param(
        [string]$Name,
        [hashtable]$Arguments = @{},
        [int]$Id = 10
    )

    return Invoke-Mcp -Payload @{
        jsonrpc = "2.0"
        id = $Id
        method = "tools/call"
        params = @{ name = $Name; arguments = $Arguments }
    }
}

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

if ($aiCatalog.Headers["Content-Type"] -notlike "application/json*") {
    throw "AI Catalog must be served as application/json"
}

if ($aiCatalog.Headers["Access-Control-Allow-Origin"] -ne "*") {
    throw "AI Catalog must allow cross-origin reads"
}

if ($ogImage.Headers["Content-Type"] -notlike "image/svg+xml*") {
    throw "OpenGraph image must be served as SVG"
}

foreach ($marker in @("1200", "630", "Dynamic field note", "FIELD NOTE", "#c89a49", "#0d0e0c")) {
    if ($ogImageBody -notlike "*$marker*") {
        throw "OpenGraph SVG is missing '$marker'"
    }
}

if ($favicon.Headers["Content-Type"] -notlike "image/svg+xml*") {
    throw "Favicon must be served as SVG"
}

foreach ($marker in @("64", "SC", "#c89a49", "#0d0e0c", "#7f9870")) {
    if ($faviconBody -notlike "*$marker*") {
        throw "Favicon SVG is missing '$marker'"
    }
}

foreach ($icon in @(
    @{ Name = "/favicon.ico"; Response = $faviconIco; Type = "image/x-icon" },
    @{ Name = "/favicon.png"; Response = $faviconPng; Type = "image/png" },
    @{ Name = "/favicon-16x16.png"; Response = $favicon16; Type = "image/png" },
    @{ Name = "/favicon-32x32.png"; Response = $favicon32; Type = "image/png" },
    @{ Name = "/android-chrome-192x192.png"; Response = $android192; Type = "image/png" },
    @{ Name = "/android-chrome-512x512.png"; Response = $android512; Type = "image/png" },
    @{ Name = "/apple-touch-icon.png"; Response = $appleTouchIcon; Type = "image/png" }
)) {
    if ($icon.Response.Headers["Content-Type"] -notlike "$($icon.Type)*") {
        throw "$($icon.Name) must be served as $($icon.Type)"
    }
}

$aiCatalogDocument = $aiCatalogBody | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($aiCatalogDocument.specVersion) -or
    $null -eq $aiCatalogDocument.host -or
    [string]::IsNullOrWhiteSpace($aiCatalogDocument.host.displayName) -or
    [string]::IsNullOrWhiteSpace($aiCatalogDocument.host.identifier) -or
    $null -eq $aiCatalogDocument.entries -or
    $aiCatalogDocument.entries.Count -eq 0) {
    throw "AI Catalog is missing specVersion, host, or entries"
}

foreach ($entry in @($aiCatalogDocument.entries)) {
    $hasUrl = -not [string]::IsNullOrWhiteSpace($entry.url)
    $hasData = $null -ne $entry.data
    if ([string]::IsNullOrWhiteSpace($entry.identifier) -or
        [string]::IsNullOrWhiteSpace($entry.displayName) -or
        [string]::IsNullOrWhiteSpace($entry.type) -or
        ($hasUrl -eq $hasData) -or
        $null -eq $entry.representativeQueries -or
        $entry.representativeQueries.Count -lt 2 -or
        $entry.representativeQueries.Count -gt 5) {
        throw "AI Catalog entry is missing required fields or has invalid url/data/query cardinality"
    }

    if ($entry.identifier -notlike "urn:air:sametcc.me:*") {
        throw "AI Catalog entry identifier must use the sametcc.me ARD URN namespace"
    }
}

$mcpInitializeDocument = $mcpInitializeBody | ConvertFrom-Json
if ($null -eq $mcpInitializeDocument.result.serverInfo -or
    $mcpInitializeDocument.result.serverInfo.name -ne "io.sametcc.personal-website") {
    throw "MCP initialize response is invalid"
}

$mcpToolsDocument = Invoke-Mcp -Payload @{ jsonrpc = "2.0"; id = 2; method = "tools/list" }
$requiredMcpTools = @("get_profile", "list_projects", "get_content", "search_content", "get_timeline", "get_skills", "list_taxonomy", "get_related_content")
$availableMcpTools = @($mcpToolsDocument.result.tools | ForEach-Object { $_.name })
foreach ($toolName in $requiredMcpTools) {
    if ($availableMcpTools -notcontains $toolName) {
        throw "MCP tools/list is missing '$toolName'"
    }
}

$toolChecks = @(
    @{ Name = "get_profile"; Arguments = @{} },
    @{ Name = "list_projects"; Arguments = @{ limit = 2 } },
    @{ Name = "get_content"; Arguments = @{ section = "projects"; slug = "booking-calendar"; includeBody = $false } },
    @{ Name = "search_content"; Arguments = @{ query = "developer"; limit = 2 } },
    @{ Name = "get_timeline"; Arguments = @{} },
    @{ Name = "get_skills"; Arguments = @{} },
    @{ Name = "list_taxonomy"; Arguments = @{} },
    @{ Name = "get_related_content"; Arguments = @{ section = "projects"; slug = "booking-calendar" } }
)

foreach ($check in $toolChecks) {
    $toolResponse = Invoke-McpTool -Name $check.Name -Arguments $check.Arguments
    if ($null -ne $toolResponse.error -or $null -eq $toolResponse.result.structuredContent) {
        throw "MCP tool '$($check.Name)' did not return structured content"
    }
    Write-Output "PASS MCP tool $($check.Name)"
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
