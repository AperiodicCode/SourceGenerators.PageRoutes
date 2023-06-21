#!/usr/bin/env bash

set -Eeuo pipefail

# pack a nupkg for each roslyn version that is supported by Mapperly
# and merge them together into one nupkg

RELEASE_VERSION=${RELEASE_VERSION:-'0.0.1-dev'}
RELEASE_NOTES=${RELEASE_NOTES:-''}

# https://stackoverflow.com/a/246128/3302887
script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)
artifacts_dir="${script_dir}/../artifacts"

dotnet pack \
	"${script_dir}/../src/AperiodicCode.SourceGenerators.PageRoutes" \
	-c Release \
	-o "${artifacts_dir}" \
	/p:Version="${RELEASE_VERSION}" \
	/p:PackageReleaseNotes=\""${RELEASE_NOTES}"\"