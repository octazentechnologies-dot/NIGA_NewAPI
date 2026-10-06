# -*- coding: utf-8 -*-
"""Add mobile integration sheets to the API workbook. Does not change existing API rows."""
from __future__ import annotations

import importlib.util
import re
from pathlib import Path

from openpyxl import load_workbook
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter

HERE = Path(__file__).resolve().parent
XLSX = HERE / "Homeocentrum_All_New_And_Updated+APIs.xlsx"
NEW_CTRL = HERE.parents[0] / "Homeocentrum.Niga.NewAPI" / "Controllers"
OLD_CTRL = HERE.parents[1] / "NIGA_OldAPI" / "Homeocentrum.Niga.OldAPI" / "Controllers"

spec = importlib.util.spec_from_file_location("devcols", HERE / "_add_developer_columns.py")
dev = importlib.util.module_from_spec(spec)
spec.loader.exec_module(dev)

HEADER_FILL = PatternFill("solid", fgColor="1F4E79")
HEADER_FONT = Font(bold=True, color="FFFFFF", name="Calibri", size=11)
WRAP = Alignment(wrap_text=True, vertical="top")
THIN = Border(
    left=Side(style="thin", color="D9E2F3"),
    right=Side(style="thin", color="D9E2F3"),
    top=Side(style="thin", color="D9E2F3"),
    bottom=Side(style="thin", color="D9E2F3"),
)

SENSITIVE = ("password", "token", "otp", "secret", "pan", "ifsc", "bank", "cvv", "pin")
STATUS_PATTERNS = [
    (200, r"\breturn\s+Ok\b|\breturn\s+Ok\("),
    (201, r"\breturn\s+Created\b"),
    (204, r"\breturn\s+NoContent\b"),
    (400, r"\breturn\s+BadRequest\b"),
    (401, r"\breturn\s+Unauthorized\b"),
    (403, r"\breturn\s+Forbid\b|StatusCodes\.Status403Forbidden|StatusCode\(\s*403"),
    (404, r"\breturn\s+NotFound\b"),
    (409, r"\breturn\s+Conflict\b|StatusCodes\.Status409Conflict|StatusCode\(\s*409"),
    (423, r"StatusCodes\.Status423Locked|StatusCode\(\s*423"),
    (429, r"StatusCodes\.Status429TooManyRequests|StatusCode\(\s*429"),
    (500, r"StatusCodes\.Status500InternalServerError|StatusCode\(\s*500"),
]

# Confirmed in the controller source. Not inferred from nullability.
OVERLAY = {
    ("POST", "/api/patient"): {
        "conditional": {
            "PatientName": "Required when patientID is 0 (create). Blank name returns 400. Confirmed in PatientController.Post.",
            "PatientID": "Send 0 to create. A value above 0 updates that patient.",
        },
        "server": {
            "DoctorID": "Doctor and reception tokens: the server sets DoctorID from the JWT. Do not send another doctor's id.",
            "LoggedInUser": "Reception: the server replaces this with the clinic doctor's user id.",
            "UserId": "Reception: the server replaces this with the clinic doctor's user id.",
            "Age": "Server sets Age from dateOfBirth. Mobile may send it; the save uses the date of birth.",
            "Message": "Server Generated on the response. Do not rely on sending it.",
        },
        "business": "Doctor, reception, or admin token. A token with no doctor is 403. This does not create a mobile login.",
        "response_type": "PatientModel",
        "response_note": "Post returns Ok(saved PatientModel). Confirmed in PatientController.Post. It is not wrapped in success/data.",
    },
    ("POST", "/api/account/forgotpassword"): {
        "conditional": {
            "UserId": "Required when the email matches more than one login. Omit it only when the email has one account.",
        },
        "business": "Anonymous. Sends a reset link. It does not email the password. Link expires in 2 hours.",
    },
    ("POST", "/api/account/forgotpasswordaccounts"): {
        "business": "Anonymous lookup of logins for an email. Call this before ForgotPassword when the person has more than one role.",
    },
}


def method_of(raw_ep: str, method_type: str) -> str:
    match = re.match(r"^(GET|POST|PUT|DELETE|PATCH)\b", (raw_ep or "").strip(), re.I)
    if match:
        return match.group(1).upper()
    kind = (method_type or "").strip().upper()
    if kind in {"GET", "POST", "PUT", "DELETE", "PATCH"}:
        return kind
    return "GET"


def clip(text: str, limit: int = 4000) -> str:
    text = (text or "").replace("\x00", "")
    return text if len(text) <= limit else text[: limit - 20] + " ...truncated"


def nullable_of(type_name: str) -> str:
    t = (type_name or "").strip()
    if t.endswith("?") or t.lower().startswith("nullable<"):
        return "Nullable"
    if dev.simple_name(t).lower() in {"string", "object"}:
        return "Nullable"
    return "Non-Nullable"


def sensitive_of(name: str) -> str:
    n = re.sub(r"[^a-z0-9]", "", name.lower())
    return "Yes" if any(k in n for k in SENSITIVE) else "No"


def attr_nums(attrs: str, pattern: str) -> str:
    m = re.search(pattern, attrs or "", re.I)
    return m.group(1) if m else "Not Defined in Code"


def validation_text(attrs: str, enums: dict, type_name: str) -> tuple[str, str, str, str, str, str]:
    bits = []
    min_len = attr_nums(attrs, r"MinLength\((\d+)\)")
    max_len = attr_nums(attrs, r"MaxLength\((\d+)\)")
    if min_len == "Not Defined in Code":
        m = re.search(r"StringLength\(\s*\d+\s*,\s*MinimumLength\s*=\s*(\d+)", attrs or "", re.I)
        min_len = m.group(1) if m else "Not Defined in Code"
    if max_len == "Not Defined in Code":
        m = re.search(r"StringLength\(\s*(\d+)", attrs or "", re.I)
        max_len = m.group(1) if m else "Not Defined in Code"
    range_m = re.search(r"Range\(\s*([^,\)]+)\s*,\s*([^\)]+)\)", attrs or "", re.I)
    min_v = range_m.group(1).strip() if range_m else "Not Defined in Code"
    max_v = range_m.group(2).strip() if range_m else "Not Defined in Code"
    if "[Required" in (attrs or ""):
        bits.append("Required attribute")
    if "[EmailAddress" in (attrs or ""):
        bits.append("EmailAddress attribute")
    if "[Phone" in (attrs or ""):
        bits.append("Phone attribute")
    rx = re.search(r"RegularExpression\(@?\"([^\"]+)\"", attrs or "")
    if rx:
        bits.append("Pattern " + rx.group(1))
    st = dev.simple_name(type_name)
    allowed = "Not Defined in Code"
    if st in enums:
        allowed = enums[st]
        bits.append("Enum " + st)
    if not bits:
        bits.append("Not Defined in Code")
    return "; ".join(bits), min_len, max_len, min_v, max_v, allowed


