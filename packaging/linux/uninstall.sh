#!/bin/sh
# Removes the program, its command and its menu entry. Your data is kept.
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
. "$HERE/app.conf"
PREFIX="${PREFIX:-$HOME/.local}"
APPDIR="$PREFIX/opt/$APP_ID"

rm -rf "$APPDIR"
# the command, only if it is this app's
[ -L "$PREFIX/bin/$APP_ID" ] && case "$(readlink "$PREFIX/bin/$APP_ID")" in
    "$APPDIR/"*) rm -f "$PREFIX/bin/$APP_ID" ;;
esac
rm -f "$PREFIX/share/applications/$APP_ID.desktop" \
      "$PREFIX/share/icons/hicolor/${ICON_SIZE}x${ICON_SIZE}/apps/$APP_ID.png"
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$PREFIX/share/applications" 2>/dev/null || true

echo "$APP_TITLE removed."
[ -n "$DATA_NOTE" ] && echo "Your data is still in $DATA_NOTE (delete it yourself to wipe it)."
true
