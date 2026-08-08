#!/usr/bin/env bash
# =============================================================================
# deploy.sh — IBSS Frontend Deployer (LOCAL)
# =============================================================================
#
# The app runs ON THIS MACHINE (host "Base", 192.168.1.41). The l-node01 nginx
# gateway proxies ibss.curium.dk / hub.mgworld.academy to this host's port
# 5050 (the ibss-frontend nginx container). Deploying = build the Vue app and
# hot-swap the files into the running container. No SSH, no remote host.
#
#   Build:   cd app && npm run build  ->  app/dist/
#   Inject:  docker cp app/dist/. ibss-frontend:/usr/share/nginx/html/
#   Container: ibss-frontend (port 5050 -> 80). If it isn't running, this
#              script (re)creates it from the ibss-frontend:latest image.
#
# USAGE:  ./deploy.sh
# =============================================================================

set -euo pipefail

CONTAINER="ibss-frontend"
IMAGE="ibss-frontend:latest"
HOST_PORT="5050"
CONTAINER_HTML="/usr/share/nginx/html"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
APP_DIR="$SCRIPT_DIR/app"

echo "▶ Building app..."
cd "$APP_DIR"
npm run build

# Recreate the container if it isn't running (first deploy after a reboot /
# fresh host); otherwise hot-swap into the live one.
if ! docker ps --format '{{.Names}}' | grep -qx "$CONTAINER"; then
  echo "▶ Container '$CONTAINER' not running — creating it ..."
  docker rm -f "$CONTAINER" 2>/dev/null || true
  docker run -d --name "$CONTAINER" -p "$HOST_PORT:80" --restart unless-stopped "$IMAGE"
  sleep 2
fi

echo "▶ Copying dist/ into container '$CONTAINER' ..."
docker cp "$APP_DIR/dist/." "$CONTAINER:$CONTAINER_HTML/"

echo "✓ Deploy complete → local: http://127.0.0.1:$HOST_PORT · public: https://ibss.curium.dk"
