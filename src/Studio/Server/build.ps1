param(
    [string]$WorkspaceRoot = $PSScriptRoot,
    [string]$StampFile
)

# Stop immediately so Visual Studio and command-line callers surface the first failing step.
$ErrorActionPreference = 'Stop'
$requiredNodeVersion = '18.20.4'
$requiredNodeVersionWithPrefix = "v$requiredNodeVersion"

function Invoke-ExternalCommand
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    # Execute the external process and fail with the full command line when the tool exits unsuccessfully.
    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0)
    {
        throw "Command failed: $FilePath $($Arguments -join ' ')"
    }
}

function Clear-VisualStudioToolchainEnvironment
{
    # Remove Visual Studio native-toolchain variables that can leak into Node native module builds.
    $variables = @(
        'VSINSTALLDIR',
        'VSCMD_VER',
        'VSCMD_ARG_app_plat',
        'VSCMD_ARG_HOST_ARCH',
        'VSCMD_ARG_TGT_ARCH',
        'VCINSTALLDIR',
        'VCIDEInstallDir',
        'VCPKG_ROOT',
        'VCToolsInstallDir',
        'VCToolsRedistDir',
        'VCToolsVersion'
    )

    foreach ($variable in $variables)
    {
        if (Test-Path "Env:$variable")
        {
            Remove-Item "Env:$variable" -ErrorAction SilentlyContinue
        }
    }

    # Force the expected MSBuild toolchain selection for native Node dependencies.
    $env:npm_config_msvs_version = '2022'
    $env:GYP_MSVS_VERSION = '2022'
}

function Use-RequiredNodeVersion
{
    # Prefer nvm when it is available so the script self-corrects on developer machines with multiple Node versions.
    $nvmCommand = Get-Command 'nvm' -ErrorAction SilentlyContinue

    if ($null -ne $nvmCommand)
    {
        & $nvmCommand.Source 'use' $requiredNodeVersion | Out-Host

        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to switch Node.js to version $requiredNodeVersion using nvm."
        }
    }

    # Validate the active Node version before restore or build work begins.
    $nodeCommand = Get-Command 'node' -ErrorAction SilentlyContinue

    if ($null -eq $nodeCommand)
    {
        throw "Node.js $requiredNodeVersionWithPrefix is required to build the Theia shell, but 'node' was not found on PATH."
    }

    $nodeVersion = (& $nodeCommand.Source '--version').Trim()

    if ($nodeVersion -ne $requiredNodeVersionWithPrefix)
    {
        throw "Node.js $requiredNodeVersionWithPrefix is required to build the Theia shell. Current version: $nodeVersion."
    }
}

function Get-ManifestPaths
{
    # Track the workspace manifests that affect restore and browser build output.
    return @(
        (Join-Path $WorkspaceRoot 'package.json'),
        (Join-Path $WorkspaceRoot 'yarn.lock'),
        (Join-Path $WorkspaceRoot 'lerna.json'),
        (Join-Path $WorkspaceRoot 'browser-app\package.json'),
        (Join-Path $WorkspaceRoot 'search-studio\package.json'),
        (Join-Path $WorkspaceRoot 'search-studio\tsconfig.json'),
        (Join-Path $WorkspaceRoot 'search-studio\scripts\copy-assets.js')
    )
}

function Get-BuildInputItems
{
    # Gather the file inputs used to decide whether the browser bundle must be rebuilt.
    $items = @()

    foreach ($path in (Get-ManifestPaths))
    {
        if (Test-Path $path)
        {
            $items += Get-Item $path
        }
    }

    $sourceRoot = Join-Path $WorkspaceRoot 'search-studio\src'

    if (Test-Path $sourceRoot)
    {
        $items += Get-ChildItem -Path $sourceRoot -Recurse -File
    }

    return $items
}