def enum_values(enums_full: dict[str, str]) -> dict[str, str]:
    return enums_full


def load_enum_lists() -> dict[str, str]:
    lists: dict[str, str] = {}
    for root in dev.SCAN_ROOTS:
        if not root.exists():
            continue
        for path in root.rglob("*.cs"):
            if any(part.lower() in {"bin", "obj"} for part in path.parts):
                continue
            text = dev.strip_comments(path.read_text(encoding="utf-8", errors="ignore"))
            for em in dev.ENUM_RE.finditer(text):
                names = re.findall(r"[A-Za-z_][A-Za-z0-9_]*", em.group(2) or "")
                if names:
                    lists[em.group(1)] = ", ".join(names[:40])
    return lists


def action_bodies(ctrl_dir: Path) -> tuple[dict[tuple[str, str], str], dict[tuple[str, str], list[tuple[str, str]]]]:
    found: dict[tuple[str, str], str] = {}
    produced: dict[tuple[str, str], list[tuple[str, str]]] = {}
    for path in sorted(ctrl_dir.glob("*Controller.cs")):
        if path.name == "BaseAPIController.cs":
            continue
        text = dev.strip_comments(path.read_text(encoding="utf-8", errors="ignore"))
        class_m = re.search(r"public\s+(?:partial\s+)?class\s+(\w+)Controller\b", text)
        ctrl_name = class_m.group(1) if class_m else path.stem.replace("Controller", "")
        prefix = ""
        if class_m:
            window = text[max(0, class_m.start() - 900) : class_m.start()]
            brace = window.rfind("}")
            if brace >= 0:
                window = window[brace + 1 :]
            prefix = window
        base = f"api/{ctrl_name}"
        for route in dev.ROUTE_ATTR.findall(prefix):
            if "[controller]" in route.lower():
                base = f"api/{ctrl_name}"
                break
            if route.lower().startswith("api") or route.startswith("/"):
                base = route
                break
        matches = list(dev.HTTP_ATTR.finditer(text))
        for i, m in enumerate(matches):
            http = m.group(1).upper()
            template = m.group(2) or ""
            after = text[m.end() : m.end() + 500]
            route_m = dev.ROUTE_ATTR.search(after[:350])
            extra = route_m.group(1) if route_m else template
            full = dev.normalize_path(dev.join_route(base, extra))
            nxt = matches[i + 1].start() if i + 1 < len(matches) else len(text)
            block = text[m.start() : nxt]
            opener = re.search(r"\bpublic\b[\s\S]*?\)\s*(?:where\b[\s\S]*?)?\{", block)
            brace = opener.end() - 1 if opener else -1
            body = dev.brace_body(block, brace) if brace >= 0 else ""
            key = (http, dev.normalize_path(full).lower())
            found[key] = body
            window = text[max(0, m.start() - 400) : m.start() + 700]
            types = []
            for match in re.finditer(r"ProducesResponseType\(typeof\(([^)]+)\)\s*,\s*([^)]+)\)", window):
                digits = re.search(r"(\d{3})", match.group(2))
                types.append((dev.simple_name(match.group(1)), digits.group(1) if digits else "Not Defined in Code"))
            if types:
                produced[key] = types
    return found, produced


def status_codes(body: str, token_required: bool) -> list[int]:
    codes = []
    for code, pattern in STATUS_PATTERNS:
        if re.search(pattern, body or ""):
            codes.append(code)
    if token_required and 401 not in codes:
        codes.append(401)
    if 200 not in codes and re.search(r"\bOk\(|return\s+\w+Model\b", body or ""):
        codes.append(200)
    return sorted(set(codes))


def checked_names(body: str) -> dict[str, str]:
    """Property names rejected with 400, and whether the nearby text says create-only."""
    out: dict[str, str] = {}
    if not body:
        return out
    for m in re.finditer(
        r"(IsNullOrWhiteSpace|string\.IsNullOrEmpty)\(\s*(?:\w+\.)?(\w+)\s*\)",
        body,
    ):
        name = m.group(2)
        window = body[max(0, m.start() - 280) : m.end() + 220]
        if "BadRequest" not in window and "400" not in window:
            continue
        if "PatientID == 0" in window or "patientID == 0" in window:
            out[name] = "Conditional"
        else:
            out[name] = "Required"
    return out


def requirement_for(prop: dict, checks: dict, overlay: dict) -> tuple[str, str]:
    name = prop["name"]
    cond = (overlay.get("conditional") or {}).get(name)
    if cond:
        return "Conditional", cond
    if checks.get(name) == "Conditional":
        return "Conditional", "Rejected when empty on the create path in this action. See the action source."
    if checks.get(name) == "Required" or prop.get("required"):
        return "Required", "Not Applicable"
    return "Optional", "Not Applicable"


def who_provides(name: str, overlay: dict, location: str) -> str:
    server = (overlay.get("server") or {}).get(name)
    if server:
        return "Server Generated"
    if location == "Path Parameter":
        return "Mobile Provided"
    if name.lower() in {"message", "entereddate", "changeddate", "whatsappoptindate"}:
        return "Server Generated"
    return "Mobile Provided"


def full_url(base: str, endpoint: str) -> str:
    base = (base or "").strip().rstrip("/")
    path = dev.normalize_path(endpoint)
    if base.endswith("/api") and path.lower().startswith("/api"):
        return base[: -len("/api")] + path
    return base + path


def mobile_use(patient: str, doctor: str) -> str:
    yes = {str(patient or "").strip().lower(), str(doctor or "").strip().lower()}
    if "yes" in yes:
        return "Yes"
    return "No"


def write_sheet(wb, name: str, headers: list[str], rows: list[list]):
    if name in wb.sheetnames:
        del wb[name]
    ws = wb.create_sheet(name)
    ws.append(headers)
    for cell in ws[1]:
        cell.fill = HEADER_FILL
        cell.font = HEADER_FONT
        cell.alignment = WRAP
    for row in rows:
        ws.append([clip(str(c)) if c is not None else "" for c in row])
    for col in ws.columns:
        for cell in col:
            cell.alignment = WRAP
            cell.border = THIN
    ws.auto_filter.ref = f"A1:{get_column_letter(len(headers))}{max(ws.max_row, 1)}"
    ws.freeze_panes = "A2"
    ws.auto_filter.ref = ws.dimensions
    widths = {
        1: 18, 2: 42, 3: 14, 4: 28, 5: 22, 6: 36, 7: 18, 8: 55,
    }
    for i in range(1, len(headers) + 1):
        ws.column_dimensions[get_column_letter(i)].width = widths.get(i, 28)
    ws.row_dimensions[1].height = 30
    return ws


