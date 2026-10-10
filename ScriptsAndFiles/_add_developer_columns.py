# -*- coding: utf-8 -*-
"""Append developer handoff columns to the API catalog. Never clears existing cells."""
from __future__ import annotations

import json
import re
from pathlib import Path

from openpyxl import load_workbook
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent / "Homeocentrum_All_New_And_Updated+APIs.xlsx"

CTRL_DIRS = [
    ROOT / "NIGA_API" / "Homeocentrum.Niga.API" / "Controllers",
    ROOT / "NIGA_OldAPI" / "Homeocentrum.Niga.OldAPI" / "Controllers",
]
SCAN_ROOTS = [
    ROOT / "NIGA_API",
    ROOT / "NIGA_OldAPI",
]

NAME = "Tufan Powar"
EMAIL = "tufanpowar001@gmail.com"
MOBILE = "7768046064"
NEW_HOST = "https://devapi2.homeocentrum.com"
OLD_HOST = "https://devapi1.homeocentrum.com"

OTP_TEST = f"""OTP TEST WHILE THE SMS PROVIDER IS NOT LIVE

POST {NEW_HOST}/api/Otp/RequestOtp
Content-Type: application/json
No Authorization header.
Body:
{{"action":"Login","entityType":"Mobile","entityId":"{MOBILE}","destination":"{MOBILE}"}}

No text message is sent. The Msg91 key is empty, so there is no OTP provider. data.delivered can still be true: the API only queues the SMS and continues. Do not wait for a phone message. Do not read the code from the database. The database stores a hash, not the digits.

The 6-digit code is data.devCode in this same JSON. The server adds data.devCode only when that API process is running with ASPNETCORE_ENVIRONMENT=Development. Use data.otpChallengeId and data.devCode within 10 minutes.

Then POST {NEW_HOST}/api/MobileDoctor/LoginWithOtp
{{"otpChallengeId": <data.otpChallengeId>, "code": "<data.devCode>", "mobileNo": "{MOBILE}"}}
data.token is the JWT. Header after that: Authorization: Bearer <data.token>

If POST {NEW_HOST}/api/Otp/RequestOtp returns 404, that published site does not have this action yet. Open {NEW_HOST}/swagger and use only the paths listed there. Account/Login on that host is the password login, not the mobile OTP login.
If the response is 200 and data.devCode is missing, that host is Production. The code is not in the JSON and not on the phone, so LoginWithOtp cannot be completed on that host until the API is started as Development or the Msg91 key is filled.
Do not call /api/Otp/VerifyOtp before LoginWithOtp. That uses the code up.
Do not use /api/PatientAuth/RequestOtp or /api/PatientAuth/VerifyOtp. Those return bookingSessionId for public booking, not a JWT.
"""

NEW_HEADERS = [
    "Dev base URL",
    "Dev full URL example",
    "Content-Type",
    "Required headers",
    "How to login before this call",
    "Path parameters with example",
    "Query parameters with example",
    "Request body fields",
    "Sample JSON body (Tufan Powar)",
    "Sample success response for developer",
    "Sample error response",
    "When to call this",
    "Developer notes",
]

HTTP_ATTR = re.compile(
    r"\[Http(Get|Post|Put|Delete|Patch)(?:\((?:Name\s*=\s*)?[\"']([^\"']*)[\"']\))?\]",
    re.I,
)
ROUTE_ATTR = re.compile(r"\[Route\([\"']([^\"']+)[\"']\)\]")
ALLOW_ANON = re.compile(r"\[AllowAnonymous\]", re.I)
AUTHORIZE = re.compile(r"\[Authorize(?:\((.*?)\))?\]", re.I | re.S)
CLASS_DECL = re.compile(
    r"public\s+(?:sealed\s+|abstract\s+|partial\s+)*class\s+(\w+)(?:\s*:\s*([^{]+))?\s*\{",
    re.M,
)
PROP_RE = re.compile(
    r"((?:\[[^\]]+\]\s*)*)public\s+(?!class\b|record\b|enum\b|interface\b|struct\b|static\b)"
    r"([\w\?\.]+(?:<[^;{]+>)?(?:\[\])?)\s+(\w+)\s*\{\s*get\s*;",
    re.M,
)
ENUM_RE = re.compile(r"public\s+enum\s+(\w+)\s*\{([^}]+)\}", re.M)
RECORD_RE = re.compile(r"public\s+(?:sealed\s+)?record\s+(\w+)\s*\(([^)]*)\)\s*[;{]", re.M)

PRIMITIVES = {
    "string", "int", "long", "short", "byte", "bool", "boolean", "decimal", "double",
    "float", "guid", "datetime", "datetimeoffset", "dateonly", "timeonly", "timespan",
    "object", "dynamic",
}

HEADER_FILL = PatternFill("solid", fgColor="1F4E79")
HEADER_FONT = Font(bold=True, color="FFFFFF", name="Calibri", size=11)
CELL_ALIGN = Alignment(wrap_text=True, vertical="top")
THIN = Border(
    left=Side(style="thin", color="D9E2F3"),
    right=Side(style="thin", color="D9E2F3"),
    top=Side(style="thin", color="D9E2F3"),
    bottom=Side(style="thin", color="D9E2F3"),
)

