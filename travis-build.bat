@echo off
setlocal
setlocal enableDelayedExpansion
set "PFX_PASSWORD="
set "PFX="
set "BIN=DivvunInstaller.exe"
  call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvars32.bat"
  MSBuild.exe Pahkat.sln /p:Configuration=Release /p:Platform=x86 || exit /b !ERRORLEVEL!
endlocal