def catalog_rows(wb) -> list[dict]:
    ws = wb["All_APIs"]
    header = [str(c.value).strip() if c.value else "" for c in next(ws.iter_rows(min_row=1, max_row=1))]
    idx = {h: i for i, h in enumerate(header)}
    out = []
    for row in ws.iter_rows(min_row=2, values_only=True):
        if not row or not row[idx["API Endpoint (as in MD)"]]:
            continue
        item = {h: ("" if row[i] is None else str(row[i]).strip()) for h, i in idx.items() if i < len(row)}
        out.append(item)
    return out


def start_here_rows() -> list[list[str]]:
    return [
        ["Topic", "Confirmed from code", "Mobile handling"],
        ["Which host", "Rows in All_APIs with Host = New-API are NIGA_NewAPI. Host = Old-API are NIGA_OldAPI. Use the host on the row. Do not switch a New API row to the Old API.", "Do not pick a host that the row does not name."],
        ["Base URL in this file", "The Base URL column already on All_APIs. Local developers also run New API on http://127.0.0.1:5002 and Old API on http://127.0.0.1:5001.", "Use the Base URL column. Do not hard-code a host that is not in that column for the environment you were given."],
        ["Authorization header", "Protected actions use [Authorize]. Send Authorization: Bearer <token>.", "On 401, log in again. Do not reuse an expired token."],
        ["Mobile login", "POST New API /api/Otp/RequestOtp with action Login, entityType Mobile, entityId and destination = the mobile. Then POST /api/Account/LoginWithOtp with otpChallengeId, code, and mobileNo. Success data.token is the JWT. CreateToken is called with 7 x 24 x 60 minutes.", "Do not call /api/Otp/VerifyOtp before LoginWithOtp. That uses the code up."],
        ["Password login", "All_APIs lists POST /api/Account/Login on the New API. userName and password are required. CreateToken is called with 7 x 24 x 60 minutes. The Old API has the same path; it is not a separate row in this workbook. Mobile uses the New API base URL on that row.", "Same header after login: Authorization: Bearer <data.token>."],
        ["Refresh token", "LoginWithOtp returns AuthModel.Token. That action does not return a refresh token.", "Not Defined in Code for a refresh call. On 401, log in again."],
        ["Logout", "POST /api/Account/Logout exists on both APIs. The website calls both.", "Call the logout on the host that issued the token you are holding."],
        ["Forgot password", "POST New API /api/Account/ForgotPasswordAccounts with email, then POST /api/Account/ForgotPassword with email and userId when more than one role exists.", "userId is the login id, not the role name."],
        ["Patient created by doctor or reception", "POST New API /api/patient. patientID 0. patientName required. DoctorID is taken from the token for doctor and reception.", "This does not create a mobile app login."],
        ["Patient from public booking", "POST /api/PatientAuth/RequestOtp then /api/PatientAuth/VerifyOtp, then POST /api/Public/Doctors/{id}/Bookings. If that mobile is not already a patient, the booking inserts a Patient row. VerifyOtp returns bookingSessionId, not a JWT.", "This is not app registration. There is no separate mobile register-account API in the New API controllers."],
        ["401", "No token, bad token, or LoginWithOtp found no active user for the mobile.", "Show a login screen. Do not show a raw exception."],
        ["403", "Token is valid and the role or doctor ownership check failed.", "Do not retry with the same token. The message may name the rule."],
        ["Dates", "Patient save uses DateTime on dateOfBirth. The website sends YYYY-MM-DDT00:00:00.000Z. Age uses DateTime.Today. Several OTP and booking expiries use DateTime.UtcNow.", "Do not assume every date field is UTC. If a field's timezone is not in the action, it is Not Defined in Code."],
        ["JSON names", "ASP.NET Core JSON uses a lowercase first letter (patientName, not PatientName).", "Match the Sample request column."],
        ["Secrets", "Do not put TokenKey, SMTP passwords, or payment secrets in the app.", "Not Applicable"],
        ["Sheets", "All_APIs is unchanged. Mobile_API_Contract is one row per API. Mobile_Request_Fields is one row per field. Mobile_Status_Codes lists codes found in that action.", "If a cell says Not Defined in Code, do not invent a value."],
    ]


def method_signature(block: str) -> tuple[str, str, list[str]]:
    pub = re.search(r"\bpublic\s+", block)
    if not pub:
        return "", "", []
    rest = block[pub.end() :]
    paren = rest.find("(")
    if paren < 0:
        return "", "", []
    head = rest[:paren].strip()
    parts = head.split()
    name = parts[-1] if parts else ""
    depth = 0
    chars: list[str] = []
    for ch in rest[paren:]:
        if ch == "(":
            depth += 1
            if depth == 1:
                continue
        elif ch == ")":
            depth -= 1
            if depth == 0:
                break
        if depth >= 1:
            chars.append(ch)
    return name, " ".join(parts[:-1]), dev.split_params("".join(chars))


