{ self, ... }:
{
  perSystem = { pkgs, ... }:
  {
    packages = 
    let
      rpmail-console = pkgs.callPackage ./console/package.nix { src = self; };
      rpmail-ui = pkgs.callPackage ./ui/package.nix { src = self; };
    in {
      inherit rpmail-ui rpmail-console;
      default = rpmail-console;
    };
  };
}