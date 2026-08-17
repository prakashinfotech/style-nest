#!/usr/bin/env bash
# =============================================================
# ENH-INFRA-006 — Blue-Green Slot Swap + Auto-Rollback Script
#
# Commands:
#   promote-green             Switch router traffic to green slot
#   rollback                  Switch router traffic back to blue
#   monitor [--timeout=N] [--threshold=N]
#                             Monitor green for N seconds (default 300).
#                             Auto-rollback if HTTP error rate > threshold%
#                             (default 1%).
#   status                    Print which slot is currently active
#
# Requires:
#   docker (for exec + healthcheck queries)
#   curl   (for health probe)
#
# Usage examples:
#   ./scripts/blue-green-swap.sh promote-green
#   ./scripts/blue-green-swap.sh monitor --timeout=300 --threshold=1
#   ./scripts/blue-green-swap.sh rollback
# =============================================================

set -euo pipefail

# ── Config ────────────────────────────────────────────────────────────────────
ROUTER_CONTAINER="${ROUTER_CONTAINER:-stylenest-router}"
ACTIVE_UPSTREAM_DIR="/etc/nginx/active"
BLUE_GATEWAY_HEALTH="http://stylenest-gateway-api-blue/health"
GREEN_GATEWAY_HEALTH="http://stylenest-gateway-api-green/health"
ROUTER_HEALTH="http://localhost:5000/router-health"
MONITOR_TIMEOUT="${MONITOR_TIMEOUT:-300}"
ERROR_THRESHOLD="${ERROR_THRESHOLD:-1}"  # percent
PROBE_INTERVAL=10  # seconds between health probes during monitoring

# ── Colours ───────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
CYAN='\033[0;36m'; RESET='\033[0m'; BOLD='\033[1m'

log()     { echo -e "${CYAN}[$(date '+%H:%M:%S')]${RESET} $*"; }
ok()      { echo -e "${GREEN}[$(date '+%H:%M:%S')] ✓${RESET} $*"; }
warn()    { echo -e "${YELLOW}[$(date '+%H:%M:%S')] ⚠${RESET} $*"; }
err()     { echo -e "${RED}[$(date '+%H:%M:%S')] ✗${RESET} $*" >&2; }
die()     { err "$*"; exit 1; }
header()  { echo -e "\n${BOLD}${CYAN}══ $* ══${RESET}\n"; }

# ── Helpers ───────────────────────────────────────────────────────────────────

# Write the nginx upstream include file and reload nginx
activate_slot() {
  local slot="$1"   # "blue" or "green"
  local upstream="${slot}_gateway"

  log "Writing upstream config for slot: ${BOLD}${slot}${RESET}"
  docker exec "${ROUTER_CONTAINER}" sh -c "
    mkdir -p ${ACTIVE_UPSTREAM_DIR}
    echo 'set \$active_upstream ${upstream};' > ${ACTIVE_UPSTREAM_DIR}/upstream.conf
    echo '${slot}' > ${ACTIVE_UPSTREAM_DIR}/slot
  "
  log "Reloading nginx..."
  docker exec "${ROUTER_CONTAINER}" nginx -s reload
  sleep 1   # brief pause for workers to finish graceful shutdown
  ok "Router now pointing at ${BOLD}${slot}${RESET} slot."
}

# Return current active slot ("blue" or "green")
current_slot() {
  docker exec "${ROUTER_CONTAINER}" cat "${ACTIVE_UPSTREAM_DIR}/slot" 2>/dev/null || echo "blue"
}

# Wait for a gateway's /health endpoint to return 200 (up to 60s)
wait_healthy() {
  local url="$1"; local label="$2"
  local deadline=$(( $(date +%s) + 60 ))
  log "Waiting for ${label} to be healthy..."
  while true; do
    if docker exec "${ROUTER_CONTAINER}" curl -sf --max-time 3 "${url}" > /dev/null 2>&1; then
      ok "${label} is healthy."
      return 0
    fi
    if (( $(date +%s) >= deadline )); then
      err "${label} did not become healthy within 60s."
      return 1
    fi
    sleep 3
  done
}

# Probe the router and return HTTP status code
probe_router() {
  curl -so /dev/null -w "%{http_code}" --max-time 5 "${ROUTER_HEALTH}" 2>/dev/null || echo "000"
}

