#!/bin/sh
# Per-user install: no admin, no sudo. Run it from the unpacked download folder.
# Copies the app to ~/.local/opt/<APP_ID>, adds the command and the menu entry.
# Re-running it (or a newer download's install.sh) upgrades in place. Your data is never touched.
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
. "$HERE/app.conf"
PREFIX="${PREFIX:-$HOME/.local}"
APPDIR="$PREFIX/opt/$APP_ID"
DESKTOP="$PREFIX/share/applications/$APP_ID.desktop"
ICONFILE="$PREFIX/share/icons/hicolor/${ICON_SIZE}x${ICON_SIZE}/apps/$APP_ID.png"

if [ ! -x "$HERE/app/$EXE" ]; then
    echo "Run this from the unpacked download folder (build it with packaging/linux/build_linux.sh)." >&2
    exit 1
fi

# replace any previous version (an upgrade); the data folder is not inside APPDIR
rm -rf "$APPDIR"
mkdir -p "$APPDIR" "$PREFIX/bin" "$(dirname "$DESKTOP")" "$(dirname "$ICONFILE")"
cp -R "$HERE/app/." "$APPDIR/"
cp "$HERE/icon.png" "$ICONFILE"
ln -sf "$APPDIR/$EXE" "$PREFIX/bin/$APP_ID"
cat > "$DESKTOP" <<DESK
[Desktop Entry]
Type=Application
Name=$APP_TITLE
Comment=$APP_COMMENT
Exec=$APPDIR/$EXE
Icon=$ICONFILE
Terminal=false
Categories=$CATEGORIES
StartupNotify=true
DESK
chmod +x "$DESKTOP"

# remove the old hand-made install: only its program folder (checked to hold the program) and its menu entry
if [ -n "$LEGACY_DIR" ] && [ -x "$PREFIX/share/$LEGACY_DIR/$EXE" ]; then
    rm -rf "$PREFIX/share/$LEGACY_DIR"
    echo "Removed the old install in $PREFIX/share/$LEGACY_DIR"
fi
if [ -n "$LEGACY_DESKTOP" ] && [ "$LEGACY_DESKTOP" != "$APP_ID.desktop" ] && [ -f "$PREFIX/share/applications/$LEGACY_DESKTOP" ]; then
    rm -f "$PREFIX/share/applications/$LEGACY_DESKTOP"
fi

command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$PREFIX/share/applications" 2>/dev/null || true
command -v gtk-update-icon-cache >/dev/null 2>&1 && gtk-update-icon-cache -q "$PREFIX/share/icons/hicolor" 2>/dev/null || true
command -v kbuildsycoca6 >/dev/null 2>&1 && kbuildsycoca6 >/dev/null 2>&1 || true

echo "Installed $APP_TITLE to $APPDIR"
echo "Open it from your applications menu, or run: $APP_ID"
