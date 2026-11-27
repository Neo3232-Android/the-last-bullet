#!/usr/bin/env bash
# Trigger a Unity Build Automation (UBA) build via the v2 API, wait for it,
# and download the primary artifact (the APK).
#
# Manual trigger only — UBA build minutes are limited (200 Windows min/month
# free), so this script never starts a build on its own.
#
# Required env:
#   UBA_ORG_ID, UBA_PROJECT_ID, UBA_TARGET
#   UNITY_SERVICE_ACCOUNT_ID, UNITY_SERVICE_ACCOUNT_SECRET
# Optional env:
#   CLEAN (true/false, default false), DOWNLOAD_DIR (default uba-artifact),
#   UBA_STATE_FILE (default uba-build-state.env, gets UBA_BUILD_NUMBER).
set -euo pipefail

: "${UBA_ORG_ID:?UBA_ORG_ID is not set}"
: "${UBA_PROJECT_ID:?UBA_PROJECT_ID is not set}"
: "${UBA_TARGET:?UBA_TARGET is not set}"
: "${UNITY_SERVICE_ACCOUNT_ID:?UNITY_SERVICE_ACCOUNT_ID is not set}"
: "${UNITY_SERVICE_ACCOUNT_SECRET:?UNITY_SERVICE_ACCOUNT_SECRET is not set}"

API="https://build-automation.services.api.unity.com/v2"
T="/orgs/${UBA_ORG_ID}/projects/${UBA_PROJECT_ID}/buildtargets/${UBA_TARGET}"
CLEAN="${CLEAN:-false}"
DOWNLOAD_DIR="${DOWNLOAD_DIR:-uba-artifact}"
STATE_FILE="${UBA_STATE_FILE:-uba-build-state.env}"

auth=(-u "${UNITY_SERVICE_ACCOUNT_ID}:${UNITY_SERVICE_ACCOUNT_SECRET}")

# Free-tier check. A project-scoped Automation User role gets 403 here —
# treat anything that is not an explicit "limit reached" as unknown and continue.
free_tier="$(curl -s "${auth[@]}" "${API}/orgs/${UBA_ORG_ID}/free-tier-status" || true)"
if echo "$free_tier" | grep -q '"freeTierLimitReached"[[:space:]]*:[[:space:]]*true'; then
  echo "::error::Unity free-tier limit reached — the build would be refused. Add a payment method in the Unity Dashboard or wait for the monthly reset."
  exit 1
fi

# Start the build. 409 means a build is already pending on this target —
# wait a little and retry a few times rather than failing immediately.
build_number=""
for attempt in 1 2 3 4 5; do
  http_code="$(curl -s -o /tmp/uba-start.json -w "%{http_code}" "${auth[@]}" \
    -X POST "${API}${T}/builds" -H "Content-Type: application/json" \
    -d "{\"clean\": ${CLEAN}}")"
  if [ "$http_code" = "202" ]; then
    build_number="$(python3 -c "
import json
d = json.load(open('/tmp/uba-start.json'))
b = d[0] if isinstance(d, list) else d
print(b.get('build') or b.get('buildNumber') or b.get('number') or '')
" 2>/dev/null || echo "")"
    if [ -n "$build_number" ]; then
      break
    fi
    echo "::error::Build started but the build number could not be read from the response:"
    cat /tmp/uba-start.json
    exit 1
  elif [ "$http_code" = "409" ]; then
    echo "A build is already pending on this target (attempt $attempt/5); waiting 60s..."
    sleep 60
  else
    echo "::error::Failed to start the build (HTTP $http_code):"
    cat /tmp/uba-start.json
    exit 1
  fi
done

if [ -z "$build_number" ]; then
  echo "::error::Could not start a build — the target stayed busy."
  exit 1
fi

echo "UBA_BUILD_NUMBER=$build_number" > "$STATE_FILE"
echo "Started UBA build #$build_number — polling every 60s..."

status=""
while true; do
  curl -s "${auth[@]}" \
    "${API}${T}/builds/${build_number}?include=testResults,failureDetails,buildReports,links.artifacts" \
    -o /tmp/uba-status.json
  status="$(python3 -c "import json; print(json.load(open('/tmp/uba-status.json')).get('buildStatus',''))" 2>/dev/null || echo "")"
  echo "$(date -u +%H:%M:%S) build #$build_number status: ${status:-unknown}"
  case "$status" in
    success|failure|canceled|unknown) break ;;
  esac
  sleep 60
done

# Summary: test results, billable minutes, failure details.
python3 - <<'PY'
import json
try:
    d = json.load(open('/tmp/uba-status.json'))
except Exception as e:
    print(f"Could not read build status: {e}")
    raise SystemExit(0)
print(f"Build #{d.get('build')} status: {d.get('buildStatus')}")
bill = d.get('billableTimeInSeconds')
if bill is not None:
    print(f"Billable time: {bill}s ({bill/60:.1f} min)")
tr = d.get('testResults') or {}
for name, r in tr.items():
    if isinstance(r, dict):
        print(f"Tests {name}: passed={r.get('passed')} failed={r.get('failed')} duration={r.get('duration')}s")
for f in d.get('failureDetails') or []:
    print(f"Failure: {f}")
PY

if [ "$status" != "success" ]; then
  echo "=== UBA build log (tail) ==="
  curl -sL "${auth[@]}" "${API}${T}/builds/${build_number}/log?maxLines=200" -o /tmp/uba-log.txt || true
  tail -n 200 /tmp/uba-log.txt || true
  exit 1
fi

# Success: download the primary artifact.
curl -s "${auth[@]}" "${API}${T}/builds/${build_number}/artifacts" -o /tmp/uba-artifacts.json
filename="$(python3 -c "
import json
arts = json.load(open('/tmp/uba-artifacts.json'))
primary = next((a for a in arts if a.get('primary')), arts[0] if arts else None)
files = (primary or {}).get('files') or []
print(files[0].get('filename','') if files else '')
" 2>/dev/null || echo "")"
if [ -z "$filename" ]; then
  echo "::error::Build succeeded but no artifact file was found:"
  cat /tmp/uba-artifacts.json
  exit 1
fi

mkdir -p "$DOWNLOAD_DIR"
# The download endpoint answers 303 with the signed URL in the JSON body
# field "url" (no Location header) — read it, then fetch it without auth.
signed_url="$(curl -s "${auth[@]}" "${API}${T}/builds/${build_number}/download/${filename}" \
  | python3 -c "import json,sys; print(json.load(sys.stdin).get('url',''))")"
if [ -z "$signed_url" ]; then
  echo "::error::Could not get a download URL for $filename"
  exit 1
fi
curl -sL -o "${DOWNLOAD_DIR}/${filename}" "$signed_url"
echo "Downloaded $filename to ${DOWNLOAD_DIR}/"
ls -la "$DOWNLOAD_DIR"
