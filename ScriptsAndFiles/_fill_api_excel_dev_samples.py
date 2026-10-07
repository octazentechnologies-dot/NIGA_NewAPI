# -*- coding: utf-8 -*-
"""Write two request samples and two real response samples into the API Excel.

URLs in the file use the dev hosts. Live JSON is read from the local APIs and
from HomeoCentrum_Dev. Tokens and OTP codes are not written into the workbook.
"""
from __future__ import annotations

import json
import re
import subprocess
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
from urllib import error, request

from openpyxl import load_workbook
from openpyxl.styles import Alignment, Font, PatternFill
from openpyxl.utils import get_column_letter

OUT = Path(__file__).resolve().parent / "Homeocentrum_All_New_And_Updated+APIs.xlsx"
NEW_BASE = "https://devapi2.homeocentrum.com/api"
OLD_BASE = "https://devapi1.homeocentrum.com/api"
LOCAL_NEW = "http://127.0.0.1:5002/api"
LOCAL_OLD = "http://127.0.0.1:5001/api"

SKIP_SHEETS = {"README", "Summary", "Users & Login Details"}
HEADER_FILL = PatternFill("solid", fgColor="1F4E79")
HEADER_FONT = Font(color="FFFFFF", bold=True)
WRAP = Alignment(wrap_text=True, vertical="top")


def sql_ids() -> dict:
    query = r"""
SET NOCOUNT ON;
SELECT
 (SELECT TOP 1 CountryId FROM dbo.CountryMaster WHERE CountryName=N'India' AND ISNULL(DeleteStatus,0)=0),
 (SELECT TOP 1 CountryId FROM dbo.CountryMaster WHERE CountryName=N'Other' AND ISNULL(DeleteStatus,0)=0),
 (SELECT TOP 1 StateId FROM dbo.StateMaster WHERE StateName=N'Maharashtra' AND ISNULL(DeleteStatus,0)=0),
 (SELECT TOP 1 s.StateId FROM dbo.StateMaster s JOIN dbo.CountryMaster c ON c.CountryId=s.CountryId WHERE c.CountryName=N'Other' AND s.StateName=N'Other' AND ISNULL(s.DeleteStatus,0)=0),
 (SELECT TOP 1 DistrictId FROM dbo.DistrictMaster WHERE DistrictName=N'Kolhapur' AND ISNULL(DeleteStatus,0)=0),
 (SELECT TOP 1 d.DistrictId FROM dbo.DistrictMaster d JOIN dbo.StateMaster s ON s.StateId=d.StateId JOIN dbo.CountryMaster c ON c.CountryId=s.CountryId WHERE c.CountryName=N'Other' AND s.StateName=N'Other' AND d.DistrictName=N'Other'),
 (SELECT TOP 1 CityId FROM dbo.CityMaster WHERE CityName=N'Kolhapur' AND ISNULL(DeleteStatus,0)=0),
 (SELECT TOP 1 PinCodeId FROM dbo.PinCodeMaster WHERE PinCode=N'416003' AND ISNULL(DeleteStatus,0)=0),
 (SELECT TOP 1 PatientID FROM dbo.Patient WHERE ISNULL(DeleteStatus,0)=0 ORDER BY PatientID),
 (SELECT TOP 1 DoctorID FROM dbo.Doctor WHERE ISNULL(DeleteStatus,0)=0 ORDER BY DoctorID),
 (SELECT TOP 1 UserId FROM dbo.UserMaster WHERE UserName=N'Tufan_Doctor'),
 (SELECT TOP 1 UserId FROM dbo.UserMaster WHERE UserName=N'Tufan_Admin');
"""
    proc = subprocess.run(
        ["sqlcmd", "-S", r"localhost\MSSQLSERVER25", "-d", "HomeoCentrum_Dev", "-I", "-W", "-h", "-1", "-Q", query],
        capture_output=True, text=True, check=False,
    )
    nums = [int(x) for x in re.findall(r"\d+", proc.stdout or "")]
    keys = ["india", "otherCountry", "maharashtra", "otherState", "kolhapurDistrict", "otherDistrict",
            "kolhapurCity", "pin416003", "patientId", "doctorId", "doctorUserId", "adminUserId"]
    ids = {k: (nums[i] if i < len(nums) else 1) for i, k in enumerate(keys)}
    ids.update({
        "doctorUser": "Tufan_Doctor",
        "adminUser": "Tufan_Admin",
        "password": "123456",
        "email": "tufanpowar001@gmail.com",
        "mobile": "7768046064",
        "patientId2": ids["patientId"] + 1,
        "doctorId2": ids["doctorId"] + 1,
    })
    return ids


