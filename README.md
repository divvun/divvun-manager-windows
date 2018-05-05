# pahkat-client-windows

## Building

Open in Visual Studio 2017, run build in Release.

In order for all supported locales to generate properly, some pseudolocales
will need to be added to your registry. The simplest way to do this
is to install [kbdi](https://github.com/divvun/kbdi), and run
`kbdi.exe language_enable` on the following tags:

- `nn-Runr`

You must also put a copy of 
[pahkat-client-core](https://github.com/divvun/pahkat-client-core) compiled
with the `ipc` feature into the `Pahkat/` directory prior to building.
