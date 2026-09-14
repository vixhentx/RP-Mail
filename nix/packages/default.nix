{ self, ... }:
{
  perSystem = { pkgs, ... }:
  {
    packages = pkgs.lib.optionalAttrs pkgs.stdenvNoCC.hostPlatform.isLinux (
    let
      rpmail-console = pkgs.callPackage ./console/package.nix { src = self; };
      rpmail-ui = pkgs.callPackage ./ui/package.nix { src = self; };
    in {
      inherit rpmail-ui rpmail-console;
    } // {
      default = rpmail-console;
    });
  };
}
