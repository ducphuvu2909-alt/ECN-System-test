import sqlite3
from pathlib import Path

import pandas as pd
import streamlit as st
import streamlit.components.v1 as components

# ========= CẤU HÌNH ĐƯỜNG DẪN =========
# DB ECN (SQLite) – mặc định nằm ở cùng thư mục với streamlit_app.py
DB_PATH = Path("ecn.db")

# File HTML ECN Manager (bản .NET UI)
ECN_HTML_PATH = Path("ECN_Manager_Fullcode/src/WebApp/wwwroot/ecn.html")


# ========= HÀM TIỆN ÍCH DB =========
@st.cache_resource
def get_connection():
    if not DB_PATH.exists():
        raise FileNotFoundError(f"Không tìm thấy file DB: {DB_PATH}")
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    return conn


def load_table(table_name: str) -> pd.DataFrame:
    """Đọc 1 bảng bất kỳ trong ecn.db, nếu không tồn tại thì trả DF rỗng."""
    conn = get_connection()
    try:
        df = pd.read_sql_query(f"SELECT * FROM {table_name}", conn)
    except Exception:
        df = pd.DataFrame()
    return df


# ========= GIAO DIỆN CHÍNH =========
st.set_page_config(
    page_title="ECN Manager - Streamlit",
    layout="wide",
)

st.title("ECN Manager – Streamlit Console")
st.caption("Bản dashboard đơn giản để xem & test hệ thống ECN trên môi trường Streamlit")


# ========= SIDEBAR: MENU =========
with st.sidebar:
    st.header("Navigation")
    page = st.radio(
        "Chọn module",
        [
            "📊 Dashboard tổng",
            "👤 Admin Users",
            "🧩 Admin Jobs / Scheduler",
            "📣 Admin Notifications",
            "🧱 ECN HTML Prototype",
            "🛠 SQL Explorer (advanced)",
        ],
    )
    st.markdown("---")
    st.write("DB path:", f"`{DB_PATH}`")


# ========= PAGE: DASHBOARD =========
if page == "📊 Dashboard tổng":
    st.subheader("Tổng quan ECN Manager (đọc từ ecn.db)")

    if not DB_PATH.exists():
        st.error(f"Không tìm thấy file DB: `{DB_PATH}`.\n\nHãy copy `ecn.db` từ WebApp sang cùng thư mục với `streamlit_app.py`.")
    else:
        col1, col2, col3, col4 = st.columns(4)

        df_ecn = load_table("ECNs")
        df_users = load_table("AdminUserConfigs")
        df_jobs = load_table("AdminJobs")
        df_notify = load_table("AdminNotificationSubscriptions")

        with col1:
            st.metric("Số lượng ECN", len(df_ecn) if not df_ecn.empty else 0)
        with col2:
            st.metric("Số user Admin", len(df_users) if not df_users.empty else 0)
        with col3:
            st.metric("Số job Scheduler", len(df_jobs) if not df_jobs.empty else 0)
        with col4:
            st.metric("Số subscription notify", len(df_notify) if not df_notify.empty else 0)

        st.markdown("### Chi tiết nhanh")
        tab1, tab2 = st.tabs(["ECN gần nhất", "Job & Notify"])

        with tab1:
            if df_ecn.empty:
                st.info("Chưa có bảng `ECNs` hoặc chưa có dữ liệu.")
            else:
                df_ecn_sorted = df_ecn.sort_values(by=df_ecn.columns[0], ascending=False)
                st.dataframe(df_ecn_sorted.head(20), use_container_width=True)

        with tab2:
            c1, c2 = st.columns(2)
            with c1:
                st.markdown("**AdminJobs**")
                if df_jobs.empty:
                    st.info("Chưa có bảng `AdminJobs` hoặc chưa có dữ liệu.")
                else:
                    st.dataframe(df_jobs, use_container_width=True, height=300)
            with c2:
                st.markdown("**AdminNotificationSubscriptions**")
                if df_notify.empty:
                    st.info("Chưa có bảng `AdminNotificationSubscriptions` hoặc chưa có dữ liệu.")
                else:
                    st.dataframe(df_notify, use_container_width=True, height=300)


