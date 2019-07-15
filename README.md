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