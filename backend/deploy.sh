#!/usr/bin/env bash
# =============================================================================
# deploy.sh — IBSS Backend Deployer (LOCAL)
# =============================================================================
#
# The app now runs ON THIS MACHINE (host "Base", 192.168.1.41). The l-node01
# nginx gateway (192.168.1.30) proxies the public domains to this host's
# ports, so deploying = build the image here and (re)start the container here.
# No SSH, no remote host.
#
# WHAT THIS SCRIPT DOES:
#   1. Clean-copies the backend source to a temp dir (excludes bin/obj so a
#      stale local build can't poison the Docker build).
#   2. Builds the Docker image locally.
#   3. (Re)starts the ibss-backend container:
#        - host port 5051 -> container 5103 (nginx on the gateway forwards
#          ibssapi.curium.dk here)
#        - docker volume `ibss-data` at /app/data (uploads; the database is
#          PostgreSQL on 192.168.1.201)
#        - production env from backend/.env
#
# USAGE:  ./deploy.sh
# =============================================================================

set -euo pipefail

APP_HOST="192.168.1.41"          # this machine's LAN IP (used for PdfService URL)
BUILD_SRC="/tmp/ibss-be-src"     # clean copy the Docker build runs against
IMAGE="ibss-backend:latest"
CONTAINER="ibss-backend"
VOLUME="ibss-data"
HOST_PORT="5051"
PDF_PORT="8081"
PUBLIC_ORIGIN="https://ibss.curium.dk"
PUBLIC_DOMAIN="ibss.curium.dk"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Secrets live in backend/.env (gitignored).
ENV_FILE="$SCRIPT_DIR/.env"
if [ ! -f "$ENV_FILE" ]; then
  echo "✗ Missing $ENV_FILE — copy backend/.env.example to backend/.env and fill in the production secrets." >&2
  exit 1
fi
set -a
# shellcheck disable=SC1090
. "$ENV_FILE"
set +a
: "${ENCRYPTION_FIELD_KEY:?set in backend/.env}"
: "${PG_CONNECTION_STRING:?set in backend/.env}"
: "${BREVO_SMTP_PASSWORD:?set in backend/.env}"

echo "▶ Clean-copying backend source to $BUILD_SRC ..."
rm -rf "$BUILD_SRC"
rsync -a \
  --exclude 'bin/' --exclude 'obj/' --exclude 'bin\Debug' \
  --exclude '**/*.db' --exclude '**/*.db-shm' --exclude '**/*.db-wal' \
  --exclude 'uploads/' --exclude '.git/' --exclude 'appsettings.Development.json' \
  --exclude '.env' --exclude 'node_modules/' \
  "$SCRIPT_DIR/" "$BUILD_SRC/"

echo "▶ Building image $IMAGE ..."
docker build -t "$IMAGE" "$BUILD_SRC"

echo "▶ Ensuring docker volume '$VOLUME' exists ..."
docker volume inspect "$VOLUME" >/dev/null 2>&1 || docker volume create "$VOLUME"

echo "▶ (Re)starting container $CONTAINER ..."
docker rm -f "$CONTAINER" 2>/dev/null || true
docker run -d \
  --name "$CONTAINER" \
  -p "$HOST_PORT:5103" \
  -v "$VOLUME:/app/data" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://0.0.0.0:5103 \
  -e ConnectionStrings__DefaultConnection="$PG_CONNECTION_STRING" \
  -e Storage__Provider=Local \
  -e PdfService__BaseUrl="http://$APP_HOST:$PDF_PORT" \
  -e DocumentScan__OllamaUrl="${DOCSCAN_OLLAMA_URL:-}" \
  -e DocumentScan__OllamaModel="${DOCSCAN_OLLAMA_MODEL:-llama3.2:latest}" \
  -e Storage__LocalRoot=/app/data/uploads \
  -e App__Domain="$PUBLIC_DOMAIN" \
  -e App__StudentOrigin="$PUBLIC_ORIGIN" \
  -e Cors__AllowedOrigins__0="$PUBLIC_ORIGIN" \
  -e Cors__AllowedOrigins__1=https://hub.mgworld.academy \
  -e Encryption__FieldKey="$ENCRYPTION_FIELD_KEY" \
  -e Brevo__SmtpHost="$BREVO_SMTP_HOST" \
  -e Brevo__SmtpPort="$BREVO_SMTP_PORT" \
  -e Brevo__SmtpLogin="$BREVO_SMTP_LOGIN" \
  -e Brevo__SmtpPassword="$BREVO_SMTP_PASSWORD" \
  -e Brevo__FromEmail="$BREVO_FROM_EMAIL" \
  -e Brevo__FromName=Odin \
  --restart unless-stopped \
  "$IMAGE"

echo "▶ Waiting for backend health ..."
status="000"
for i in $(seq 1 12); do
  status=$(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:$HOST_PORT/v1/public/document-types" 2>/dev/null || echo "000")
  [ "$status" = "200" ] && { echo "✓ Backend up at http://$APP_HOST:$HOST_PORT"; break; }
  sleep 2
  [ "$i" -eq 12 ] && echo "⚠ Backend did not respond within 24s — check: docker logs $CONTAINER --tail 50"
done

echo "✓ Deploy complete → local: http://127.0.0.1:$HOST_PORT · public: https://ibssapi.curium.dk"