LOGIN_HOWTO = f"""MOBILE APP LOGIN (patient app and doctor app). API only.

Step 1. POST {NEW_HOST}/api/Otp/RequestOtp
Header: Content-Type: application/json
Do not send Authorization.
Body:
{{"action":"Login","entityType":"Mobile","entityId":"{MOBILE}","destination":"{MOBILE}"}}
action must be the word Login. entityType is forced to Mobile for this action.
Success JSON: success true, data.otpChallengeId, data.expiresAt, data.channel, data.delivered.
{OTP_TEST}
The code is valid for 10 minutes. More than 3 requests in 1 minute returns 429. Wait and try again.
5 wrong codes lock the challenge for 15 minutes.

Step 2. POST {NEW_HOST}/api/MobileDoctor/LoginWithOtp
Header: Content-Type: application/json
Do not send Authorization.
Body:
{{"otpChallengeId":1,"code":"<data.devCode from step 1>","mobileNo":"{MOBILE}"}}
Replace otpChallengeId with data.otpChallengeId from step 1. Replace code with data.devCode. Do not call /api/Otp/VerifyOtp first. VerifyOtp marks the code used, and LoginWithOtp will then say the OTP is already used.
Success JSON: success true, message "Login successful", data.token is the JWT.
Also in data: userId, userName (display name), role, roleId, doctorId, firmIds, isSuperUser.
Send this header on every later call that needs a token:
Authorization: Bearer <data.token>
The token lasts 7 days (7 x 24 x 60 minutes).

If several users share mobile {MOBILE}, LoginWithOtp returns the first active user it finds. Dev script 27 sets the Tufan seed users to this mobile, so the role can differ from the user you expected. To force a role, use password login below.

WEBSITE AND CLINIC WEB PASSWORD LOGIN. Old API.
POST {OLD_HOST}/api/Account/Login
Header: Content-Type: application/json
Body uses the C# names userName and password (lowercase first letter):
{{"userName":"Tufan_Patient","password":"123456"}}
Usernames on the Users & Login Details sheet, all password 123456:
Tufan_Admin (Admin), Tufan_Doctor (Doctor), Tufan_Reception (Reception), Tufan_Account (Account), Tufan_Pharmacy (Pharmacy), Tufan_Patient (Patient), Tufan_Caregiver (caregiver on the patient).
Success data.token is used the same way: Authorization: Bearer <data.token>

DO NOT use these for app login:
POST /api/PatientAuth/RequestOtp
POST /api/PatientAuth/VerifyOtp
Those are anonymous public-booking checks. VerifyOtp returns bookingSessionId and mobile. It does not return a JWT. A booking session cannot call patient-app or doctor-app APIs.
"""

ERROR_TEXT = """400 Bad Request: {"success":false,"message":"<the message names the missing or wrong field>"}
401 Unauthorized: no token, expired token, or no active user for this mobile. Log in again and send Authorization: Bearer <data.token>
403 Forbidden: the token is valid and the role is not allowed on this action.
404 Not Found: the id in the URL or body does not exist.
423 Locked: OTP locked after 5 wrong codes. Wait 15 minutes.
429 Too Many Requests: OTP requested more than 3 times in 1 minute.
500: read message. Do not retry in a loop.
Send JSON with a lowercase first letter on each name (mobileNo, not MobileNo). If the message says a field is missing, retry that one field with a capital first letter.
"""

NO_TOKEN = """No Authorization header.
This action is anonymous. Sending a leftover Bearer token is harmless on most of these actions, but do not require a login before calling it.
App login, when you need it later, is API POST /api/Otp/RequestOtp with action Login, then POST /api/MobileDoctor/LoginWithOtp. See the login rows.
POST /api/PatientAuth/RequestOtp and /api/PatientAuth/VerifyOtp do not log the app in. They only create a bookingSessionId.
"""


def camel(name: str) -> str:
    if not name:
        return name
    if name.isupper():
        return name.lower()
    return name[0].lower() + name[1:]


def normalize_path(path: str) -> str:
    s = (path or "").strip()
    s = re.sub(r"^(GET|POST|PUT|DELETE|PATCH)\s+", "", s, flags=re.I)
    s = s.split("?")[0].strip()
    if not s.startswith("/"):
        s = "/" + s
    s = re.sub(r"/+", "/", s)
    s = re.sub(r"\{([^}:]+)[^}]*\}", r"{\1}", s)
    return s.rstrip("/") or "/"


def path_key(method: str, path: str) -> tuple[str, str]:
    return ((method or "GET").upper(), normalize_path(path).lower())


def join_route(base: str, extra: str) -> str:
    base = (base or "").strip("/")
    extra = (extra or "").strip()
    if not extra:
        return "/" + base
    if extra.lower().startswith("api/") or extra.startswith("/api") or extra == "/health":
        return "/" + extra.lstrip("/")
    if extra.startswith("/"):
        return re.sub(r"/+", "/", f"/{base}{extra}")
    return re.sub(r"/+", "/", f"/{base}/{extra}")


def strip_comments(text: str) -> str:
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    return "\n".join(re.sub(r"//.*", "", line) for line in text.splitlines())


def clip(text: str, limit: int = 32000) -> str:
    text = (text or "").replace("\x00", "")
    if len(text) <= limit:
        return text
    return text[: limit - 40] + "\n... truncated. Open Swagger for the rest."


def split_params(signature: str) -> list[str]:
    parts: list[str] = []
    buf: list[str] = []
    depth = 0
    for ch in signature:
        if ch in "<([":
            depth += 1
        elif ch in ">)]":
            depth = max(0, depth - 1)
        if ch == "," and depth == 0:
            piece = "".join(buf).strip()
            if piece:
                parts.append(piece)
            buf = []
        else:
            buf.append(ch)
    tail = "".join(buf).strip()
    if tail:
        parts.append(tail)
    return parts


def brace_body(text: str, open_index: int) -> str:
    depth = 0
    for i in range(open_index, len(text)):
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                return text[open_index + 1 : i]
    return ""


def simple_name(type_name: str) -> str:
    t = (type_name or "").strip().rstrip("?")
    t = t.split(".")[-1]
    return t


def is_primitive(type_name: str) -> bool:
    t = simple_name(type_name).lower()
    if t in PRIMITIVES:
        return True
    if t.startswith("nullable<"):
        return True
    return False


def collection_inner(type_name: str) -> str | None:
    t = (type_name or "").strip().rstrip("?")
    m = re.match(r"(?:System\.Collections\.Generic\.)?(?:List|IList|IEnumerable|ICollection|IReadOnlyList|IReadOnlyCollection)<\s*([^>]+)\s*>$", t)
    if m:
        return m.group(1).strip()
    if t.endswith("[]"):
        return t[:-2].strip()
    return None


def identity_for(prop: str) -> str | None:
    n = re.sub(r"[^a-z0-9]", "", prop.lower())
    if n.endswith("email") or n in {"email", "emailid", "emailaddress", "mail"}:
        return EMAIL
    if any(k in n for k in ("mobile", "phone", "whatsapp", "contactno")):
        return MOBILE
    if n in {"firstname", "fname"}:
        return "Tufan"
    if n in {"lastname", "lname", "surname"}:
        return "Powar"
    if n in {"middlename", "mname"}:
        return ""
    if n in {"patientname", "fullname", "displayname", "doctorname", "registeredname", "personname"}:
        return NAME
    blocked = ("user", "file", "host", "role", "menu", "type", "code", "key", "column", "table", "sheet", "field", "param", "action", "status", "module", "api", "header", "query")
    if n == "name" or (n.endswith("name") and not any(b in n for b in blocked)):
        return NAME
    return None


