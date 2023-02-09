# pahkat-client-windows

[![Build Status](https://dev.azure.com/divvun/divvun-installer/_apis/build/status/divvun.pahkat-client-windows?branchName=master)](https://dev.azure.com/divvun/divvun-installer/_build/latest?definitionId=5&branchName=master)

## Building

Open in Visual Studio 2017, run build in Release.

In order for all supported locales to generate properly, some pseudolocales
will need to be added to your registry. The simplest way to do this
is to install [kbdi](https://github.com/divvun/kbdi), and run
`kbdi.exe language_enable` on the following tags:

- `nn-Runr`

For a debug build, run `build-2019-dev.bat`.

You will need [pahkat-client-core](https://github.com/divvun/pahkat-client-core) checked out in the shared parent directory where you have cloned this repository for the build to succeed.

## Localisation

To add a new language you need to add `DivvunInstaller/Strings.[your_lang].resx` file with translated strings and add your new language tag in `Divvun.Installer/UI/Settings/SettingsWindow.xaml.cs`.

Localisation of entries: see description in the
[Divvun Manager for macOS README](https://github.com/divvun/divvun-manager-macos#generating-localisations).