# ========= PAGE: ADMIN USERS =========
elif page == "👤 Admin Users":
    st.subheader("Admin Users – Cấu hình người dùng & Global ID")

    if not DB_PATH.exists():
        st.error(f"Không tìm thấy file DB: `{DB_PATH}`.")
    else:
        df = load_table("AdminUserConfigs")
        if df.empty:
            st.info("Chưa có bảng `AdminUserConfigs` hoặc chưa có dữ liệu. Hãy dùng ECN Admin (trong WebApp) để tạo trước.")
        else:
            st.dataframe(df, use_container_width=True)

        st.markdown("### Thêm user mới (demo ghi trực tiếp DB)")
        with st.form("add_admin_user_form"):
            col1, col2 = st.columns(2)
            with col1:
                name = st.text_input("Họ tên")
                gid = st.text_input("Global ID")
                email = st.text_input("Email")
            with col2:
                dept = st.text_input("Phòng ban", value="FE")
                role = st.text_input("Vai trò", value="Viewer")
                status = st.selectbox("Trạng thái", ["Active", "Suspended"])
            note = st.text_area("Ghi chú", height=60)

            submitted = st.form_submit_button("Lưu vào DB")
            if submitted:
                if not name or not gid:
                    st.warning("Cần nhập Họ tên và Global ID.")
                else:
                    conn = get_connection()
                    cur = conn.cursor()
                    cur.execute(
                        """
                        INSERT INTO AdminUserConfigs (Name, GlobalId, Email, Dept, Role, Status, Note)
                        VALUES (?, ?, ?, ?, ?, ?, ?)
                        """,
                        (name, gid, email, dept, role, status, note),
                    )
                    conn.commit()
                    st.success("Đã thêm user mới vào AdminUserConfigs.")
                    st.experimental_rerun()


# ========= PAGE: ADMIN JOBS =========
elif page == "🧩 Admin Jobs / Scheduler":
    st.subheader("Admin Jobs / Scheduler – Cấu hình job sync SAP & ECN")

    if not DB_PATH.exists():
        st.error(f"Không tìm thấy file DB: `{DB_PATH}`.")
    else:
        df = load_table("AdminJobs")
        if df.empty:
            st.info("Chưa có bảng `AdminJobs` hoặc chưa có dữ liệu. Hãy tạo job từ ECN Admin hoặc form dưới.")
        else:
            st.dataframe(df, use_container_width=True)

        st.markdown("### Thêm job mới (demo ghi trực tiếp DB)")
        with st.form("add_job_form"):
            name = st.text_input("Tên Job", value="SAP Valid BOM Sync")
            jtype = st.text_input("Loại Job", value="SAP Valid BOM Sync")
            src = st.text_input("Nguồn dữ liệu (share path / URL)", value="\\\\sap-share\\ECN\\export_valid_bom.xlsx")
            schedule = st.text_input("Lịch chạy (mô tả text)", value="Mỗi 15 phút")
            enabled = st.checkbox("Bật job (Enabled)", value=True)
            note = st.text_area("Ghi chú", height=60)

            submitted = st.form_submit_button("Lưu vào DB")
            if submitted:
                conn = get_connection()
                cur = conn.cursor()
                cur.execute(
                    """
                    INSERT INTO AdminJobs (Name, Type, SourcePath, Schedule, Enabled, Note)
                    VALUES (?, ?, ?, ?, ?, ?)
                    """,
                    (name, jtype, src, schedule, 1 if enabled else 0, note),
                )
                conn.commit()
                st.success("Đã thêm job mới vào AdminJobs.")
                st.experimental_rerun()


