# -*- coding: utf-8 -*-
"""
Rebuild NIGA_NewAPI/ScriptsAndFiles/Homeocentrum_All_New_And_Updated+APIs.xlsx
as the single API documentation source (controllers + preserved sample payloads).
"""
from __future__ import annotations

import re
from collections import Counter, defaultdict
from datetime import date
from pathlib import Path

from openpyxl import Workbook, load_workbook
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter
from openpyxl.worksheet.table import Table, TableStyleInfo

ROOT = Path(__file__).resolve().parents[2]
NEW_CTRL = ROOT / "NIGA_NewAPI" / "Homeocentrum.Niga.NewAPI" / "Controllers"
OUT = Path(__file__).resolve().parent / "Homeocentrum_All_New_And_Updated+APIs.xlsx"
EXISTING = OUT

NEW_HOST = "http://127.0.0.1:5002"
OLD_HOST = "http://127.0.0.1:5001"

HTTP_ATTR = re.compile(
    r"\[Http(Get|Post|Put|Delete|Patch)(?:\((?:Name\s*=\s*)?[\"']([^\"']*)[\"']\))?\]",
    re.I,
)
ROUTE_ATTR = re.compile(r"\[Route\([\"']([^\"']+)[\"']\)\]")
CLASS_CTRL = re.compile(r"class\s+(\w+)Controller\b")
ALLOW_ANON = re.compile(r"\[AllowAnonymous\]", re.I)

HEADERS = [
    "Sr No",
    "API Endpoint (as in MD)",
    "Method Type",
    "New / Existing / Updated",
    "What was developed this week",
    "MD section",
    "API User",
    "Week",
    "Who should call it",
    "Patient app",
    "Doctor mobile app",
    "Clinic web",
    "Common for all",
    "Host",
    "Token required",
    "Task IDs",
    "What it is used for",
    "Sample request",
    "Sample response",
    "API Number",
    "Source doc",
]


def normalize_path(path: str) -> str:
    s = (path or "").strip()
    s = re.sub(r"^(GET|POST|PUT|DELETE|PATCH)\s+", "", s, flags=re.I)
    s = s.split("?")[0].strip()
    if not s.startswith("/"):
        s = "/" + s
    s = re.sub(r"/+", "/", s)
    # normalize typed params
    s = re.sub(r"\{([^}:]+)[^}]*\}", r"{\1}", s)
    return s.rstrip("/") or "/"


def path_key(method: str, path: str) -> tuple[str, str]:
    return (method.upper(), normalize_path(path).lower())


def join_route(base: str, extra: str) -> str:
    base = (base or "").strip("/")
    extra = (extra or "").strip()
    if not extra:
        return "/" + base
    if extra.lower().startswith("api/") or extra.startswith("/api"):
        return "/" + extra.lstrip("/")
    if extra.startswith("/"):
        return f"/{base}{extra}".replace("//", "/")
    return f"/{base}/{extra}".replace("//", "/")


def extract_controllers(ctrl_dir: Path) -> list[dict]:
    rows: list[dict] = []
    for path in sorted(ctrl_dir.glob("*Controller.cs")):
        if path.name == "BaseAPIController.cs":
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        ctrl_m = CLASS_CTRL.search(text)
        ctrl_name = ctrl_m.group(1) if ctrl_m else path.stem.replace("Controller", "")
        head = text[: text.find("{")]
        class_routes = ROUTE_ATTR.findall(head)
        base = f"api/{ctrl_name}"
        for r in class_routes:
            if "[controller]" in r.lower():
                base = f"api/{ctrl_name}"
                break
            if r.lower().startswith("api"):
                base = r
                break

        # Walk Http* attributes; estimate anonymous if AllowAnonymous nearby above
        for m in HTTP_ATTR.finditer(text):
            http = m.group(1).upper()
            template = m.group(2) or ""
            window = text[max(0, m.start() - 250) : m.end() + 350]
            route_m = ROUTE_ATTR.search(text[m.end() : m.end() + 350])
            extra = route_m.group(1) if route_m else template
            full = join_route(base, extra)
            anon = bool(ALLOW_ANON.search(window))
            rows.append(
                {
                    "method": http,
                    "endpoint": full,
                    "md_endpoint": f"{http} {full}",
                    "file": path.name,
                    "controller": ctrl_name,
                    "token": "no" if anon else "yes",
                    "host": "New-API",
                }
            )
    return rows


