#!/usr/bin/env bash
#
# veraPDF compliance gate.
#
# Resolves the veraPDF greenfield engine from Maven Central, compiles the
# Validate helper, and validates every PDF in <pdf-dir> against the given
# flavours. Exits non-zero if any file is non-compliant — wire it into CI.
#
# Usage:
#   tools/verapdf/validate.sh <pdf-dir> [flavours]
#   tools/verapdf/validate.sh generated-pdfs ua1,1b
#
# Requirements: a JDK (java/javac 11+) and Maven on PATH. Both are provided
# by actions/setup-java in CI; locally, install a JDK + Maven.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PDF_DIR="${1:?usage: validate.sh <pdf-dir> [flavours]}"
FLAVOURS="${2:-ua1,1b}"

# Resolve dependencies into tools/verapdf/lib (offline-cached after first run).
if [ ! -d "$HERE/lib" ] || [ -z "$(ls -A "$HERE/lib" 2>/dev/null)" ]; then
  echo "==> Resolving veraPDF engine from Maven Central..."
  mvn -q -f "$HERE/pom.xml" dependency:copy-dependencies -DoutputDirectory=lib
fi

# Compile the validator once (or when the source is newer than the class).
if [ ! -f "$HERE/Validate.class" ] || [ "$HERE/Validate.java" -nt "$HERE/Validate.class" ]; then
  echo "==> Compiling Validate.java..."
  javac -cp "$HERE/lib/*" -d "$HERE" "$HERE/Validate.java"
fi

echo "==> Validating $PDF_DIR against [$FLAVOURS]"
java -Djava.util.logging.config.file="$HERE/logging.properties" \
  -cp "$HERE/lib/*:$HERE" Validate "$PDF_DIR" "$FLAVOURS"