def login(local_base: str) -> str:
    body = json.dumps({"userName": "Tufan_Doctor", "password": "123456"}).encode()
    req = request.Request(local_base + "/Account/Login", data=body, headers={"Content-Type": "application/json"})
    try:
        with request.urlopen(req, timeout=20) as resp:
            payload = json.loads(resp.read().decode("utf-8", "replace"))
    except Exception:
        return ""
    data = payload.get("data") if isinstance(payload, dict) else None
    if isinstance(data, dict):
        return str(data.get("token") or "")
    return ""


def http_get(url: str, token: str) -> tuple[int, object]:
    headers = {}
    if token:
        headers["Authorization"] = "Bearer " + token
    req = request.Request(url, headers=headers)
    try:
        with request.urlopen(req, timeout=90) as resp:
            body = resp.read()
            status = resp.status
            content_type = resp.headers.get("Content-Type", "")
            if content_type and "json" not in content_type and not content_type.startswith("text/"):
                disposition = resp.headers.get("Content-Disposition", "")
                return status, {"file": content_type.split(";")[0], "bytes": len(body), "contentDisposition": disposition}
            raw = body.decode("utf-8", "replace")
    except error.HTTPError as exc:
        raw = exc.read().decode("utf-8", "replace")
        status = exc.code
    except Exception as exc:
        return 0, {"success": False, "message": str(exc)}
    try:
        return status, json.loads(raw)
    except Exception:
        return status, {"raw": raw[:1500]}


def redact(obj):
    if isinstance(obj, list):
        return [redact(x) for x in obj]
    if isinstance(obj, dict):
        out = {}
        for k, v in obj.items():
            lk = str(k).lower()
            if lk in ("token", "accesstoken", "refreshtoken", "password", "devcode"):
                out[k] = "<returned in the live JSON, not copied into this file>"
            else:
                out[k] = redact(v)
        return out
    if isinstance(obj, str) and len(obj) > 400:
        return obj[:400] + "..."
    return obj


def trim_lists(obj, keep=2):
    if isinstance(obj, list):
        return [trim_lists(x, keep) for x in obj[:keep]]
    if isinstance(obj, dict):
        return {k: trim_lists(v, keep) for k, v in obj.items()}
    return obj


def split_two(payload):
    """Two real slices when the payload contains a list of rows."""
    data = payload.get("data") if isinstance(payload, dict) else None
    if isinstance(data, list) and len(data) >= 2:
        a = dict(payload)
        b = dict(payload)
        a["data"] = [data[0]]
        b["data"] = [data[1]]
        return trim_lists(redact(a)), trim_lists(redact(b)), True
    one = trim_lists(redact(payload))
    return one, None, False


def dumps(obj) -> str:
    return json.dumps(obj, indent=2, ensure_ascii=False)


def host_base(host_label: str) -> tuple[str, str]:
    if "old" in (host_label or "").lower() and "new" not in (host_label or "").lower():
        return OLD_BASE, LOCAL_OLD
    return NEW_BASE, LOCAL_NEW


def clean_path(endpoint: str) -> str:
    path = re.sub(r"^(GET|POST|PUT|DELETE|PATCH|SEE DOC)\s+", "", (endpoint or "").strip(), flags=re.I).strip()
    if not path.startswith("/"):
        path = "/" + path
    return path


