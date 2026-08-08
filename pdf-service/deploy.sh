#!/usr/bin/env bash
# =============================================================================
# deploy.sh — IBSS PDF Service Deployer (LOCAL)
# =============================================================================
#
# Runs ON THIS MACHINE (host "Base", 192.168.1.41). The backend reaches it at
# http://192.168.1.41:8081. Deploying = build the image here and (re)start the
# container here. No SSH, no remote host.
#
# USAGE:  ./deploy.sh
# =============================================================================

set -euo pipefail

IMAGE="ibss-pdf-service:latest"
CONTAINER="ibss-pdf-service"
HOST_PORT="8081"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "▶ Building image $IMAGE ..."
docker build -t "$IMAGE" "$SCRIPT_DIR"

echo "▶ (Re)starting container $CONTAINER ..."
docker rm -f "$CONTAINER" 2>/dev/null || true
docker run -d --name "$CONTAINER" -p "$HOST_PORT:8080" --restart unless-stopped "$IMAGE"

echo "▶ Waiting for pdf-service health ..."
status="000"
for i in $(seq 1 8); do
  status=$(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:$HOST_PORT/health" 2>/dev/null || echo "000")
  [ "$status" = "200" ] && { echo "✓ pdf-service up at http://192.168.1.41:$HOST_PORT"; exit 0; }
  sleep 2
done
echo "⚠ pdf-service did not respond — check: docker logs $CONTAINER --tail 50"
