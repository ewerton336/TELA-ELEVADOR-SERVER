#!/usr/bin/env bash
set -euo pipefail

DEPLOY_DIR="${1:-/opt/tela-elevador-server}"
API_PORT="${2:-8080}"
PACKAGE_PATH="${3:-/tmp/tela-elevador-server-deploy.tar.gz}"

echo "=== Extracting code ==="
mkdir -p "$DEPLOY_DIR"
tar -xzf "$PACKAGE_PATH" -C "$DEPLOY_DIR"
rm -f "$PACKAGE_PATH"

echo "=== Rebuilding and starting compose ==="
cd "$DEPLOY_DIR"
export COMPOSE_DOCKER_CLI_BUILD=1
export DOCKER_BUILDKIT=1
docker compose up -d --build --remove-orphans

echo "=== Waiting startup ==="
sleep 15

echo "=== Compose status ==="
docker compose ps

echo "=== API connectivity check ==="
HTTP_STATUS="$(curl -s -o /dev/null -w '%{http_code}' "http://localhost:${API_PORT}" || true)"
if [ -z "$HTTP_STATUS" ] || [ "$HTTP_STATUS" = "000" ]; then
  echo "API did not respond on localhost:${API_PORT}"
  docker compose logs --tail=120 api || true
  exit 1
fi

echo "API responded with HTTP ${HTTP_STATUS}"

echo "=== API container stability check ==="
API_STATE="$(docker inspect -f '{{.State.Status}}|{{.State.Restarting}}|{{.State.ExitCode}}' elevador_api 2>/dev/null || true)"
echo "API state: ${API_STATE}"
echo "$API_STATE" | grep -q '^running|false|0$' || {
  echo "API container is not stable"
  docker compose logs --tail=120 api || true
  exit 1
}

echo "=== Worker running check ==="
RUNNING_SERVICES="$(docker compose ps --services --filter status=running)"
echo "$RUNNING_SERVICES"
echo "$RUNNING_SERVICES" | grep -q '^worker$' || {
  echo "Worker service is not running"
  docker compose logs --tail=120 worker || true
  exit 1
}

echo "=== Deploy remoto concluido ==="