# ── Commands ──────────────────────────────────────────────────────────────────

cmd_status() {
  local slot
  slot=$(current_slot)
  header "Active Slot"
  echo -e "  Router container : ${BOLD}${ROUTER_CONTAINER}${RESET}"
  echo -e "  Active slot      : ${BOLD}${slot}${RESET}"
  echo -e "  Router health    : $(probe_router)"
}

cmd_promote_green() {
  header "Promoting GREEN slot"

  # 1. Verify green gateway is healthy before switching
  wait_healthy "${GREEN_GATEWAY_HEALTH}" "Green gateway" \
    || die "Green gateway unhealthy — aborting promotion."

  # 2. Switch router to green
  activate_slot "green"

  # 3. Verify router responds after switch
  sleep 2
  local status
  status=$(probe_router)
  if [[ "${status}" != "200" ]]; then
    warn "Router probe returned ${status} after switch — rolling back..."
    cmd_rollback
    die "Promotion aborted due to unhealthy router probe."
  fi

  ok "GREEN slot is now ACTIVE. Run 'monitor' to watch for error spikes."
}

cmd_rollback() {
  header "Rolling back to BLUE slot"
  activate_slot "blue"

  # Verify router
  sleep 2
  local status
  status=$(probe_router)
  if [[ "${status}" == "200" ]]; then
    ok "BLUE slot restored successfully."
  else
    err "Router probe returned ${status} after rollback. Manual investigation required."
    exit 1
  fi
}

cmd_monitor() {
  # Parse optional flags
  for arg in "$@"; do
    case "${arg}" in
      --timeout=*)  MONITOR_TIMEOUT="${arg#*=}" ;;
      --threshold=*) ERROR_THRESHOLD="${arg#*=}" ;;
    esac
  done

  header "Monitoring active slot for ${MONITOR_TIMEOUT}s (error threshold: ${ERROR_THRESHOLD}%)"
  local slot
  slot=$(current_slot)
  log "Monitoring slot: ${BOLD}${slot}${RESET}"

  local total=0 errors=0 elapsed=0
  local start
  start=$(date +%s)

  while (( elapsed < MONITOR_TIMEOUT )); do
    sleep "${PROBE_INTERVAL}"
    elapsed=$(( $(date +%s) - start ))

    local status
    status=$(probe_router)
    (( total++ )) || true

    if [[ "${status}" != "200" ]]; then
      (( errors++ )) || true
      warn "Probe #${total}: status=${status}  errors=${errors}/${total}"
    else
      log "Probe #${total}: status=${status}  errors=${errors}/${total}  elapsed=${elapsed}s"
    fi

    # Calculate error rate (avoid division by zero)
    if (( total > 0 )); then
      local error_pct
      error_pct=$(awk "BEGIN { printf \"%.1f\", (${errors} / ${total}) * 100 }")
      if awk "BEGIN { exit !(${error_pct} > ${ERROR_THRESHOLD}) }"; then
        err "Error rate ${error_pct}% exceeds threshold ${ERROR_THRESHOLD}% — triggering AUTO-ROLLBACK!"
        cmd_rollback
        die "Auto-rollback triggered after ${elapsed}s. Error rate: ${error_pct}%"
      fi
    fi
  done

  local final_pct
  final_pct=$(awk "BEGIN { printf \"%.1f\", (${errors} / ${total}) * 100 }")
  ok "Monitoring complete. Probes: ${total}  Errors: ${errors}  Error rate: ${final_pct}%"
  ok "Slot ${BOLD}${slot}${RESET} is STABLE. Safe to decommission blue slot."
}

# ── Entrypoint ────────────────────────────────────────────────────────────────

COMMAND="${1:-status}"
shift || true

case "${COMMAND}" in
  promote-green)   cmd_promote_green "$@" ;;
  rollback)        cmd_rollback      "$@" ;;
  monitor)         cmd_monitor       "$@" ;;
  status)          cmd_status        "$@" ;;
  *)
    echo "Usage: $0 {promote-green|rollback|monitor|status} [--timeout=N] [--threshold=N]"
    exit 1
    ;;
esac
