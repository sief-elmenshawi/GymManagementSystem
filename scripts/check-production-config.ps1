<#
.SYNOPSIS
    Checks that required production configuration names are present without printing values.

.DESCRIPTION
    Reads configuration from the current process environment by default, or from an
    env file when -EnvFile is supplied. It checks presence and the documented shape
    of the AutoMapper key; it does not print, log, or echo any secret value.

.EXAMPLE
    ./scripts/check-production-config.ps1

.EXAMPLE
    ./scripts/check-production-config.ps1 -EnvFile /etc/gymmanagement/production.env

.EXAMPLE
    ./scripts/check-production-config.ps1 -EnvFile ./production.env -RequireSeedPassword

.EXAMPLE
    ./scripts/check-production-config.ps1 -EnvFile ./production.env -AllowNoSmtpHost
#>
[CmdletBinding()]
param(
    [string]$EnvFile,

    [switch]$RequireSeedPassword,

    [switch]$AllowNoSmtpHost
)

$script:failureCount = 0
$script:useEnvFile = $false
$script:envFileValues = @{}

function Write-Pass {
    param([string]$Message)
    Write-Host "PASS: $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message)
    Write-Host "FAIL: $Message" -ForegroundColor Red
    $script:failureCount++
}

function Test-HasValue {
    param([string]$Value)
    return -not [string]::IsNullOrWhiteSpace($Value)
}

function Read-EnvFile {
    param([string]$Path)

    $values = @{}
    $lineNumber = 0

    foreach ($line in Get-Content -LiteralPath $Path) {
        $lineNumber++
        $entry = $line.Trim()

        if ([string]::IsNullOrWhiteSpace($entry) -or $entry.StartsWith('#')) {
            continue
        }

        if ($entry.StartsWith('export ')) {
            $entry = $entry.Substring('export '.Length).TrimStart()
        }

        $separator = $entry.IndexOf('=')
        if ($separator -le 0) {
            throw "Invalid entry in environment file at line $lineNumber (expected KEY=VALUE); value not shown."
        }

        $key = $entry.Substring(0, $separator).Trim()
        $value = $entry.Substring($separator + 1).Trim()

        if ($value.Length -ge 2) {
            $first = $value[0]
            $last = $value[$value.Length - 1]
            if (($first -eq '"' -and $last -eq '"') -or ($first -eq "'" -and $last -eq "'")) {
                $value = $value.Substring(1, $value.Length - 2)
            }
        }

        $values[$key] = $value
    }

    return $values
}

function Get-ConfigValue {
    param([string]$Name)

    if ($script:useEnvFile) {
        if ($script:envFileValues.ContainsKey($Name)) {
            return [string]$script:envFileValues[$Name]
        }
        return $null
    }

    return [Environment]::GetEnvironmentVariable($Name)
}

function Find-ConfigSource {
    param([string[]]$Names)

    foreach ($name in $Names) {
        $value = Get-ConfigValue -Name $name
        if (Test-HasValue -Value $value) {
            return [pscustomobject]@{
                Name  = $name
                Value = $value
            }
        }
    }

    return $null
}

function Assert-AnyPresent {
    param(
        [string[]]$Names,
        [string]$Label,
        [switch]$AllowMissing
    )

    $source = Find-ConfigSource -Names $Names
    if ($null -ne $source) {
        Write-Pass "$Label (via $($source.Name))"
        return
    }

    if ($AllowMissing) {
        Write-Warning "WARN: $Label is not set."
        return
    }

    Write-Fail "$Label is missing. Set one of: $($Names -join ', ')."
}

function Test-LicenseKeyShape {
    param([string]$Value)

    $segments = $Value.Split('.')
    if ($segments.Count -ne 3) {
        return $false
    }

    $segmentPattern = '^[A-Za-z0-9_-]+={0,2}$'
    return ($segments[0] -match $segmentPattern) -and
           ($segments[1] -match $segmentPattern) -and
           ([string]::IsNullOrEmpty($segments[2]) -or $segments[2] -match $segmentPattern)
}

if (-not [string]::IsNullOrWhiteSpace($EnvFile)) {
    if (-not (Test-Path -LiteralPath $EnvFile -PathType Leaf)) {
        throw "Environment file not found: $EnvFile"
    }

    $script:envFileValues = Read-EnvFile -Path $EnvFile
    $script:useEnvFile = $true
}

$environmentSource = Find-ConfigSource -Names @('ASPNETCORE_ENVIRONMENT', 'DOTNET_ENVIRONMENT')
if ($null -eq $environmentSource) {
    Write-Fail "Environment is missing. Set ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT to Production."
}
elseif ($environmentSource.Value -ine 'Production') {
    Write-Fail "Environment must be Production. Check ASPNETCORE_ENVIRONMENT/DOTNET_ENVIRONMENT."
}
else {
    Write-Pass "Environment is Production (via $($environmentSource.Name))"
}

Assert-AnyPresent -Names @(
    'ConnectionStrings__DefaultConnection',
    'ConnectionStrings:DefaultConnection'
) -Label 'SQL connection string'

$licenseSource = Find-ConfigSource -Names @(
    'AutoMapper__LicenseKey',
    'AutoMapper:LicenseKey',
    'AUTOMAPPER_LICENSE_KEY',
    'LUCKYPENNY_LICENSE_KEY'
)
if ($null -eq $licenseSource) {
    Write-Fail 'AutoMapper license key is missing. Set AUTOMAPPER_LICENSE_KEY, LUCKYPENNY_LICENSE_KEY, or AutoMapper__LicenseKey.'
}
elseif (-not (Test-LicenseKeyShape -Value $licenseSource.Value)) {
    Write-Fail "AutoMapper license key set via $($licenseSource.Name) is not a well-formed JWT."
}
else {
    Write-Pass "AutoMapper license key (via $($licenseSource.Name))"
}

Assert-AnyPresent -Names @(
    'Email__Smtp__Host',
    'Email:Smtp:Host'
) -Label 'SMTP host' -AllowMissing:$AllowNoSmtpHost

Assert-AnyPresent -Names @(
    'GYM_SEED_PASSWORD',
    'SeedUsers__Password',
    'SeedUsers:Password'
) -Label 'Seed password' -AllowMissing:(-not $RequireSeedPassword)

$smtpUser = Find-ConfigSource -Names @('Email__Smtp__User', 'Email:Smtp:User')
$smtpPassword = Find-ConfigSource -Names @('Email__Smtp__Password', 'Email:Smtp:Password')
if ($null -ne $smtpUser -and $null -eq $smtpPassword) {
    Write-Warning 'WARN: SMTP user is set but SMTP password is not. Some SMTP providers require both.'
}

$from = Find-ConfigSource -Names @('Email__From', 'Email:From')
if ($null -eq $from) {
    Write-Warning 'WARN: Email sender is not set; the runtime default will be used.'
}

if ($script:failureCount -gt 0) {
    Write-Host ''
    Write-Host "$($script:failureCount) required production setting(s) missing or invalid. No values were printed." -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host 'Production configuration check passed. No values were printed.' -ForegroundColor Green
exit 0
