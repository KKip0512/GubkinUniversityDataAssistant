import html
from pathlib import Path

import streamlit as st

ACCENT = "#2a53a4"
INK    = "#212529"
MUTED  = "#6c757d"
LINE   = "#dee2e6"
CARD_BG = "#ffffff"


def apply_style():
    css = Path(__file__).parent.joinpath("style.css").read_text(encoding="utf-8")
    st.markdown(f"<style>{css}</style>", unsafe_allow_html=True)


def header(title: str, subtitle: str):
    st.markdown(f"""
    <div style="background:#ffffff; border-bottom:1px solid {LINE};
                padding:20px 0 18px 0; margin-bottom:26px;">
        <div style="font-size:34px; font-weight:800; color:{INK};
                    line-height:1.15; letter-spacing:-0.5px;">
            {title}
        </div>
        <div style="font-size:15px; color:{MUTED}; margin-top:6px;">
            {subtitle}
        </div>
        <div style="height:4px; width:96px; background:{ACCENT};
                    margin-top:16px;"></div>
    </div>
    """, unsafe_allow_html=True)


def examples(items):
    rows = "".join(
        f'<li style="margin-bottom:10px; color:{INK}; font-size:16px;">{i}</li>'
        for i in items
    )
    st.markdown(f"""
    <div style="background:{CARD_BG}; border:1px solid {LINE};
                border-left:4px solid {ACCENT}; padding:18px 22px;
                margin-bottom:12px;">
        <div style="font-weight:700; color:{INK}; font-size:17px;
                    margin-bottom:12px;">Примеры вопросов</div>
        <ul style="margin:0; padding-left:20px;">{rows}</ul>
    </div>
    """, unsafe_allow_html=True)


def bubble(text: str, kind: str = "normal"):
    # Текст приходит от пользователя/БД/модели, а вставляется в HTML: экранируем, иначе '<' ломает вёрстку
    text = html.escape(str(text))
    if kind == "error":
        bg, border, color = "#fdf2f2", "#c1121f", "#922b21"
    elif kind == "user":
        bg, border, color = "#eef3fb", ACCENT, INK
    else:
        bg, border, color = CARD_BG, ACCENT, INK
    st.markdown(f"""
    <div style="background:{bg}; border:1px solid {LINE};
                border-left:4px solid {border}; padding:16px 20px;
                margin-bottom:12px; color:{color}; font-size:16px;
                line-height:1.5;">
        {text}
    </div>
    """, unsafe_allow_html=True)