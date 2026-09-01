{
  buildDotnetModule,
  dotnetCorePackages,
  lib,
  src,
  chromium,
  fontconfig,
  freetype,
  libx11,
  libxcursor,
  libxi,
  libxrandr,
  libxrender,
  libice,
  libsm,
  libxkbcommon
}:
buildDotnetModule {
  pname = "RPMailUI";
  version = "0.1.0";
  inherit src;
  projectFile = "RPMailUI/RPMailUI.csproj";
  dotnet-sdk = dotnetCorePackages.sdk_10_0;
  dotnet-runtime = dotnetCorePackages.runtime_10_0;
  nugetDeps = ./deps.json;

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

  # PuppeteerSharp resolves "chromium" from PATH at runtime.
  makeWrapperArgs = [
    "--prefix" "PATH" ":" (lib.makeBinPath [ chromium ])
  ];
}
