{
  lib,
  runCommand,
  stdenvNoCC,
  nfpm,
  rpmail-console,
  rpmail-ui,
}:

let
  formats = {
    deb = "deb";
    rpm = "rpm";
    apk = "apk";
    archlinux = "pkg.tar.zst";
    ipk = "ipk";
  };

  nfpmArch =
    if stdenvNoCC.hostPlatform.system == "x86_64-linux" then "amd64"
    else if stdenvNoCC.hostPlatform.system == "aarch64-linux" then "arm64"
    else throw "Unsupported nFPM release architecture: ${stdenvNoCC.hostPlatform.system}";

  mkPackage = { app, executable, pname, version, packager, extension, description, dependencies ? { } }:
    runCommand "${pname}-${version}-${packager}"
      {
        nativeBuildInputs = [ nfpm ];
      }
      ''
        set -euo pipefail

        root="$TMPDIR/root"
        mkdir -p "$root/usr/lib/${pname}" "$root/usr/bin" "$out"
        cp -a ${app}/lib/${executable}/. "$root/usr/lib/${pname}/"
        cat > "$root/usr/bin/${pname}" <<EOF
        #!/usr/bin/env sh
        exec /usr/lib/${pname}/${executable} "\$@"
        EOF
        chmod 0755 "$root/usr/bin/${pname}"

        cat > nfpm.yaml <<EOF
        name: ${pname}
        arch: ${nfpmArch}
        platform: linux
        version: ${version}
        section: mail
        priority: optional
        maintainer: RobotPilot Software R&D Department <w1084349470@outlook.com>
        vendor: RobotPilots
        homepage: https://github.com/vixhentx/RP-Mail
        license: MIT
        description: ${description}
        depends: []
        contents:
          - src: $root/usr/lib/${pname}
            dst: /usr/lib/${pname}
          - src: $root/usr/bin/${pname}
            dst: /usr/bin/${pname}
        deb:
          fields:
            Bugs: https://github.com/vixhentx/RP-Mail/issues
        rpm:
          summary: RobotPilots Mail Sender console application
        overrides:
          deb:
            depends: [ ${lib.concatMapStringsSep ", " (dependency: "\"${dependency}\"") dependencies.deb or [ ]} ]
          rpm:
            depends: [ ${lib.concatMapStringsSep ", " (dependency: "\"${dependency}\"") dependencies.rpm or [ ]} ]
          apk:
            depends: [ ${lib.concatMapStringsSep ", " (dependency: "\"${dependency}\"") dependencies.apk or [ ]} ]
          archlinux:
            depends: [ ${lib.concatMapStringsSep ", " (dependency: "\"${dependency}\"") dependencies.archlinux or [ ]} ]
          ipk:
            depends: [ ${lib.concatMapStringsSep ", " (dependency: "\"${dependency}\"") dependencies.ipk or [ ]} ]
        EOF

        nfpm package --config nfpm.yaml --packager ${packager} --target "$out/${pname}-${version}-${nfpmArch}.${extension}"
      '';
in
{
  console = lib.mapAttrs
    (packager: extension: mkPackage {
      inherit packager extension;
      app = rpmail-console;
      executable = "RPMailConsole";
      pname = "rpmail-console";
      version = rpmail-console.version or "0.9.0";
      description = "Console mail sender with Scriban templates and Typst PDF rendering.";
    }) formats;

  ui = lib.mapAttrs
    (packager: extension: mkPackage {
      inherit packager extension;
      app = rpmail-ui;
      executable = "RPMailUI";
      pname = "rpmail-ui";
      version = rpmail-ui.version or "0.9.0";
      description = "Avalonia desktop UI for RobotPilots Mail Sender.";
      dependencies = {
        deb = [ "libfontconfig1" "libfreetype6" "libx11-6" "libxcursor1" "libxi6" "libxrandr2" "libxrender1" "libice6" "libsm6" "libxkbcommon0" ];
        rpm = [ "fontconfig" "freetype" "libX11" "libXcursor" "libXi" "libXrandr" "libXrender" "libICE" "libSM" "libxkbcommon" ];
        apk = [ "fontconfig" "freetype" "libx11" "libxcursor" "libxi" "libxrandr" "libxrender" "libice" "libsm" "libxkbcommon" ];
        archlinux = [ "fontconfig" "freetype2" "libx11" "libxcursor" "libxi" "libxrandr" "libxrender" "libice" "libsm" "libxkbcommon" ];
        ipk = [ "fontconfig" "freetype" "libx11" "libxcursor" "libxi" "libxrandr" "libxrender" "libice" "libsm" "libxkbcommon" ];
      };
    }) formats;
}
