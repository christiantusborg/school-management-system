# syntax=docker/dockerfile:1
#
# Multi-stage build for IBSS backend (ASP.NET Core 10, SQLite).
#
# The DatabaseSeeder shells out to a Node.js script at
#   /Odin.Web/scripts/compute-opaque-credentials.mjs
# to compute OPAQUE credentials for the initial admin user (same crypto as
# the frontend). We stage that script + its npm deps in their own layer and
# install Node in the runtime image so first-boot seeding succeeds.

# ── 1. .NET publish ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish SharedLibrary.Basics.Opaque.Api/SharedLibrary.Basics.Opaque.Api.csproj \
    -c Debug \
    -o /out \
    --nologo

# ── 2. npm install for the seed script ──────────────────────────────────────
FROM node:22-slim AS seed-deps
WORKDIR /Odin.Web
COPY Odin.Web/package.json Odin.Web/package-lock.json ./
COPY Odin.Web/scripts ./scripts
RUN npm ci --omit=dev --no-audit --no-fund

# ── 3. Runtime ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Node.js is required by DatabaseSeeder.cs and partner creation (OPAQUE cred
# generation). Copied from the pinned node:22-slim stage the build already
# uses — no nodesource download, no DNS dependency, and no way to silently
# fall back to Debian's old Node 18 (which once broke partner creation).
COPY --from=seed-deps /usr/local/bin/node /usr/local/bin/node
RUN node -v | grep -q '^v22'

# OCR toolchain for the DocumentScanWorker — same recipe as ~/projects/ocr:
# tesseract + language packs, ghostscript for PDF page rasterisation.
RUN apt-get update && apt-get install -y --no-install-recommends \
        tesseract-ocr tesseract-ocr-dan tesseract-ocr-deu tesseract-ocr-nld \
        tesseract-ocr-nor tesseract-ocr-swe ghostscript \
    && rm -rf /var/lib/apt/lists/*

# Common document fonts for the letter designer: Liberation (Arial/Times/
# Courier metric clones), Carlito (Calibri clone), DejaVu, Noto, URW base35.
RUN apt-get update && apt-get install -y --no-install-recommends \
        fontconfig fonts-liberation fonts-crosextra-carlito fonts-dejavu-core \
        fonts-noto-core fonts-urw-base35 \
    && rm -rf /var/lib/apt/lists/* && fc-cache -f

COPY --from=build /out ./
COPY --from=seed-deps /Odin.Web /Odin.Web

# Persistent data (SQLite + uploads) lives on a mounted volume so container
# restarts don't wipe it.
RUN mkdir -p /app/data/uploads && chown -R $APP_UID /app/data

EXPOSE 5103
ENV ASPNETCORE_URLS=http://0.0.0.0:5103
ENTRYPOINT ["dotnet", "SharedLibrary.Basics.Opaque.Api.dll"]