def enrich_actions(ctrl_dir: Path, actions: dict) -> None:
    """Fill method names and parameters when the nested-generic signature was missed."""
    if not ctrl_dir.exists():
        return
    for path in sorted(ctrl_dir.glob("*Controller.cs")):
        if path.name == "BaseAPIController.cs":
            continue
        text = dev.strip_comments(path.read_text(encoding="utf-8", errors="ignore"))
        class_m = re.search(r"public\s+(?:partial\s+)?class\s+(\w+)Controller\b", text)
        ctrl_name = class_m.group(1) if class_m else path.stem.replace("Controller", "")
        prefix = ""
        if class_m:
            window = text[max(0, class_m.start() - 900) : class_m.start()]
            brace = window.rfind("}")
            if brace >= 0:
                window = window[brace + 1 :]
            prefix = window
        base = f"api/{ctrl_name}"
        for route in dev.ROUTE_ATTR.findall(prefix):
            if "[controller]" in route.lower():
                base = f"api/{ctrl_name}"
                break
            if route.lower().startswith("api") or route.startswith("/"):
                base = route
                break
        matches = list(dev.HTTP_ATTR.finditer(text))
        for i, match in enumerate(matches):
            http = match.group(1).upper()
            template = match.group(2) or ""
            after = text[match.end() : match.end() + 500]
            route_m = dev.ROUTE_ATTR.search(after[:350])
            extra = route_m.group(1) if route_m else template
            full = dev.normalize_path(dev.join_route(base, extra))
            nxt = matches[i + 1].start() if i + 1 < len(matches) else len(text)
            block = text[match.start() : nxt]
            name, _ret, params = method_signature(block)
            key = dev.path_key(http, full)
            info = actions.get(key)
            if info is None:
                continue
            if name:
                info["action"] = name
            path_names = {p.split(":")[0] for p in re.findall(r"\{([^}]+)\}", full)}
            query = []
            route_params = []
            body_type = ""
            form = False
            file_field = ""
            for param in params:
                if "CancellationToken" in param or "FromServices" in param:
                    continue
                default = "Not Defined in Code"
                default_m = re.search(r"=\s*(.+)$", param.strip())
                if default_m:
                    default = default_m.group(1).strip()
                cleaned = re.sub(r"\[[^\]]+\]", " ", param)
                cleaned = re.sub(r"\s*=\s*.*$", "", cleaned).strip()
                bits = cleaned.split()
                if len(bits) < 2:
                    continue
                p_name = bits[-1].strip()
                p_type = bits[-2].strip()
                if "IFormFile" in param:
                    form = True
                    file_field = p_name
                    continue
                if "FromForm" in param:
                    form = True
                if "FromBody" in param:
                    body_type = p_type
                    continue
                if p_name in path_names or "FromRoute" in param:
                    route_params.append((p_name, p_type))
                    continue
                if p_type.lower().rstrip("?") in dev.PRIMITIVES or "FromQuery" in param or dev.simple_name(p_type).lower() in dev.PRIMITIVES:
                    query.append((p_name, p_type, default))
                    continue
                if not body_type and http in {"POST", "PUT", "PATCH"}:
                    body_type = p_type
            if name or body_type or query or route_params:
                info["query"] = query
                info["route_params"] = route_params
                info["form"] = form or info.get("form")
                info["file_field"] = file_field or info.get("file_field")
                if body_type:
                    info["body_type"] = body_type


def stacked_aliases(ctrl_dir: Path) -> list[tuple[tuple[str, str], tuple[str, str]]]:
    """HTTP attributes stacked on one method. Maps the extra path to the path that owns the method."""
    pairs = []
    if not ctrl_dir.exists():
        return pairs
    for path in sorted(ctrl_dir.glob("*Controller.cs")):
        if path.name == "BaseAPIController.cs":
            continue
        text = dev.strip_comments(path.read_text(encoding="utf-8", errors="ignore"))
        class_m = re.search(r"public\s+(?:partial\s+)?class\s+(\w+)Controller\b", text)
        ctrl_name = class_m.group(1) if class_m else path.stem.replace("Controller", "")
        prefix = ""
        if class_m:
            window = text[max(0, class_m.start() - 900) : class_m.start()]
            brace = window.rfind("}")
            if brace >= 0:
                window = window[brace + 1 :]
            prefix = window
        base = f"api/{ctrl_name}"
        for route in dev.ROUTE_ATTR.findall(prefix):
            if "[controller]" in route.lower():
                base = f"api/{ctrl_name}"
                break
            if route.lower().startswith("api") or route.startswith("/"):
                base = route
                break
        matches = list(dev.HTTP_ATTR.finditer(text))
        entries = []
        for i, match in enumerate(matches):
            http = match.group(1).upper()
            template = match.group(2) or ""
            after = text[match.end() : match.end() + 400]
            route_m = dev.ROUTE_ATTR.search(after[:300])
            extra = route_m.group(1) if route_m else template
            full = dev.normalize_path(dev.join_route(base, extra))
            nxt = matches[i + 1].start() if i + 1 < len(matches) else len(text)
            block = text[match.start() : nxt]
            entries.append((dev.path_key(http, full), bool(re.search(r"\bpublic\b", block))))
        for i, (key, has_method) in enumerate(entries):
            if has_method:
                continue
            for later_key, later_has in entries[i + 1 :]:
                if later_has:
                    pairs.append((key, later_key))
                    break
    return pairs


def apply_aliases(actions, bodies, produced, pairs) -> None:
    for extra, owner in pairs:
        src = actions.get(owner)
        if src and src.get("action"):
            dst = actions.get(extra)
            if dst is None or not dst.get("action"):
                copied = dict(src)
                if dst and dst.get("path"):
                    copied["path"] = dst["path"]
                actions[extra] = copied
        if owner in bodies and not (bodies.get(extra) or "").strip():
            bodies[extra] = bodies[owner]
        if owner in produced and extra not in produced:
            produced[extra] = list(produced[owner])


def append_model_fields(rows, method, path, classes, enum_first, type_name, parent, note):
    props = [p for p in (classes.get(type_name) or []) if isinstance(p, dict)]
    if not props:
        rows.append([
            method, path, parent or type_name, "", type_name, "",
            "Type named in code. Members were not found in the scanned models.",
            "", "Not Defined in Code", "Read Only", "Needs Confirmation",
        ])
        return
    for prop in props:
        row = response_row(method, path, prop, parent or type_name)
        row[6] = note
        rows.append(row)
        inner = dev.collection_inner(prop["type"])
        nested_name = inner or dev.simple_name(prop["type"])
        if nested_name and nested_name in classes and nested_name != type_name and not dev.is_primitive(prop["type"]):
            for child in classes[nested_name]:
                if not isinstance(child, dict):
                    continue
                child_row = response_row(method, path, child, f"{parent or type_name}.{dev.camel(prop['name'])}")
                child_row[6] = "Nested member of " + nested_name + ". One level only."
                rows.append(child_row)


def append_responses(rows, method, path, classes, enum_first, overlay, produced):
    key = (method, path.lower())
    if key in {("POST", "/api/account/loginwithotp"), ("POST", "/api/account/login")}:
        otp = key[1].endswith("loginwithotp")
        rows.append([method, path, "object", "success", "bool", "Non-Nullable", "Set true on success.", "true", "Field", "Read Only", "Check before reading data."])
        rows.append([method, path, "object", "message", "string", "Nullable", "Login successful on the success path.", "Login successful", "Field", "Read Only", "May be shown."])
        rows.append([method, path, "object", "data", "AuthModel", "Nullable", "JWT and the signed-in user.", "", "Nested object", "Read Only", "Read data.token for later calls."])
        if not otp:
            rows.append([method, path, "object", "warning", "string", "Nullable", "Set when a plaintext password was migrated. Often null.", "", "Field", "Read Only", "Do not show unless it has text."])
        assigned = {
            "UserId", "UserName", "IsSuperUser", "Role", "RoleId", "FirmIds", "Token", "DoctorId",
        }
        if not otp:
            assigned.update({"IsPlanActive", "IslastFiveDays", "DaysRemaining"})
        for prop in classes.get("AuthModel") or []:
            if not isinstance(prop, dict):
                continue
            row = response_row(method, path, prop, "data")
            if prop["name"] == "Token":
                row[6] = "JWT. Send as Authorization: Bearer <token>. This action calls CreateToken with 7 x 24 x 60 minutes."
            elif prop["name"] in assigned:
                row[6] = "Assigned by this login action."
            else:
                row[6] = "Not assigned by this login action. Do not rely on it."
            rows.append(row)
        return
    types = []
    if overlay.get("response_type"):
        types.append(overlay["response_type"])
    for typ, code in produced or []:
        if str(code) == "200" and typ.lower() not in {"string", "object"} and typ not in types:
            types.append(typ)
    note = overlay.get("response_note") or "Member of the 200 type on ProducesResponseType. Wrapper fields are Not Defined in Code unless this sheet lists them."
    if not types:
        rows.append([
            method, path, "", "", "", "",
            "Not Defined in Code. Use the Sample response cell on All_APIs. Request body fields are not the response.",
            "", "Not Applicable", "Read Only", "Do not invent nested fields.",
        ])
        return
    for typ in types:
        append_model_fields(rows, method, path, classes, enum_first, typ, typ, note)


