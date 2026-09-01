{
  buildDotnetModule,
  dotnetCorePackages,
  src
}:
buildDotnetModule {
  pname = "RPMailUI";
  version = "0.1.0";
  inherit src;
  projectFile = "RPMailUI/RPMailUI.csproj";
  dotnet-sdk = dotnetCorePackages.sdk_10_0;
  dotnet-runtime = dotnetCorePackages.runtime_10_0;
  nugetDeps = ./nuget-deps.json;
}
