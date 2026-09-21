#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

version="${VERSION:-$(git describe --tags --always --dirty 2>/dev/null | sed 's/^v//')}"
version="${version#v}"
rid="${RID:-linux-x64}"
nfpm_arch="${NFPM_ARCH:-amd64}"
package_dir="artifacts/packages"

package_app() {
  local name="$1"
  local project="$2"
  local executable="$3"
  local description="$4"
  local deb_depends="$5"
  local rpm_depends="$6"
  local apk_depends="$7"
  local arch_depends="$8"
  local ipk_depends="$9"
  local publish_dir="artifacts/publish/${name}-${rid}"
  local pkg_root="artifacts/pkgroot/${name}-${rid}"

  rm -rf "$publish_dir" "$pkg_root"
  mkdir -p "$pkg_root/usr/lib/${name}" "$pkg_root/usr/bin" "$package_dir"

  dotnet publish "$project" \
    --configuration Release \
    --runtime "$rid" \
    --self-contained true \
    --output "$publish_dir" \
    -p:PublishAot=true \
    -p:NuGetAudit=false

  cp -a "$publish_dir"/. "$pkg_root/usr/lib/${name}/"
  cat > "$pkg_root/usr/bin/${name}" <<EOF
#!/usr/bin/env sh
exec /usr/lib/${name}/${executable} "\$@"
EOF
  chmod 0755 "$pkg_root/usr/bin/${name}"

  if [[ "$name" == "rpmail-ui" ]]; then
    install -D -m 0644 build/nfpm/rpmail-ui.desktop \
      "$pkg_root/usr/share/applications/rpmail-ui.desktop"
    install -D -m 0644 RPMailUI/Assets/icon.svg \
      "$pkg_root/usr/share/icons/hicolor/scalable/apps/rpmail-ui.svg"
  fi

  export VERSION="$version"
  export NFPM_ARCH="$nfpm_arch"
  export PKG_ROOT="$pkg_root"
  export NAME="$name"
  export DESCRIPTION="$description"
  export DEB_DEPENDS="$deb_depends"
  export RPM_DEPENDS="$rpm_depends"
  export APK_DEPENDS="$apk_depends"
  export ARCH_DEPENDS="$arch_depends"
  export IPK_DEPENDS="$ipk_depends"

  local nfpm_config="$pkg_root/${name}.nfpm.yaml"
  sed \
    -e "s#@VERSION@#$VERSION#g" \
    -e "s#@NFPM_ARCH@#$NFPM_ARCH#g" \
    -e "s#@PKG_ROOT@#$PKG_ROOT#g" \
    -e "s#@NAME@#$NAME#g" \
    -e "s#@DESCRIPTION@#$DESCRIPTION#g" \
    -e "s#@DEB_DEPENDS@#$DEB_DEPENDS#g" \
    -e "s#@RPM_DEPENDS@#$RPM_DEPENDS#g" \
    -e "s#@APK_DEPENDS@#$APK_DEPENDS#g" \
    -e "s#@ARCH_DEPENDS@#$ARCH_DEPENDS#g" \
    -e "s#@IPK_DEPENDS@#$IPK_DEPENDS#g" \
    build/nfpm/rpmail-linux.yaml > "$nfpm_config"

  if [[ "$name" == "rpmail-ui" ]]; then
    local nfpm_config_with_ui="${nfpm_config}.tmp"
    awk -v root="$PKG_ROOT" '
      /^deb:/ {
        print "  - src: " root "/usr/share/applications/rpmail-ui.desktop"
        print "    dst: /usr/share/applications/rpmail-ui.desktop"
        print "  - src: " root "/usr/share/icons/hicolor/scalable/apps/rpmail-ui.svg"
        print "    dst: /usr/share/icons/hicolor/scalable/apps/rpmail-ui.svg"
      }
      { print }
    ' "$nfpm_config" > "$nfpm_config_with_ui"
    mv "$nfpm_config_with_ui" "$nfpm_config"
  fi

  for packager in deb rpm apk archlinux ipk; do
    local extension="$packager"
    if [[ "$packager" == archlinux ]]; then
      extension="pkg.tar.zst"
    fi
    nfpm package \
      --config "$nfpm_config" \
      --packager "$packager" \
      --target "$package_dir/${name}-${version}-${nfpm_arch}.${extension}"
  done
}

package_app "rpmail-console" "RPMailConsole/RPMailConsole.csproj" "RPMailConsole" \
  "Console mail sender with Scriban templates and Typst PDF rendering." \
  '["libicu74 | libicu76 | libicu72 | libicu70 | libicu66", "libgcc-s1"]' \
  '["libicu", "libgcc"]' \
  '["icu-libs", "libgcc"]' \
  '["icu", "gcc-libs"]' \
  '["libicu", "libgcc"]'

package_app "rpmail-ui" "RPMailUI/RPMailUI.csproj" "RPMailUI" \
  "Avalonia desktop UI for RobotPilots Mail Sender." \
  '["libicu74 | libicu76 | libicu72 | libicu70 | libicu66", "libgcc-s1", "libfontconfig1", "libfreetype6", "libx11-6", "libxcursor1", "libxi6", "libxrandr2", "libxrender1", "libice6", "libsm6", "libxkbcommon0"]' \
  '["libicu", "libgcc", "fontconfig", "freetype", "libX11", "libXcursor", "libXi", "libXrandr", "libXrender", "libICE", "libSM", "libxkbcommon"]' \
  '["icu-libs", "libgcc", "fontconfig", "freetype", "libx11", "libxcursor", "libxi", "libxrandr", "libxrender", "libice", "libsm", "libxkbcommon"]' \
  '["icu", "gcc-libs", "fontconfig", "freetype2", "libx11", "libxcursor", "libxi", "libxrandr", "libxrender", "libice", "libsm", "libxkbcommon"]' \
  '["libicu", "libgcc", "fontconfig", "freetype", "libx11", "libxcursor", "libxi", "libxrandr", "libxrender", "libice", "libsm", "libxkbcommon"]'