def load_existing_payloads(xlsx: Path) -> dict[tuple[str, str], dict]:
    if not xlsx.exists():
        return {}
    wb = load_workbook(xlsx, read_only=True, data_only=True)
    if "All_APIs" not in wb.sheetnames:
        return {}
    ws = wb["All_APIs"]
    rows = list(ws.iter_rows(values_only=True))
    hdr = [str(c).strip() if c else "" for c in (rows[0] or [])]
    idx = {h: i for i, h in enumerate(hdr) if h}
    ep_i = idx.get("API Endpoint (as in MD)", 1)
    meth_i = idx.get("Method Type", 2)
    out: dict[tuple[str, str], dict] = {}
    for row in rows[1:]:
        if not row or ep_i >= len(row) or not row[ep_i]:
            continue
        raw = str(row[ep_i]).strip()
        meth = method_from(raw, row[meth_i] if meth_i < len(row) else "")
        path = normalize_path(raw)
        key = path_key(meth, path)
        data = {}
        for name in (
            "Sample request",
            "Sample response",
            "What it is used for",
            "Task IDs",
            "API User",
            "Week",
            "Patient app",
            "Doctor mobile app",
            "Clinic web",
            "Common for all",
            "Token required",
            "MD section",
            "New / Existing / Updated",
            "What was developed this week",
            "Host",
            "Source doc",
        ):
            i = idx.get(name)
            if i is not None and i < len(row) and row[i] is not None:
                data[name] = str(row[i]).strip()
        # keep best filled
        prev = out.get(key)
        if prev is None or (
            bool(data.get("Sample request")) + bool(data.get("Sample response"))
            > bool(prev.get("Sample request")) + bool(prev.get("Sample response"))
        ):
            out[key] = data
    return out


def method_from(endpoint: str, fallback: str = "") -> str:
    m = re.match(r"^(GET|POST|PUT|DELETE|PATCH)\b", (endpoint or "").strip(), re.I)
    if m:
        return m.group(1).upper()
    return (fallback or "GET").upper()


def classify_week(controller: str, path: str) -> str:
    p = path.lower()
    c = controller.lower()
    if c.startswith("s4") or "/s4" in p:
        return "S4_Week4"
    if any(
        x in p
        for x in (
            "/tele/",
            "/support/",
            "/waitlist",
            "/help",
            "/refill",
            "/fees/",
            "/payments/",
            "/whatsapp",
            "/patientappointment/reschedule",
            "/patientappointment/cancel",
            "/changelog",
        )
    ):
        return "S3_Week3"
    if any(
        x in p
        for x in (
            "/public/",
            "/profile/",
            "/availability",
            "/enquiry",
            "/threed",
            "/doctorprofile",
            "/registration",
        )
    ):
        return "S2_Week2"
    if any(
        x in p
        for x in (
            "/account/",
            "/otp/",
            "/family",
            "/caregiver",
            "/consent",
            "/device/",
            "/patientprofile",
            "/patientportal",
            "/patientauth",
            "/welcome/",
            "/securefile",
            "/securedocument",
            "/adminacl",
        )
    ):
        return "S1_Week1"
    return "Platform"