function Test-BrowserBuildRequired
{
    # Always build when no stamp file was supplied or the previous output does not exist yet.
    if ([string]::IsNullOrWhiteSpace($StampFile))
    {
        return $true
    }

    if (-not (Test-Path $StampFile))
    {
        return $true
    }

    $requiredOutputs = @(
        (Join-Path $WorkspaceRoot 'browser-app\lib\backend\main.js'),
        (Join-Path $WorkspaceRoot 'browser-app\lib\frontend\bundle.js'),
        (Join-Path $WorkspaceRoot 'search-studio\lib\browser\home\search-studio-home-widget.css'),
        (Join-Path $WorkspaceRoot 'search-studio\lib\browser\search-studio-frontend-module.js'),
        (Join-Path $WorkspaceRoot 'search-studio\lib\browser\assets\ukho-logo-transparent.png'),
        (Join-Path $WorkspaceRoot 'search-studio\lib\node\search-studio-backend-module.js')
    )

    if (($requiredOutputs | Where-Object { -not (Test-Path $_) }).Count -gt 0)
    {
        return $true
    }

    $stampItem = Get-Item $StampFile
    $buildInputs = Get-BuildInputItems

    if ($buildInputs.Count -eq 0)
    {
        return $false
    }

    $latestInput = $buildInputs | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    return $latestInput.LastWriteTimeUtc -gt $stampItem.LastWriteTimeUtc
}

function Test-YarnInstallRequired
{
    # Re-run yarn install when the workspace dependencies are missing or the manifests changed since the last restore.
    $nodeModulesPath = Join-Path $WorkspaceRoot 'node_modules'
    $installStampPath = Join-Path $nodeModulesPath '.install-stamp'

    if (-not (Test-Path $nodeModulesPath))
    {
        return $true
    }

    if (-not (Test-Path $installStampPath))
    {
        return $true
    }

    $installStamp = Get-Item $installStampPath
    $manifests = Get-ManifestPaths | Where-Object { Test-Path $_ } | ForEach-Object { Get-Item $_ }

    if ($manifests.Count -eq 0)
    {
        return $false
    }

    $latestManifest = $manifests | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    return $latestManifest.LastWriteTimeUtc -gt $installStamp.LastWriteTimeUtc
}

function Write-StampFile
{
    # Persist the incremental build marker only when the caller requested one.
    if ([string]::IsNullOrWhiteSpace($StampFile))
    {
        return
    }

    $stampDirectory = Split-Path -Path $StampFile -Parent

    if (-not [string]::IsNullOrWhiteSpace($stampDirectory))
    {
        New-Item -ItemType Directory -Path $stampDirectory -Force | Out-Null
    }

    Set-Content -Path $StampFile -Value ([DateTimeOffset]::UtcNow.ToString('O'))
}

Push-Location $WorkspaceRoot

try
{
    # Normalize the toolchain environment before any JavaScript work starts.
    Clear-VisualStudioToolchainEnvironment
    Use-RequiredNodeVersion

    $yarnCommand = Get-Command 'yarn' -ErrorAction SilentlyContinue

    if ($null -eq $yarnCommand)
    {
        throw "Yarn classic is required to build the Theia shell, but 'yarn' was not found on PATH."
    }

    if (Test-YarnInstallRequired)
    {
        Write-Host 'Restoring Theia workspace dependencies...'
        Invoke-ExternalCommand -FilePath $yarnCommand.Source -Arguments @('install', '--ignore-engines')

        $installStampPath = Join-Path $WorkspaceRoot 'node_modules\.install-stamp'
        Set-Content -Path $installStampPath -Value ([DateTimeOffset]::UtcNow.ToString('O'))
    }

    if (Test-BrowserBuildRequired)
    {
        Write-Host 'Building Theia browser shell...'
        Invoke-ExternalCommand -FilePath $yarnCommand.Source -Arguments @('build:browser')
        Write-StampFile
    }
    else
    {
        Write-Host 'Theia browser shell is up to date.'
    }
}
finally
{
    # Restore the caller's working directory even when the build fails.
    Pop-Location
}
