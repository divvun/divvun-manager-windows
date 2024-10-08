# Windows Divvun Manager and OneClick Installer

[![Build Status](https://divvun-tc.giellalt.org/api/github/v1/repository/divvun/divvun-installer-windows/main/badge.svg)](https://divvun-tc.giellalt.org/api/github/v1/repository/divvun/divvun-installer-windows/main/latest)

## Download

- [Stable](https://pahkat.uit.no/divvun-installer/download/divvun-installer?platform=windows)
- [Nightly build](https://pahkat.uit.no/divvun-installer/download/divvun-installer?channel=nightly&platform=windows)
- [One-Click (Stable)](https://pahkat.uit.no/divvun-installer/download/divvun-installer-oneclick?platform=windows)

(One-Click is [currently](https://github.com/divvun/pahkat.uit.no-index/blob/main/oneclick.json#L2) only buildable off of a Stable release)

## Building

#### SDK Versions

- .NET Standard Core 2.1
- .NET Framework 4.6 or higher

**Note:** Tested with Visual Studio 2019 with .NET 5.0

First, [pahkat](https://github.com/divvun/pahkat) needs to be built to your local directory.

`git clone https://github.com/divvun/pahkat.git`

**Important:** Divvun Manager does not talk to the language index itself. It only communicate locally running pahkat service. Pahkat is responsible to install and update languages. So, pahkat service should be up and running in your Windows's Services.

(if you want to make your life easier, grab the OneClick Installer above which will get you both `kbdi` config and Pahkat service installed)

Open `pahkat-client-core` directory in a terminal and source env for the platform `$Env:CARGO_FEATURE_WINDOWS="true"` before `cargo build`. Make sure the build to succeed.

Second, you should clone the divvun manager repo in the same pahkat folder.

`git clone https://github.com/divvun/divvun-manager-windows.git`

Be sure `divvun-manager-windows` and `pahkat-client-core` share the same root directory.

Open `Divvun.Installer.sln` in Visual Studio 2019 and build only `Pahkat.Sdk` project from the solution explorer to generate `Pahkat.Sdk.dll` file before building the whole solution.

Finally, **Build > Build Solution (Ctrl-Shift-B)**

When you try to run `Divvun.Installer`, it's not going to work because some pseudolocales will need to be added to your registry. The simplest way to do this is to install [kbdi](https://github.com/divvun/kbdi) outside of pahkat directory.

`git clone https://github.com/divvun/kbdi.git` then `cargo build --release --target i686-pc-windows-msvc --bin kbdi`

After the build is succeeded. Come to `.\target\i686-pc-windows-msvc\release` and run `kbdi.exe language_enable nn-Runr` command.

If everything goes well, you can run `Divvun.Installer` and it should work just fine.

#### Troubleshot

You might end up with enabling old .Net SDKs in Windows Feature Panel.

### Releases

Releases use Pahkat to provide installers to users via special urls. The Pahkat Index, which represents packages available for download, can be viewed [here](https://github.com/divvun/pahkat.uit.no-index). Except for Stable releases, which must be done manually, new installers are added to the index automatically on successful build.


```sh
nano version.json #change the version
git commit version -m "v2.5.0 ..." 
git tag v2.5.0
git push
git push origin v2.5.0
```

This will trigger the nightly build jobs first. And then trigger the job to build you 2.5.0.

Nightlies are auto updated in the repository. New versions are not. Well.. They kinda are. But they're added to the beta channel. So you need to copy the release of the beta version (which still needs to be there it seems?) and just remove the beta channel making it a main release.

 So you need to go to git@github.com:divvun/pahkat.uit.no-index.git. Edit divvun-installer/packages/divvun-installer/index.toml and divvun-installer/packages/divvun-installer-oneclick/index.toml

See https://github.com/divvun/pahkat.uit.no-index/commit/6bdc7f1a98b9242ca3ae2661d28bf2aad8c146ce for reference.

The repo will need to be pushed onto the pahkat repository. This gets done any time any other project is updated. So you can go ahead and just trigger any job that would generate an artifact. 


## How to Add/Remove Languages and Regions

Divvun Manager is rendering this landing page https://pahkat.uit.no/main/landing/index.html to display all available languages and regions.

In order to add a new language or a region, [region.json](https://github.com/divvun/pahkat-web-ui/blob/master/public/regions.json) should be updated in the [pahkat-web-ui](https://github.com/divvun/pahkat-web-ui) repository and genereted files needs to be copied into `pahkat.uit.no-index/main/landing` directory in the [pahkat.uit.no-index](https://github.com/divvun/pahkat.uit.no-index) repo.


### Old guide

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