def classify_apps(path: str, controller: str) -> dict:
    p = path.lower()
    patient = doctor = clinic = common = False
    if any(x in p for x in ("/public/", "/welcome/", "/getlanguages", "/enquiry", "/help", "/health", "/account/login")):
        patient = doctor = clinic = True
        common = True
    elif any(x in p for x in ("/family", "/caregiver", "/patientprofile", "/patientportal", "/patientauth", "/otp/")):
        patient = True
    elif any(x in p for x in ("/tele/", "/doctormobile", "/refill")):
        doctor = True
        patient = "/tele/" in p or "/patient" in p
        clinic = "/tele/" in p
    elif any(x in p for x in ("/reception", "/support/", "/whatsapp", "/fees", "/payments")):
        clinic = True
        doctor = True
    elif any(x in p for x in ("/admin", "/ai", "/knowledgegraph", "/audio", "/masters", "/repertor", "/rubric", "/section", "/subsection", "/remedy")):
        clinic = True
    else:
        clinic = True
        doctor = True
    if patient and doctor and clinic:
        common = True
    return {
        "patient_app": "Yes" if patient else "No",
        "doctor_mobile": "Yes" if doctor else "No",
        "clinic_web": "Yes" if clinic else "No",
        "common_all": "Yes" if common else "No",
    }


def who_calls(flags: dict) -> str:
    if flags.get("common_all") == "Yes":
        return "Common for all"
    parts = []
    if flags.get("patient_app") == "Yes":
        parts.append("Patient app")
    if flags.get("doctor_mobile") == "Yes":
        parts.append("Doctor mobile app")
    if flags.get("clinic_web") == "Yes":
        parts.append("Clinic web")
    return " and ".join(parts) if parts else "Clinic web"


def api_user(flags: dict, path: str) -> str:
    users = []
    p = path.lower()
    if flags.get("patient_app") == "Yes":
        users.append("Patient")
    if flags.get("doctor_mobile") == "Yes" or flags.get("clinic_web") == "Yes":
        if "reception" in p:
            users.append("Reception")
        else:
            users.append("Doctor")
    if "admin" in p or "ai" in p:
        users.append("Admin")
    if any(x in p for x in ("/public/", "/welcome/", "/enquiry", "/account/login", "/register", "/otp/request")):
        users.append("Public")
    if not users:
        users.append("Authenticated")
    seen = []
    for u in users:
        if u not in seen:
            seen.append(u)
    return ", ".join(seen)


def sample_request(method: str, path: str, host: str, preserved: str) -> str:
    if preserved:
        # rewrite old port
        return (
            preserved.replace("localhost:5038", "localhost:5002")
            .replace("127.0.0.1:5038", "127.0.0.1:5002")
        )
    base = NEW_HOST if host.startswith("New") else OLD_HOST
    url = f"{base}{path}"
    if method == "GET":
        return f"GET {url}\nAuthorization: Bearer <token>  # omit if Token=no"
    if method == "DELETE":
        return f"DELETE {url}\nAuthorization: Bearer <token>"
    body = "{\n  /* see controller DTO */\n}"
    if "/login" in path.lower():
        body = '{\n  "userName": "Tufan_Doctor",\n  "password": "123456"\n}'
    elif "/otp/request" in path.lower():
        body = '{\n  "mobile": "7768046064",\n  "purpose": "Login"\n}'
    elif "/whatsapp" in path.lower():
        body = '{\n  "to": "7768046064",\n  "templateName": "appointment_reminder",\n  "languageCode": "en"\n}'
    return f"{method} {url}\nAuthorization: Bearer <token>\nContent-Type: application/json\n\n{body}"


def sample_response(path: str, preserved: str) -> str:
    if preserved:
        return preserved
    if path.lower().endswith("/health") or path.lower() == "/health":
        return '{"success":true,"status":"Healthy","api":"New API"}'
    if "/login" in path.lower():
        return '{"success":true,"data":{"token":"<jwt>","userName":"Tufan_Doctor","role":"Doctor"}}'
    return '{"success":true,"message":"OK","data":{}}'


