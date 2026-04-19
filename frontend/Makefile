# =============================================================================
# Makefile — IBSS Frontend
# =============================================================================
# Targets:
#   make build    — build the Vue/Vite app locally (output: app/dist/)
#   make deploy   — build + push into the running Docker container on 192.168.1.77
#   make help     — show this message
# =============================================================================

.PHONY: build deploy help

build:
	cd app && npm run build

deploy: build
	./deploy.sh

help:
	@echo "Usage:"
	@echo "  make build    Build the frontend (app/dist/)"
	@echo "  make deploy   Build and deploy to 192.168.1.77:5050 (Docker container)"