def example_token(prop: str, type_name: str, enums: dict[str, str]) -> object:
    n = re.sub(r"[^a-z0-9]", "", prop.lower())
    t = simple_name(type_name)
    tl = t.lower()
    ident = identity_for(prop)
    numeric = tl in {"int", "long", "short", "byte", "int32", "int64"} or tl.startswith("int") or tl.startswith("long")

    if n == "age":
        return 30
    if "gender" in n:
        return 1 if numeric or tl in {"", "object"} else "Male"
    if n in {"action"}:
        return "Login"
    if n in {"entitytype"}:
        return "Mobile"
    if n in {"entityid", "destination"}:
        return MOBILE
    if n in {"code", "otp", "otpcode"}:
        return "123456"
    if "password" in n:
        return "ExamplePassword1"
    if n in {"username", "loginname"}:
        return "Tufan_Patient"
    if n in {"istele", "isteleconsult"}:
        return False
    if n in {"visittype", "consultmode"} or n.endswith("visittype") or n.endswith("consultmode"):
        return "InClinic"
    if n in {"relation", "relationname"}:
        return "Spouse"
    if "language" in n:
        return "en"
    if "time" in n and tl in {"string", "timeonly", "timespan"}:
        return "09:30"
    if n in {"city"} or "address" in n or n in {"state"}:
        return "Pune" if n != "state" else "Maharashtra"
    if "pincode" in n or "postal" in n or "zip" in n:
        return "411001"
    if any(k in n for k in ("note", "remark", "comment", "description", "message", "transcript", "expression", "meaning")):
        return f"Note for {NAME}"
    if ident is not None and (tl == "string" or not numeric):
        return ident

    if numeric:
        if "page" in n and "size" not in n:
            return 1
        if "pagesize" in n or n == "take" or n == "top":
            return 20
        if "skip" in n:
            return 0
        return 1
    if tl in {"bool", "boolean"}:
        return True
    if tl in {"decimal", "double", "float"}:
        if "amount" in n or "fee" in n or "price" in n:
            return 500
        return 0
    if tl == "guid":
        return "00000000-0000-0000-0000-000000000001"
    if tl in {"datetime", "datetimeoffset", "dateonly"}:
        if "birth" in n or n == "dob":
            return "1990-01-15"
        return "2026-10-02T09:00:00"
    if tl in {"timeonly", "timespan"}:
        return "09:30:00"
    if tl in enums:
        return 0
    if "status" in n:
        return "New"
    if tl == "string":
        return f"Example for {NAME}"
    return f"Example for {NAME}"


def load_types() -> tuple[dict[str, list[dict]], dict[str, str]]:
    classes: dict[str, list[dict]] = {}
    enums: dict[str, str] = {}
    for root in SCAN_ROOTS:
        if not root.exists():
            continue
        for path in root.rglob("*.cs"):
            if any(part.lower() in {"bin", "obj", "migrations"} for part in path.parts):
                continue
            text = strip_comments(path.read_text(encoding="utf-8", errors="ignore"))
            for em in ENUM_RE.finditer(text):
                first = re.search(r"[A-Za-z_][A-Za-z0-9_]*", em.group(2) or "")
                enums[em.group(1)] = first.group(0) if first else "0"
            for rm in RECORD_RE.finditer(text):
                props = []
                for part in split_params(rm.group(2)):
                    bits = part.split()
                    if len(bits) >= 2:
                        props.append({"name": bits[-1], "type": bits[-2], "required": True, "attrs": ""})
                if props and (rm.group(1) not in classes or len(props) > len(classes[rm.group(1)])):
                    classes[rm.group(1)] = props
            for cm in CLASS_DECL.finditer(text):
                body = brace_body(text, cm.end() - 1)
                props = []
                for pm in PROP_RE.finditer(body):
                    attrs = pm.group(1) or ""
                    props.append(
                        {
                            "name": pm.group(3),
                            "type": pm.group(2).strip(),
                            "required": "[Required" in attrs,
                            "attrs": attrs,
                        }
                    )
                bases = []
                if cm.group(2):
                    for raw in cm.group(2).split(","):
                        raw = raw.strip()
                        if not raw or raw.startswith("I") and raw[1:2].isupper():
                            continue
                        bases.append(simple_name(raw).split("<")[0])
                if props or bases:
                    current = classes.get(cm.group(1))
                    if current is None or len(props) >= len(current):
                        classes[cm.group(1)] = props
                        classes["__bases__" + cm.group(1)] = bases  # type: ignore[assignment]
    # stitch one level of base properties
    for name, props in list(classes.items()):
        if name.startswith("__bases__"):
            continue
        bases = classes.get("__bases__" + name) or []
        if not isinstance(bases, list):
            continue
        merged = list(props)
        have = {p["name"] for p in merged}
        for base in bases:
            if not isinstance(base, str):
                continue
            for bp in classes.get(base) or []:
                if isinstance(bp, dict) and bp["name"] not in have:
                    merged.append(bp)
                    have.add(bp["name"])
        classes[name] = merged
    classes = {k: v for k, v in classes.items() if not k.startswith("__bases__") and isinstance(v, list)}
    return classes, enums


def sample_object(type_name: str, classes: dict, enums: dict, depth: int = 0, seen: set | None = None) -> object:
    seen = seen or set()
    inner = collection_inner(type_name)
    if inner:
        return [sample_object(inner, classes, enums, depth + 1, seen)]
    t = simple_name(type_name)
    if is_primitive(type_name) or t.lower() in PRIMITIVES or t in enums:
        return example_token("value", t, enums)
    if depth >= 2 or t in seen or t not in classes:
        return {}
    seen = set(seen)
    seen.add(t)
    obj = {}
    for prop in classes.get(t) or []:
        if not isinstance(prop, dict):
            continue
        key = camel(prop["name"])
        pt = prop["type"]
        if collection_inner(pt) or (simple_name(pt) in classes and depth < 2):
            obj[key] = sample_object(pt, classes, enums, depth + 1, seen)
        else:
            obj[key] = example_token(prop["name"], pt, enums)
    return obj


def field_lines(type_name: str, classes: dict, enums: dict) -> str:
    inner = collection_inner(type_name)
    if inner:
        return "JSON array. One item has:\n" + field_lines(inner, classes, enums)
    t = simple_name(type_name)
    props = classes.get(t) or []
    if not props:
        return f"Body type {t}. Fields were not found as a C# class in the API source. Open Swagger and copy the schema. Do not invent field names."
    lines = [f"JSON object. C# type {t}. Send each name with a lowercase first letter."]
    for prop in props:
        if not isinstance(prop, dict):
            continue
        req = "required" if prop.get("required") else "optional"
        enum_note = ""
        st = simple_name(prop["type"])
        if st in enums:
            enum_note = f" Enum. Send 0 for the first declared value ({enums[st]}), or the number Swagger shows."
        max_m = re.search(r"MaxLength\((\d+)\)", prop.get("attrs") or "")
        max_note = f" Max length {max_m.group(1)}." if max_m else ""
        example = example_token(prop["name"], prop["type"], enums)
        lines.append(f"- {camel(prop['name'])} ({prop['type']}, {req}){max_note}{enum_note} Example: {json.dumps(example, ensure_ascii=False)}")
    return "\n".join(lines)


