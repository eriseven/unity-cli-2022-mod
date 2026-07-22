#!/usr/bin/env bash
# Unity CLI Installer
# https://unity.com
#
# Usage:
#   curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | bash
#
# Environment variables:
#   UNITY_CLI_HOME    - Installation directory (default: ~/.unity)
#   UNITY_CLI_VERSION - Version to install (default: latest from manifest)
#   UNITY_CLI_CHANNEL - Release channel: alpha, beta, or empty for stable

set -euo pipefail

CDN_BASE="${UNITY_CLI_CDN_BASE:-https://public-cdn.cloud.unity3d.com/hub/prod/cli/}"
INSTALL_DIR="${UNITY_CLI_HOME:-$HOME/.unity}"
BIN_DIR="$INSTALL_DIR/bin"

# Channel selects the version manifest: latest.json, latest-beta.json, latest-alpha.json
CHANNEL=$(printf '%s' "${UNITY_CLI_CHANNEL:-}" | tr '[:upper:]' '[:lower:]')
case "$CHANNEL" in
  "")    MANIFEST_FILE="latest.json" ;;
  alpha) MANIFEST_FILE="latest-alpha.json" ;;
  beta)  MANIFEST_FILE="latest-beta.json" ;;
  *)     echo -e "\n  ${RED}error${NC} Invalid UNITY_CLI_CHANNEL: '$CHANNEL'. Allowed: alpha, beta, or empty for stable.\n" >&2; exit 1 ;;
esac

# Use $HOME-relative path for portability in shell config files
if [[ "$BIN_DIR" == "$HOME/"* ]]; then
  BIN_DIR_REF="\$HOME${BIN_DIR#"$HOME"}"
else
  BIN_DIR_REF="$BIN_DIR"
fi

# ─── Colors ───────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
BOLD='\033[1m'
NC='\033[0m'

info()  { echo -e "  ${BLUE}info${NC}  $1"; }
warn()  { echo -e "  ${YELLOW}warn${NC}  $1" >&2; }
error() { echo -e "\n  ${RED}error${NC} $1\n" >&2; exit 1; }

# ─── Requirements ─────────────────────────────────────────────────────────────
check_requirements() {
  if ! command -v curl &>/dev/null && ! command -v wget &>/dev/null; then
    error "curl or wget is required to install Unity CLI."
  fi
}

# ─── Platform detection ───────────────────────────────────────────────────────
detect_platform() {
  local os arch
  os="$(uname -s)"
  arch="$(uname -m)"

  case "$os" in
    Darwin) os="darwin" ;;
    Linux)  os="linux" ;;
    MINGW*|MSYS*|CYGWIN*)
      error "Windows detected. Use the PowerShell installer instead:\n\n    irm ${CDN_BASE}install.ps1 | iex"
      ;;
    *) error "Unsupported operating system: $os" ;;
  esac

  case "$arch" in
    x86_64|amd64) arch="x64" ;;
    arm64|aarch64) arch="arm64" ;;
    *) error "Unsupported architecture: $arch" ;;
  esac

  echo "${os}-${arch}"
}

# ─── HTTP helpers ─────────────────────────────────────────────────────────────
http_get() {
  local url="$1"
  if command -v curl &>/dev/null; then
    curl -fsSL "$url"
  else
    wget -qO- "$url"
  fi
}

http_download() {
  local url="$1" dest="$2"
  if command -v curl &>/dev/null; then
    curl -fsSL "$url" -o "$dest"
  else
    wget -qO "$dest" "$url"
  fi
}

