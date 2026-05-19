#!/bin/bash

# BonniePDF Version Update Script
# This script manages version numbers in the [yyyy].[mm].[patch] format.
# It automatically increments the patch number for releases in the same month,
# or resets to .0 when moving to a new month.

set -e  # Exit on error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Default values
VERSION=""
WHAT_IF=false
COMMIT=false
TAG=false
QUIET=false

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSIONS_FILE="$SCRIPT_DIR/../versions.json"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# Functions
log_info() {
    if [ "$QUIET" = false ]; then
        echo -e "${GRAY}$1${NC}"
    fi
}

log_success() {
    if [ "$QUIET" = false ]; then
        echo -e "${GREEN}$1${NC}"
    fi
}

log_warning() {
    if [ "$QUIET" = false ]; then
        echo -e "${YELLOW}$1${NC}"
    fi
}

log_error() {
    echo -e "${RED}$1${NC}" >&2
}

log_cyan() {
    if [ "$QUIET" = false ]; then
        echo -e "${CYAN}$1${NC}"
    fi
}

print_usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Updates BonniePDF version numbers across all projects.

OPTIONS:
    -v, --version VERSION    Specify a specific version (e.g., 2026.04.0)
    -w, --what-if            Preview changes without modifying files
    -c, --commit             Create a git commit with the changes
    -t, --tag                Create a git tag for the new version (requires --commit)
    -q, --quiet              Suppress non-error output
    -h, --help               Show this help message

EXAMPLES:
    $0                                      Auto-increment version
    $0 --version 2026.04.5                  Set specific version
    $0 --what-if                            Preview changes
    $0 --commit --tag                       Update, commit, and tag

EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        -w|--what-if)
            WHAT_IF=true
            shift
            ;;
        -c|--commit)
            COMMIT=true
            shift
            ;;
        -t|--tag)
            TAG=true
            shift
            ;;
        -q|--quiet)
            QUIET=true
            shift
            ;;
        -h|--help)
            print_usage
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            print_usage
            exit 1
            ;;
    esac
done

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    log_error "jq is required but not installed. Please install jq."
    log_error "  Ubuntu/Debian: sudo apt-get install jq"
    log_error "  macOS: brew install jq"
    log_error "  Windows: choco install jq"
    exit 1
fi