def extract_actions(ctrl_dir: Path, host: str) -> dict[tuple[str, str], dict]:
    found: dict[tuple[str, str], dict] = {}
    if not ctrl_dir.exists():
        return found
    for path in sorted(ctrl_dir.rglob("*Controller.cs")):
        if path.name == "BaseAPIController.cs":
            continue
        raw = path.read_text(encoding="utf-8", errors="ignore")
        text = strip_comments(raw)
        class_m = re.search(r"public\s+(?:partial\s+)?class\s+(\w+)Controller\b", text)
        ctrl_name = class_m.group(1) if class_m else path.stem.replace("Controller", "")
        prefix = ""
        if class_m:
            window = text[max(0, class_m.start() - 900) : class_m.start()]
            brace = window.rfind("}")
            if brace >= 0:
                window = window[brace + 1 :]
            prefix = window
        class_routes = ROUTE_ATTR.findall(prefix)
        base = f"api/{ctrl_name}"
        class_anon = bool(ALLOW_ANON.search(prefix))
        for route in class_routes:
            if "[controller]" in route.lower():
                base = f"api/{ctrl_name}"
                break
            if route.lower().startswith("api") or route.startswith("/"):
                base = route
                break
        matches = list(HTTP_ATTR.finditer(text))
        for i, m in enumerate(matches):
            http = m.group(1).upper()
            template = m.group(2) or ""
            after = text[m.end() : m.end() + 500]
            route_m = ROUTE_ATTR.search(after[:350])
            extra = route_m.group(1) if route_m else template
            full = join_route(base, extra)
            nxt = matches[i + 1].start() if i + 1 < len(matches) else min(len(text), m.end() + 2500)
            block = text[m.start() : nxt]
            sig = re.search(
                r"public\s+(?:async\s+)?(?:[\w\.]+\s*(?:<[^>]+>\s*)?)\s+(\w+)\s*\((.*?)\)\s*(?:where\b.*?)?\{",
                block,
                re.S,
            )
            params = split_params(sig.group(2)) if sig else []
            above = text[max(0, m.start() - 500) : m.start()]
            anon = class_anon or bool(ALLOW_ANON.search(above)) or bool(ALLOW_ANON.search(block[:400]))
            auth_bits = AUTHORIZE.findall(above[-400:])
            policy = " ".join(a.strip() for a in auth_bits if a.strip())
            body_type = ""
            query: list[tuple[str, str]] = []
            route_params: list[tuple[str, str]] = []
            form = False
            file_field = ""
            path_names = {p.split(":")[0] for p in re.findall(r"\{([^}]+)\}", full)}
            for param in params:
                if "CancellationToken" in param or "FromServices" in param:
                    continue
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
                if p_type.lower().rstrip("?") in PRIMITIVES or "FromQuery" in param or simple_name(p_type).lower() in PRIMITIVES:
                    query.append((p_name, p_type))
                    continue
                if not body_type and http in {"POST", "PUT", "PATCH"}:
                    body_type = p_type
            key = path_key(http, full)
            info = {
                "method": http,
                "path": normalize_path(full),
                "file": path.name,
                "controller": ctrl_name,
                "anon": anon,
                "policy": policy,
                "body_type": body_type,
                "query": query,
                "route_params": route_params,
                "form": form,
                "file_field": file_field,
                "action": sig.group(1) if sig else "",
                "host": host,
            }
            prev = found.get(key)
            if prev is None or (info["body_type"] and not prev.get("body_type")):
                found[key] = info
    return found


def fill_path(path: str) -> str:
    def repl(match: re.Match) -> str:
        name = match.group(1).split(":")[0]
        ident = identity_for(name)
        if ident:
            return str(ident)
        n = name.lower()
        if "guid" in n:
            return "00000000-0000-0000-0000-000000000001"
        return "1"

    return re.sub(r"\{([^}]+)\}", repl, path)


def query_from_sample(sample: str) -> str:
    if not sample:
        return ""
    m = re.search(r"\?([^\s#]+)", sample)
    return m.group(1).strip() if m else ""


def host_for(cells: dict[str, str]) -> str:
    status = (cells.get("New / Existing / Updated") or "").lower()
    host = (cells.get("Host") or "").lower()
    if "old-api" in status or "old api" in status or "old-api" in host or "5001" in host:
        return OLD_HOST
    return NEW_HOST


def needs_token(cells: dict[str, str], action: dict | None) -> bool:
    raw = (cells.get("Token required") or "").strip().lower()
    if raw in {"no", "n", "false", "anonymous", "public"}:
        return False
    if raw.startswith("no ") or raw.startswith("omit"):
        return False
    if action and action.get("anon") and not raw:
        return False
    if raw:
        return True
    if action and action.get("anon"):
        return False
    return True


def dumps(obj: object) -> str:
    return json.dumps(obj, indent=2, ensure_ascii=False)