# ─── Manifest parsing ─────────────────────────────────────────────────────────
# Sets globals: MANIFEST_VERSION, MANIFEST_FILENAME, MANIFEST_SHA256
parse_manifest() {
  local manifest="$1" platform="$2"

  if command -v jq &>/dev/null; then
    MANIFEST_VERSION=$(printf '%s' "$manifest" | jq -r '.version')
    MANIFEST_FILENAME=$(printf '%s' "$manifest" | jq -r ".binaries[\"${platform}\"].filename // empty")
    MANIFEST_SHA256=$(printf '%s'  "$manifest"  | jq -r ".binaries[\"${platform}\"].sha256 // empty")
  else
    # Compact JSON for pattern matching (no jq dependency)
    local compact
    compact=$(printf '%s' "$manifest" \
      | tr -d '\n\r' \
      | sed 's/[[:space:]]*:[[:space:]]*/:/g' \
      | sed 's/[[:space:]]*{[[:space:]]*/\{/g' \
      | sed 's/[[:space:]]*}[[:space:]]*/\}/g' \
      | sed 's/[[:space:]]*,[[:space:]]*/,/g')

    MANIFEST_VERSION=$(printf '%s' "$compact" | grep -o '"version":"[^"]*"' | cut -d'"' -f4)
    local section
    section=$(printf '%s' "$compact" | grep -o "\"${platform}\":{[^}]*}" || true)
    MANIFEST_FILENAME=$(printf '%s' "$section" | grep -o '"filename":"[^"]*"' | cut -d'"' -f4 || true)
    MANIFEST_SHA256=$(printf '%s'   "$section" | grep -o '"sha256":"[^"]*"'   | cut -d'"' -f4 || true)
  fi

  [ -n "$MANIFEST_VERSION"  ] || error "Failed to parse version from manifest."
  [ -n "$MANIFEST_FILENAME" ] || error "No binary available for platform: ${platform}."
  [ -n "$MANIFEST_SHA256"   ] || error "No checksum available for platform: ${platform}."
}

# ─── SHA-256 verification ─────────────────────────────────────────────────────
verify_sha256() {
  local file="$1" expected="$2"
  local actual

  if command -v shasum &>/dev/null; then
    actual=$(shasum -a 256 "$file" | awk '{print $1}')
  elif command -v sha256sum &>/dev/null; then
    actual=$(sha256sum "$file" | awk '{print $1}')
  else
    warn "shasum/sha256sum not found — skipping checksum verification."
    return 0
  fi

  if [ "$actual" != "$expected" ]; then
    error "SHA-256 verification failed.\n    Expected: ${expected}\n    Actual:   ${actual}"
  fi
}

# ─── Shell PATH configuration ─────────────────────────────────────────────────
# Returns: 0 = added, 1 = not added (no file / no write), 2 = already present
add_to_path() {
  local shell_config="$1"
  [ -f "$shell_config" ] || return 1

  if ! [ -w "$shell_config" ]; then
    warn "Cannot write to $shell_config (permission denied), skipping."
    return 1
  fi

  if grep -qF "$INSTALL_DIR" "$shell_config" 2>/dev/null; then
    return 2  # already configured
  fi

  {
    echo ""
    echo "# Unity CLI"
    echo ". \"$INSTALL_DIR/env\""
  } >> "$shell_config"
  return 0
}

configure_shell_path() {
  PATH_CONFIGURED="false"
  SHELL_CONFIG_UPDATED=""

  case "${SHELL:-}" in
    */zsh)
      local zsh_dir="${ZDOTDIR:-$HOME}"
      local r=0
      add_to_path "$zsh_dir/.zshrc" || r=$?
      [ "$r" -eq 0 ] && { PATH_CONFIGURED="true";    SHELL_CONFIG_UPDATED="$zsh_dir/.zshrc";  return; }
      [ "$r" -eq 2 ] && { PATH_CONFIGURED="already";                                           return; }
      # Fallback: .zshenv (sourced by all zsh session types including IDEs)
      add_to_path "$zsh_dir/.zshenv" || r=$?
      [ "$r" -eq 0 ] && { PATH_CONFIGURED="true";    SHELL_CONFIG_UPDATED="$zsh_dir/.zshenv"; return; }
      [ "$r" -eq 2 ] && { PATH_CONFIGURED="already";                                           return; }
      ;;
    */bash)
      local r=0
      add_to_path "$HOME/.bashrc" || r=$?
      [ "$r" -eq 0 ] && { PATH_CONFIGURED="true";    SHELL_CONFIG_UPDATED="$HOME/.bashrc";       return; }
      [ "$r" -eq 2 ] && { PATH_CONFIGURED="already";                                              return; }
      add_to_path "$HOME/.bash_profile" || r=$?
      [ "$r" -eq 0 ] && { PATH_CONFIGURED="true";    SHELL_CONFIG_UPDATED="$HOME/.bash_profile"; return; }
      [ "$r" -eq 2 ] && { PATH_CONFIGURED="already";                                              return; }
      ;;
    */fish)
      local fish_dir="${XDG_CONFIG_HOME:-$HOME/.config}/fish/conf.d"
      local fish_config="$fish_dir/unity-cli.fish"
      if [ -f "$fish_config" ] && grep -qF "$INSTALL_DIR" "$fish_config" 2>/dev/null; then
        PATH_CONFIGURED="already"
        return
      fi
      mkdir -p "$fish_dir"
      {
        echo "# Unity CLI"
        echo "source \"$INSTALL_DIR/env.fish\""
      } > "$fish_config"
      PATH_CONFIGURED="true"
      SHELL_CONFIG_UPDATED="$fish_config"
      ;;
  esac
}

