#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="/opt/apps/myapp"
PUBLISH_DIR="$APP_ROOT/api/publish"

echo "Deploy backend: start"
case "$PUBLISH_DIR" in "/"|"/opt"|"/opt/"|"/opt/apps"|"/opt/apps/"|"/opt/apps/myapp"|"/opt/apps/myapp/") echo "ERROR: deploy dir too broad: $PUBLISH_DIR"; exit 3;; esac
mkdir -p "$PUBLISH_DIR"

if [ ! -f "$PUBLISH_DIR/Api.dll" ]; then
  echo "ERROR: Api.dll not found in $PUBLISH_DIR"
  ls -la "$PUBLISH_DIR" || true
  exit 4
fi

echo "Restarting service..."
sudo systemctl restart myapp-api

echo "Health check..."
curl -fsS http://127.0.0.1:5000/health >/dev/null && echo "OK: health" || (echo "ERROR: health failed"; sudo journalctl -u myapp-api -n 120 --no-pager; exit 5)

echo "Deploy backend: done"
