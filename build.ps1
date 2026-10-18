param([switch]$Test)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet publish MinitBrowser.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    Copy-Item -LiteralPath LICENSE,README.md,WEBVIEW2-LICENSE.txt,WEBVIEW2-NOTICE.txt,DOTNET-LICENSE.txt,DOTNET-THIRD-PARTY-NOTICES.txt,WINFORMS-LICENSE.txt -Destination publish
    if ($Test) {
        $testProcess = Start-Process -FilePath (Join-Path $PSScriptRoot 'publish/MinitBrowser.exe') -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -Wait
        Get-Content publish/self-test.txt
        if ($testProcess.ExitCode -ne 0) { throw 'Self-test failed.' }
    }
} finally { Pop-Location }