def special_case(method: str, path: str) -> dict | None:
    key = (method.upper(), normalize_path(path).lower())
    mobile_login_req = {
        "action": "Login",
        "entityType": "Mobile",
        "entityId": MOBILE,
        "destination": MOBILE,
    }
    mobile_login_ok = {
        "success": True,
        "data": {
            "otpChallengeId": 1,
            "destinationMasked": "******6064",
            "expiresAt": "2026-10-02T10:10:00Z",
            "channel": "sms",
            "delivered": False,
            "message": "OTP created but sms delivery did not confirm. Check provider/SMTP settings.",
            "devCode": "123456",
        },
    }
    login_otp_req = {"otpChallengeId": 1, "code": "123456", "mobileNo": MOBILE}
    login_otp_ok = {
        "success": True,
        "message": "Login successful",
        "data": {
            "userId": 1,
            "userName": NAME,
            "firstName": None,
            "lastName": None,
            "isSuperUser": False,
            "role": "Patient",
            "token": "<JWT. Paste this value after Bearer >",
            "firmIds": "",
            "roleId": 1,
            "doctorId": None,
            "receptionStaffId": None,
            "isPlanActive": False,
            "islastFiveDays": False,
            "daysRemaining": 0,
        },
    }
    table = {
        ("POST", "/api/otp/requestotp"): {
            "body": mobile_login_req,
            "fields": "action string required. For the mobile app send Login. PatientAuth here is not app login.\nentityType string required. Send Mobile.\nentityId string required. Send 7768046064.\ndestination string required. Send 7768046064. For Login the server rewrites entityType, entityId, and destination to the digits of destination.",
            "success": mobile_login_ok,
            "notes": OTP_TEST,
        },
        ("POST", "/api/otp/verifyotp"): {
            "body": {"otpChallengeId": 1, "code": "123456"},
            "fields": "otpChallengeId number required. From RequestOtp data.otpChallengeId.\ncode string required. The 6-digit devCode or SMS code.",
            "success": {"success": True, "message": "OTP verified."},
            "notes": "Do not use this as the mobile app login. It only marks the challenge used and returns no JWT. After this, LoginWithOtp rejects the same code as already used. Mobile login is RequestOtp (action Login) then MobileDoctor/LoginWithOtp.",
        },
        ("POST", "/api/mobiledoctor/loginwithotp"): {
            "body": login_otp_req,
            "fields": "otpChallengeId number required. data.otpChallengeId from RequestOtp.\ncode string required. data.devCode from RequestOtp. Max length 12.\nmobileNo string required. 7768046064. Must be the same mobile used in RequestOtp. Max length 20.",
            "success": login_otp_ok,
            "notes": "This is step 2 of mobile login. data.userName is FirstName + LastName, so it is Tufan Powar when that is the user record. data.token is the JWT. The challenge Action must be Login. A PatientAuth challenge is rejected with 'OTP is not a login challenge.' If no UserMaster row has this mobile, the response is 401 No account for this mobile number. If IsUserActivated is not true, the response is 401 Account is deactivated.",
        },
        ("POST", "/api/account/login"): {
            "body": {"userName": "Tufan_Patient", "password": "123456"},
            "fields": "userName string required. One of Tufan_Admin, Tufan_Doctor, Tufan_Reception, Tufan_Account, Tufan_Pharmacy, Tufan_Patient, Tufan_Caregiver.\npassword string required. Seed password on the Users sheet is 123456.",
            "success": login_otp_ok,
            "notes": f"Password login. Same JSON on both hosts: POST {NEW_HOST}/api/Account/Login and POST {OLD_HOST}/api/Account/Login. The clinic website uses {OLD_HOST}. JSON names are userName and password. Mobile apps do not use this for the OTP screen. They call Otp/RequestOtp then MobileDoctor/LoginWithOtp on {NEW_HOST}. Change userName to Tufan_Doctor, Tufan_Reception, Tufan_Admin, Tufan_Account, Tufan_Pharmacy, or Tufan_Caregiver when you need that role. Password for each is 123456.",
        },
        ("GET", "/health"): {
            "body": None,
            "fields": "No body.",
            "success": {"success": True, "status": "Healthy", "api": "API"},
            "notes": f"No login. GET {NEW_HOST}/health and the same path on Old API {OLD_HOST}/health. Old API returns api = Old API. This is not counted by the rate limit. If the published host returns 404, that build does not expose /health; use {NEW_HOST}/swagger to see the live paths.",
        },
        ("POST", "/api/patientauth/requestotp"): {
            "body": {"mobile": MOBILE},
            "fields": "mobile string required. 7768046064.",
            "success": {
                "success": True,
                "otpChallengeId": 1,
                "expiresAt": "2026-10-02T10:10:00Z",
                "destinationMasked": "******6064",
                "devCode": "123456",
            },
            "notes": "PUBLIC BOOKING ONLY. This does not log in the patient app or the doctor app. There is no token in this response. The challenge Action stored is PatientAuth, and MobileDoctor/LoginWithOtp will reject it. Use it only before PatientAuth/VerifyOtp when the public site books a visit.",
        },
        ("POST", "/api/patientauth/verifyotp"): {
            "body": {"mobile": MOBILE, "code": "123456"},
            "fields": "mobile string required. 7768046064.\ncode string required. devCode from PatientAuth/RequestOtp.",
            "success": {"success": True, "bookingSessionId": 1, "mobile": MOBILE},
            "notes": "PUBLIC BOOKING ONLY. bookingSessionId is the OTP challenge id. It is not a JWT. Do not send it as Authorization. The patient app must not call this to enter the app. App entry is Otp/RequestOtp action Login, then MobileDoctor/LoginWithOtp, then Authorization: Bearer data.token.",
        },
    }
    return table.get(key)


def related_routes(actions: dict, needle: str) -> list[str]:
    needle = (needle or "").lower().strip().strip("/")
    if not needle or needle in {"see", "doc", "api"}:
        return []
    uniq = []
    seen = set()
    for info in actions.values():
        if needle not in (info.get("path") or "").lower():
            continue
        line = f"{info['method']} {info.get('host') or NEW_HOST}{info['path']}"
        if line not in seen:
            seen.add(line)
            uniq.append(line)
    return sorted(uniq)


