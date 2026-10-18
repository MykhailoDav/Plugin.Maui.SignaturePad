#!/usr/bin/env bash
# Records new public API in src/Plugin.Maui.SignaturePad/PublicAPI/<target>/PublicAPI.Unshipped.txt for every target.
# dotnet format fixes only the first target of a multi-targeted project, so the script runs it once per target, with
# SIGNATUREPAD_SINGLE_TARGET making the project build only that target. On macOS and Linux the Windows target is
# compiled with EnableWindowsTargeting.
set -euo pipefail
cd "$(dirname "$0")/.."

project=src/Plugin.Maui.SignaturePad/Plugin.Maui.SignaturePad.csproj
targets=(net10.0 net10.0-android net10.0-windows10.0.19041.0)
if [[ "$(uname -s)" != "Linux" ]]; then
  targets+=(net10.0-ios net10.0-maccatalyst)
fi

for target in "${targets[@]}"; do
  echo "Updating the public API of $target"
  SIGNATUREPAD_SINGLE_TARGET="$target" EnableWindowsTargeting=true \
    dotnet format analyzers "$project" --diagnostics RS0016 RS0017 --severity info
done
