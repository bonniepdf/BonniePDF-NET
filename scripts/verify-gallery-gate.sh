#!/usr/bin/env bash
# Wait for the configured cross-engine gate before publishing this source revision.
set -euo pipefail
: "${GH_TOKEN:?Configure PARITY_WORKFLOW_TOKEN with Actions read/write access to the validation repository}"
: "${PARITY_WORKFLOW_REPOSITORY:?Configure the validation repository variable}"
: "${GALLERY_ENGINE:?Expected dotnet or typescript}"
request="gallery-${GITHUB_RUN_ID}-${GITHUB_RUN_ATTEMPT}-${GALLERY_ENGINE}"
case "$GALLERY_ENGINE" in
  dotnet) source_input=dotnet_ref ;;
  typescript) source_input=typescript_ref ;;
  *) echo 'Invalid engine'; exit 1 ;;
esac
gh workflow run parity.yml --repo "$PARITY_WORKFLOW_REPOSITORY" --ref master \
  -f "$source_input=$GITHUB_SHA" -f "request_id=$request"
deadline=$((SECONDS + 180))
run_id=""
while [ "$SECONDS" -lt "$deadline" ]; do
  run_id=$(gh run list --repo "$PARITY_WORKFLOW_REPOSITORY" --workflow parity.yml \
    --event workflow_dispatch --limit 50 --json databaseId,displayTitle \
    --jq ".[] | select(.displayTitle == \"Gallery $request\") | .databaseId" | head -n1)
  if [ -n "$run_id" ]; then break; fi
  sleep 5
done
if [ -z "$run_id" ]; then echo 'Gallery validation run did not appear; refusing to publish'; exit 1; fi
gh run watch "$run_id" --repo "$PARITY_WORKFLOW_REPOSITORY" --exit-status
conclusion=$(gh run view "$run_id" --repo "$PARITY_WORKFLOW_REPOSITORY" --json conclusion --jq '.conclusion')
if [ "$conclusion" != success ]; then echo "Gallery validation concluded $conclusion; refusing to publish"; exit 1; fi