def build_row(cells: dict[str, str], action: dict | None, classes: dict, enums: dict, actions: dict | None = None) -> list[str]:
    endpoint = cells.get("API Endpoint (as in MD)") or ""
    method = (cells.get("Method Type") or "").upper()
    if not method:
        m = re.match(r"^(GET|POST|PUT|DELETE|PATCH)\b", endpoint.strip(), re.I)
        method = m.group(1).upper() if m else "GET"
    path = normalize_path(endpoint)
    if "see doc" in endpoint.lower() or path.endswith("/...") or method in {"SEE", "SEE DOC"}:
        needle = [p for p in path.lower().split("/") if p and p not in {"see", "doc", "api", "..."}]
        needle_s = needle[-1] if needle else ""
        routes = related_routes(actions or {}, needle_s)
        listing = "\n".join(routes) if routes else f"Open Old API Swagger at {OLD_HOST}/swagger and search this controller name."
        text = (
            f"This row is a group pointer, not one URL. Do not call the text in the old endpoint cell.\n"
            f"These are the real calls whose path contains '{needle_s}':\n{listing}\n"
            f"Host for these classic doctor-web calls is Old API {OLD_HOST} unless a line above shows {NEW_HOST}.\n"
            "Login first with the password login on the Users & Login Details sheet, then send Authorization: Bearer <data.token>.\n"
            f"Sample person when a body has a name, email, or mobile: {NAME}, {EMAIL}, {MOBILE}."
        )
        return [
            OLD_HOST,
            f"See the list in Developer notes. Group: {needle_s}",
            "application/json when the real call has a body",
            "Accept: application/json\nAuthorization: Bearer <data.token from password login>\nContent-Type: application/json",
            LOGIN_HOWTO,
            "Each real URL is listed in Developer notes. Replace {id} with 1 until you have the real id.",
            "Read the query on the real action in Swagger if the list above has no question mark.",
            "Open the real URL in Swagger for the field list. Do not send a body to the placeholder text.",
            "{}",
            cells.get("Sample response") or '{"success":true,"message":"OK","data":{}}',
            ERROR_TEXT,
            (cells.get("What it is used for") or "") + "\nCall only a real URL from Developer notes.",
            text,
        ]
    base = host_for(cells)
    filled = fill_path(path)
    token = needs_token(cells, action)
    special = special_case(method, path)

    query_pairs = list(action.get("query") or []) if action else []
    route_pairs = list(action.get("route_params") or []) if action else []
    if not route_pairs:
        route_pairs = [(n.split(":")[0], "string") for n in re.findall(r"\{([^}]+)\}", path)]

    sample_q = query_from_sample(cells.get("Sample request") or "")
    if query_pairs:
        bits = []
        for name, typ in query_pairs:
            bits.append(f"{camel(name)}={example_token(name, typ, enums)}")
        q = "&".join(str(b) for b in bits)
        if sample_q:
            known = {b.split("=")[0] for b in bits}
            for piece in sample_q.split("&"):
                key = piece.split("=")[0]
                if key and key not in known and camel(key) not in known:
                    q = q + "&" + piece
    else:
        q = sample_q
    full = base + filled + (("?" + q) if q else "")

    body_type = (action or {}).get("body_type") or ""
    form = bool((action or {}).get("form"))
    if special:
        fields = special["fields"]
        success = dumps(special["success"])
        if special.get("body") is None:
            content = "No body"
            body_json = "No JSON body. Call the URL in Dev full URL example."
        else:
            content = "application/json"
            body_json = dumps(special["body"])
    elif form:
        content = "multipart/form-data"
        file_field = (action or {}).get("file_field") or "file"
        body_json = f"No JSON body. Send form-data. File field name: {file_field}. Other form fields are listed under Request body fields."
        fields = f"Form file field `{file_field}`: choose a small jpg or pdf from this PC.\n" + (field_lines(body_type, classes, enums) if body_type else "Add any other [FromForm] fields Swagger shows for this action.")
        success = success_text(cells)
    elif body_type and method in {"POST", "PUT", "PATCH"}:
        content = "application/json"
        obj = sample_object(body_type, classes, enums)
        if isinstance(obj, dict):
            # force identity keys if the generator left a generic sample
            for k, v in list(obj.items()):
                if isinstance(v, str) and v == "sample":
                    ident = identity_for(k)
                    if ident is not None:
                        obj[k] = ident
        body_json = dumps(obj) if obj != {} else "{}"
        fields = field_lines(body_type, classes, enums)
        success = success_text(cells)
    elif method in {"POST", "PUT", "PATCH"}:
        content = "application/json"
        body_json = "{}"
        fields = "No request class was matched in the controller source. If Swagger shows a body, copy that schema. If Swagger shows no body, send {} or no body."
        success = success_text(cells)
    else:
        content = "No body"
        body_json = "No JSON body. Call the URL in Dev full URL example."
        fields = "No body."
        success = success_text(cells)

    if route_pairs:
        path_text = "\n".join(
            f"{{{name}}} = {fill_path('{' + name + '}')} ({typ}). Replace the 1 if you already know the real id."
            for name, typ in route_pairs
        )
    else:
        path_text = "No path id. The URL path is fixed."

    if query_pairs or q:
        if query_pairs:
            q_text = "\n".join(
                f"{camel(name)} ({typ}) example {example_token(name, typ, enums)}"
                for name, typ in query_pairs
            )
            if sample_q:
                q_text += "\nQuery already written in the old Sample request column was kept when the name was not in source: " + sample_q
        else:
            q_text = "Use this query string from the existing Sample request column: ?" + q
    else:
        q_text = "No query string."

    headers = ["Accept: application/json"]
    if content == "application/json":
        headers.append("Content-Type: application/json")
    elif content == "multipart/form-data":
        headers.append("Content-Type: multipart/form-data (the HTTP client sets the boundary; do not type the boundary by hand)")
    if token:
        headers.append("Authorization: Bearer <data.token from login>")
    else:
        headers.append("Do not send Authorization.")

    who = cells.get("Who should call it") or ""
    purpose = cells.get("What it is used for") or ""
    tasks = cells.get("Task IDs") or ""
    when = "\n".join(
        [
            f"Who should call it: {who or 'See the API User column.'}",
            f"Patient app: {cells.get('Patient app') or ''}",
            f"Doctor mobile app: {cells.get('Doctor mobile app') or ''}",
            f"Clinic web: {cells.get('Clinic web') or ''}",
            f"Common for all: {cells.get('Common for all') or ''}",
            f"API user / role named in the catalog: {cells.get('API User') or ''}",
            f"What it is used for: {purpose}",
            f"Task IDs: {tasks or 'none on this row'}",
            f"Week sheet: {cells.get('Week') or ''}",
        ]
    )

    notes = [
        f"Sample person in this row's new columns: {NAME}, {EMAIL}, {MOBILE}.",
        "Existing columns on this row were left as they were. Use this right-hand block when the old sample is empty or only says OK.",
        "JSON property names: lowercase the first letter. userName, mobileNo, otpChallengeId.",
        "Dates: yyyy-MM-dd or yyyy-MM-ddTHH:mm:ss. Booleans: true or false, not 1 or yes.",
        "Numbers that are ids: 1 means 'replace with the id you already received from a previous list API'.",
    ]
    if "visittype" in fields.lower() or "consultmode" in fields.lower() or "InClinic" in body_json:
        notes.append("visitType and consultMode accept InClinic or Tele. For a clinic visit send InClinic and isTele false. For a video visit send Tele and isTele true. AppointmentTime is 24-hour HH:mm, for example 09:30.")
    if "relation" in fields.lower():
        notes.append("Spouse is an example relation label. Call GET /api/Family/Relations with the token and use a relationId from that list when the screen has loaded relations.")
    if action:
        notes.append(f"Source controller: {action.get('file')} action {action.get('action')}.")
        if action.get("policy"):
            notes.append("Extra authorize rule near this action: " + re.sub(r"\s+", " ", action["policy"])[:500])
        if action.get("body_type"):
            notes.append(f"Request C# type: {action['body_type']}.")
    else:
        notes.append("No matching controller method was parsed for this path. The URL and the old columns are still the contract to call. Confirm the body in Swagger if this is a write call.")
    if special and special.get("notes"):
        notes.append(special["notes"])
    notes.append(f"Swagger UI: {base}/swagger  Login for Swagger is written on the README sheet of this same workbook. Do not ask for it separately.")
    notes.append("After login, store data.token. When a call returns 401, login again. Do not keep calling with the old token.")

    howto = LOGIN_HOWTO if token else NO_TOKEN
    if special and special.get("notes") and path.lower() in {
        "/api/account/login",
        "/api/mobiledoctor/loginwithotp",
        "/api/otp/requestotp",
        "/api/otp/verifyotp",
        "/api/patientauth/requestotp",
        "/api/patientauth/verifyotp",
        "/health",
    }:
        howto = howto + "\n" + special["notes"]

    return [
        clip(base),
        clip(full),
        clip(content),
        clip("\n".join(headers)),
        clip(howto),
        clip(path_text),
        clip(q_text),
        clip(fields),
        clip(body_json),
        clip(success if isinstance(success, str) else dumps(success)),
        clip(ERROR_TEXT),
        clip(when),
        clip("\n".join(notes)),
    ]


