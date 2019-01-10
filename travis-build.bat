@echo off
setlocal
setlocal enableDelayedExpansion
set "PFX_PASSWORD=1234"
set "PFX=C:\Users\Martin\source\repos\pahkat-client-windows\tmp.pfxxx"
set "BIN=DivvunInstaller.exe"
  set PATH=%PATH%;c:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin;c:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool
  call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvars32.bat"
  rem nuget restore || exit /b !ERRORLEVEL!
  MSBuild.exe Pahkat.sln /p:Configuration=Release /p:Platform=x86 || exit /b !ERRORLEVEL!
  signtool sign /debug /t http://timestamp.verisign.com/scripts/timstamp.dll /f %PFX% /p %PFX_PASSWORD% /d "Divvun Updater" .\Pahkat\bin\x86\Release\updater.exe || exit /b !ERRORLEVEL!
  signtool sign /debug /t http://timestamp.verisign.com/scripts/timstamp.dll /f %PFX% /p %PFX_PASSWORD% /d "Divvun Installer" .\Pahkat\bin\x86\Release\Pahkat.Sdk.dll || exit /b !ERRORLEVEL!
  signtool sign /debug /t http://timestamp.verisign.com/scripts/timstamp.dll /f %PFX% /p %PFX_PASSWORD% /d "Divvun Installer" .\Pahkat\bin\x86\Release\pahkat_client.dll || exit /b !ERRORLEVEL!
  signtool sign /debug /t http://timestamp.verisign.com/scripts/timstamp.dll /f %PFX% /p %PFX_PASSWORD% /d "Divvun Installer" .\Pahkat\bin\x86\Release\%BIN% || exit /b !ERRORLEVEL!
  "C:\Program Files (x86)\Inno Setup 5\ISCC.exe" /Qp /O.\output /S"signtool=signtool.exe sign /t http://timestamp.verisign.com/scripts/timstamp.dll /f %PFX% /p %PFX_PASSWORD% $f" setup.iss
endlocal
