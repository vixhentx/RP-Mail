{
  buildDotnetModule,
  clang,
  dotnetCorePackages,
  lib,
  stdenvNoCC,
  src,
  zlib,
}:
buildDotnetModule {
  pname = "rpmail-console";
  version = "0.9.0";
  inherit src;
  projectFile = "RPMailConsole/RPMailConsole.csproj";
  dotnet-sdk = dotnetCorePackages.sdk_10_0;
  dotnet-runtime = dotnetCorePackages.runtime_10_0;
  nugetDeps = ./deps.json;
  selfContainedBuild = true;
  runtimeId = dotnetCorePackages.systemToDotnetRid stdenvNoCC.hostPlatform.system;
  executables = [ "RPMailConsole" ];
  nativeBuildInputs = [ clang ];
  buildInputs = [ zlib ];

  installPhase = ''
    runHook preInstall

    installPath="$out/lib/rpmail-console"
    mkdir -p "$installPath"

    dotnet publish RPMailConsole/RPMailConsole.csproj \
      --configuration Release \
      --runtime ${dotnetCorePackages.systemToDotnetRid stdenvNoCC.hostPlatform.system} \
      --self-contained true \
      --output "$installPath" \
      --no-restore \
      -p:ContinuousIntegrationBuild=true \
      -p:Deterministic=true \
      -p:InformationalVersion=$version \
      -p:NuGetAudit=false

    runHook postInstall
  '';

  meta = {
    description = "Console mail sender with Scriban templates and Typst PDF rendering";
    homepage = "https://github.com/vixhentx/RP-Mail";
    mainProgram = "RPMailConsole";
    platforms = lib.platforms.linux;
  };

}
