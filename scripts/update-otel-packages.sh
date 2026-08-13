#!/usr/bin/env bash
set -euo pipefail

# Usage: ./update-packages.sh <major.minor> [package-prefix]
# Example: ./update-packages.sh 1.16
# Example: ./update-packages.sh 1.16 OpenTelemetry
# IMPORTANT: This is only for maintainers see MAINTAINING.md

VERSION_PREFIX="${1:?Usage: $0 <major.minor> [package-prefix]}"
PKG_PREFIX="${2:-OpenTelemetry}"

for pkg in $(grep -oP "(?<=Include=\")${PKG_PREFIX}[^\"]*" Directory.Packages.props | sort -u); do
  lower=$(echo "$pkg" | tr '[:upper:]' '[:lower:]')
  latest=$(curl -s "https://api.nuget.org/v3-flatcontainer/${lower}/index.json" \
    | grep -oP '"\d+\.\d+\.\d+[^"]*"' \
    | tr -d '"' \
    | grep "^${VERSION_PREFIX}\." \
    | tail -1)
  echo "$pkg -> [$latest]"
  if [ -n "$latest" ]; then
    sed -i "s|\(<PackageVersion Include=\"$pkg\" Version=\"\)[^\"]*|\1[$latest]|" Directory.Packages.props
  fi
done
