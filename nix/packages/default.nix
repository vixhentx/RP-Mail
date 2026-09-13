{ self, ... }:
{
  perSystem = { pkgs, ... }:
  {
    packages = pkgs.lib.optionalAttrs pkgs.stdenvNoCC.hostPlatform.isLinux (
    let
      rpmail-console = pkgs.callPackage ./console/package.nix { src = self; };
      rpmail-ui = pkgs.callPackage ./ui/package.nix { src = self; };
      rpmail-console-bundles =
        pkgs.callPackage ./bundles.nix { inherit rpmail-console rpmail-ui; };
    in {
      inherit rpmail-ui rpmail-console;
      rpmail-console-deb = rpmail-console-bundles.console.deb;
      rpmail-console-rpm = rpmail-console-bundles.console.rpm;
      rpmail-console-apk = rpmail-console-bundles.console.apk;
      rpmail-console-archlinux = rpmail-console-bundles.console.archlinux;
      rpmail-console-ipk = rpmail-console-bundles.console.ipk;
      rpmail-ui-deb = rpmail-console-bundles.ui.deb;
      rpmail-ui-rpm = rpmail-console-bundles.ui.rpm;
      rpmail-ui-apk = rpmail-console-bundles.ui.apk;
      rpmail-ui-archlinux = rpmail-console-bundles.ui.archlinux;
      rpmail-ui-ipk = rpmail-console-bundles.ui.ipk;
    } // {
      default = rpmail-console;
    });
  };
}
