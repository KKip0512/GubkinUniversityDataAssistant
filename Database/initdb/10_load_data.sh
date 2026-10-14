#!/bin/bash
# Загрузка данных. Порядок важен из-за внешних ключей.
# Файлы *_rows.sql смонтированы в /seed (см. docker-compose.yml).
set -euo pipefail

SEED_DIR="${SEED_DIR:-/seed}"

for table in faculties education_levels programs subjects curriculum streams student_groups employees students \
             ege_scores applications grades lectures seminars; do
  echo ">> загружаю ${table}"
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
       -f "${SEED_DIR}/${table}_rows.sql"
done

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -c "ANALYZE"

echo ">> данные загружены"
