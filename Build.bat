@echo off
SET publishFolder=%~dp0Build
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=true -p:DebugType=none -o "%publishFolder%"
xcopy "VirtuosoMIDICompanion.ini" "%publishFolder%" /Y
echo Published project to /Build
pause