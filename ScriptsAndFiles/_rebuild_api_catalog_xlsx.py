# -*- coding: utf-8 -*-
"""
Rebuild NIGA_API/ScriptsAndFiles/Homeocentrum_All_New_And_Updated+APIs.xlsx
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
NEW_CTRL = ROOT / "NIGA_API" / "Homeocentrum.Niga.API" / "Controllers"
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
    "Sample Real request",
    "Sample response",
    "Auth",
    "Errors",
    "When to call it",
    "Do not call this for",
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


def strip_source_comments(text: str) -> str:
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    return "\n".join(re.sub(r"//.*", "", line) for line in text.splitlines())


def extract_controllers(ctrl_dir: Path) -> list[dict]:
    rows: list[dict] = []
    for path in sorted(ctrl_dir.rglob("*Controller.cs")):
        if path.name == "BaseAPIController.cs":
            continue
        text = strip_source_comments(path.read_text(encoding="utf-8", errors="ignore"))
        class_m = re.search(r"public\s+(?:partial\s+)?class\s+(\w+)Controller\b", text)
        ctrl_name = class_m.group(1) if class_m else path.stem.replace("Controller", "")
        # Attributes sit after the namespace brace, so do not stop at the first "{".
        prefix = ""
        if class_m:
            window = text[max(0, class_m.start() - 800) : class_m.start()]
            brace = window.rfind("}")
            if brace >= 0:
                window = window[brace + 1 :]
            prefix = window
        class_routes = ROUTE_ATTR.findall(prefix)
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
                    "host": "API",
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
    if c.startswith("s5") or "/s5" in p:
        return "S5_Week5"
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
        return '{"success":true,"status":"Healthy","api":"API"}'
    if "/login" in path.lower():
        return '{"success":true,"data":{"token":"<jwt>","userName":"Tufan_Doctor","role":"Doctor"}}'
    return '{"success":true,"message":"OK","data":{}}'


def _g(use, user, token, when, avoid, tasks, request, response, patient="No", doctor="No", clinic="Yes", common="No", auth=""):
    return {
        "use": use,
        "user": user,
        "token": token,
        "when": when,
        "avoid": avoid,
        "tasks": tasks,
        "request": request,
        "response": response,
        "patient_app": patient,
        "doctor_mobile": doctor,
        "clinic_web": clinic,
        "common_all": common,
        "auth": auth or ("Bearer JWT" if token == "yes" else "No token"),
        "errors": "400 validation, 401 no/expired token, 403 wrong role, 404 missing row, 500 unexpected. Body is success, message, traceId.",
    }


def guide_for(method: str, path: str) -> dict | None:
    """Developer handoff for Week 4 and Week 5 routes, plus the patient-app calls those weeks reuse."""
    p = normalize_path(path).lower()
    m = method.upper()
    host = NEW_HOST
    bearer = "Authorization: Bearer <token>"

    def req(body: str = "") -> str:
        head = f"{m} {host}{path}\n{bearer}\nContent-Type: application/json"
        return head if not body else head + "\n\n" + body

    if p.startswith("/api/fees/public/"):
        return _g(
            "Read the doctor's current in-clinic fee, tele fee, and whether pay-at-clinic is allowed. Use this before showing a price on the public booking page.",
            "Public, Patient", "no",
            "Doctor profile and the book-a-visit screen, before checkout.",
            "Do not use this to change a fee or to take payment.",
            "PAY-01.02",
            f"GET {host}{path}",
            '{"success":true,"data":{"doctorId":1010,"inClinicFee":500.00,"teleFee":400.00,"instantSurcharge":100.00,"currency":"INR","payAtClinicEnabled":true}}',
            patient="Yes", doctor="Yes", clinic="Yes", common="Yes", auth="No token",
        )
    if p == "/api/fees" and m == "PUT":
        return _g("Doctor or admin saves consult fees. Each save is kept in fee history.", "Doctor, Admin", "yes",
                  "Doctor consult-fee screen after the doctor edits amounts.", "Do not call from the patient app.",
                  "PAY-01.02", req('{\n  "inClinicFee": 500,\n  "teleFee": 400,\n  "instantSurcharge": 100,\n  "currency": "INR",\n  "payAtClinicEnabled": true\n}'),
                  '{"success":true}', doctor="Yes")
    if p == "/api/fees/history":
        return _g("List past fee changes for the signed-in doctor, or for a doctor the admin asks for.", "Doctor, Admin", "yes",
                  "Fee history tab.", "Not the public price. Use GET /api/Fees/Public/{doctorId} for that.",
                  "PAY-01.02", req(), '{"success":true,"data":[]}', doctor="Yes")
    if p == "/api/payments/consultorders":
        return _g("Create the online consult payment from the doctor's fee. The amount is not taken from the client.", "Patient, Public", "no",
                  "Checkout after the patient confirms a visit and chooses to pay online.",
                  "Do not mark the visit paid from this response. Wait for the webhook or reception collection.",
                  "PAY-03.02", f"POST {host}{path}\nContent-Type: application/json\n\n{{\n  \"patientAppId\": 27357,\n  \"payAtClinic\": false\n}}",
                  '{"success":true,"data":{"paymentOrderId":1,"amount":500.00,"currency":"INR","status":"CREATED"}}',
                  patient="Yes", clinic="Yes", auth="Optional. Public booking may call it without a token.")
    if p == "/api/payments/webhook":
        return _g("Razorpay server callback. Verifies the signature and is the only online path that marks a payment captured.", "Razorpay", "no",
                  "Configure this URL in the Razorpay dashboard. The server calls it.",
                  "The website must not call this to fake a success.",
                  "PAY-02.03", f"POST {host}{path}\nX-Razorpay-Signature: <signature>\nContent-Type: application/json\n\n{{ \"event\": \"payment.captured\" }}",
                  '{"success":true}', patient="No", doctor="No", clinic="No", auth="Razorpay signature. No JWT.")
    if p == "/api/payments/verify":
        return _g("Optional client check after checkout. It does not replace the webhook.", "Patient", "yes",
                  "Only if the app must show a pending state before the webhook arrives.",
                  "Do not treat a 200 here as proof the bank captured the money.",
                  "PAY-03.02", req('{\n  "paymentOrderId": 1,\n  "gatewayPaymentId": "pay_xxx"\n}'),
                  '{"success":true}', patient="Yes", clinic="No")
    if p == "/api/payments/collectatreception":
        return _g("Reception records cash, UPI, card at the desk, or a pay link. This marks the visit collected and writes the ledger.", "Reception, Account", "yes",
                  "Reception desk when the patient pays in the clinic.",
                  "Do not use for online Razorpay checkout.",
                  "PAY-04.02", req('{\n  "patientAppId": 27357,\n  "method": "CASH"\n}'),
                  '{"success":true,"status":"COLLECTED"}', doctor="Yes")
    if p.startswith("/api/payments/appointments/"):
        return _g("Payment status for one appointment.", "Patient, Doctor, Reception", "yes",
                  "Appointment card and the patient payments screen.",
                  "Not the list of all payments. Use GET /api/Patient/Payments for that.",
                  "PAY-03.02", req(), '{"success":true,"data":{"status":"PAY_AT_CLINIC"}}', patient="Yes", doctor="Yes")
    if p == "/api/payments/medicineorders":
        return _g("Start payment for a medicine order after the patient accepts a quote. COD is a status, not a gateway charge.", "Patient", "yes",
                  "After the patient accepts the pharmacy quote.",
                  "Do not call before a quote exists.",
                  "PAY-07.02, MED-09.01", req('{\n  "medicineOrderId": 1,\n  "method": "ONLINE"\n}'),
                  '{"success":true}', patient="Yes", clinic="No")
    if p == "/api/patient/payments":
        return _g("The signed-in patient's payment and refund list.", "Patient", "yes",
                  "Patient app Payments screen.", "Not the account ledger.",
                  "CON-08.01, PAT-49.02", req(), '{"success":true,"data":[]}', patient="Yes", doctor="No", clinic="No")
    if p.startswith("/api/refunds") or p == "/api/account/refunds":
        return _g("Refund policy, create a refund, and list refunds. Account role creates them. The webhook completes them.", "Account, Admin, Patient", "yes",
                  "Account refund screen, or the patient asking what the policy is.",
                  "Do not refund by editing the ledger directly.",
                  "PAY-09.02", req('{\n  "paymentOrderId": 1,\n  "amount": 500,\n  "reason": "Visit cancelled"\n}'),
                  '{"success":true}', patient="Yes")
    if p.startswith("/api/invoices/"):
        return _g("Read or generate the invoice PDF for one payment.", "Account, Patient", "yes",
                  "After a payment is collected or captured.",
                  "Not the tax CSV. Use GET /api/Account/Tax/Export for that.",
                  "PAY-10.02", req(), '{"success":true,"data":{"number":"INV-1"}}', patient="Yes")
    if p.startswith("/api/account/ledger"):
        return _g("Account ledger list or CSV. Rows are written only by payment and settlement code.", "Account, Admin", "yes",
                  "Account Ledger screen and export.",
                  "The website must not POST ledger lines.",
                  "FIN-01.02", req(), '{"success":true,"data":[]}')
    if p == "/api/account/reconciliation":
        return _g("Consult payments compared with appointments for the account desk.", "Account, Admin", "yes",
                  "Account reconciliation screen.", "CSV of the same data is GET /api/Reports/Reconciliation/Export.",
                  "FIN-02.02", req(), '{"success":true,"data":{"count":0}}')
    if p == "/api/account/medicineledger":
        return _g("Medicine payment split: seller, platform, delivery.", "Account, Admin", "yes",
                  "Account medicine-ledger screen.", "Not the clinical prescription.",
                  "FIN-03.02", req(), '{"success":true,"data":[]}')
    if p.startswith("/api/account/settlements"):
        return _g("Create a settlement run (dry-run or commit), then list or open one run.", "Account, Admin", "yes",
                  "Account settlements screen.", "Payout approval is /api/Account/Payouts, not this route.",
                  "FIN-04.02", req('{\n  "commit": false\n}'), '{"success":true,"data":[]}')
    if "/api/account/payouts" in p:
        return _g("List payouts, send the OTP, then approve or reject. Approve needs the OTP.", "Account, Admin", "yes",
                  "Payouts screen. The button label is Send OTP.",
                  "Do not approve without the OTP step.",
                  "FIN-05.02", req(), '{"success":true,"data":[]}')
    if p.startswith("/api/account/exceptions"):
        return _g("Payment exceptions from webhook mismatches. List, open, retry, or resolve.", "Account, Admin", "yes",
                  "Account exceptions screen.", "Not pharmacy order exceptions. Those are /api/Admin/HomemedsExceptions.",
                  "FIN-07.02", req(), '{"success":true,"data":[]}')
    if p.startswith("/api/account/tax"):
        return _g("GST report and its CSV. Rates come from TaxConfig.", "Account, Admin", "yes",
                  "Account tax screen and download.", "Not the invoice PDF.",
                  "FIN-08.02, RPT-07.01", req(), '{"success":true,"fileName":"tax.csv","csv":"Stream,Direction,Amount,Gst,At"}')
    if p.startswith("/api/account/payees"):
        return _g("Bank payees. Changing the bank account requires the OTP route first.", "Account, Admin", "yes",
                  "Payees screen.", "Do not store the bank change without the OTP call.",
                  "FIN-09.02", req(), '{"success":true,"data":[]}')
    if p == "/api/account/cliniccollections":
        return _g("Cash and offline UPI collected at the clinic, grouped for the account desk.", "Account, Admin", "yes",
                  "Clinic collections screen.", "Online Razorpay totals are in reconciliation.",
                  "FIN-10.02", req(), '{"success":true,"data":[]}')
    if p.startswith("/api/account/trail"):
        return _g("Audit trail for one appointment, payment, or doctor.", "Account, Admin", "yes",
                  "Account investigation screen.", "Not the patient clinical timeline.",
                  "PAY-11.02", req(), '{"success":true,"data":[]}')
    if p.startswith("/api/trust"):
        return _g("Doctor trust queue. Doctor reads My Status. Admin lists, opens, and decides Approved, Rejected, or NeedsInfo.", "Doctor, Admin", "yes",
                  "Doctor credentials page and the admin trust queue.",
                  "Public doctor search already hides unverified doctors. Do not filter that list in the app.",
                  "TRU-01.02, TRU-02.02", req(), '{"success":true,"data":[]}', doctor="Yes")
    if p.startswith("/api/reviews"):
        return _g("Patient writes a review, reads their own, and can appeal. Public approved reviews are by doctor. Admin resolves appeals.", "Patient, Doctor, Admin", "yes",
                  "Patient app review screens and the doctor appeal inbox.",
                  "Do not show a review until its status is approved.",
                  "TRU-05.02, TRU-06.02, PAT-47.02, PAT-48.02", req('{\n  "patientAppId": 27357,\n  "rating": 5,\n  "text": "Clear visit"\n}'),
                  '{"success":true}', patient="Yes", doctor="Yes")
    if p.startswith("/api/doctors/rankingexplain/"):
        return _g("Why a doctor is ranked where they are. Weights come from RankingWeight, not a fixed sort in the app.", "Public, Patient", "no",
                  "Optional explain icon on a doctor card.", "Do not re-sort the doctor list in the app.",
                  "TRU-07.02", f"GET {host}{path}", '{"success":true}', patient="Yes", clinic="Yes", common="Yes", auth="No token")
    if p.startswith("/api/erx"):
        return _g("Sign a prescription, read it, download the PDF, and handle refill requests. Remedy names stay hidden from the patient until the pharmacy accepts the order.", "Doctor, Patient", "yes",
                  "Doctor sign-eRx screen and the patient prescription screen.",
                  "Do not show remedy names on the patient API before pharmacy accept.",
                  "ERX-01.02 through ERX-11.01", req(), '{"success":true}', patient="Yes", doctor="Yes")
    if p.startswith("/api/pharmacy") or p.startswith("/api/medicineorders") or p.startswith("/api/admin/homemedsexceptions"):
        return _g("Pharmacy onboard, seller list, consent, quote, accept with OTP, tracking, and admin re-route.", "Pharmacy, Patient, Admin", "yes",
                  "Pharmacy workspace and the patient medicine-order screens.",
                  "Doctor sign does not reveal remedy names. Accept is the reveal step.",
                  "MED-01.02 through MED-16.01", req(), '{"success":true}', patient="Yes", doctor="Yes")
    if p.startswith("/api/patient/medicineorders"):
        return _g("Patient's medicine orders.", "Patient", "yes",
                  "Patient app Medicines tab.", "Not the pharmacy workspace list.",
                  "MED-16.01, PAT-41.02", req(), '{"success":true,"data":[]}', patient="Yes", doctor="No", clinic="No")
    if p.startswith("/api/patient/"):
        return _g("Patient continuity: timeline, note, documents, follow-ups, diary, progress, consents, data requests, and profile.", "Patient, Doctor", "yes",
                  "Patient app care screens. Doctor reads follow-ups and diary for their patient.",
                  "Registration of a new patient stays on the reception dashboard, not these routes.",
                  "CON-03.02, CON-04.02, CON-05.02, CON-06.02, CON-07.02, CON-10.02, PAT-50.02, PAT-51.02",
                  req(), '{"success":true,"data":{}}', patient="Yes", doctor="Yes")
    if p.startswith("/api/sms/"):
        return _g("SMS templates, send, and history. Send stays LOGGED until Sms:AuthKey is set. Opt-out patients are refused.", "Admin", "yes",
                  "Admin template screen, or a server job that asks the API to send.",
                  "Do not call a third-party SMS URL from the mobile app.",
                  "COM-01.02", req('{\n  "templateCode": "APPT_CONFIRM",\n  "mobile": "9999999999"\n}'),
                  '{"success":true,"status":"LOGGED","mobile":"9999999999"}', doctor="No")
    if p == "/api/whatsapp/receipts":
        return _g("WhatsApp delivery receipt from the provider. Matches MetaMessageId and stores status.", "WhatsApp provider", "no",
                  "Provider callback only.", "The app does not post receipts.",
                  "COM-02.02", f"POST {host}{path}\nContent-Type: application/json\n\n{{\n  \"metaMessageId\": \"wamid.xxx\",\n  \"status\": \"delivered\"\n}}",
                  '{"success":true,"updated":0}', patient="No", doctor="No", clinic="No", auth="No token. Provider callback.")
    if p == "/api/whatsapp/bulk":
        return _g("Admin list of bulk WhatsApp rows and their delivery status.", "Admin", "yes",
                  "Admin campaign status screen.", "Sending a campaign is the existing WhatsApp send path, not this GET.",
                  "COM-02.02", req(), '{"success":true,"data":[]}', doctor="No")
    if p == "/api/notifications/send":
        return _g("Store a notification for one user. Push stays LOGGED until Fcm:ServerKey is set.", "Doctor, Admin", "yes",
                  "When the clinic must tell one user something.",
                  "The patient app should call GET /api/Notifications, not this.",
                  "COM-03.02, PAT-57.02", req('{\n  "userId": 10033,\n  "title": "Visit update",\n  "body": "Your visit is confirmed."\n}'),
                  '{"success":true,"pushStatus":"LOGGED"}', doctor="Yes")
    if p == "/api/notifications":
        return _g("Notifications for the signed-in user.", "Patient, Doctor", "yes",
                  "Notification centre on open and on pull-to-refresh.",
                  "Do not send from this GET.",
                  "COM-05.02, PAT-52.02", req(), '{"success":true,"data":[]}', patient="Yes", doctor="Yes", clinic="No")
    if p.startswith("/api/notifications/"):
        return _g("Mark one notification read or unread. Only the owning user can patch it.", "Patient, Doctor", "yes",
                  "When the user opens a notification.", "Do not mark another user's row.",
                  "COM-05.02", f"PATCH {host}{path}?isRead=true\n{bearer}",
                  '{"success":true,"isRead":true}', patient="Yes", doctor="Yes", clinic="No")
    if p.startswith("/api/email/receipts/"):
        return _g("Send the receipt email for one payment. A captured payment already writes a LOGGED row without sending SMTP.", "Account, Admin", "yes",
                  "Account chooses Send receipt.",
                  "Do not call this on every page load. It can send mail.",
                  "COM-04.02", req(), '{"success":true,"status":"LOGGED"}')
    if p == "/api/email/history":
        return _g("Recent receipt and payment emails.", "Account, Admin", "yes",
                  "Account email history.", "Not the patient inbox.",
                  "COM-04.01", req(), '{"success":true,"data":[]}')
    if p == "/api/admindashboard/overview":
        return _g("Counts for appointments, medicine orders, and open follow-ups.", "Admin", "yes",
                  "Admin home.", "Not the doctor dashboard charts. Use GetPatientStatsCharts for those.",
                  "RPT-01.02", req(), '{"success":true,"data":{"appointments":0,"medicineOrders":0,"openFollowUps":0}}', doctor="No")
    if p == "/api/reports/followupdue":
        return _g("Open follow-ups due today or earlier. Doctors see their own patients. Admin sees the clinic.", "Doctor, Admin, Reception", "yes",
                  "Doctor follow-up page.", "Completed tasks are in the summary, not this list.",
                  "RPT-03.02", req(), '{"success":true,"data":[]}', doctor="Yes")
    if p == "/api/reports/followupsummary":
        return _g("Follow-up counts by status.", "Doctor, Admin, Reception", "yes",
                  "A small summary next to the due list.", "Not the patient diary.",
                  "RPT-03.02", req(), '{"success":true,"data":[{"name":"OPEN","cnt":0}]}', doctor="Yes")
    if p == "/api/reports/clinicperformance":
        return _g("Visit mix (in-clinic, tele, first) and paid totals for a date range.", "Doctor, Admin, Reception, Account", "yes",
                  "Clinic performance report. Defaults to the last 30 days.",
                  "Doctor earnings buckets are GET /api/Earnings/Buckets.",
                  "RPT-04.02", req(), '{"success":true,"visits":[],"paid":{"amount":0,"cnt":0}}', doctor="Yes")
    if p.startswith("/api/reports/") and p.endswith("/export"):
        return _g("CSV download data for reconciliation, settlements, or payouts. The file text is in the csv field.", "Account, Admin", "yes",
                  "Export button on that account screen.", "Tax CSV is GET /api/Account/Tax/Export.",
                  "RPT-05.01, RPT-06.01", req(), '{"success":true,"fileName":"export.csv","csv":""}')
    if p == "/api/reports/medicineorders":
        return _g("Medicine orders counted by status.", "Account, Admin, Pharmacy", "yes",
                  "Medicine order report.", "The patient list is GET /api/Patient/MedicineOrders.",
                  "RPT-09.02", req(), '{"success":true,"data":[]}', doctor="Yes")
    if p == "/api/earnings/summary":
        return _g("Doctor earnings summary for the mobile app.", "Doctor", "yes",
                  "Doctor app earnings home.", "Monthly buckets are GET /api/Earnings/Buckets.",
                  "DMO-10.02, RPT-08.02", req(), '{"success":true,"data":{"totalCaptured":0}}', doctor="Yes", clinic="Yes")
    if p == "/api/earnings/buckets":
        return _g("Paid amounts grouped by month.", "Doctor, Admin", "yes",
                  "Earnings chart.", "The summary card is GET /api/Earnings/Summary.",
                  "RPT-08.02", req(), '{"success":true,"data":[]}', doctor="Yes")
    if p == "/api/admin/users/export":
        return _g("User CSV including role and doctor verification status. Passwords are never included.", "Admin", "yes",
                  "Admin user export.", "Do not email this file.",
                  "RPT-10.02", req(), '{"success":true,"fileName":"users.csv","csv":"UserId,UserName,EmailId,MobileNo,Role,UserStatus,VerificationStatus"}', doctor="No")
    if p == "/api/admin/users/import":
        return _g("Dry run only. Lists names that already exist and names that do not. It does not create passwords or users.", "Admin", "yes",
                  "Admin checks a spreadsheet before any manual create.",
                  "Do not expect new logins from this call.",
                  "RPT-10.02", req('{\n  "users": [{ "userName": "Tufan_Admin" }]\n}'),
                  '{"success":true,"created":0,"existing":["Tufan_Admin"],"unknown":[]}', doctor="No")
    if p == "/api/security/posture":
        return _g("Where secrets live, and whether SMS, FCM, and Key Vault are configured. Secret values are not returned.", "Admin", "yes",
                  "Admin security check.", "Do not show this JSON in the patient or doctor app.",
                  "NFR-01.01", req(),
                  '{"success":true,"directoryBrowsingEnabled":false,"secretsStoredIn":"appsettings.json","keyVaultConfigured":false}',
                  doctor="No")
    if p == "/api/ops/metrics":
        return _g("Request count and server-error count since this process started.", "Admin", "yes",
                  "Ops check after deploy.", "Not a business report.",
                  "", req(), '{"success":true,"requests":0,"serverErrors":0,"tenant":"single-clinic"}', doctor="No")
    if p == "/api/device/register":
        return _g("Save this device's FCM or APNs token for the signed-in user.", "Patient, Doctor", "yes",
                  "After login, when the app receives a push token.",
                  "Do not send the push from the device. The server stores the token.",
                  "COM-03.02, PAT-57.02", req('{\n  "platform": "FCM",\n  "token": "<device-token>"\n}'),
                  '{"success":true}', patient="Yes", doctor="Yes", clinic="No")
    if p.startswith("/api/help"):
        return _g("Published help articles.", "Patient, Public", "no",
                  "Help centre.", "Do not ship a hard-coded article list in the app.",
                  "PAT-53.02", f"GET {host}{path}", '{"success":true,"data":[]}',
                  patient="Yes", doctor="Yes", clinic="Yes", common="Yes", auth="No token")
    if p == "/api/support/assistancerequest":
        return _g("Ask the clinic to book for the patient.", "Patient", "yes",
                  "Assisted booking.", "Normal self-booking uses the public booking routes.",
                  "PAT-55.02", req('{\n  "note": "Please call me to book"\n}'),
                  '{"success":true}', patient="Yes", doctor="No", clinic="Yes")
    if "/doctordashboard/getpatientstatscharts" in p:
        return _g("Doctor dashboard charts. The response includes visitMix by VisitType or ConsultMode.", "Doctor", "yes",
                  "Doctor dashboard charts.", "Admin clinic totals are GET /api/AdminDashboard/Overview.",
                  "RPT-02.02", req(), '{"success":true,"visitMix":[{"mode":"InClinic","count":1}]}', doctor="Yes")
    return None


def apply_guide(row: dict) -> None:
    g = guide_for(row["method"], row["endpoint"])
    if not g:
        row.setdefault("auth", "Bearer JWT" if str(row.get("token")).lower() == "yes" else "No token")
        row.setdefault("errors", "400 validation, 401 no/expired token, 403 wrong role, 500 unexpected.")
        row.setdefault("when", row.get("use") or "")
        row.setdefault("avoid", "")
        return
    row["use"] = g["use"]
    row["user"] = g["user"]
    row["token"] = g["token"]
    row["tasks"] = g["tasks"] or row.get("tasks") or ""
    row["sample_request"] = g["request"]
    row["sample_response"] = g["response"]
    row["patient_app"] = g["patient_app"]
    row["doctor_mobile"] = g["doctor_mobile"]
    row["clinic_web"] = g["clinic_web"]
    row["common_all"] = g["common_all"]
    row["who"] = who_calls(g)
    row["auth"] = g["auth"]
    row["errors"] = g["errors"]
    row["when"] = g["when"]
    row["avoid"] = g["avoid"]
    if row.get("developed_note", "").startswith("HTTP on API"):
        row["developed_note"] = g["use"]


def build_rows() -> list[dict]:
    existing = load_existing_payloads(EXISTING)
    code = extract_controllers(NEW_CTRL)
    code.append(
        {
            "method": "GET",
            "endpoint": "/health",
            "md_endpoint": "GET /health",
            "file": "Program.cs",
            "controller": "Health",
            "token": "no",
            "host": "API",
        }
    )
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
        host = "API"
        developed = prev.get("New / Existing / Updated") or "API developed"
        developed_note = prev.get("What was developed this week") or (
            "HTTP on API (catalog regenerated from controllers)."
        )
        use = prev.get("What it is used for") or f"{c['controller']} — {c['method']} {c['endpoint']}"
        section = prev.get("MD section") or c["controller"]
        req = sample_request(c["method"], c["endpoint"], host, prev.get("Sample request", ""))
        res = sample_response(c["endpoint"], prev.get("Sample response", ""))
        row = {
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
        apply_guide(row)
        rows.append(row)

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
        host = "Old-API" if "old" in host_prev else "API"
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
        apply_guide(rows[-1])

    # Platform health on API
    hk = path_key("GET", "/health")
    if hk not in seen:
        rows.append(
            {
                "method": "GET",
                "endpoint": "/health",
                "md_endpoint": "GET /health",
                "developed": "API developed",
                "developed_note": "Health check on API and Old-API.",
                "section": "Platform",
                "user": "Public",
                "week": "Platform",
                "who": "Common for all",
                "patient_app": "Yes",
                "doctor_mobile": "Yes",
                "clinic_web": "Yes",
                "common_all": "Yes",
                "host": "API and Old-API",
                "token": "no",
                "tasks": "",
                "use": "Process health. No login.",
                "sample_request": f"GET {NEW_HOST}/health",
                "sample_response": '{"success":true,"status":"Healthy","api":"API"}',
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
            "S5_Week5": "S5",
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
                r.get("sample_real_request") or "",
                r["sample_response"],
                r.get("auth") or "",
                r.get("errors") or "",
                r.get("when") or "",
                r.get("avoid") or "",
                r["api_number"],
                r["source"],
            ]
        )
    ws.freeze_panes = "A2"
    ws.auto_filter.ref = f"A1:{get_column_letter(len(HEADERS))}{ws.max_row}"
    widths = [6, 48, 10, 22, 36, 22, 18, 12, 22, 12, 16, 12, 14, 16, 12, 18, 42, 46, 55, 42, 28, 36, 42, 42, 12, 22]
    for i, w in enumerate(widths, 1):
        ws.column_dimensions[get_column_letter(i)].width = w
    for row in ws.iter_rows(min_row=2, max_row=ws.max_row, min_col=18, max_col=20):
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
        [f"File: NIGA_API/ScriptsAndFiles/{OUT.name}"],
        [f"Updated: {today}. Regenerated from API controllers + preserved sample payloads."],
        ["API host: http://127.0.0.1:5002/api   Old-API host: http://127.0.0.1:5001/api"],
        ["Swagger login: Homeocentrum_Developer / HomeocentrumDeveloper@12345"],
        ["This Excel is the only API documentation handoff. Do not rely on separate .md/.txt API docs."],
        ["Sheets: All_APIs, S1–S5, Platform, NIGA_APIs, Patient_App, Doctor_Mobile, Clinic_Web, Common_For_All, Week4_Week5_Developer, Users & Login Details."],
        ["Week 4 and Week 5: use columns When to call it, Do not call this for, Auth, Sample request, and Sample response. Login is POST http://127.0.0.1:5001/api/Account/Login with username and password 123456."],
        ["Database scripts: NIGA_API/ScriptsAndFiles/S4_Week4/Database scripts and S5_Week5/Database scripts. Run 01 then 02 then 03. S5 also has 03 demo templates."],
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
    ws_sum.append(["API host", NEW_HOST])
    ws_sum.append(["Old-API host", OLD_HOST])

    write_sheet(wb, "All_APIs", rows)
    for week in ("S1_Week1", "S2_Week2", "S3_Week3", "S4_Week4", "S5_Week5", "Platform"):
        subset = [r for r in rows if r["week"] == week]
        if subset:
            write_sheet(wb, week, subset)

    write_sheet(wb, "NIGA_APIs", [r for r in rows if str(r["host"]).startswith("New")])
    write_sheet(wb, "Patient_App", [r for r in rows if r["patient_app"] == "Yes"])
    write_sheet(wb, "Doctor_Mobile", [r for r in rows if r["doctor_mobile"] == "Yes"])
    write_sheet(wb, "Clinic_Web", [r for r in rows if r["clinic_web"] == "Yes"])
    write_sheet(wb, "Common_For_All", [r for r in rows if r["common_all"] == "Yes"])
    write_sheet(
        wb,
        "Week4_Week5_Developer",
        [r for r in rows if r["week"] in ("S4_Week4", "S5_Week5") or r.get("tasks") or r.get("avoid")],
    )

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
        ("Tufan_Caregiver", "Patient", "Caregiver grant on Tufan Patient", "Tufan_Caregiver", "123456", "HomeoCentrum_Dev", "Active", "Old-API :5001", f"POST {OLD_HOST}/api/Account/Login", "Same email as the other Tufan logins; pick this username"),
        ("Tufan_Account", "Account", "Account department", "Tufan_Account", "123456", "HomeoCentrum_Dev", "Active", "Old-API :5001", f"POST {OLD_HOST}/api/Account/Login", "Seed login"),
        ("Tufan_Pharmacy", "PharmacyPartner", "Pharmacy partner", "Tufan_Pharmacy", "123456", "HomeoCentrum_Dev", "Active", "Old-API :5001", f"POST {OLD_HOST}/api/Account/Login", "Seed login"),
        ("Swagger", "Docs", "Swagger gate", "Homeocentrum_Developer", "HomeocentrumDeveloper@12345", "Local", "Active", "New :5002 / Old :5001", "/swagger-login", "SwaggerAuth in appsettings.json"),
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