def success_text(cells: dict[str, str]) -> str:
    existing = (cells.get("Sample response") or "").strip()
    generic = re.sub(r"\s+", "", existing)
    if existing and generic not in {
        '{"success":true,"message":"OK","data":{}}',
        '{"success":true,"message":"OK","data":null}',
    }:
        return (
            "Use this response shape. It is copied here so you do not have to read the old column.\n"
            + existing
            + f"\n\nWhere a person is returned, the sample identity is {NAME}, {EMAIL}, {MOBILE}."
        )
    return (
        '{\n  "success": true,\n  "message": "OK",\n  "data": {}\n}\n\n'
        "data is filled by this action. Call it once after login and keep the JSON you receive. "
        f"Do not invent fields. Person fields inside data use {NAME}, {EMAIL}, {MOBILE} when you send those in the request. "
        "Login calls are different: data.token is the JWT. See How to login before this call."
    )


def header_map(ws) -> dict[str, int]:
    mapping = {}
    for col in range(1, ws.max_column + 1):
        val = ws.cell(1, col).value
        if val:
            mapping[str(val).strip()] = col
    return mapping


def ensure_headers(ws) -> int:
    mapping = header_map(ws)
    if all(h in mapping for h in NEW_HEADERS):
        return mapping[NEW_HEADERS[0]]
    # drop a partial trailing copy of our headers so a re-run cannot duplicate them
    start = ws.max_column + 1
    while start > 1 and not ws.cell(1, start - 1).value:
        start -= 1
    for offset, header in enumerate(NEW_HEADERS):
        cell = ws.cell(1, start + offset, header)
        cell.fill = HEADER_FILL
        cell.font = HEADER_FONT
        cell.alignment = CELL_ALIGN
    return start


def widen(ws, start: int) -> None:
    widths = {
        "Dev base URL": 28,
        "Dev full URL example": 64,
        "Content-Type": 28,
        "Required headers": 46,
        "How to login before this call": 62,
        "Path parameters with example": 42,
        "Query parameters with example": 42,
        "Request body fields": 62,
        "Sample JSON body (Tufan Powar)": 62,
        "Sample success response for developer": 62,
        "Sample error response": 55,
        "When to call this": 55,
        "Developer notes": 62,
    }
    for offset, header in enumerate(NEW_HEADERS):
        ws.column_dimensions[get_column_letter(start + offset)].width = widths[header]


def expand_filters(ws) -> None:
    last = get_column_letter(ws.max_column)
    end = max(ws.max_row, 2)
    ref = f"A1:{last}{end}"
    if ws.auto_filter and ws.auto_filter.ref:
        ws.auto_filter.ref = ref
    for table in ws.tables.values():
        table.ref = ref


def read_cells(ws, mapping: dict[str, int], row: int) -> dict[str, str]:
    out = {}
    for name, col in mapping.items():
        if name in NEW_HEADERS:
            continue
        val = ws.cell(row, col).value
        out[name] = "" if val is None else str(val).strip()
    return out


def fill_catalog(ws, actions, classes, enums) -> int:
    start = ensure_headers(ws)
    mapping = header_map(ws)
    ep_col = mapping.get("API Endpoint (as in MD)")
    if not ep_col:
        return 0
    written = 0
    for row in range(2, ws.max_row + 1):
        if not ws.cell(row, ep_col).value:
            continue
        cells = read_cells(ws, mapping, row)
        endpoint = cells.get("API Endpoint (as in MD)") or ""
        method = (cells.get("Method Type") or "").upper()
        action = actions.get(path_key(method, endpoint))
        values = build_row(cells, action, classes, enums, actions)
        for offset, value in enumerate(values):
            cell = ws.cell(row, start + offset, value)
            cell.alignment = CELL_ALIGN
            cell.border = THIN
        written += 1
    widen(ws, start)
    expand_filters(ws)
    ws.auto_filter.ref = f"A1:{get_column_letter(ws.max_column)}{max(ws.max_row, 2)}"
    ws.freeze_panes = "A2"
    return written


