#!/usr/bin/env bash
# =============================================================================
# deploy.sh — IBSS Backend Deployer
# =============================================================================
#
# WHAT THIS SCRIPT DOES:
#   1. Syncs the backend source tree to the remote host (excludes bin/obj/db).
#   2. Builds the Docker image on the remote.
#   3. Stops & removes the old ibss-backend container if present.
#   4. Recreates it with:
#        - host port 5051 → container port 5103 (the global nginx on the box
#          terminates TLS for ibssapi.curium.dk and forwards to :5051)
#        - docker volume `ibss-data` mounted at /app/data (uploads only;
#          the database lives in PostgreSQL on 192.168.1.201)
#        - production env vars (secrets baked into the script, not the image)
#
# NOTE: the frontend container is not touched — it reaches the backend via
# the public URL (ibssapi.curium.dk), not via docker networking.
#
# USAGE:
#   ./deploy.sh
#
# =============================================================================

set -euo pipefail

REMOTE="192.168.1.77"
REMOTE_SRC="/tmp/ibss-backend-src"
IMAGE="ibss-backend:latest"
CONTAINER="ibss-backend"
VOLUME="ibss-data"
HOST_PORT="5051"
PUBLIC_ORIGIN="https://ibss.curium.dk"
PUBLIC_DOMAIN="ibss.curium.dk"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Secrets live in backend/.env (gitignored). Copy .env.example → .env and fill
# it in. Required: ENCRYPTION_FIELD_KEY, PG_CONNECTION_STRING, BREVO_SMTP_*.
ENV_FILE="$SCRIPT_DIR/.env"
if [ ! -f "$ENV_FILE" ]; then
  echo "✗ Missing $ENV_FILE — copy backend/.env.example to backend/.env and fill in the production secrets." >&2
  exit 1
fi
set -a
# shellcheck disable=SC1090
. "$ENV_FILE"
set +a

# Fail early with a clear message if a required secret is unset.
: "${ENCRYPTION_FIELD_KEY:?set in backend/.env}"
: "${PG_CONNECTION_STRING:?set in backend/.env}"
: "${BREVO_SMTP_PASSWORD:?set in backend/.env}"

echo "▶ Syncing backend source to $REMOTE:$REMOTE_SRC ..."
# 'bin\Debug' (literal backslash) is a stray dirname from a prior Windows-style
# build; excluding explicitly since the bin/ pattern misses it.
rsync -a --delete \
  --exclude 'bin/' --exclude 'obj/' --exclude 'bin\Debug' \
  --exclude '**/*.db' --exclude '**/*.db-shm' --exclude '**/*.db-wal' \
  --exclude 'uploads/' --exclude '.git/' --exclude 'appsettings.Development.json' \
  --exclude '.env' --exclude 'node_modules/' \
  "$SCRIPT_DIR/" "$REMOTE:$REMOTE_SRC/"

echo "▶ Building image $IMAGE on remote ..."
ssh "$REMOTE" "cd $REMOTE_SRC && docker build -t $IMAGE ."

echo "▶ Ensuring docker volume '$VOLUME' exists ..."
ssh "$REMOTE" "docker volume inspect $VOLUME >/dev/null 2>&1 || docker volume create $VOLUME"

echo "▶ (Re)starting container $CONTAINER ..."
ssh "$REMOTE" "docker rm -f $CONTAINER 2>/dev/null || true"
ssh "$REMOTE" "docker run -d \
  --name $CONTAINER \
  -p $HOST_PORT:5103 \
  -v $VOLUME:/app/data \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://0.0.0.0:5103 \
  -e ConnectionStrings__DefaultConnection='$PG_CONNECTION_STRING' \
  -e Storage__Provider=Local \
  -e Storage__LocalRoot=/app/data/uploads \
  -e App__Domain=$PUBLIC_DOMAIN \
  -e App__StudentOrigin=$PUBLIC_ORIGIN \
  -e Cors__AllowedOrigins__0=$PUBLIC_ORIGIN \
  -e Encryption__FieldKey=$ENCRYPTION_FIELD_KEY \
  -e Brevo__SmtpHost=$BREVO_SMTP_HOST \
  -e Brevo__SmtpPort=$BREVO_SMTP_PORT \
  -e Brevo__SmtpLogin=$BREVO_SMTP_LOGIN \
  -e Brevo__SmtpPassword=$BREVO_SMTP_PASSWORD \
  -e Brevo__FromEmail=$BREVO_FROM_EMAIL \
  -e Brevo__FromName=Odin \
  --restart unless-stopped \
  $IMAGE"

echo "▶ Waiting for backend health ..."
for i in 1 2 3 4 5 6 7 8 9 10; do
  status=$(ssh "$REMOTE" "curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:$HOST_PORT/v1/public/document-types" 2>/dev/null || echo "000")
  if [ "$status" = "200" ]; then
    echo "✓ Backend up at http://$REMOTE:$HOST_PORT"
    break
  fi
  sleep 2
  [ $i -eq 10 ] && echo "⚠ Backend did not respond within 20s — check: ssh $REMOTE 'docker logs $CONTAINER --tail 50'"
done

echo "✓ Deploy complete → internal: http://$REMOTE:$HOST_PORT · public: https://ibssapi.curium.dk"
