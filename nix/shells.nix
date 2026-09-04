{ ... }:
{
  perSystem = { pkgs, ...}:
  {
    devShells.default = pkgs.mkShell {
      buildInputs = [
        pkgs.dotnet-sdk_10
        pkgs.chromium
      ];
      LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath [
        pkgs.fontconfig
        pkgs.freetype
        pkgs.libx11
        pkgs.libxcursor
        pkgs.libxi
        pkgs.libxrandr
        pkgs.libxrender
        pkgs.libice
        pkgs.libsm
        pkgs.libxkbcommon
      ];
	  DOTNET_ROOT = "${pkgs.dotnet-sdk_10}/share/dotnet";
    };
  };
}