def main() -> None:
    classes, enum_first = dev.load_types()
    enum_lists = load_enum_lists()
    new_actions = dev.extract_actions(NEW_CTRL, "New-API")
    old_actions = dev.extract_actions(OLD_CTRL, "Old-API")
    enrich_actions(NEW_CTRL, new_actions)
    enrich_actions(OLD_CTRL, old_actions)
    new_bodies, new_produced = action_bodies(NEW_CTRL)
    old_bodies, old_produced = action_bodies(OLD_CTRL)
    apply_aliases(new_actions, new_bodies, new_produced, stacked_aliases(NEW_CTRL))
    apply_aliases(old_actions, old_bodies, old_produced, stacked_aliases(OLD_CTRL))
    wb = load_workbook(XLSX)
    catalog = catalog_rows(wb)

    contract = []
    fields = []
    statuses = []
    responses = []

    for item in catalog:
        raw_ep = item.get("API Endpoint (as in MD)", "")
        method = method_of(raw_ep, item.get("Method Type", ""))
        path = dev.normalize_path(raw_ep)
        key = (method, path.lower())
        host = item.get("Host", "New-API")
        is_old = "old" in host.lower()
        action = (old_actions if is_old else new_actions).get(dev.path_key(method, path))
        body = (old_bodies if is_old else new_bodies).get(key, "")
        produced = (old_produced if is_old else new_produced).get(key, [])
        overlay = OVERLAY.get(key, {})
        token_text = item.get("Token required", "")
        token_required = dev.needs_token(item, action)
        codes = status_codes(body, token_required)
        checks = checked_names(body)
        patient = item.get("Patient app", "")
        doctor = item.get("Doctor mobile app", "")
        use = mobile_use(patient, doctor)
        base = item.get("Base URL", "")
        body_type = (action or {}).get("body_type") or ""
        props = []
        if body_type:
            props = [p for p in (classes.get(dev.simple_name(body_type)) or []) if isinstance(p, dict)]
        req, opt, cond = [], [], []
        for prop in props:
            level, _ = requirement_for(prop, checks, overlay)
            label = dev.camel(prop["name"])
            if level == "Required":
                req.append(label)
            elif level == "Conditional":
                cond.append(label)
            else:
                opt.append(label)
        path_params = ", ".join(f"{n} ({t})" for n, t in (action or {}).get("route_params") or [])
        query_params = ", ".join(f"{q[0]} ({q[1]})" for q in ((action or {}).get("query") or []))
        if not path_params:
            names = re.findall(r"\{([^}:]+)", path)
            path_params = ", ".join(names) if names else "Not Applicable"
        if not query_params:
            query_params = "Not Applicable"
        file_note = "Yes" if (action or {}).get("form") or (action or {}).get("file_field") else "No"
        page = "Yes" if re.search(r"page(number|size)?|skip|take", query_params, re.I) else "No"
        deprecated = "Yes" if re.search(r"\bdeprecat|no longer|do not use|obsolete", body or "", re.I) else "No"
        replacement = "Not Applicable"
        repl = re.search(r"use instead[^\n]{0,120}|Use POST[^\n]{0,120}", body or "", re.I)
        if repl:
            replacement = repl.group(0)[:180]
        policy = (action or {}).get("policy") or ""
        authz = token_text or ("Anonymous" if not token_required else "Bearer JWT")
        if policy:
            authz = authz + " | " + policy
        api_name = (action or {}).get("action") or "Not Defined in Code"
        business = overlay.get("business") or "Not Defined in Code"
        if "see doc" in path.lower():
            api_name = "Needs Confirmation"
            business = "The endpoint cell in All_APIs is a note, not one URL. Needs Confirmation."
        if path.lower() == "/health":
            api_name = "Health"
            business = "Host health check. It is not a controller action in the API project. Authentication is Not Defined in Code."
        contract.append([
            api_name,
            item.get("What it is used for", ""),
            (action or {}).get("controller") or item.get("MD section", ""),
            (action or {}).get("file") or item.get("Source doc", ""),
            method,
            path,
            base,
            full_url(base, path),
            "Old API" if "old" in host.lower() else "New API",
            item.get("New / Existing / Updated", ""),
            use,
            patient,
            doctor,
            item.get("Clinic web", ""),
            item.get("When to call it", "") or item.get("What it is used for", ""),
            item.get("API User", "") or "Not Defined in Code",
            "No" if not token_required else "Yes",
            authz,
            deprecated,
            replacement,
            ", ".join(req) or "None confirmed",
            ", ".join(opt) or "None",
            ", ".join(cond) or "None",
            path_params,
            query_params,
            ", ".join(str(c) for c in codes) or "Not Defined in Code",
            page,
            file_note,
            business,
            item.get("Do not call this for", "") or "Not Applicable",
            "Not Defined in Code",
            "See All_APIs Sample request and Sample response. Those cells were not rewritten.",
        ])

        for pname, ptype in (action or {}).get("route_params") or []:
            fields.append(field_row(method, path, pname, "Path Parameter", ptype, "Required", "Non-Nullable", "Not Applicable", "Not Applicable", enum_lists, {}, overlay))
        if not (action or {}).get("route_params"):
            for pname in re.findall(r"\{([^}:]+)", path):
                fields.append(field_row(method, path, pname, "Path Parameter", "Not Defined in Code", "Required", "Not Defined in Code", "Not Applicable", "Not Applicable", enum_lists, {}, overlay))
        for q in (action or {}).get("query") or []:
            pname, ptype = q[0], q[1]
            default = q[2] if len(q) > 2 else "Not Defined in Code"
            q_level = checks.get(pname) or "Optional"
            if q_level not in {"Required", "Conditional", "Optional"}:
                q_level = "Optional"
            if q_level == "Optional" and default == "Not Defined in Code":
                simple = dev.simple_name(ptype).lower()
                if not ptype.endswith("?") and simple in {"int", "long", "short", "byte", "decimal", "double", "float", "bool", "datetime", "guid"}:
                    q_level = "Required"
            fields.append(field_row(method, path, pname, "Query Parameter", ptype, q_level, nullable_of(ptype), "Not Defined in Code", "Not Applicable" if q_level != "Required" else "Non-nullable value with no default. [ApiController] rejects a missing value.", enum_lists, {}, overlay, default))
        if (action or {}).get("file_field"):
            fields.append(field_row(method, path, (action or {}).get("file_field"), "Form Data", "IFormFile", "Needs Confirmation", "Not Defined in Code", "File upload. Maximum size is Not Defined in Code on this action.", "Not Applicable", enum_lists, {}, overlay))
        for prop in props:
            level, cond = requirement_for(prop, checks, overlay)
            fields.append(field_row(method, path, prop["name"], "Request Body", prop["type"], level, nullable_of(prop["type"]), prop.get("attrs") or "", cond, enum_lists, enum_first, overlay))
        append_responses(responses, method, path, classes, enum_first, overlay, produced)

        handling = {
            200: "Read the success JSON. Field names start with a lowercase letter.",
            201: "Read the created resource JSON.",
            204: "Success with no body.",
            400: "Show message when it names the field. Otherwise use a generic validation message.",
            401: "Clear the token and open login.",
            403: "Do not retry. The role or ownership check failed.",
            404: "The id was not found. Do not create a replacement id on the device.",
            409: "The state conflicts (already cancelled, slot taken, or similar). Refresh and show message.",
            423: "Wait and try again later. OTP lock is 15 minutes where that action sets it.",
            429: "Wait and try again. Do not loop.",
            500: "Generic failure message. Do not retry in a loop.",
        }
        for code in codes:
            statuses.append([
                method,
                path,
                code,
                "Returned by this action or by [Authorize] when the token is missing." if code == 401 and "Unauthorized" not in body else "Return statement or status constant in this action.",
                handling.get(code, "Not Defined in Code"),
                '{"success":false,"message":"..."}' if code >= 400 else "See All_APIs Sample response",
            ])
        if not codes:
            statuses.append([method, path, "Not Defined in Code", "This action body did not contain a recognized status return.", "Needs Confirmation", "Not Defined in Code"])

    contract_headers = [
        "API Name", "API Description", "Module", "Controller file", "HTTP Method", "Endpoint",
        "Base URL", "Full URL", "Old API / New API", "Catalog status", "Mobile should use",
        "Patient app", "Doctor mobile app", "Clinic web", "Purpose / When to call", "Who can call",
        "Authentication required", "Authorization / token note", "Deprecated", "Replacement API",
        "Required body fields", "Optional body fields", "Conditional body fields",
        "Path parameters", "Query parameters", "HTTP status codes found", "Pagination",
        "File upload", "Business rule confirmed", "Do not call this for", "Mobile screen",
        "Examples",
    ]
    field_headers = [
        "HTTP Method", "Endpoint", "Field Name", "Location", "Data Type", "Required / Optional / Conditional",
        "Nullable", "Default Value", "Description", "Example Value", "Validation Rule",
        "Minimum Length", "Maximum Length", "Minimum Value", "Maximum Value", "Allowed Values",
        "Format", "Read Only / Editable", "Server Generated / Mobile Provided", "Sensitive", "Condition",
    ]
    status_headers = ["HTTP Method", "Endpoint", "Status Code", "When it occurs", "Mobile handling", "Response structure"]
    response_headers = [
        "HTTP Method", "Endpoint", "Parent type", "Field Name", "Data Type", "Nullable",
        "Description", "Example Value", "Nested / Array", "Read Only", "Mobile usage",
    ]

    write_sheet(wb, "Mobile_Start_Here", ["Topic", "Confirmed from code", "Mobile handling"], start_here_rows()[1:])
    write_sheet(wb, "Mobile_API_Contract", contract_headers, contract)
    write_sheet(wb, "Mobile_Request_Fields", field_headers, fields)
    write_sheet(wb, "Mobile_Response_Fields", response_headers, responses)
    write_sheet(wb, "Mobile_Status_Codes", status_headers, statuses)
    flows = [
        ["1", "Mobile OTP login", "POST /api/Otp/RequestOtp", "Anonymous. action=Login, entityType=Mobile, entityId and destination = the mobile.", "otpChallengeId", "POST /api/Account/LoginWithOtp"],
        ["2", "Mobile OTP login", "POST /api/Account/LoginWithOtp", "Send otpChallengeId, code, mobileNo. Do not call VerifyOtp first.", "data.token", "Any New API row whose Authentication required is Yes"],
        ["3", "Password login", "POST /api/Account/Login", "New API row in this workbook. userName and password are both required. Token lifetime in this action is 7 days.", "data.token", "Protected calls on the same host."],
        ["4", "Forgot password", "POST /api/Account/ForgotPasswordAccounts", "New API. Anonymous. Body email.", "userId when more than one account", "POST /api/Account/ForgotPassword"],
        ["5", "Forgot password", "POST /api/Account/ForgotPassword", "userId is Conditional: required only when that email has more than one login.", "Not Applicable", "POST /api/Account/ResetPassword with the token from the link"],
        ["6", "Doctor or reception adds a patient", "POST /api/patient", "New API. patientID 0 and patientName required. Does not create an app login.", "patientID", "Not Defined in Code"],
        ["7", "Public booking can insert a patient", "POST /api/PatientAuth/RequestOtp", "Anonymous. This is booking, not app registration.", "otp challenge", "POST /api/PatientAuth/VerifyOtp"],
        ["8", "Public booking can insert a patient", "POST /api/PatientAuth/VerifyOtp", "Returns bookingSessionId. It does not return a JWT.", "bookingSessionId", "POST /api/Public/Doctors/{id}/Bookings"],
        ["9", "Logout", "POST /api/Account/Logout", "Call the host that issued the token.", "Not Applicable", "Not Applicable"],
    ]
    write_sheet(
        wb,
        "Mobile_Flows",
        ["Step", "Flow", "API", "What to send", "Value used next", "Next API"],
        flows,
    )

    readme = wb["README"]
    note = "Mobile columns were added on every API row in the API sheets. Detail sheets: Mobile_Start_Here, Mobile_API_Contract, Mobile_Request_Fields, Mobile_Response_Fields, Mobile_Status_Codes, Mobile_Flows. Existing API rows were not deleted."
    already = False
    for row in readme.iter_rows(max_col=1, values_only=True):
        if row and row[0] and "Mobile handoff sheets" in str(row[0]):
            already = True
    if not already:
        readme.append([note])

    extra_by_key = {}
    for row in contract:
        # contract columns: 0 name, 4 method, 5 path, 7 full url, 8 old/new, 9 status, 10 mobile use,
        # 15 who, 16 auth, 17 authz, 18 deprecated, 19 replacement, 20 required, 21 optional, 22 conditional,
        # 25 codes, 26 pagination, 27 file, 28 business, 30 screen
        key = (row[4], str(row[5]).lower())
        extra_by_key[key] = row
    field_groups: dict[tuple, list] = {}
    for row in fields:
        field_groups.setdefault((row[0], str(row[1]).lower()), []).append(row)
    response_groups: dict[tuple, list] = {}
    for row in responses:
        response_groups.setdefault((row[0], str(row[1]).lower()), []).append(row)
    status_groups: dict[tuple, list] = {}
    for row in statuses:
        status_groups.setdefault((row[0], str(row[1]).lower()), []).append(row)
    stamp_api_sheets(wb, extra_by_key, field_groups, response_groups, status_groups)

    wb.save(XLSX)
    print(f"catalog {len(catalog)} contract {len(contract)} fields {len(fields)} responses {len(responses)} statuses {len(statuses)}")


