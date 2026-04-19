#!/usr/bin/env bash
# =============================================================================
# deploy.sh — IBSS Frontend Deployer
# =============================================================================
#
# WHAT THIS SCRIPT DOES (for humans):
#   1. Builds the Vue 3 / Vite app in app/ (npm run build → app/dist/)
#   2. Copies the dist/ folder to the remote host via rsync
#   3. Injects the new files into the running Docker container with docker cp
#   No Docker rebuild, no nginx changes, no config edits — live hot-swap only.
#
# WHAT THIS SCRIPT DOES (for AI agents):
#   - Working dir: /home/ctu/projects/ibss/frontend
#   - Build cmd:   cd app && npm run build
#   - Remote host: 192.168.1.77  (SSH key auth assumed)
#   - Remote tmp:  /tmp/ibss-dist/
#   - Container:   ibss-frontend  (already running, port 5050→80)
#   - Inject cmd:  docker cp /tmp/ibss-dist/. ibss-frontend:/usr/share/nginx/html/
#   - Side effects: none outside the container's html dir
#
# USAGE:
#   ./deploy.sh
#
# =============================================================================

set -euo pipefail

REMOTE="192.168.1.77"
REMOTE_TMP="/tmp/ibss-dist"
CONTAINER="ibss-frontend"
CONTAINER_HTML="/usr/share/nginx/html"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
APP_DIR="$SCRIPT_DIR/app"

echo "▶ Building app..."
cd "$APP_DIR"
npm run build

echo "▶ Syncing dist/ to $REMOTE:$REMOTE_TMP ..."
rsync -a --delete "$APP_DIR/dist/" "$REMOTE:$REMOTE_TMP/"

echo "▶ Copying into container '$CONTAINER' ..."
ssh "$REMOTE" "docker cp $REMOTE_TMP/. $CONTAINER:$CONTAINER_HTML/"

echo "✓ Deploy complete → http://$REMOTE:5050"
