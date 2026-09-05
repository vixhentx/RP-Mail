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
  dotnet-sdk = dotnetCorePackages.sdk_10_0;
  dotnet-runtime = dotnetCorePackages.runtime_10_0;
  nugetDeps = ./deps.json;

}