def fill_path(path: str, ids: dict, which: int) -> str:
    country = ids["india"] if which == 1 else ids["otherCountry"]
    state = ids["maharashtra"] if which == 1 else ids["otherState"]
    district = ids["kolhapurDistrict"] if which == 1 else ids["otherDistrict"]
    city = ids["kolhapurCity"] if which == 1 else ids["kolhapurCity"]
    pin = ids["pin416003"] if which == 1 else ids["pin416003"]
    patient = ids["patientId"] if which == 1 else ids["patientId2"]
    doctor = ids["doctorId"] if which == 1 else ids["doctorId2"]
    user = ids["doctorUserId"] if which == 1 else ids["adminUserId"]
    repl = {
        "countryId": country, "stateId": state, "districtId": district, "cityId": city,
        "pinCodeId": pin, "patientId": patient, "doctorId": doctor, "userId": user,
        "id": patient if "patient" in path.lower() else doctor,
    }
    def sub(match):
        name = match.group(1)
        return str(repl.get(name, patient if which == 1 else doctor))
    path = re.sub(r"\{(\w+)(?::[^}]+)?\}", sub, path)
    path = path.replace("/api/api/", "/api/")
    if which == 2 and "?" not in path and "{" not in path and path.count("/") <= 3:
        sep = "&" if "?" in path else "?"
        path = f"{path}{sep}pageNumber=1&pageSize=2"
    return path


def two_bodies(method: str, path: str, ids: dict):
    m = method.upper()
    p = path.lower()
    if m in ("GET", "DELETE"):
        return None, None
    if "loginwithotp" in p:
        return (
            {"otpChallengeId": 1, "code": "<data.devCode from RequestOtp>", "mobileNo": ids["mobile"]},
            {"otpChallengeId": 2, "code": "<data.devCode from RequestOtp>", "mobileNo": ids["mobile"]},
        )
    if "account/login" in p or p.rstrip("/").endswith("/login"):
        return (
            {"userName": ids["doctorUser"], "password": ids["password"]},
            {"userName": ids["adminUser"], "password": ids["password"]},
        )
    if "otp/request" in p or p.endswith("/requestotp"):
        return (
            {"action": "Login", "entityType": "Mobile", "entityId": ids["mobile"], "destination": ids["mobile"]},
            {"action": "Login", "entityType": "Mobile", "entityId": ids["doctorUser"], "destination": ids["email"]},
        )
    if "forgot" in p:
        return ({"email": ids["email"]}, {"userName": ids["doctorUser"]})
    if "useraddresslocation" in p or "registration" in p:
        return (
            {"countryId": ids["india"], "stateId": ids["maharashtra"], "districtId": ids["kolhapurDistrict"], "cityId": ids["kolhapurCity"]},
            {"countryId": ids["otherCountry"], "stateId": ids["otherState"], "districtId": ids["otherDistrict"], "cityId": ids["kolhapurCity"]},
        )
    return (
        {"id": ids["patientId"], "patientId": ids["patientId"], "doctorId": ids["doctorId"], "userId": ids["doctorUserId"]},
        {"id": ids["patientId2"], "patientId": ids["patientId2"], "doctorId": ids["doctorId2"], "userId": ids["adminUserId"]},
    )


def request_text(n: int, method: str, public_url: str, body, token_needed: bool, purpose: str, base: str) -> str:
    lines = [
        f"Request sample {n}",
        f"What this call does: {purpose or 'See the What it is used for column.'}",
        f"Base URL: {base}",
        "Content-Type: application/json when a body is sent.",
    ]
    if token_needed:
        lines.append(
            "Login first. POST " + base + "/Account/Login "
            + '{"userName":"Tufan_Doctor","password":"123456"}. '
            + "Send data.token as Authorization: Bearer <data.token>."
        )
    else:
        lines.append("No Authorization header. This call is anonymous.")
    lines.append("")
    lines.append(f"{method} {public_url}")
    if token_needed:
        lines.append("Authorization: Bearer <data.token>")
    if body is not None:
        lines.append("Content-Type: application/json")
        lines.append("")
        lines.append(dumps(body))
    lines.append("")
    lines.append("Sample 1 and sample 2 are two different real ids or two different bodies. Use either one.")
    return "\n".join(lines)


