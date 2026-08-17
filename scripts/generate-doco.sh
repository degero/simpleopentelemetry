#!/usr/bin/env bash
set -euo pipefail

PROPS_FILE="Directory.Packages.props"
OUTPUT_FILE="docs/otel-component-versions.md"

# Extract tagged plugin entries: Name and Version, in file order
mapfile -t rows < <(
  grep -oP '<PackageVersion Include="[^"]+" Version="[^"]+" IsReflectionPlugin="true"' "$PROPS_FILE" \
    | sed -E 's/<PackageVersion Include="([^"]+)" Version="([^"]+)".*/| \1 | \2 |/'
)

if [ ${#rows[@]} -eq 0 ]; then
  echo "No IsReflectionPlugin entries found in $PROPS_FILE — check the tag is present." >&2
  exit 1
fi

{
  echo "# SimpleOpenTelemetry tested otel components"
  echo
  echo "> This file is auto-generated from Directory.Packages.props. Do not edit by hand."
  echo
  echo "| Package | Tested Version |"
  echo "|---|---|"
  printf '%s\n' "${rows[@]}"
} > "$OUTPUT_FILE"

echo "Wrote $OUTPUT_FILE with ${#rows[@]} tested plugin versions."
