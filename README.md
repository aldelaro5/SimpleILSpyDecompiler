# SimpleILSpyDecompiler
```
Description:
  Decompile an assembly to a csproj project using ILSpy with custom settings supplied in JSON

Usage:
  SimpleILSpyDecompiler decompile <assembly> <output directory> <settings> [options]
  SimpleILSpyDecompiler generate-settings [options]

Commands:
  d, decompile          Decompile an assembly with a JSON settings file
  g, generate-settings  Generate a JSON string containing all the default settings of ILSpy

Arguments:
  <assembly>          The assembly file to decompile to a csproj project
  <output directory>  The directory to output the csproj and project files. It will be created if it doesn't exist
  <settings>          The JSON settings file. Any setting not specified will be their default value

Options:
  -v, --version   Show version information
  -?, -h, --help  Show help and usage information
```
