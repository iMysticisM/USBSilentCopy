@echo off
chcp 65001 > nul
echo === Build USBSilentCopy ===
dotnet test tests/USBSilentCopy.Tests.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo Error during tests!
    exit /b %ERRORLEVEL%
)

dotnet publish src/USBSilentCopy.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
copy /y config\config.json publish\config.json
echo === Build Complete! Output in publish\ folder ===