def build_rows() -> list[dict]:
    existing = load_existing_payloads(EXISTING)
    code = extract_controllers(NEW_CTRL)
    rows: list[dict] = []
    seen = set()

    for c in code:
        key = path_key(c["method"], c["endpoint"])
        if key in seen:
            continue
        seen.add(key)
        prev = existing.get(key, {})
        week = prev.get("Week") or classify_week(c["controller"], c["endpoint"])
        flags = {
            "patient_app": prev.get("Patient app") or "",
            "doctor_mobile": prev.get("Doctor mobile app") or "",
            "clinic_web": prev.get("Clinic web") or "",
            "common_all": prev.get("Common for all") or "",
        }
        if flags["patient_app"] not in ("Yes", "No"):
            flags = classify_apps(c["endpoint"], c["controller"])
        token = prev.get("Token required") or c["token"]
        host = "New-API"
        developed = prev.get("New / Existing / Updated") or "New API developed"
        developed_note = prev.get("What was developed this week") or (
            "HTTP on New-API (catalog regenerated from controllers)."
        )
        use = prev.get("What it is used for") or f"{c['controller']} — {c['method']} {c['endpoint']}"
        section = prev.get("MD section") or c["controller"]
        req = sample_request(c["method"], c["endpoint"], host, prev.get("Sample request", ""))
        res = sample_response(c["endpoint"], prev.get("Sample response", ""))
        rows.append(
            {
                "method": c["method"],
                "endpoint": c["endpoint"],
                "md_endpoint": c["md_endpoint"],
                "developed": developed,
                "developed_note": developed_note,
                "section": section,
                "user": prev.get("API User") or api_user(flags, c["endpoint"]),
                "week": week,
                "who": who_calls(flags),
                **flags,
                "host": host,
                "token": token,
                "tasks": prev.get("Task IDs") or "",
                "use": use,
                "sample_request": req,
                "sample_response": res,
                "source": prev.get("Source doc") or c["file"],
            }
        )

    # Keep Old-API login / health from existing excel if present
    for key, prev in existing.items():
        meth, path = key
        host_prev = (prev.get("Host") or "").lower()
        if key in seen:
            continue
        if "old" not in host_prev and path not in ("/health", "/api/account/login"):
            # only carry classic-only rows that were marked Old-API
            if "old-api" not in host_prev:
                continue
        seen.add(key)
        flags = {
            "patient_app": prev.get("Patient app") or "Yes",
            "doctor_mobile": prev.get("Doctor mobile app") or "Yes",
            "clinic_web": prev.get("Clinic web") or "Yes",
            "common_all": prev.get("Common for all") or "Yes",
        }
        host = "Old-API" if "old" in host_prev else "New-API"
        endpoint = path if path.startswith("/") else "/" + path
        # restore original casing from sample if possible
        md = f"{meth} {endpoint}"
        rows.append(
            {
                "method": meth,
                "endpoint": endpoint,
                "md_endpoint": md,
                "developed": prev.get("New / Existing / Updated") or "Existing URL (keep host)",
                "developed_note": prev.get("What was developed this week") or "Classic/Old-API host kept.",
                "section": prev.get("MD section") or "Old-API",
                "user": prev.get("API User") or "Common for all",
                "week": prev.get("Week") or "Platform",
                "who": who_calls(flags),
                **flags,
                "host": host,
                "token": prev.get("Token required") or "no",
                "tasks": prev.get("Task IDs") or "",
                "use": prev.get("What it is used for") or md,
                "sample_request": sample_request(meth, endpoint, host, prev.get("Sample request", "")),
                "sample_response": sample_response(endpoint, prev.get("Sample response", "")),
                "source": prev.get("Source doc") or "existing catalog",
            }
        )

    # Platform health on New-API
    hk = path_key("GET", "/health")
    if hk not in seen:
        rows.append(
            {
                "method": "GET",
                "endpoint": "/health",
                "md_endpoint": "GET /health",
                "developed": "New API developed",
                "developed_note": "Health check on New-API and Old-API.",
                "section": "Platform",
                "user": "Public",
                "week": "Platform",
                "who": "Common for all",
                "patient_app": "Yes",
                "doctor_mobile": "Yes",
                "clinic_web": "Yes",
                "common_all": "Yes",
                "host": "New-API and Old-API",
                "token": "no",
                "tasks": "",
                "use": "Process health. No login.",
                "sample_request": f"GET {NEW_HOST}/health",
                "sample_response": '{"success":true,"status":"Healthy","api":"New API"}',
                "source": "Program.cs / Startup.cs",
            }
        )

    # assign numbers
    week_counters: Counter = Counter()
    for r in sorted(rows, key=lambda x: (x["week"], x["md_endpoint"])):
        week_counters[r["week"]] += 1
        n = week_counters[r["week"]]
        prefix = {
            "S1_Week1": "S1",
            "S2_Week2": "S2",
            "S3_Week3": "S3",
            "S4_Week4": "S4",
            "Platform": "PLT",
        }.get(r["week"], "API")
        r["api_number"] = f"{prefix}-{n:03d}"
        r["week_sr"] = n
    return sorted(rows, key=lambda x: (x["week"], x["md_endpoint"]))


