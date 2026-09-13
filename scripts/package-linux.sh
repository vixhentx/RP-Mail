#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

version="${VERSION:-$(git describe --tags --always --dirty 2>/dev/null | sed 's/^v//')}"
version="${version#v}"
rid="${RID:-linux-x64}"
nfpm_arch="${NFPM_ARCH:-amd64}"
package_app() {
local name="$1"
local project="$2"
local executable="$3"
local description="$4"
local deb_depends="${5:-[]}"
local rpm_depends="${6:-[]}"
local apk_depends="${7:-[]}"
local arch_depends="${8:-[]}"
local ipk_depends="${9:-[]}"
local publish_dir="artifacts/publish/${name}-${rid}"
local pkg_root="artifacts/pkgroot/${name}-${rid}"
local package_dir="artifacts/packages"

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

export VERSION="$version"
export NFPM_ARCH="$nfpm_arch"
export PKG_ROOT="$pkg_root"

nfpm_config="$pkg_root/${name}.nfpm.yaml"
sed \
  -e "s|@VERSION@|$VERSION|g" \
  -e "s|@NFPM_ARCH@|$NFPM_ARCH|g" \
  -e "s|@PKG_ROOT@|$PKG_ROOT|g" \
  -e "s|@NAME@|$name|g" \
  -e "s|@EXECUTABLE@|$executable|g" \
  -e "s|@DESCRIPTION@|$description|g" \
  -e "s|@DEPENDS@|[]|g" \
  -e "s|@DEB_DEPENDS@|$deb_depends|g" \
  -e "s|@RPM_DEPENDS@|$rpm_depends|g" \
  -e "s|@APK_DEPENDS@|$apk_depends|g" \
  -e "s|@ARCH_DEPENDS@|$arch_depends|g" \
  -e "s|@IPK_DEPENDS@|$ipk_depends|g" \
  build/nfpm/rpmail-console.yaml > "$nfpm_config"

for packager in deb rpm apk archlinux ipk; do
  nfpm package \
    --config "$nfpm_config" \
    --packager "$packager" \
    --target "$package_dir"
done

tar -C "$publish_dir" -czf "$package_dir/${name}-${version}-${rid}.tar.gz" .
}

package_app "rpmail-console" "RPMailConsole/RPMailConsole.csproj" "RPMailConsole" "Console mail sender with Scriban templates and Typst PDF rendering."
package_app "rpmail-ui" "RPMailUI/RPMailUI.csproj" "RPMailUI" "Avalonia desktop UI for RobotPilots Mail Sender." \
  '["libfontconfig1", "libfreetype6", "libx11-6", "libxcursor1", "libxi6", "libxrandr2", "libxrender1", "libice6", "libsm6", "libxkbcommon0"]' \
  '["fontconfig", "freetype", "libX11", "libXcursor", "libXi", "libXrandr", "libXrender", "libICE", "libSM", "libxkbcommon"]' \
  '["fontconfig", "freetype", "libx11", "libxcursor", "libxi", "libxrandr", "libxrender", "libice", "libsm", "libxkbcommon"]' \
  '["fontconfig", "freetype2", "libx11", "libxcursor", "libxi", "libxrandr", "libxrender", "libice", "libsm", "libxkbcommon"]' \
  '["fontconfig", "freetype", "libx11", "libxcursor", "libxi", "libxrandr", "libxrender", "libice", "libsm", "libxkbcommon"]'
