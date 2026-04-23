#!/usr/bin/env bash
# Download a Fairy-Stockfish "largeboard" build (required for >8x8 boards)
# into ./engine/.  Detects OS/arch.  If no prebuilt binary exists for your
# platform (notably macOS arm64), falls back to instructions for building
# from source.

set -euo pipefail

cd "$(dirname "$0")/.."
mkdir -p engine

OS="$(uname -s)"
ARCH="$(uname -m)"
TAG="${FAIRY_TAG:-fairy_sf_14_0_1_xq}"   # last release with macOS binaries; override via env
RELEASE_BASE="https://github.com/ianfab/Fairy-Stockfish/releases/download/${TAG}"

case "$OS:$ARCH" in
  Linux:x86_64)
    ASSET="fairy-stockfish-largeboard_x86-64-bmi2"
    ;;
  Darwin:x86_64)
    ASSET="fairy-stockfish-largeboard_x86-64-modern-mac"
    ;;
  Darwin:arm64)
    cat <<'EOF' >&2
[!] Fairy-Stockfish does not publish a native macOS arm64 binary.
    Two options:

    1) Build from source (recommended, ~2 min):
         git clone https://github.com/ianfab/Fairy-Stockfish.git /tmp/fsf
         cd /tmp/fsf/src && make -j build ARCH=apple-silicon largeboards=yes all=yes
         cp stockfish PATH/TO/yes-hybrid/engine/fairy-stockfish

    2) Use the x86_64 build under Rosetta:
         FAIRY_TAG=fairy_sf_14_0_1_xq ARCH_OVERRIDE=x86_64 ./scripts/download-engine.sh
         (then run the binary; macOS will prompt to install Rosetta)
EOF
    if [[ "${ARCH_OVERRIDE:-}" == "x86_64" ]]; then
      ASSET="fairy-stockfish-largeboard_x86-64-modern-mac"
    else
      exit 1
    fi
    ;;
  MINGW*:*|MSYS*:*|CYGWIN*:*)
    ASSET="fairy-stockfish-largeboard_x86-64-bmi2.exe"
    ;;
  *)
    echo "[!] Unsupported platform: $OS $ARCH" >&2
    exit 1
    ;;
esac

URL="${RELEASE_BASE}/${ASSET}"
OUT="engine/fairy-stockfish"
[[ "$ASSET" == *.exe ]] && OUT="${OUT}.exe"

echo "[*] Downloading $URL"
if command -v curl >/dev/null 2>&1; then
  curl -fL --retry 3 -o "$OUT" "$URL"
else
  wget -O "$OUT" "$URL"
fi

chmod +x "$OUT"
echo "[+] Engine installed at $OUT"
echo "[*] Verifying..."
echo "uci" | "$OUT" | head -n 5 || {
  echo "[!] Engine failed to start.  You may need a different binary." >&2
  exit 1
}
echo "[+] OK"
