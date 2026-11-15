#!/bin/sh
# Build the self-contained Linux download: dist/<APP_ID>-<version>-linux-<arch>.tar.gz
# The build machine needs the .NET SDK (Fedora: sudo dnf install dotnet-sdk-8.0, or the version the
# project targets). No admin rights. The people using the app need nothing installed: .NET is included.
set -e
cd "$(dirname "$0")/../.."                    # the repo root
. packaging/linux/app.conf

command -v dotnet >/dev/null 2>&1 || { echo "The dotnet command is not on PATH." >&2; exit 1; }

# one version: <Version> in the .csproj
VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$CSPROJ" | head -1)"
[ -n "$VERSION" ] || { echo "No <Version> in $CSPROJ." >&2; exit 1; }
case "$(uname -m)" in
    x86_64)  RID=linux-x64 ;;
    aarch64) RID=linux-arm64 ;;
    *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
esac
NAME="$APP_ID-$VERSION-linux-$(uname -m)"
OUT="dist/$NAME"

echo "== Building $APP_TITLE $VERSION ($RID)"
rm -rf build/linux "$OUT" "$OUT.tar.gz"
dotnet publish "$CSPROJ" -c Release -r "$RID" --self-contained true \
    -p:DebugType=none -p:DebugSymbols=false -o build/linux/app

[ -x "build/linux/app/$EXE" ] || { echo "Publish failed: build/linux/app/$EXE is missing." >&2; exit 1; }

# assemble the download: the app, a no-admin installer and uninstaller, and their settings
mkdir -p "$OUT"
cp -R build/linux/app "$OUT/app"
cp packaging/linux/install.sh packaging/linux/uninstall.sh packaging/linux/app.conf "$OUT/"
cp "$ICON" "$OUT/icon.png"
chmod +x "$OUT/install.sh" "$OUT/uninstall.sh"
tar -C dist -czf "$OUT.tar.gz" "$NAME"

echo "Built $OUT.tar.gz ($(du -h "$OUT.tar.gz" | cut -f1))"
echo "Install it:  sh $OUT/install.sh"
