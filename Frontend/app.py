# app.py
import os

import pandas as pd
import requests
import streamlit as st

from styles import apply_style, header, examples, bubble

# Адрес бэкенда берём из переменной окружения (в docker-compose это http://backend:8080/api/query).
# Локально, без Docker, бэкенд слушает http://localhost:5136 (см. launchSettings.json).
API_URL = os.getenv("API_URL", "http://localhost:5136/api/query")
# Первый запрос может быть долгим: Ollama загружает модель в память, а бэкенд делает до 2 попыток.
REQUEST_TIMEOUT = int(os.getenv("REQUEST_TIMEOUT", "420"))

UNAVAILABLE = "Сервис временно недоступен. Попробуйте позже."

st.set_page_config(page_title="Ассистент по данным", page_icon="📊", layout="centered")
apply_style()
header("Ассистент по данным", "РГУ нефти и газа (НИУ) имени И.М. Губкина")

if "messages" not in st.session_state:
    st.session_state.messages = []


def ask_backend(question: str) -> dict:
    """Отправляет вопрос бэкенду. Контракт ответа:
    {answer, sql, columns: [str], rows: [[...], ...], error}"""
    try:
        resp = requests.post(API_URL, json={"question": question}, timeout=REQUEST_TIMEOUT)
        resp.raise_for_status()
        return resp.json()
    except Exception:
        return {"answer": "", "sql": "", "columns": [], "rows": [], "error": UNAVAILABLE}


def render_answer(data: dict):
    """Рисует ответ ассистента. Используется и для новых ответов, и для истории чата."""
    if data.get("error"):
        bubble(data["error"], kind="error")
        if data.get("sql"):
            with st.expander("Показать SQL-запрос"):
                st.code(data["sql"], language="sql")
        return

    bubble(data.get("answer", ""))
    if data.get("sql"):
        with st.expander("Показать SQL-запрос"):
            st.code(data["sql"], language="sql")
    if data.get("rows"):
        # Бэкенд отдаёт строки массивами, а названия колонок отдельно
        df = pd.DataFrame(data["rows"], columns=data.get("columns") or None)
        st.dataframe(df)
        st.caption(f"Строк: {len(df)}")
    else:
        st.info("Запрос выполнен, но строк не найдено.")


if not st.session_state.messages:
    examples([
        "Сколько студентов учится на каждом факультете?",
        "Покажи 10 абитуриентов с самым высоким баллом на бюджет",
        "Какой средний балл БРС по каждому курсу?",
        "Кто преподаёт курс «Базы данных»?",
    ])

for msg in st.session_state.messages:
    if msg["role"] == "user":
        bubble(msg["answer"], kind="user")
    else:
        render_answer(msg)

question = st.chat_input("Ваш вопрос к базе данных")

if question:
    st.session_state.messages.append({"role": "user", "answer": question})
    bubble(question, kind="user")

    with st.spinner("Генерирую SQL и выполняю запрос..."):
        data = ask_backend(question)

    render_answer(data)
    st.session_state.messages.append({"role": "assistant", **data})
