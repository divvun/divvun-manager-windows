@echo off
setlocal
setlocal enableDelayedExpansion
set "PFX_PASSWORD="
set "PFX="
set "BIN=DivvunInstaller.exe"
  call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars32.bat"
  rustup default nightly-i686
  pushd ..\pahkat-client-core
  cargo build --release --target i686-pc-windows-msvc || exit /b !ERRORLEVEL!
  popd

  copy ..\pahkat-client-core\target\i686-pc-windows-msvc\release\pahkat_client.dll .\Pahkat.Sdk
  MSBuild.exe Pahkat.sln /p:Configuration=Release /p:Platform=x86 || exit /b !ERRORLEVEL!
  
  signtool sign /t http://timestamp.verisign.com/scripts/timstamp.dll /f %PFX% /p %PFX_PASSWORD% /d "Divvun Updater" .\Pahkat\bin\x86\Release\updater.exe || exit /b !ERRORLEVEL!
  signtool sign /t http://timestamp.verisign.com/scripts/timstamp.dll /f %PFX% /p %PFX_PASSWORD% /d "Divvun Installer" .\Pahkat\bin\x86\Release\Pahkat.Sdk.dll  || exit /b !ERRORLEVEL!
  signtool sign /t http://timestamp.verisign.com/scripts/timstamp.dll /f %PFX% /p %PFX_PASSWORD% /d "Divvun Installer" .\Pahkat\bin\x86\Release\pahkat_client.dll  || exit /b !ERRORLEVEL!
  signtool sign /t http://timestamp.verisign.com/scripts/timstamp.dll /f %PFX% /p %PFX_PASSWORD% /d "Divvun Installer" .\Pahkat\bin\x86\Release\%BIN%  || exit /b !ERRORLEVEL!
  "C:\Program Files (x86)\Inno Setup 5\ISCC.exe" /Qp /O.\output /S"signtool=signtool.exe sign /t http://timestamp.verisign.com/scripts/timstamp.dll /f %PFX% /p %PFX_PASSWORD% $f" setup.iss
endlocal
