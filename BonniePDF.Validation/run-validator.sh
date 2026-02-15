#!/bin/bash

echo "========================================"
echo "BonniePDF PDF/A Validation Tool"
echo "========================================"
echo ""

# Check if VeraPDF service is running
echo "Checking VeraPDF API..."
if ! curl -s http://localhost:8080/ > /dev/null 2>&1; then
    echo ""
    echo "ERROR: Cannot connect to VeraPDF API at http://localhost:8080/"
    echo ""
    echo "Please start the VeraPDF service first:"
    echo "  docker run -d -p 8080:8080 verapdf/verapdf-rest:latest"
    echo ""
    exit 1
fi

echo "VeraPDF API is running!"
echo ""

# Build and run the validator
dotnet run --project BonniePDF.Validation/BonniePDF.Validation.csproj "$@"

echo ""
echo "========================================"
echo "Validation complete!"
echo "========================================"
