{ ... }:
{
  perSystem = { pkgs, ...}:
  {
    devShells.default = pkgs.mkShell {
      buildInputs = [
        pkgs.dotnet-sdk_10
        pkgs.chromium
      ];
    };
  };
}