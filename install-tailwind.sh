#!/usr/bin/env bash
set -euo pipefail

# This script installs Tailwind CLI v4.0.6 if it isn't already present.
# It automatically detects your OS and architecture and picks the correct installer.
# It also verifies the download against the provided SHA256 checksum.

# If Tailwind CLI is already installed (in the current directory), exit.
if [ -f "./tailwindcss" ]; then
  echo "Tailwind CLI already exists."
  exit 0
fi

# Define version and base URL.
VERSION="v4.0.6"
BASE_URL="https://github.com/tailwindlabs/tailwindcss/releases/download/${VERSION}"

# Detect OS and architecture.
OS=$(uname -s)
ARCH=$(uname -m)

installer=""

if [ "$OS" = "Linux" ]; then
  # Determine if musl libc is used (common in Alpine Linux).
  if ldd --version 2>&1 | grep -q musl; then
    MUSL_SUFFIX="-musl"
  else
    MUSL_SUFFIX=""
  fi

  case "$ARCH" in
    x86_64)
      installer="tailwindcss-linux-x64${MUSL_SUFFIX}"
      ;;
    aarch64 | arm64)
      installer="tailwindcss-linux-arm64${MUSL_SUFFIX}"
      ;;
    *)
      echo "Unsupported architecture: $ARCH"
      exit 1
      ;;
  esac

elif [ "$OS" = "Darwin" ]; then
  case "$ARCH" in
    x86_64)
      installer="tailwindcss-macos-x64"
      ;;
    arm64)
      installer="tailwindcss-macos-arm64"
      ;;
    *)
      echo "Unsupported architecture: $ARCH"
      exit 1
      ;;
  esac

else
  echo "Unsupported OS: $OS"
  exit 1
fi

echo "Downloading ${installer} from ${BASE_URL}..."

# Download the binary using curl or wget.
URL="${BASE_URL}/${installer}"
if command -v curl >/dev/null 2>&1; then
  curl -L "$URL" -o "$installer"
elif command -v wget >/dev/null 2>&1; then
  wget -O "$installer" "$URL"
else
  echo "Error: neither curl nor wget is installed."
  exit 1
fi

# Download the sha256sums.txt file and extract the expected checksum.
SHA256SUMS=$(curl -L "${BASE_URL}/sha256sums.txt")
EXPECTED_SUM=$(echo "$SHA256SUMS" | awk "\$2==\"./${installer}\" {print \$1}")

if [ -z "$EXPECTED_SUM" ]; then
  echo "Error: Could not find checksum for ${installer} in sha256sums.txt."
  exit 1
fi

# Calculate the actual checksum.
if command -v sha256sum >/dev/null 2>&1; then
  ACTUAL_SUM=$(sha256sum "$installer" | awk '{print $1}')
elif command -v shasum >/dev/null 2>&1; then
  ACTUAL_SUM=$(shasum -a 256 "$installer" | awk '{print $1}')
else
  echo "Warning: No sha256sum tool found. Skipping checksum verification."
  ACTUAL_SUM="$EXPECTED_SUM"
fi

if [ "$ACTUAL_SUM" != "$EXPECTED_SUM" ]; then
  echo "Checksum verification failed! Removing downloaded file."
  rm "$installer"
  exit 1
fi

# Rename the downloaded file to "tailwindcss" and make it executable.
mv "$installer" tailwindcss
chmod +x tailwindcss

echo "Tailwind CLI downloaded and installed successfully."

