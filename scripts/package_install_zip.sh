#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="${ROOT_DIR}/bin/Release/net8.0"
STAGE_DIR="${ROOT_DIR}/dist/CS2DataExport"
ZIP_PATH="${ROOT_DIR}/dist/CS2DataExport-windows-install.zip"

if [[ ! -d "${BUILD_DIR}" ]]; then
  echo "Build output not found at ${BUILD_DIR}"
  echo "Run: dotnet build -c Release"
  exit 1
fi

rm -rf "${STAGE_DIR}"
mkdir -p "${STAGE_DIR}"

cp -R "${BUILD_DIR}/." "${STAGE_DIR}/"
cp "${ROOT_DIR}/INSTALL.md" "${STAGE_DIR}/INSTALL.md"
cp "${ROOT_DIR}/SCHEMA.md" "${STAGE_DIR}/SCHEMA.md"
mkdir -p "${STAGE_DIR}/sample"
cp "${ROOT_DIR}/sample/latest.sample.json" "${STAGE_DIR}/sample/latest.sample.json"

rm -f "${ZIP_PATH}"
(
  cd "${ROOT_DIR}/dist"
  zip -r "$(basename "${ZIP_PATH}")" "CS2DataExport" >/dev/null
)

echo "Created ${ZIP_PATH}"