def style_header(ws):
    fill = PatternFill("solid", fgColor="1F4E79")
    font = Font(color="FFFFFF", bold=True)
    for cell in ws[1]:
        cell.fill = fill
        cell.font = font
        cell.alignment = Alignment(wrap_text=True, vertical="center")


def write_sheet(wb: Workbook, name: str, rows: list[dict], start_sr: int = 1):
    ws = wb.create_sheet(name)
    ws.append(HEADERS)
    style_header(ws)
    for i, r in enumerate(rows, start=start_sr):
        ws.append(
            [
                i,
                r["md_endpoint"],
                r["method"],
                r["developed"],
                r["developed_note"],
                r["section"],
                r["user"],
                r["week"],
                r["who"],
                r["patient_app"],
                r["doctor_mobile"],
                r["clinic_web"],
                r["common_all"],
                r["host"],
                r["token"],
                r["tasks"],
                r["use"],
                r["sample_request"],
                r["sample_response"],
                r["api_number"],
                r["source"],
            ]
        )
    ws.freeze_panes = "A2"
    ws.auto_filter.ref = f"A1:{get_column_letter(len(HEADERS))}{ws.max_row}"
    widths = [6, 48, 10, 22, 28, 22, 18, 12, 18, 10, 12, 10, 12, 14, 10, 14, 36, 40, 40, 12, 22]
    for i, w in enumerate(widths, 1):
        ws.column_dimensions[get_column_letter(i)].width = w
    for row in ws.iter_rows(min_row=2, max_row=ws.max_row, min_col=18, max_col=19):
        for cell in row:
            cell.alignment = Alignment(wrap_text=True, vertical="top")
    return ws