# Main execution
main() {
    log_cyan "=== BonniePDF Version Update ==="
    echo ""
    
    log_info "Versions file: $VERSIONS_FILE"
    log_info "Root directory: $ROOT_DIR"
    echo ""
    
    # Check if versions.json exists
    if [ ! -f "$VERSIONS_FILE" ]; then
        log_error "Versions file not found at: $VERSIONS_FILE"
        exit 1
    fi
    
    # Read current version
    log_info "Reading versions file..."
    OLD_VERSION=$(jq -r '.currentVersion' "$VERSIONS_FILE")
    LAST_UPDATE=$(jq -r '.lastUpdate' "$VERSIONS_FILE")
    log_info "Current version: $OLD_VERSION"
    echo ""
    
    # Determine new version
    if [ -n "$VERSION" ]; then
        log_cyan "Using specified version: $VERSION"
        
        # Validate version format
        if ! [[ "$VERSION" =~ ^[0-9]{4}\.[0-9]{1,2}\.[0-9]+$ ]]; then
            log_error "Invalid version format: $VERSION"
            log_error "Expected format: yyyy.mm.patch (e.g., 2026.04.0)"
            exit 1
        fi
        
        IFS='.' read -r YEAR MONTH PATCH <<< "$VERSION"
        
        # Validate values
        if [ "$YEAR" -lt 2020 ] || [ "$YEAR" -gt 2100 ]; then
            log_error "Invalid year: $YEAR. Must be between 2020 and 2100."
            exit 1
        fi
        
        if [ "$MONTH" -lt 1 ] || [ "$MONTH" -gt 12 ]; then
            log_error "Invalid month: $MONTH. Must be between 1 and 12."
            exit 1
        fi
        
        if [ "$PATCH" -lt 0 ]; then
            log_error "Invalid patch: $PATCH. Must be 0 or greater."
            exit 1
        fi
        
        NEW_VERSION="$VERSION"
        # Extract year, month, patch from the specified version for history tracking
        CURRENT_YEAR="$YEAR"
        CURRENT_MONTH="$MONTH"
        NEW_PATCH="$PATCH"
    else
        log_cyan "Auto-calculating next version..."
        
        # Get current date
        CURRENT_YEAR=$(date +%Y)
        CURRENT_MONTH=$(date +%m)
        
        # Parse last update date
        LAST_YEAR=$(echo "$LAST_UPDATE" | cut -d'-' -f1)
        LAST_MONTH=$(echo "$LAST_UPDATE" | cut -d'-' -f2)
        
        # Remove leading zeros for comparison
        LAST_MONTH=$((10#$LAST_MONTH))
        CURRENT_MONTH=$((10#$CURRENT_MONTH))
        
        if [ "$CURRENT_YEAR" -eq "$LAST_YEAR" ] && [ "$CURRENT_MONTH" -eq "$LAST_MONTH" ]; then
            # Same month - increment patch
            LAST_PATCH=$(jq -r '.history[0].patch' "$VERSIONS_FILE")
            NEW_PATCH=$((LAST_PATCH + 1))
            log_cyan "Same month detected. Incrementing patch to $NEW_PATCH"
        else
            # New month - reset patch to 0
            NEW_PATCH=0
            log_cyan "New month detected. Resetting patch to 0"
        fi
        
        # Format month with leading zero
        printf -v FORMATTED_MONTH "%02d" $CURRENT_MONTH
        NEW_VERSION="${CURRENT_YEAR}.${FORMATTED_MONTH}.${NEW_PATCH}"
    fi
    
    log_success "New version: $NEW_VERSION"
    echo ""
    
    # What-if mode
    if [ "$WHAT_IF" = true ]; then
        log_warning "WHAT IF MODE: No changes will be made."
        echo ""
        log_cyan "Would update:"
        log_info "  - versions.json: $OLD_VERSION -> $NEW_VERSION"
        
        # Find all .csproj files
        while IFS= read -r -d '' file; do
            REL_PATH="${file#$ROOT_DIR/}"
            log_info "  - $REL_PATH"
        done < <(find "$ROOT_DIR" -name "*.csproj" -type f -print0)
        
        if [ -f "$ROOT_DIR/Directory.Build.props" ]; then
            log_info "  - Directory.Build.props"
        fi
        
        if [ "$COMMIT" = true ]; then
            log_info "  - Would create git commit"
        fi
        
        if [ "$TAG" = true ]; then
            log_info "  - Would create git tag v$NEW_VERSION"
        fi
        
        exit 0
    fi
    
    # Update versions.json
    log_info "Updating versions.json..."
    
    # Get current date in ISO format
    CURRENT_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    
    # Create new versions.json
    jq --arg new_version "$NEW_VERSION" \
       --arg new_date "$CURRENT_DATE" \
       --argjson new_patch "$NEW_PATCH" \
       --argjson new_year "${CURRENT_YEAR:-$(date +%Y)}" \
       --argjson new_month "${CURRENT_MONTH:-$(date +%m)}" \
       '.currentVersion = $new_version | 
        .lastUpdate = $new_date |
        .history = [{
            version: $new_version,
            date: $new_date,
            patch: $new_patch,
            year: $new_year,
            month: $new_month
        }] + .history' \
       "$VERSIONS_FILE" > "${VERSIONS_FILE}.tmp"
    
    mv "${VERSIONS_FILE}.tmp" "$VERSIONS_FILE"
    log_success "✓ Updated versions.json: $OLD_VERSION -> $NEW_VERSION"
    
    # Update all .csproj files
    echo ""
    log_info "Updating .csproj files..."
    
    UPDATED_COUNT=0
    while IFS= read -r -d '' file; do
        REL_PATH="${file#$ROOT_DIR/}"
        
        # Check if file contains Version element
        if grep -q '<Version>' "$file"; then
            # Extract current version using sed (more portable than grep -P)
            CURRENT_PROJ_VERSION=$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$file" | head -1)
            
            # Only update if version is different and not empty
            if [ -n "$CURRENT_PROJ_VERSION" ] && [ "$CURRENT_PROJ_VERSION" != "$NEW_VERSION" ]; then
                # Replace version using awk (more reliable than sed for complex replacements)
                awk -v old="$CURRENT_PROJ_VERSION" -v new="$NEW_VERSION" '{
                    gsub("<Version>" old "</Version>", "<Version>" new "</Version>")
                    print
                }' "$file" > "${file}.tmp"
                mv "${file}.tmp" "$file"
                rm -f "${file}.bak"
                log_success "✓ Updated $REL_PATH: $CURRENT_PROJ_VERSION -> $NEW_VERSION"
                UPDATED_COUNT=$((UPDATED_COUNT + 1))
            fi
        else
            # Version element doesn't exist, add it after TargetFramework or in first PropertyGroup
            if grep -q '<PropertyGroup>' "$file"; then
                # Find the first PropertyGroup and add Version after it
                awk -v version="$NEW_VERSION" '
                    /<PropertyGroup>/ {
                        print
                        getline
                        print "    <Version>" version "</Version>"
                        print $0
                        next
                    }
                    { print }
                ' "$file" > "${file}.tmp"
                mv "${file}.tmp" "$file"
                log_success "✓ Added version to $REL_PATH: $NEW_VERSION"
                UPDATED_COUNT=$((UPDATED_COUNT + 1))
            fi
        fi
    done < <(find "$ROOT_DIR" -name "*.csproj" -type f -print0)
    
    if [ "$UPDATED_COUNT" -eq 0 ]; then
        log_warning "No .csproj files needed updating."
    fi
    
    # Update Directory.Build.props
    echo ""
    log_info "Updating Directory.Build.props..."
    
    BUILD_PROPS="$ROOT_DIR/Directory.Build.props"
    
    if [ -f "$BUILD_PROPS" ]; then
        if grep -q '<Version>' "$BUILD_PROPS"; then
            # Extract current version using sed (more portable than grep -P)
            CURRENT_PROPS_VERSION=$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$BUILD_PROPS" | head -1)
            
            # Only update if version is different and not empty
            if [ -n "$CURRENT_PROPS_VERSION" ] && [ "$CURRENT_PROPS_VERSION" != "$NEW_VERSION" ]; then
                # Replace version using awk (more reliable than sed for complex replacements)
                awk -v old="$CURRENT_PROPS_VERSION" -v new="$NEW_VERSION" '{
                    gsub("<Version>" old "</Version>", "<Version>" new "</Version>")
                    print
                }' "$BUILD_PROPS" > "${BUILD_PROPS}.tmp"
                mv "${BUILD_PROPS}.tmp" "$BUILD_PROPS"
                rm -f "${BUILD_PROPS}.bak"
                log_success "✓ Updated Directory.Build.props: $CURRENT_PROPS_VERSION -> $NEW_VERSION"
            else
                log_warning "Directory.Build.props already up to date."
            fi
        else
            # Version property doesn't exist, add it
            if grep -q '<PropertyGroup>' "$BUILD_PROPS"; then
                awk -v version="$NEW_VERSION" '
                    /<PropertyGroup>/ {
                        print
                        getline
                        print "  <Version>" version "</Version>"
                        print $0
                        next
                    }
                    { print }
                ' "$BUILD_PROPS" > "${BUILD_PROPS}.tmp"
                mv "${BUILD_PROPS}.tmp" "$BUILD_PROPS"
                log_success "✓ Added version to Directory.Build.props: $NEW_VERSION"
            fi
        fi
    else
        log_warning "Directory.Build.props not found at $BUILD_PROPS"
    fi
    
    # Create git commit if requested
    if [ "$COMMIT" = true ]; then
        echo ""
        log_info "Creating git commit..."
        
        cd "$ROOT_DIR"
        
        # Check if we're in a git repository
        if git rev-parse --git-dir > /dev/null 2>&1; then
            # Check for changes
            if [ -n "$(git status --porcelain)" ]; then
                # Stage changes
                git add versions.json
                git add Directory.Build.props
                
                # Stage all .csproj files
                find . -name "*.csproj" -type f -exec git add {} \;
                
                # Commit
                git commit -m "chore: bump version to $NEW_VERSION"
                
                log_success "✓ Created git commit: chore: bump version to $NEW_VERSION"
            else
                log_warning "No changes to commit."
            fi
        else
            log_warning "Not in a git repository. Skipping commit."
        fi
    fi
    
    # Create git tag if requested
    if [ "$TAG" = true ]; then
        if [ "$COMMIT" = false ]; then
            log_warning "Warning: --tag requires --commit. Skipping tag creation."
        else
            echo ""
            log_info "Creating git tag..."
            
            cd "$ROOT_DIR"
            
            # Check if we're in a git repository
            if git rev-parse --git-dir > /dev/null 2>&1; then
                TAG_NAME="v$NEW_VERSION"
                
                # Check if tag already exists
                if git rev-parse "$TAG_NAME" > /dev/null 2>&1; then
                    log_warning "Tag $TAG_NAME already exists. Skipping tag creation."
                else
                    git tag -a "$TAG_NAME" -m "Version $NEW_VERSION"
                    log_success "✓ Created git tag: $TAG_NAME"
                fi
            else
                log_warning "Not in a git repository. Skipping tag."
            fi
        fi
    fi
    
    echo ""
    log_success "=== Version update complete ==="
    log_cyan "New version: $NEW_VERSION"
    
    exit 0
}

# Run main function
main
