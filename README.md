# Starion PowerFX

## Environment

- .NET SDK. [Download Link](https://dotnet.microsoft.com/en-us/download)
- .NET 8.0
- .NET 7.0

## Command Line

To build this project, run the following commands:

### .Net 8
Edit file `PowerFxWasm.csproj`:

```
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PowerFxVersion>1.2.0</PowerFxVersion>
    <WebAssemblyVerion>8.0.0</WebAssemblyVerion>
    <UICulture>en-GB</UICulture>
  </PropertyGroup>
```

Then run command line:

```sh
sh publish
cp -R bin/Release/net8.0/publish/wwwroot/ dist/8
```

### .Net 7
Edit file `PowerFxWasm.csproj`:

```
<PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PowerFxVersion>1.2.0</PowerFxVersion>
    <WebAssemblyVerion>7.0.0</WebAssemblyVerion>
    <UICulture>en-GB</UICulture>
  </PropertyGroup>
```

Then run command line:

```sh
sh publish
cp -R bin/Debug/net7.0/publish/wwwroot/ dist/7
```
