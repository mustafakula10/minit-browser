param([string]$IsccPath = 'ISCC.exe')
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $compiler = Get-Command $IsccPath -ErrorAction SilentlyContinue
    if (-not $compiler) { throw 'Install Inno Setup 6.7+ from https://jrsoftware.org/isdl.php and pass -IsccPath with the full path to ISCC.exe.' }
    & ./build.ps1
    if (-not (Test-Path publish/MinitBrowser.exe)) { throw 'Published executable missing.' }
    & $compiler.Source minit-setup.iss
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
    Get-FileHash dist/Minit-v0.3-Windows-x64-Setup.exe -Algorithm SHA256 |
        ForEach-Object { "$($_.Hash.ToLower())  Minit-v0.3-Windows-x64-Setup.exe" } |
        Set-Content dist/Minit-v0.3-Setup-SHA256.txt
} finally { Pop-Location }
