#!/bin/bash
# Загрузка данных. Порядок важен из-за внешних ключей.
# Файлы *_rows.sql смонтированы в /seed (см. docker-compose.yml).
set -euo pipefail

SEED_DIR="${SEED_DIR:-/seed}"

for table in faculties employees programs courses students applications exam_scores grades; do
  echo ">> загружаю ${table}"
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
       -f "${SEED_DIR}/${table}_rows.sql"
done

echo ">> данные загружены"
