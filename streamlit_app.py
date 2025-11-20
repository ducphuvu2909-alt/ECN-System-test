import sqlite3
from pathlib import Path

import pandas as pd
import streamlit as st
import streamlit.components.v1 as components

# ================== CẤU HÌNH ĐƯỜNG DẪN ==================
# File DB ECN (SQLite)
DB_PATH = Path("ecn.db")

# File giao diện hệ thống ECN Manager (bản HTML của WebApp .NET)
ECN_HTML_PATH = Path("src/WebApp/wwwroot/ecn.html")


# ================== HÀM DB TIỆN ÍCH ==================
@st.cache_resource
def get_connection():
    """
    Lấy connection tới ecn.db.
    Nếu chưa có file DB thì tạo rỗng để tránh lỗi kết nối.
    """
    if not DB_PATH.exists():
        conn = sqlite3.connect(DB_PATH)
        conn.close()
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    return conn


def users_table_exists() -> bool:
    """Kiểm tra xem có bảng Users trong ecn.db hay không."""
    conn = get_connection()
    cur = conn.cursor()
    cur.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name='Users';"
    )
    row = cur.fetchone()
    return row is not None


def check_login(username: str, password: str) -> bool:
    """
    Logic login:
    1. Nếu có bảng Users trong DB:
       - cố gắng dùng cột Username / Password (giản đơn).
    2. Nếu không có / lỗi:
       - fallback account demo: admin / 123456
    """
    conn = get_connection()
    try:
        if users_table_exists():
            df = pd.read_sql_query("SELECT * FROM Users", conn)
            # tìm cột khả dụng
            cols = [c.lower() for c in df.columns]

            # map tên cột linh hoạt một chút
            def find_col(names):
                for n in names:
                    if n.lower() in cols:
                        return df.columns[cols.index(n.lower())]
                return None

            user_col = find_col(["Username", "UserName", "Login", "User"])
            pass_col = find_col(["Password", "Pass", "Pwd"])

            if user_col and pass_col:
                row = df[
                    (df[user_col] == username) &
                    (df[pass_col] == password)
                ]
                if not row.empty:
                    return True
    except Exception:
        # có vấn đề gì thì bỏ qua, dùng fallback
        pass

    # Fallback demo nếu không check được từ DB
    return username == "admin" and password == "123456"


# ================== STREAMLIT APP ==================
st.set_page_config(
    page_title="ECN Manager - Login",
    layout="wide",
)

# Trạng thái session
if "logged_in" not in st.session_state:
    st.session_state.logged_in = False
if "username" not in st.session_state:
    st.session_state.username = None


# ---------- MÀN HÌNH LOGIN ----------
if not st.session_state.logged_in:
    st.title("ECN Manager – Đăng nhập")

    # Thông tin DB cho anh dễ debug
    st.caption(f"DB path đang dùng: `{DB_PATH}`")

    with st.form("login_form", clear_on_submit=False):
        username = st.text_input("Username", value="", key="login_user")
        password = st.text_input("Password", type="password", value="", key="login_pass")
        submitted = st.form_submit_button("Login")

        if submitted:
            if check_login(username, password):
                st.session_state.logged_in = True
                st.session_state.username = username
                st.success("Đăng nhập thành công.")
                st.rerun()
            else:
                st.error("Sai username hoặc password.")

    st.stop()  # dừng, không render phần dưới khi chưa login


# ---------- SAU KHI LOGIN: VÀO THẲNG ECN.HTML ----------
# Sidebar nhỏ để logout
with st.sidebar:
    st.success(f"Logged in as: {st.session_state.username}")
    if st.button("Logout"):
        st.session_state.logged_in = False
        st.session_state.username = None
        st.rerun()

st.title("ECN Manager – Giao diện hệ thống (ecn.html)")

# Kiểm tra file ecn.html
if not ECN_HTML_PATH.exists():
    st.error(f"Không tìm thấy file ecn.html tại: `{ECN_HTML_PATH}`")
    st.info(
        "Hãy kiểm tra lại cấu trúc repo. "
        "Mặc định code đang tìm: src/WebApp/wwwroot/ecn.html"
    )
else:
    html = ECN_HTML_PATH.read_text(encoding="utf-8")

    st.info(
        "Đây là giao diện hệ thống ECN Manager (`ecn.html`) được nhúng trực tiếp vào Streamlit.\n"
        "Các API `/api/...` của backend .NET sẽ không hoạt động trong môi trường Streamlit, "
        "nhưng giao diện và logic JavaScript phía client vẫn có thể dùng để demo / test UI."
    )

    # Nhúng nguyên file ecn.html vào trong Streamlit
    components.html(html, height=900, scrolling=True)


