#!/bin/bash
# Пользователь только для чтения, под которым бэкенд ходит в БД.
# Персональные колонки (паспорт, телефон, e-mail, адрес) закрыты на уровне БД:
# даже если модель сгенерирует SELECT по ним, PostgreSQL ответит "permission denied".
set -euo pipefail

: "${READONLY_USER:?READONLY_USER не задан}"
: "${READONLY_PASSWORD:?READONLY_PASSWORD не задан}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
     -v ro_user="$READONLY_USER" -v ro_pass="$READONLY_PASSWORD" -v db="$POSTGRES_DB" <<'SQL'
SELECT format('CREATE ROLE %I LOGIN PASSWORD %L', :'ro_user', :'ro_pass') \gexec
SELECT format('GRANT CONNECT ON DATABASE %I TO %I', :'db', :'ro_user') \gexec
SELECT format('GRANT USAGE ON SCHEMA public TO %I', :'ro_user') \gexec

-- Таблицы без персональных данных: читать можно всё
SELECT format('GRANT SELECT ON public.faculties, public.education_levels, public.programs, public.subjects, public.curriculum, public.streams, public.student_groups, public.ege_scores, public.applications, public.grades, public.lectures, public.seminars TO %I', :'ro_user') \gexec

-- Таблицы с персональными данными: только безопасные колонки
SELECT format('GRANT SELECT (student_id, last_name, first_name, middle_name, gender, birth_date, program_id, group_id, admission_year, course, study_form, is_budget, military_training, military_start_year, military_end_year) ON public.students TO %I', :'ro_user') \gexec
SELECT format('GRANT SELECT (employee_id, last_name, first_name, middle_name, gender, birth_date, faculty_id, role, academic_degree, hire_year, employment_form) ON public.employees TO %I', :'ro_user') \gexec

-- На случай, если модель сгенерирует что-то опасное: ни записи, ни долгих запросов
SELECT format('ALTER ROLE %I SET default_transaction_read_only = on', :'ro_user') \gexec
SELECT format('ALTER ROLE %I SET statement_timeout = ''15s''', :'ro_user') \gexec
SQL

echo ">> пользователь ${READONLY_USER} создан"