EXTRA_HEADERS = [
    "API action name",
    "Controller",
    "Full URL",
    "Old API or New API",
    "Mobile should use",
    "Authentication required",
    "Authorization",
    "Deprecated or active",
    "Replacement API",
    "Required fields",
    "Optional fields",
    "Conditional fields",
    "Request fields",
    "Response fields",
    "HTTP status codes",
    "Error handling",
    "How to authenticate",
    "Role and permission",
    "API sequence",
    "Pagination",
    "Search filter sort",
    "Date and time",
    "File upload",
    "Master data",
    "Notifications",
    "Business rules",
    "Which API mobile should use",
    "Version and change",
    "Network behavior",
    "Security",
    "Mobile screen",
]


def format_request_fields(rows: list) -> str:
    if not rows:
        return "Not Applicable"
    lines = []
    for row in rows:
        lines.append(
            " | ".join(
                [
                    str(row[2]),
                    str(row[3]),
                    str(row[4]),
                    str(row[5]),
                    str(row[6]),
                    "Default: " + str(row[7]),
                    "Example: " + str(row[9]),
                    "Validation: " + str(row[10]),
                    "Min length: " + str(row[11]),
                    "Max length: " + str(row[12]),
                    "Min value: " + str(row[13]),
                    "Max value: " + str(row[14]),
                    "Allowed: " + str(row[15]),
                    "Format: " + str(row[16]),
                    str(row[17]),
                    str(row[18]),
                    "Sensitive: " + str(row[19]),
                    "Condition: " + str(row[20]),
                ]
            )
        )
    return "\n".join(lines)


