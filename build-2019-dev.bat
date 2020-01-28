@echo off
setlocal
setlocal enableDelayedExpansion
set "RUSTC_BOOTSTRAP=1"
set "BIN=DivvunInstaller.exe"
  call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars32.bat"
  rustup default stable-i686
  pushd ..\pahkat-client-core
  cargo build --release --target i686-pc-windows-msvc --features windows,ffi || exit /b !ERRORLEVEL!
  popd

  copy ..\pahkat-client-core\target\i686-pc-windows-msvc\release\pahkat_client.dll .\Pahkat.Sdk
  MSBuild.exe Pahkat.sln /p:Configuration=Release /p:Platform=x86 || exit /b !ERRORLEVEL!
endlocal
