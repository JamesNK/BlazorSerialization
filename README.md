# Blazor serialization

This repo contains a Blazor WebAssembly app that compares the time to load data from JSON and gRPC-Web endpoints. Recorded time includes getting data from the server and deserializing it in WebAssembly.

Requires .NET 5 RC2 daily SDK or later. Available [here](https://github.com/dotnet/installer/blob/master/README.md#installers-and-binaries).

Usage:
1. Run server project
2. Open browser at `https://localhost:5001/` if not already opened.
3. Compare loading data on the **Fetch data** page