def format_response_fields(rows: list) -> str:
    if not rows:
        return "Not Defined in Code. Use the Sample response cell on this row."
    lines = []
    for row in rows:
        parent = str(row[2] or "")
        name = str(row[3] or "")
        label = name if not parent or parent == name else f"{parent}.{name}"
        if not label:
            lines.append(str(row[6]))
            continue
        lines.append(
            " | ".join(
                [
                    label,
                    str(row[4]),
                    "Nullable: " + str(row[5]),
                    str(row[8]),
                    "Read Only",
                    str(row[6]),
                    "Example: " + str(row[7]),
                ]
            )
        )
    return "\n".join(lines)


def format_statuses(rows: list) -> str:
    if not rows:
        return "Not Defined in Code"
    return "\n".join(
        f"{row[2]}: {row[3]} Mobile: {row[4]} Body: {row[5]}"
        for row in rows
    )


def sequence_for(method: str, path: str) -> str:
    key = (method, path.lower())
    known = {
        ("POST", "/api/otp/requestotp"): "Conditional. action Login then POST /api/Account/LoginWithOtp. Do not call VerifyOtp before LoginWithOtp.",
        ("POST", "/api/account/loginwithotp"): "Previous: POST /api/Otp/RequestOtp. Pass otpChallengeId, code, and mobileNo. Next: protected APIs with data.token.",
        ("POST", "/api/account/login"): "No previous API. Next: protected APIs with data.token.",
        ("POST", "/api/account/forgotpasswordaccounts"): "Next: POST /api/Account/ForgotPassword. Pass userId when more than one account is returned.",
        ("POST", "/api/account/forgotpassword"): "Previous: POST /api/Account/ForgotPasswordAccounts. Next: POST /api/Account/ResetPassword with the token from the link.",
        ("POST", "/api/account/resetpassword"): "Previous: POST /api/Account/ForgotPassword.",
        ("POST", "/api/patient"): "Requires a doctor, reception, or admin token from login. Does not create a mobile login.",
        ("POST", "/api/patientauth/requestotp"): "Next: POST /api/PatientAuth/VerifyOtp. This is booking, not app login.",
        ("POST", "/api/patientauth/verifyotp"): "Previous: POST /api/PatientAuth/RequestOtp. Next: POST /api/Public/Doctors/{id}/Bookings with bookingSessionId.",
        ("POST", "/api/account/logout"): "Previous: the login that issued the token.",
    }
    return known.get(key, "Not Defined in Code")


