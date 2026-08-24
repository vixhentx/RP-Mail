{
  buildDotnetModule,
  dotnetCorePackages,
  src
}:
buildDotnetModule {
  pname = "RPMailConsole";
  version = "0.1.0";
  inherit src;
  projectFile = "RPMailConsole/RPMailConsole.csproj";
  dotnet-sdk = dotnetCorePackages.sdk_9_0;
  dotnet-runtime = dotnetCorePackages.runtime_9_0;
  nugetDeps = ./nuget-deps.json;
}