def fill_users(ws) -> None:
    start = ws.max_column + 1
    while start > 1 and not ws.cell(1, start - 1).value:
        start -= 1
    headers = [
        "Sample person",
        "Email for samples",
        "Mobile for OTP",
        "Mobile app login (use this for the apps)",
        "Step 1 Request OTP URL",
        "Step 1 JSON",
        "Step 1 success",
        "Step 2 LoginWithOtp URL",
        "Step 2 JSON",
        "Step 2 success",
        "Password login JSON for THIS row",
        "Do not use for app login",
    ]
    mapping = header_map(ws)
    if headers[0] in mapping:
        start = mapping[headers[0]]
    else:
        for offset, header in enumerate(headers):
            cell = ws.cell(1, start + offset, header)
            cell.fill = HEADER_FILL
            cell.font = HEADER_FONT
            cell.alignment = CELL_ALIGN
    user_col = mapping.get("Login username") or mapping.get("User / Test User") or 4
    for row in range(2, ws.max_row + 1):
        username = ws.cell(row, user_col).value
        if not username:
            continue
        username = str(username).strip()
        values = [
            NAME,
            EMAIL,
            MOBILE,
            f"Patient app and doctor app login host is {NEW_HOST}. Step 1 then Step 2. Do not use the Old API password URL for the mobile OTP screen. {OTP_TEST}",
            f"POST {NEW_HOST}/api/Otp/RequestOtp",
            dumps({"action": "Login", "entityType": "Mobile", "entityId": MOBILE, "destination": MOBILE}),
            "HTTP 200. Save data.otpChallengeId. The 6-digit code is data.devCode, and only when the API environment is Development. No SMS is sent while the Msg91 key is empty. data.delivered true does not mean the phone received a message. No JWT yet. 404 means this path is not on the published site; check /swagger.",
            f"POST {NEW_HOST}/api/MobileDoctor/LoginWithOtp",
            dumps({"otpChallengeId": 1, "code": "<data.devCode>", "mobileNo": MOBILE}),
            f"HTTP 200. data.token is the JWT. data.userName is the display name ({NAME} when that is the user). Header after this: Authorization: Bearer <data.token>. Token lasts 7 days.",
            dumps({"userName": username, "password": "123456"}),
            "POST /api/PatientAuth/RequestOtp and POST /api/PatientAuth/VerifyOtp return bookingSessionId only. They are for public booking. They do not open the app. Also do not call /api/Otp/VerifyOtp between step 1 and step 2; that uses up the code.",
        ]
        note = (
            f"Password login for this row only: POST {OLD_HOST}/api/Account/Login with the JSON in Password login JSON for THIS row. "
            f"OTP login with {MOBILE} does not choose {username}. It returns the first active user whose mobile is {MOBILE}. "
            "Use password login when you must be this role."
        )
        values[3] = values[3] + " " + note
        for offset, value in enumerate(values):
            cell = ws.cell(row, start + offset, clip(value))
            cell.alignment = CELL_ALIGN
            cell.border = THIN
    for offset, header in enumerate(headers):
        ws.column_dimensions[get_column_letter(start + offset)].width = 36 if offset < 3 else 62


def readme_lines() -> list[str]:
    return [
        "Developer columns added 02-Oct-2026",
        "Columns to the right of the old Source doc column were added for web and mobile developers. Existing cells stay. Hosts in this workbook are the public API hosts.",
        f"API base: {NEW_HOST}/api    Old API base: {OLD_HOST}/api",
        f"Sample person: {NAME}. Email: {EMAIL}. Mobile: {MOBILE}.",
        f"Mobile app login: POST {NEW_HOST}/api/Otp/RequestOtp with action Login, then POST {NEW_HOST}/api/MobileDoctor/LoginWithOtp. The JWT is data.token.",
        OTP_TEST,
        f"Clinic web password login: POST {OLD_HOST}/api/Account/Login. Usernames and password 123456 are on Users & Login Details. JSON fields are userName and password.",
        "POST /api/PatientAuth/RequestOtp and POST /api/PatientAuth/VerifyOtp are public booking. They return bookingSessionId, not a JWT.",
        "If several seed users share mobile 7768046064, OTP login returns the first active match. Use password login with the username on Users & Login Details when the role must be exact.",
    ]


def append_readme(ws) -> None:
    lines = readme_lines()
    marker = lines[0]
    start = None
    for row in range(1, ws.max_row + 1):
        val = ws.cell(row, 1).value
        if val and marker in str(val):
            start = row
            break
    if start is None:
        start = ws.max_row + 2
    for offset, line in enumerate(lines):
        cell = ws.cell(start + offset, 1, line)
        cell.alignment = Alignment(wrap_text=True, vertical="top")


def swap_published_hosts(wb) -> None:
    repls = (
        ("http://127.0.0.1:5002", NEW_HOST),
        ("http://localhost:5002", NEW_HOST),
        ("https://127.0.0.1:5002", NEW_HOST),
        ("http://127.0.0.1:5001", OLD_HOST),
        ("http://localhost:5001", OLD_HOST),
        ("https://127.0.0.1:5001", OLD_HOST),
        ("Old-API :5001", f"Old-API {OLD_HOST}"),
        ("API :5002", f"API {NEW_HOST}"),
    )
    for ws in wb.worksheets:
        for row in ws.iter_rows():
            for cell in row:
                if not isinstance(cell.value, str):
                    continue
                text = cell.value
                if "127.0.0.1" not in text and "localhost:" not in text and ":5001" not in text and ":5002" not in text:
                    continue
                for old, new in repls:
                    text = text.replace(old, new)
                cell.value = text


def append_summary(ws) -> None:
    marker = "Developer columns added"
    for row in range(1, ws.max_row + 1):
        val = ws.cell(row, 1).value
        if val and marker in str(val):
            return
    row = ws.max_row + 2
    ws.cell(row, 1, "Developer columns added 02-Oct-2026 on every API sheet, to the right of the old columns. Endpoint counts above are unchanged. Old cells were not removed.")


def main() -> None:
    classes, enums = load_types()
    actions: dict[tuple[str, str], dict] = {}
    for folder in CTRL_DIRS:
        parsed = extract_actions(folder, NEW_HOST if "NIGA_API" in str(folder) else OLD_HOST)
        for key, info in parsed.items():
            prev = actions.get(key)
            if prev is None or (info.get("body_type") and not prev.get("body_type")):
                actions[key] = info
    wb = load_workbook(OUT)
    total = 0
    matched = 0
    for name in wb.sheetnames:
        ws = wb[name]
        if ws.cell(1, 1).value and str(ws.cell(1, 1).value).strip() == "Sr No":
            before = 0
            mapping = header_map(ws)
            ep_col = mapping.get("API Endpoint (as in MD)", 2)
            method_col = mapping.get("Method Type", 3)
            for row in range(2, ws.max_row + 1):
                ep = ws.cell(row, ep_col).value
                if not ep:
                    continue
                method = str(ws.cell(row, method_col).value or "")
                if path_key(method, str(ep)) in actions:
                    matched += 1
                before += 1
            total += fill_catalog(ws, actions, classes, enums)
            print(f"{name}: wrote {before} rows, controller matches in index {matched}")
            matched = 0
        elif name.startswith("Users"):
            fill_users(ws)
            print(f"{name}: login columns added")
        elif name == "README":
            append_readme(ws)
            print("README: note appended")
        elif name == "Summary":
            append_summary(ws)
            print("Summary: note appended")
    swap_published_hosts(wb)
    wb.save(OUT)
    print(f"saved {OUT} data rows written {total} actions indexed {len(actions)} types {len(classes)}")


if __name__ == "__main__":
    main()