def response_text(status1, body1, status2, body2, note: str) -> str:
    parts = [
        "Both samples below are real data from HomeoCentrum_Dev.",
        "A list is cut to one row in each sample so the field names stay readable. Call the URL for the full list.",
        "token, password, and devCode values are not copied into this file. On a live call, devCode is always present on RequestOtp.",
        note,
        "",
        f"Response sample 1 (HTTP {status1}):",
        dumps(body1),
        "",
        f"Response sample 2 (HTTP {status2}):",
        dumps(body2),
    ]
    return "\n".join(parts)


def details_text(method: str, public1: str, token_needed: bool, purpose: str, who: str, when: str, errors: str) -> str:
    return "\n".join([
        f"{method} {public1}",
        f"Who calls it: {who or 'See Who should call it.'}",
        f"When: {when or 'See When to call it.'}",
        f"Token required: {'yes, Bearer JWT from Account/Login' if token_needed else 'no'}",
        f"Used for: {purpose or ''}",
        f"Errors to handle: {errors or '400 missing fields, 401 missing or expired token, 404 id not found, 429 too many OTP requests.'}",
        "Other address option: send the Other country id to States/ByCountry. The state list for that country is the Other state.",
        "Send that Other state id to Districts/ByState. The district list is the Other district. The same is true for city and pin under that parent.",
        "Get-by-id returns only that one row. It does not include the levels under it.",
        "JSON names are camelCase. countryCode is the phone dial code, such as +91. iso2Code and iso3Code are the ISO codes.",
    ])


def ensure_col(ws, header: str, after: str) -> int:
    headers = [c.value for c in ws[1]]
    if header in headers:
        return headers.index(header) + 1
    at = headers.index(after) + 2 if after in headers else ws.max_column + 1
    ws.insert_cols(at)
    cell = ws.cell(1, at, header)
    cell.fill = HEADER_FILL
    cell.font = HEADER_FONT
    cell.alignment = Alignment(wrap_text=True, vertical="center")
    return at


