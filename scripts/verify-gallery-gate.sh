#!/usr/bin/env bash
# Wait for the configured cross-engine gate before publishing this source revision.
#
# Polls once every GATE_POLL_SECONDS with a single API call. `gh run watch`
# makes several calls every few seconds, which exhausts the token owner's
# hourly API budget during a long gate run. If no runner picks the gate up
# within GATE_QUEUE_MINUTES, the queued run is cancelled and the release stops
# with a clear message rather than waiting until the job times out.
set -euo pipefail
: "${GH_TOKEN:?Configure PARITY_WORKFLOW_TOKEN with Actions read/write access to the validation repository}"
: "${PARITY_WORKFLOW_REPOSITORY:?Configure the validation repository variable}"
: "${GALLERY_ENGINE:?Expected dotnet or typescript}"
poll="${GATE_POLL_SECONDS:-30}"
queue_limit=$(( ${GATE_QUEUE_MINUTES:-15} * 60 ))
run_limit=$(( ${GATE_RUN_MINUTES:-60} * 60 ))
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
  sleep 10
done
if [ -z "$run_id" ]; then echo 'Gallery validation run did not appear; refusing to publish'; exit 1; fi
url="https://github.com/$PARITY_WORKFLOW_REPOSITORY/actions/runs/$run_id"
echo "Waiting for gallery validation: $url"
started=$SECONDS
status=""
conclusion=""
while :; do
  # A transient API failure (including a rate limit) is retried at the next poll.
  if result=$(gh run view "$run_id" --repo "$PARITY_WORKFLOW_REPOSITORY" \
      --json status,conclusion --jq '.status + " " + .conclusion' 2>&1); then
    read -r status conclusion <<<"$result"
  else
    echo "Could not read gate status (will retry): $result"
  fi
  if [ "$status" = completed ]; then break; fi
  elapsed=$((SECONDS - started))
  if [ "$status" = queued ] && [ "$elapsed" -ge "$queue_limit" ]; then
    gh run cancel "$run_id" --repo "$PARITY_WORKFLOW_REPOSITORY" || true
    echo "::error::Gallery validation was still queued after $((queue_limit / 60)) minutes. No runner picked it up: check that the validation repository's self-hosted runner is online. Cancelled $url; refusing to publish."
    exit 1
  fi
  if [ "$elapsed" -ge "$run_limit" ]; then
    echo "::error::Gallery validation did not finish within $((run_limit / 60)) minutes ($url); refusing to publish."
    exit 1
  fi
  echo "Gate is ${status:-unknown} after $((elapsed / 60))m; checking again in ${poll}s"
  sleep "$poll"
done
if [ "$conclusion" != success ]; then echo "::error::Gallery validation concluded $conclusion ($url); refusing to publish"; exit 1; fi
echo "Gallery validation passed: $url"
