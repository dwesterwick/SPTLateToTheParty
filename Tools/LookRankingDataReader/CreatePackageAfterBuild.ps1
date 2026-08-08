param (
    [string]$modName,
    [string]$modVersion,
    [string]$configuration
)

if ($configuration -eq "DEBUG")
{
    Write-Host ("Files will not be packaged for debug builds")
    exit 0
}

# Set path to 7-Zip executable
$pathTo7z = "C:\Program Files\7-Zip\7z.exe"

Write-Host ('Packaging {0}_LootRankingDataReader v{1}...' -f $modName, $modVersion)

Set-Location $PSScriptRoot

# Create the build folder
$packageFolderAbsolute = Join-Path $PSScriptRoot "..\..\Dist_LootRankingDataReader"

try
{
    New-Item -ItemType Directory -Path $packageFolderAbsolute -Force -errorAction stop | Out-Null
    Remove-Item -Path ('{0}\*' -f $packageFolderAbsolute) -Recurse -Force -errorAction stop | Out-Null
}
catch
{
    Write-Error ('Could not create Dist directory or empty its contents: {0}' -f $_.Exception.Message)
    exit 1
}


# Create server and client folders
$serverFolderAbsolute = Join-Path $packageFolderAbsolute ('SPT\user\mods\{0}\LootRankingDataReader' -f $modName)

try
{
    New-Item -ItemType Directory -Path $serverFolderAbsolute -Force -errorAction stop | Out-Null
}
catch
{
    Write-Error ('Could not create Dist directory subfolders: {0}' -f $_.Exception.Message)
    exit 1
}

# Copy all files

Write-Host ('Packaging {0}_LootRankingDataReader v{1}...copying files...' -f $modName, $modVersion)

$toolLibraryAbsolute = Join-Path $PSScriptRoot ('bin\Release\net9.0-windows7.0\LootRankingDataReader.dll' -f $modName)
$toolProgramAbsolute = Join-Path $PSScriptRoot ('bin\Release\net9.0-windows7.0\LootRankingDataReader.exe' -f $modName)
$toolRuntimeSettingsAbsolute = Join-Path $PSScriptRoot ('bin\Release\net9.0-windows7.0\LootRankingDataReader.runtimeconfig.json' -f $modName)
$newtonsoftLibraryAbsolute = Join-Path $PSScriptRoot ('bin\Release\net9.0-windows7.0\Newtonsoft.Json.dll' -f $modName)

try
{
    Copy-Item -Path $toolLibraryAbsolute -Destination $serverFolderAbsolute -errorAction stop | Out-Null
    Copy-Item -Path $toolProgramAbsolute -Destination $serverFolderAbsolute -errorAction stop | Out-Null
    Copy-Item -Path $toolRuntimeSettingsAbsolute -Destination $serverFolderAbsolute -errorAction stop | Out-Null
    Copy-Item -Path $newtonsoftLibraryAbsolute -Destination $serverFolderAbsolute -errorAction stop | Out-Null
}
catch
{
    Write-Error ('Could not copy files to Dist directory: {0}' -f $_.Exception.Message)
    exit 1
}

# Create 7zip archive

Write-Host ('Packaging {0}_LootRankingDataReader v{1}...creating archive...' -f $modName, $modVersion)

$archiveName = Join-Path $packageFolderAbsolute ('{0}_LootRankingDataReader-{1}.7z' -f $modName, $modVersion)
$sourceFiles = Join-Path $packageFolderAbsolute '*'
$arguments = "a", "-t7z", $archiveName, $sourceFiles

try
{
    & $pathTo7z $arguments | Out-Null
}
catch
{
    Write-Error ('Could not create 7-Zip archive {0}: {1}' -f $archiveName, $_.Exception.Message)
    exit 1
}

Write-Host ('Packaging {0}_LootRankingDataReader v{1}...done.' -f $modName, $modVersion)