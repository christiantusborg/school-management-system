#!/usr/bin/env bash
# =============================================================================
# deploy.sh — IBSS PDF service (PyMuPDF field extraction / form fill)
# Third container on the box. The backend proxies to it at
# http://192.168.1.77:8081 (PdfService__BaseUrl); it is never exposed
# through nginx.
# USAGE: ./deploy.sh
# =============================================================================
set -euo pipefail
REMOTE="192.168.1.77"
REMOTE_SRC="/tmp/ibss-pdf-service-src"
IMAGE="ibss-pdf-service:latest"
CONTAINER="ibss-pdf-service"
HOST_PORT="8081"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "▶ Syncing pdf-service source to $REMOTE:$REMOTE_SRC ..."
rsync -a --delete --exclude 'venv/' "$SCRIPT_DIR/" "$REMOTE:$REMOTE_SRC/"

echo "▶ Building image $IMAGE on remote ..."
ssh "$REMOTE" "cd $REMOTE_SRC && docker build -t $IMAGE ."

echo "▶ (Re)starting container $CONTAINER ..."
ssh "$REMOTE" "docker rm -f $CONTAINER 2>/dev/null || true"
ssh "$REMOTE" "docker run -d --name $CONTAINER -p $HOST_PORT:8080 --restart unless-stopped $IMAGE"

echo "▶ Waiting for health ..."
for i in 1 2 3 4 5 6 7 8 9 10; do
  status=$(ssh "$REMOTE" "curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:$HOST_PORT/health" 2>/dev/null || echo "000")
  [ "$status" = "200" ] && { echo "✓ pdf-service up at http://$REMOTE:$HOST_PORT"; exit 0; }
  sleep 2
done
echo "⚠ pdf-service did not respond — check: ssh $REMOTE 'docker logs $CONTAINER --tail 50'"