# ─── Main ─────────────────────────────────────────────────────────────────────
main() {
  echo ""
  echo -e "${BOLD}Unity CLI Installer${NC}"
  echo ""

  check_requirements

  local platform
  platform=$(detect_platform)

  # Fetch version manifest
  info "Checking for latest version..."
  local manifest
  manifest=$(http_get "${CDN_BASE}${MANIFEST_FILE}") \
    || error "Failed to fetch version manifest. Check your network connection.\n\n    ${CDN_BASE}${MANIFEST_FILE}"

  parse_manifest "$manifest" "$platform"

  # Allow explicit version pin (uses filename from manifest, version in URL path)
  local version="${UNITY_CLI_VERSION:-$MANIFEST_VERSION}"
  [ "$version" != "$MANIFEST_VERSION" ] && info "Using requested version: $version"

  info "Installing version $version for $platform..."

  # Create directories
  mkdir -p "$BIN_DIR"

  # Download binary to a temp file (not `local` — the EXIT trap runs after main() returns)
  tmp_file="$(mktemp)"
  trap 'rm -f "$tmp_file"' EXIT

  local binary_url="${CDN_BASE}${version}/${MANIFEST_FILENAME}"
  info "Downloading binary..."
  http_download "$binary_url" "$tmp_file" \
    || error "Failed to download binary from:\n\n    $binary_url"

  # Verify checksum. When a version is pinned via UNITY_CLI_VERSION, the manifest's checksum
  # belongs to the manifest's own version — not the pinned one — so skip verification.
  if [ -n "${UNITY_CLI_VERSION:-}" ]; then
    warn "Version pinned via UNITY_CLI_VERSION — skipping checksum verification."
  else
    info "Verifying checksum..."
    verify_sha256 "$tmp_file" "$MANIFEST_SHA256"
  fi

  # Install the binary
  cp "$tmp_file" "$BIN_DIR/unity"
  chmod +x "$BIN_DIR/unity"

  # Write POSIX sh env file (sourced from .bashrc / .zshrc)
  cat > "$INSTALL_DIR/env" << ENVEOF
# Unity CLI — added by installer
case ":\${PATH}:" in
  *:"${BIN_DIR}":*) ;;
  *) export PATH="${BIN_DIR}:\$PATH" ;;
esac
ENVEOF

  # Write fish env file
  cat > "$INSTALL_DIR/env.fish" << FISHEOF
# Unity CLI — added by installer
if not contains -- "${BIN_DIR}" \$PATH
  set -x PATH "${BIN_DIR}" \$PATH
end
FISHEOF

  # Configure shell PATH
  configure_shell_path

  # ─── Success ──────────────────────────────────────────────────────────────
  local display_install="${INSTALL_DIR/#$HOME/\~}"

  echo ""
  echo -e "  ${GREEN}✔${NC}  ${BOLD}Unity CLI $version installed!${NC}"
  echo ""
  echo -e "  ${BOLD}Get started:${NC}"
  echo    "    unity --help           Show available commands"
  echo    "    unity editors list     List installed Unity editors"
  echo    "    unity install          Install a Unity editor"
  echo    "    unity upgrade          Upgrade to the latest CLI version"
  echo ""

  if [ "$PATH_CONFIGURED" = "true" ] && [ -n "$SHELL_CONFIG_UPDATED" ]; then
    local display_config="${SHELL_CONFIG_UPDATED/#$HOME/\~}"
    echo -e "  Run ${BLUE}source $display_config${NC} or restart your terminal to use ${BOLD}unity${NC}."
  elif [ "$PATH_CONFIGURED" = "already" ]; then
    echo -e "  ${BOLD}unity${NC} is ready — restart your terminal or run ${BLUE}unity --help${NC}."
  else
    echo -e "  ${YELLOW}note${NC}: Add unity to your PATH by sourcing the env file:"
    echo ""
    echo    "    . \"${display_install}/env\""
    echo ""
    echo -e "  Or run unity directly: ${BLUE}${BIN_DIR_REF}/unity${NC}"
  fi

  echo ""
}

main "$@"
