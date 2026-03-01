#!/usr/bin/env bash
set -euo pipefail
TS=$(date +"%Y%m%d_%H%M%S")
OUT="/opt/apps/myapp/backups/workshopdb_${TS}.sql.gz"
sudo -u postgres pg_dump workshopdb | gzip > "$OUT"
echo "Backup: $OUT"
