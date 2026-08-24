{ self, ... }:
{
  perSystem = { pkgs, ... }:
  {
    packages = 
    let
      rpmail-console = pkgs.callPackage ./packages/rpmail-console/default.nix { src = self; };
      rpmail-ui = pkgs.callPackage ./packages/rpmail-ui/default.nix { src = self; };
    in {
      inherit rpmail-ui rpmail-console;
      default = rpmail-console;
    };
  };
}