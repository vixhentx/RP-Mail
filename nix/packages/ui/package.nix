{
  buildDotnetModule,
  clang,
  dotnetCorePackages,
  lib,
  stdenvNoCC,
  src,
  fontconfig,
  freetype,
  libx11,
  libxcursor,
  libxi,
  libxrandr,
  libxrender,
  libice,
  libsm,
  libxkbcommon,
  zlib
}:
buildDotnetModule {
  pname = "rpmail-ui";
  version = "27.0.1";
  inherit src;
  projectFile = "RPMailUI/RPMailUI.csproj";
  dotnet-sdk = dotnetCorePackages.sdk_10_0;
  dotnet-runtime = dotnetCorePackages.runtime_10_0;
  nugetDeps = ./deps.json;
  selfContainedBuild = true;
  runtimeId = dotnetCorePackages.systemToDotnetRid stdenvNoCC.hostPlatform.system;
  executables = [ "RPMailUI" ];
  nativeBuildInputs = [ clang ];
  buildInputs = [ zlib ];

  installPhase = ''
    runHook preInstall

    installPath="$out/lib/rpmail-ui"
    mkdir -p "$installPath"

    dotnet publish RPMailUI/RPMailUI.csproj \
      --configuration Release \
      --runtime ${dotnetCorePackages.systemToDotnetRid stdenvNoCC.hostPlatform.system} \
      --self-contained true \
      --output "$installPath" \
      --no-restore \
      -p:ContinuousIntegrationBuild=true \
      -p:Deterministic=true \
      -p:InformationalVersion=$version \
      -p:NuGetAudit=false

    install -D -m 0644 build/nfpm/rpmail-ui.desktop \
      "$out/share/applications/rpmail-ui.desktop"
    install -D -m 0644 RPMailUI/Assets/icon.svg \
      "$out/share/icons/hicolor/scalable/apps/rpmail-ui.svg"

    runHook postInstall
  '';

  # Native libraries required by Avalonia at runtime; automatically wrapped into LD_LIBRARY_PATH.
  runtimeDeps = [
    fontconfig
    freetype
    libx11
    libxcursor
    libxi
    libxrandr
    libxrender
    libice
    libsm
    libxkbcommon
  ];

  meta = {
    description = "Avalonia desktop UI for RobotPilots Mail Sender";
    homepage = "https://github.com/vixhentx/RP-Mail";
    mainProgram = "RPMailUI";
    platforms = lib.platforms.linux;
  };

}
