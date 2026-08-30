#!/usr/bin/env bash
#
# ZUGFeRD / Factur-X compliance gate (Mustang project).
#
# Downloads the Mustang command-line validator and validates a Factur-X /
# ZUGFeRD PDF: it checks the PDF/A-3 container, the embedded CII XML against
# the EN 16931 schematron, and that the XMP `fx:` metadata matches. Exits
# non-zero if the file is non-compliant - wire it into CI.
#
# Usage:
#   tools/mustang/validate.sh <pdf> [more.pdf ...]
#   tools/mustang/validate.sh generated-pdfs/factur-x_invoice.pdf
#
# Env:
#   MUSTANG_VERSION  Mustang release to use (default below).
#
# Requirements: a JDK (java 11+) and curl on PATH. Both are provided by
# actions/setup-java in CI; locally, install a JDK.
#
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MUSTANG_VERSION="${MUSTANG_VERSION:-2.16.2}"
JAR="$HERE/Mustang-CLI-${MUSTANG_VERSION}.jar"

if [ "$#" -lt 1 ]; then
  echo "usage: validate.sh <pdf> [more.pdf ...]" >&2
  exit 2
fi

if [ ! -f "$JAR" ]; then
  url="https://github.com/ZUGFeRD/mustangproject/releases/download/core-${MUSTANG_VERSION}/Mustang-CLI-${MUSTANG_VERSION}.jar"
  echo "==> Downloading Mustang CLI ${MUSTANG_VERSION} from ${url}"
  curl -fsSL "$url" -o "$JAR"
fi

status=0
for pdf in "$@"; do
  echo "==> Validating ${pdf} with Mustang (ZUGFeRD / EN 16931)"
  # --action validate exits non-zero when the document is invalid. --no-notices
  # keeps informational notices from failing the gate.
  if java -jar "$JAR" --action validate --no-notices --source "$pdf"; then
    echo "PASS  ${pdf}"
  else
    echo "FAIL  ${pdf}"
    status=1
  fi
done

exit "$status"