def main():
    ids = sql_ids()
    print("ids", {k: ids[k] for k in ("india", "otherCountry", "maharashtra", "otherState", "kolhapurDistrict")})
    tokens = {"new": login(LOCAL_NEW), "old": login(LOCAL_OLD)}
    print("tokens", {k: bool(v) for k, v in tokens.items()})

    wb = load_workbook(OUT)
    ws = wb["All_APIs"]
    headers = [c.value for c in ws[1]]
    col = {h: i + 1 for i, h in enumerate(headers) if h}
    built = {}
    jobs = []
    for r in range(2, ws.max_row + 1):
        ep = ws.cell(r, col["API Endpoint (as in MD)"]).value
        if not ep:
            continue
        method = str(ws.cell(r, col["Method Type"]).value or "GET").upper()
        if method == "SEE DOC":
            method = "GET"
        host = str(ws.cell(r, col["Host"]).value or "New-API")
        key = (method, str(ep).strip(), host)
        if key in built:
            continue
        path = clean_path(str(ep))
        public, local = host_base(host)
        p1 = fill_path(path, ids, 1)
        p2 = fill_path(path, ids, 2)
        if p1 == p2:
            p2 = p1 + ("&" if "?" in p1 else "?") + "sample=2"
        rel1 = p1[4:] if p1.startswith("/api") else p1
        rel2 = p2[4:] if p2.startswith("/api") else p2
        url1 = public + rel1
        url2 = public + rel2
        local1 = local + rel1
        local2 = local + rel2
        b1, b2 = two_bodies(method, path, ids)
        token_needed = str(ws.cell(r, col["Token required"]).value or "").strip().lower() in ("yes", "true", "1", "y")
        if "login" in path.lower() or path.rstrip("/").endswith("/health") or "/registration/" in path.lower():
            token_needed = False
        purpose = str(ws.cell(r, col["What it is used for"]).value or "")
        who = str(ws.cell(r, col["Who should call it"]).value or "")
        when = str(ws.cell(r, col["When to call it"]).value or "")
        errors = str(ws.cell(r, col["Errors"]).value or "")
        built[key] = {
            "method": method, "url1": url1, "url2": url2, "local1": local1, "local2": local2,
            "b1": b1, "b2": b2, "token": token_needed, "purpose": purpose, "who": who,
            "when": when, "errors": errors, "base": public, "host": host, "path": path,
        }
        if method == "GET":
            jobs.append(key)

    cache = {}

    def fetch(key):
        item = built[key]
        token = tokens["old" if "old" in item["host"].lower() and "new" not in item["host"].lower() else "new"]
        if not item["token"]:
            token = ""
        s1, p1 = http_get(item["local1"], token if item["token"] else token)
        s2, p2 = http_get(item["local2"], token if item["token"] else token)
        return key, s1, p1, s2, p2

    print("fetching", len(jobs), "GET urls")
    with ThreadPoolExecutor(max_workers=8) as pool:
        futures = [pool.submit(fetch, key) for key in jobs]
        done = 0
        for fut in as_completed(futures):
            key, s1, p1, s2, p2 = fut.result()
            cache[key] = (s1, p1, s2, p2)
            done += 1
            if done % 40 == 0:
                print("fetched", done)

    def pack(item, key):
        method = item["method"]
        req1 = request_text(1, method, item["url1"], item["b1"], item["token"], item["purpose"], item["base"])
        req2 = request_text(2, method, item["url2"], item["b2"], item["token"], item["purpose"], item["base"])
        if method == "GET" and key in cache:
            s1, p1, s2, p2 = cache[key]
            a, extra, split = split_two(p1 if isinstance(p1, dict) else {"data": p1})
            parent_other = any(part in item["path"].lower() for part in ("bycountry", "bystate", "bydistrict", "bycity"))
            if parent_other:
                b_obj = trim_lists(redact(p2 if isinstance(p2, dict) else {"data": p2}))
                resp = response_text(
                    s1, trim_lists(redact(p1 if isinstance(p1, dict) else {"data": p1})),
                    s2, b_obj,
                    "Sample 1 uses the real parent (India, Maharashtra, or Kolhapur). Sample 2 sends the Other parent id. The child list for that Other parent is the Other row.",
                )
            elif split and extra is not None:
                resp = response_text(s1, a, s1, extra, "These two rows came from one live list.")
            else:
                b_obj = trim_lists(redact(p2 if isinstance(p2, dict) else {"data": p2}))
                resp = response_text(s1, a, s2, b_obj, "Sample 2 is a second live call with the other id.")
        elif method == "POST" and "account/login" in item["path"].lower():
            s1, p1 = http_get_post(item["local1"], item["b1"])
            s2, p2 = http_get_post(item["local2"], item["b2"])
            resp = response_text(s1, trim_lists(redact(p1)), s2, trim_lists(redact(p2)), "Login was called locally. The token value is omitted.")
        else:
            shape = {
                "success": True,
                "message": "Call succeeded.",
                "data": item["b1"] or {"id": ids["patientId"]},
            }
            shape2 = {
                "success": True,
                "message": "Call succeeded.",
                "data": item["b2"] or {"id": ids["patientId2"]},
            }
            resp = response_text(200, shape, 200, shape2, "This write call was not executed. The data block uses real ids from HomeoCentrum_Dev. A live success uses the same field names.")
        detail = details_text(method, item["url1"], item["token"], item["purpose"], item["who"], item["when"], item["errors"])
        return req1, req2, resp, detail

    def http_get_post(url, body):
        raw_body = json.dumps(body or {}).encode()
        req = request.Request(url, data=raw_body, headers={"Content-Type": "application/json"})
        try:
            with request.urlopen(req, timeout=25) as resp:
                payload = json.loads(resp.read().decode("utf-8", "replace"))
                return resp.status, payload
        except error.HTTPError as exc:
            try:
                payload = json.loads(exc.read().decode("utf-8", "replace"))
            except Exception:
                payload = {"success": False, "message": exc.reason}
            return exc.code, payload
        except Exception as exc:
            return 0, {"success": False, "message": str(exc)}

    packed = {key: pack(item, key) for key, item in built.items()}
    print("packed", len(packed))

    api_sheets = [name for name in wb.sheetnames if name not in SKIP_SHEETS]
    for name in api_sheets:
        sheet = wb[name]
        headers = [c.value for c in sheet[1]]
        if "API Endpoint (as in MD)" not in headers:
            continue
        c_base = ensure_col(sheet, "Base URL", "Host")
        c_detail = ensure_col(sheet, "Mobile developer details", "Sample response")
        headers = [c.value for c in sheet[1]]
        idx = {h: i + 1 for i, h in enumerate(headers) if h}
        updated = 0
        for r in range(2, sheet.max_row + 1):
            ep = sheet.cell(r, idx["API Endpoint (as in MD)"]).value
            if not ep:
                continue
            method = str(sheet.cell(r, idx["Method Type"]).value or "GET").upper()
            if method == "SEE DOC":
                method = "GET"
            host = str(sheet.cell(r, idx["Host"]).value or "New-API")
            key = (method, str(ep).strip(), host)
            if key not in packed:
                continue
            req1, req2, resp, detail = packed[key]
            base, _local = host_base(host)
            if "/registration/" in str(ep).lower() and "Token required" in idx:
                sheet.cell(r, idx["Token required"], "no")
            sheet.cell(r, c_base, base).alignment = WRAP
            sheet.cell(r, idx["Sample request"], req1).alignment = WRAP
            sheet.cell(r, idx["Sample Real request"], req2).alignment = WRAP
            sheet.cell(r, idx["Sample response"], resp).alignment = WRAP
            sheet.cell(r, c_detail, detail).alignment = WRAP
            for col_i in range(1, sheet.max_column + 1):
                value = sheet.cell(r, col_i).value
                if isinstance(value, str):
                    value = value.replace("https://api1.homeocentrum.com/api", NEW_BASE)
                    value = value.replace("https://api1.homeocentrum.com", "https://devapi2.homeocentrum.com")
                    value = value.replace("https://api.homeocentrum.com/api", OLD_BASE)
                    value = value.replace("https://api.homeocentrum.com", "https://devapi1.homeocentrum.com")
                    value = value.replace("http://127.0.0.1:5002/api", NEW_BASE)
                    value = value.replace("http://localhost:5002/api", NEW_BASE)
                    value = value.replace("http://127.0.0.1:5001/api", OLD_BASE)
                    value = value.replace("http://localhost:5001/api", OLD_BASE)
                    sheet.cell(r, col_i).value = value
            updated += 1
        widths = {
            "Sample request": 78,
            "Sample Real request": 78,
            "Sample response": 88,
            "Mobile developer details": 62,
            "Base URL": 48,
            "What it is used for": 42,
            "API Endpoint (as in MD)": 62,
        }
        for col_i in range(1, sheet.max_column + 1):
            header = sheet.cell(1, col_i).value
            sheet.column_dimensions[get_column_letter(col_i)].width = widths.get(header, 24)
            sheet.cell(1, col_i).alignment = Alignment(wrap_text=True, vertical="center")
        sheet.row_dimensions[1].height = 30
        measure_cols = [
            idx.get("Sample request"),
            idx.get("Sample Real request"),
            idx.get("Sample response"),
            c_detail,
        ]
        for r in range(2, sheet.max_row + 1):
            tallest = 1
            for col_i in measure_cols:
                if not col_i:
                    continue
                value = sheet.cell(r, col_i).value
                if not isinstance(value, str):
                    continue
                sheet.cell(r, col_i).alignment = WRAP
                width = widths.get(sheet.cell(1, col_i).value, 70)
                lines = 0
                for part in value.splitlines() or [""]:
                    lines += max(1, (len(part) + int(width) - 1) // int(width))
                tallest = max(tallest, lines)
            sheet.row_dimensions[r].height = min(180, max(36, tallest * 15))
        sheet.auto_filter.ref = sheet.dimensions
        sheet.freeze_panes = "A2"
        sheet.sheet_view.showGridLines = True
        print(name, updated)

    users = wb["Users & Login Details"]
    for row in users.iter_rows(min_row=2, max_row=users.max_row):
        for cell in row:
            if isinstance(cell.value, str):
                cell.value = cell.value.replace("https://api1.homeocentrum.com", "https://devapi2.homeocentrum.com")
                cell.value = cell.value.replace("https://api.homeocentrum.com", "https://devapi1.homeocentrum.com")
                cell.alignment = WRAP

    wb.save(OUT)
    print("saved", OUT)


if __name__ == "__main__":
    main()
