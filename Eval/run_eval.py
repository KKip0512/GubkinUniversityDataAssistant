import argparse
import datetime as dt
import decimal
import json
import re
import sys
import time
import urllib.request
from pathlib import Path

import psycopg
import yaml

UNAVAILABLE = "Сервис временно недоступен"
FLOAT_TOLERANCE = 0.051


def ask(api, question):
    request = urllib.request.Request(
        api,
        data=json.dumps({"question": question}).encode("utf-8"),
        headers={"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(request, timeout=600) as response:
        return json.load(response)


def run_gold(conn, sql):
    with conn.cursor() as cur:
        cur.execute(sql)
        rows = cur.fetchall()
    return [list(row) for row in rows]


def normalize(value):
    if value is None or isinstance(value, bool):
        return value
    if isinstance(value, (int, float, decimal.Decimal)):
        return float(value)
    if isinstance(value, (dt.date, dt.datetime)):
        value = value.isoformat()
    text = str(value).strip()
    return text[:10] if re.fullmatch(r"\d{4}-\d{2}-\d{2}T00:00:00", text) else text


def sort_key(value):
    if value is None:
        return 0, ""
    if isinstance(value, bool):
        return 1, value
    if isinstance(value, float):
        return 2, value
    return 3, value


def columns(rows):
    width = len(rows[0]) if rows else 0
    return [sorted((normalize(row[i]) for row in rows), key=sort_key) for i in range(width)]


def same_value(a, b):
    if isinstance(a, float) and isinstance(b, float):
        return abs(a - b) <= FLOAT_TOLERANCE
    return a == b


def same_column(a, b):
    return len(a) == len(b) and all(same_value(x, y) for x, y in zip(a, b))


def compare(gold_rows, rows):
    if len(rows) != len(gold_rows):
        return f"строк {len(rows)}, в эталоне {len(gold_rows)}"
    if not gold_rows:
        return None
    candidates = columns(rows)
    used = set()
    for gold in columns(gold_rows):
        match = next((i for i, c in enumerate(candidates) if i not in used and same_column(gold, c)), None)
        if match is None:
            sample = ", ".join(repr(v) for v in gold[:3])
            return f"нет колонки со значениями эталона ({sample}, ...)"
        used.add(match)
    return None


def check(item, conn, api):
    started = time.time()
    response = ask(api, item["question"])
    elapsed = time.time() - started
    error = response.get("error")

    if error and UNAVAILABLE in error:
        return False, f"ошибка сервиса: {error}", response, elapsed

    if item.get("no_answer"):
        if response["rows"]:
            return False, "модель должна была отказаться, но вернула данные", response, elapsed
        return True, None, response, elapsed

    if error:
        return False, f"ошибка: {error}", response, elapsed

    reason = compare(run_gold(conn, item["sql"]), response["rows"])
    return reason is None, reason, response, elapsed


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description="Проверка качества генерации SQL")
    parser.add_argument("--api", default="http://localhost:8080/api/query")
    parser.add_argument("--db", default="host=localhost port=5433 dbname=university "
                                        "user=readonly_user password=readonly_user")
    parser.add_argument("--questions", default=str(Path(__file__).with_name("questions.yaml")))
    parser.add_argument("--only", help="id вопросов через запятую")
    parser.add_argument("--report", help="сохранить подробный отчёт в JSON")
    args = parser.parse_args()

    items = yaml.safe_load(Path(args.questions).read_text(encoding="utf-8"))
    if args.only:
        wanted = set(args.only.split(","))
        items = [i for i in items if i["id"] in wanted]

    results = []
    with psycopg.connect(args.db, autocommit=True) as conn:
        for n, item in enumerate(items, start=1):
            ok, reason, response, elapsed = check(item, conn, args.api)
            results.append(dict(id=item["id"], question=item["question"], ok=ok, reason=reason,
                                sql=response.get("sql"), seconds=round(elapsed, 1)))
            print(f"[{n:>2}/{len(items)}] {'OK  ' if ok else 'FAIL'} {item['id']} ({elapsed:.1f} c)")
            if not ok:
                print(f"       вопрос: {item['question']}")
                print(f"       причина: {reason}")
                if response.get("sql"):
                    print("       SQL: " + response["sql"].replace("\n", "\n            "))

    passed = sum(r["ok"] for r in results)
    total = len(results)
    print(f"\nВерно: {passed} из {total} ({100 * passed / max(total, 1):.0f}%), "
          f"среднее время {sum(r['seconds'] for r in results) / max(total, 1):.1f} c")

    if args.report:
        Path(args.report).write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