def main() -> None:
    rows = build_rows()
    wb = Workbook()
    # README
    ws = wb.active
    ws.title = "README"
    today = date.today().strftime("%d-%b-%Y")
    readme = [
        ["Homeocentrum — APIs (single documentation file)"],
        [f"File: NIGA_NewAPI/ScriptsAndFiles/{OUT.name}"],
        [f"Updated: {today}. Regenerated from New-API controllers + preserved sample payloads."],
        ["New-API host: http://127.0.0.1:5002/api   Old-API host: http://127.0.0.1:5001/api"],
        ["Swagger login: Homeocentrum_Developer / HomeocentrumDeveloper@12345"],
        ["This Excel is the only API documentation handoff. Do not rely on separate .md/.txt API docs."],
        ["Sheets: All_APIs (full), week filters S1–S4, New_APIs, Patient_App, Doctor_Mobile, Clinic_Web, Common_For_All, Users & Login Details."],
        ["Sample request/response: carried forward from prior catalog when present; otherwise template from method/path."],
    ]
    for line in readme:
        ws.append(line)
    ws.column_dimensions["A"].width = 120

    # Summary
    ws_sum = wb.create_sheet("Summary", 1)
    ws_sum.append(["Counts"])
    ws_sum.append(["Week", "Total", "With sample request", "With sample response"])
    by_week = defaultdict(list)
    for r in rows:
        by_week[r["week"]].append(r)
    for week in sorted(by_week.keys()):
        rs = by_week[week]
        ws_sum.append(
            [
                week,
                len(rs),
                sum(1 for x in rs if x["sample_request"]),
                sum(1 for x in rs if x["sample_response"]),
            ]
        )
    ws_sum.append([])
    ws_sum.append(["Grand total", len(rows)])
    ws_sum.append(["New-API host", NEW_HOST])
    ws_sum.append(["Old-API host", OLD_HOST])

    write_sheet(wb, "All_APIs", rows)
    for week in ("S1_Week1", "S2_Week2", "S3_Week3", "S4_Week4", "Platform"):
        subset = [r for r in rows if r["week"] == week]
        if subset:
            write_sheet(wb, week, subset)

    write_sheet(wb, "New_APIs", [r for r in rows if str(r["host"]).startswith("New")])
    write_sheet(wb, "Patient_App", [r for r in rows if r["patient_app"] == "Yes"])
    write_sheet(wb, "Doctor_Mobile", [r for r in rows if r["doctor_mobile"] == "Yes"])
    write_sheet(wb, "Clinic_Web", [r for r in rows if r["clinic_web"] == "Yes"])
    write_sheet(wb, "Common_For_All", [r for r in rows if r["common_all"] == "Yes"])

    # Users sheet
    wu = wb.create_sheet("Users & Login Details")
    wu.append(
        [
            "User / Test User",
            "Role",
            "Purpose",
            "Login username",
            "Password",
            "Environment",
            "Active/Inactive",
            "Login host",
            "Login URL",
            "Notes",
        ]
    )
    style_header(wu)
    users = [
        ("Tufan_Admin", "Admin", "Admin portal", "Tufan_Admin", "123456", "HomeoCentrum_Dev", "Active", "Old-API :5001", f"POST {OLD_HOST}/api/Account/Login", "Seed login"),
        ("Tufan_Doctor", "Doctor", "Clinic doctor", "Tufan_Doctor", "123456", "HomeoCentrum_Dev", "Active", "Old-API :5001", f"POST {OLD_HOST}/api/Account/Login", "Seed login"),
        ("Tufan_Reception", "Reception", "Front desk", "Tufan_Reception", "123456", "HomeoCentrum_Dev", "Active", "Old-API :5001", f"POST {OLD_HOST}/api/Account/Login", "Seed login"),
        ("Tufan_Patient", "Patient", "Patient portal/app", "Tufan_Patient", "123456", "HomeoCentrum_Dev", "Active", "Old-API :5001", f"POST {OLD_HOST}/api/Account/Login", "Seed login"),
        ("Swagger", "Docs", "Swagger gate", "Homeocentrum_Developer", "HomeocentrumDeveloper@12345", "Local", "Active", "New :5002 / Old :5001", "/swagger-login", "appsettings SwaggerAuth"),
    ]
    for u in users:
        wu.append(list(u))
    for i, w in enumerate([18, 12, 22, 18, 28, 16, 12, 16, 40, 24], 1):
        wu.column_dimensions[get_column_letter(i)].width = w

    OUT.parent.mkdir(parents=True, exist_ok=True)
    wb.save(OUT)
    print(f"Wrote {OUT}")
    print(f"Total rows: {len(rows)}")
    for week, rs in sorted(by_week.items()):
        print(f"  {week}: {len(rs)}")


if __name__ == "__main__":
    main()