def extra_cells(contract_row: list, request_rows: list, response_rows: list, status_rows: list) -> list[str]:
    method = contract_row[4]
    path = contract_row[5]
    auth_required = contract_row[16]
    host = contract_row[8]
    mobile = contract_row[10]
    deprecated = contract_row[18]
    codes = contract_row[25]
    page = contract_row[26]
    file_note = contract_row[27]
    query = contract_row[24]
    if auth_required == "Yes":
        how = f"Authorization: Bearer <data.token>. Obtain the token from {host} POST /api/Account/Login or POST /api/Account/LoginWithOtp."
    else:
        how = "No token. Do not send Authorization."
    role = (
        f"Workbook who-can-call: {contract_row[15]}. "
        f"Patient app column: {contract_row[11]}. Doctor mobile column: {contract_row[12]}. "
        f"Code authorization: {contract_row[17]}. "
        "Allowed or Not Allowed for a role is Needs Confirmation unless that authorization text names the role."
    )
    search = "Not Applicable" if query in {"", "Not Applicable"} else (
        "Query parameters: " + query + ". Each is Required or Optional in Request fields. "
        "Sort order and default filter behavior are Not Defined in Code unless a parameter name says so."
    )
    date_bits = []
    blob = "\n".join(str(r[4]) + " " + str(r[16]) for r in request_rows)
    if "date-time" in blob or "JSON time" in blob:
        date_bits.append("Date or time fields use JSON date-time or time. Timezone is Not Defined in Code for this action.")
    if not date_bits:
        date_bits.append("Not Applicable")
    if file_note == "Yes":
        file_text = "This action accepts form or file data. Field name is in Request fields. MIME type, maximum size, and multiple-file behavior are Not Defined in Code."
    else:
        file_text = "Not Applicable"
    lookup = bool(re.search(r"country|state|city|language|gender|dropdown|master|speciali", path, re.I))
    master = (
        "Lookup endpoint. Use the response id and display fields when Response fields names them. Cache behavior is Not Defined in Code."
        if lookup else "Not Applicable"
    )
    notify = (
        "Notification or device endpoint. Payload fields are in Response fields when the action names a type. Deep link is Not Defined in Code."
        if re.search(r"notification|device|sms|push|fcm", path, re.I) else "Not Applicable"
    )
    errors = (
        "Use the HTTP status codes column. Show the API message when it names the field. Otherwise use a generic message. "
        "401: clear the token and open login. 403: do not retry. 500: generic failure, do not loop. "
        "Field-level error shape is Not Defined in Code unless Request fields lists a validation attribute."
    )
    which = (
        f"Use this row on {host}. Mobile should use: {mobile}. "
        "Do not switch this path to the other host unless another row in this workbook names that host."
    )
    version = (
        f"Catalog status: {contract_row[9]}. Deprecated: {deprecated}. "
        "Contract version, last updated, and breaking change are Not Defined in Code."
    )
    active = "Deprecated" if deprecated == "Yes" else "Active"
    return [
        contract_row[0],
        contract_row[3],
        contract_row[7],
        host,
        mobile,
        auth_required,
        contract_row[17],
        active,
        contract_row[19],
        contract_row[20],
        contract_row[21],
        contract_row[22],
        format_request_fields(request_rows),
        format_response_fields(response_rows),
        codes + "\n" + format_statuses(status_rows),
        errors,
        how,
        role,
        sequence_for(method, path),
        "Yes. Parameters are in Request fields. Total records and total pages are Not Defined in Code unless Response fields names them." if page == "Yes" else "No",
        search,
        " ".join(date_bits),
        file_text,
        master,
        notify,
        contract_row[28],
        which,
        version,
        "Timeout, retry, idempotency, duplicate handling, offline support, and cache behavior are Not Defined in Code.",
        "Use HTTPS on the public host. Send the bearer token when Authentication required is Yes. Do not log passwords, OTPs, or tokens. No extra security header is defined in this action.",
        "Not Defined in Code",
    ]


def stamp_api_sheets(wb, extra_by_key, field_groups, response_groups, status_groups) -> None:
    for name in wb.sheetnames:
        if name.startswith("Mobile_") or name in {"README", "Summary", "Users & Login Details"}:
            continue
        ws = wb[name]
        headers = [c.value for c in ws[1]]
        if "API Endpoint (as in MD)" not in headers:
            continue
        ep_col = headers.index("API Endpoint (as in MD)") + 1
        method_col = headers.index("Method Type") + 1 if "Method Type" in headers else None
        if EXTRA_HEADERS[0] in headers:
            start = headers.index(EXTRA_HEADERS[0]) + 1
        else:
            start = ws.max_column + 1
            for offset, header in enumerate(EXTRA_HEADERS):
                cell = ws.cell(1, start + offset, header)
                cell.fill = HEADER_FILL
                cell.font = HEADER_FONT
                cell.alignment = WRAP
            for offset in range(len(EXTRA_HEADERS)):
                ws.column_dimensions[get_column_letter(start + offset)].width = 36
        for row_index in range(2, ws.max_row + 1):
            raw = ws.cell(row_index, ep_col).value
            if not raw:
                continue
            method_type = ws.cell(row_index, method_col).value if method_col else ""
            method = method_of(str(raw), str(method_type or ""))
            path = dev.normalize_path(str(raw))
            key = (method, path.lower())
            contract_row = extra_by_key.get(key)
            if contract_row is None:
                values = ["Needs Confirmation"] * len(EXTRA_HEADERS)
            else:
                values = extra_cells(
                    contract_row,
                    field_groups.get(key, []),
                    response_groups.get(key, []),
                    status_groups.get(key, []),
                )
            for offset, value in enumerate(values):
                cell = ws.cell(row_index, start + offset, clip(str(value), 30000))
                cell.alignment = WRAP


def field_row(method, path, name, location, type_name, level, nullable, attrs, condition, enum_lists, enum_first, overlay, given_default=None):
    rules, min_len, max_len, min_v, max_v, allowed = validation_text(attrs if isinstance(attrs, str) else "", enum_lists, type_name)
    st = dev.simple_name(type_name)
    if st in enum_lists:
        allowed = enum_lists[st]
    example = ""
    try:
        example = str(dev.example_token(name, type_name, enum_first))
    except Exception:
        example = "Not Defined in Code"
    server_note = (overlay.get("server") or {}).get(name, "")
    desc = server_note or "Confirmed as a C# member on this request. No further description in the attribute."
    if location == "Path Parameter":
        desc = "Path id from the previous screen or from a list API."
        level = "Required"
    provided = who_provides(name, overlay, location)
    editable = "Read Only" if provided == "Server Generated" and location == "Request Body" and name.lower() in {"message", "age"} else "Editable"
    fmt = "JSON date-time" if st.lower() in {"datetime", "datetimeoffset", "dateonly"} else ("JSON time" if st.lower() in {"timeonly", "timespan"} else "Not Applicable")
    default = given_default or "Not Defined in Code"
    return [
        method, path, dev.camel(name) if name[:1].isupper() else name, location, type_name, level, nullable,
        default, desc, example, rules, min_len, max_len, min_v, max_v, allowed, fmt, editable, provided,
        sensitive_of(name), condition or "Not Applicable",
    ]


def response_row(method, path, prop, parent):
    t = prop["type"]
    nested = "Array" if dev.collection_inner(t) else ("Nested object" if dev.simple_name(t) and not dev.is_primitive(t) and dev.simple_name(t).lower() not in dev.PRIMITIVES else "Field")
    example = ""
    try:
        example = str(dev.example_token(prop["name"], t, {}))
    except Exception:
        example = "Not Defined in Code"
    return [
        method,
        path,
        parent,
        dev.camel(prop["name"]),
        t,
        nullable_of(t),
        "Present on this type when the action returns the model. Wrapper fields such as success/data are Not Defined in Code unless the Sample response on All_APIs shows them.",
        example,
        nested,
        "Read Only",
        "Parse if the success JSON includes this name.",
    ]


if __name__ == "__main__":
    main()