# ========= PAGE: ADMIN NOTIFICATIONS =========
elif page == "📣 Admin Notifications":
    st.subheader("Admin Notifications – Đăng ký nhận cảnh báo ECN")

    if not DB_PATH.exists():
        st.error(f"Không tìm thấy file DB: `{DB_PATH}`.")
    else:
        df = load_table("AdminNotificationSubscriptions")
        if df.empty:
            st.info("Chưa có bảng `AdminNotificationSubscriptions` hoặc chưa có dữ liệu.")
        else:
            st.dataframe(df, use_container_width=True)

        st.markdown("### Thêm subscription mới (demo ghi trực tiếp DB)")
        with st.form("add_notify_form"):
            col1, col2 = st.columns(2)
            with col1:
                name = st.text_input("Người nhận", value="QC Manager")
                email = st.text_input("Email", value="qc.manager@company.com")
                dept = st.text_input("Bộ phận", value="QC")
            with col2:
                evt_valid = st.checkbox("Valid ECN thay đổi", value=True)
                evt_new = st.checkbox("New ECN Effective", value=True)
                evt_deadline = st.checkbox("Deadline ECN sắp tới", value=True)
                evt_joberr = st.checkbox("Job sync lỗi", value=True)

            channel = st.selectbox("Kênh nhận", ["Popup", "Email", "Popup + Email"], index=2)
            freq = st.selectbox("Tần suất", ["Real-time", "Hourly digest", "Daily summary"], index=0)
            note = st.text_area("Ghi chú", height=60)

            submitted = st.form_submit_button("Lưu vào DB")
            if submitted:
                conn = get_connection()
                cur = conn.cursor()
                cur.execute(
                    """
                    INSERT INTO AdminNotificationSubscriptions
                    (Name, Email, Dept, EvtValid, EvtNew, EvtDeadline, EvtJobError, Channel, Frequency, Note)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """,
                    (
                        name,
                        email,
                        dept,
                        1 if evt_valid else 0,
                        1 if evt_new else 0,
                        1 if evt_deadline else 0,
                        1 if evt_joberr else 0,
                        channel,
                        freq,
                        note,
                    ),
                )
                conn.commit()
                st.success("Đã thêm subscription mới.")
                st.experimental_rerun()


# ========= PAGE: ECN HTML PROTOTYPE =========
elif page == "🧱 ECN HTML Prototype":
    st.subheader("Prototype giao diện ECN (ecn.html) bên trong Streamlit")

    if not ECN_HTML_PATH.exists():
        st.error(f"Không tìm thấy file ecn.html tại: `{ECN_HTML_PATH}`")
        st.info("Hãy đảm bảo repo có thư mục `ECN_Manager_Fullcode/src/WebApp/wwwroot/ecn.html`.")
    else:
        html = ECN_HTML_PATH.read_text(encoding="utf-8")
        st.info(
            "Đây là bản HTML tĩnh của ECN Manager (bản .NET). "
            "Một số chức năng gọi API `/api/...` của backend .NET sẽ không hoạt động trong môi trường Streamlit, "
            "nhưng anh có thể dùng để demo giao diện."
        )
        components.html(html, height=900, scrolling=True)


# ========= PAGE: SQL EXPLORER =========
elif page == "🛠 SQL Explorer (advanced)":
    st.subheader("SQL Explorer – Đọc bảng tùy ý trong ecn.db (chỉ nên xem, hạn chế sửa)")

    if not DB_PATH.exists():
        st.error(f"Không tìm thấy file DB: `{DB_PATH}`.")
    else:
        conn = get_connection()
        tables = pd.read_sql_query(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name", conn
        )
        if tables.empty:
            st.info("Database chưa có bảng nào.")
        else:
            tname = st.selectbox(
                "Chọn bảng để xem",
                tables["name"].tolist(),
                index=0,
            )
            st.write(f"**Nội dung bảng `{tname}`:**")
            df = load_table(tname)
            st.dataframe(df, use_container_width=True)
