"""Live smoke for S1/S2 dual-API + auth (no invented endpoints)."""
from __future__ import annotations

import json
import urllib.error
import urllib.request

OLD = "http://127.0.0.1:5001"
NEW = "http://127.0.0.1:5038"


def req(method, url, body=None, token=None, content_type="application/json"):
    data = None
    headers = {"Accept": "application/json"}
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = content_type
    if token:
        headers["Authorization"] = f"Bearer {token}"
    r = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(r, timeout=30) as resp:
            raw = resp.read()[:4000]
            return resp.status, raw.decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        raw = e.read()[:2000].decode("utf-8", "replace")
        return e.code, raw
    except Exception as e:
        return 0, str(e)


def token_from(body: str) -> str:
    try:
        j = json.loads(body)
    except json.JSONDecodeError:
        return ""
    data = j.get("data") or j.get("resultObject") or j
    if isinstance(data, dict):
        return data.get("token") or data.get("Token") or ""
    return ""


def main():
    rows = []

    def check(name, status, body, expect):
        ok = status in expect
        rows.append((name, status, "PASS" if ok else "FAIL", body[:180].replace("\n", " ")))
        print(f"{'PASS' if ok else 'FAIL'}  {status:4}  {name}")

    s, b = req("GET", f"{NEW}/api/Public/Doctors?pageNumber=1&pageSize=5")
    check("NEW GET /api/Public/Doctors", s, b, {200})

    s, b = req("GET", f"{NEW}/api/Public/Doctors/999999")
    check("NEW GET /api/Public/Doctors/999999 (missing)", s, b, {404, 400})

    s, b = req("POST", f"{OLD}/api/Account/Login", {"userName": "Tufan_Doctor", "password": "123456"})
    check("OLD POST /api/Account/Login Tufan_Doctor", s, b, {200})
    doc_tok = token_from(b)

    s, b = req("POST", f"{OLD}/api/Account/Login", {"userName": "wrong", "password": "nope"})
    check("OLD login invalid", s, b, {400, 401, 404})

    s, b = req("POST", f"{OLD}/api/Account/Login", {"userName": "Tufan_Account", "password": "123456"})
    check("OLD login Tufan_Account", s, b, {200})
    acc_tok = token_from(b)

    s, b = req("POST", f"{OLD}/api/Account/Login", {"userName": "Tufan_Pharmacy", "password": "123456"})
    check("OLD login Tufan_Pharmacy", s, b, {200})
    pharm_tok = token_from(b)

    s, b = req("POST", f"{OLD}/api/Account/Login", {"userName": "Tufan_Reception", "password": "123456"})
    check("OLD login Tufan_Reception", s, b, {200})

    s, b = req("POST", f"{OLD}/api/Account/Login", {"userName": "admin", "password": "1234"})
    check("OLD login admin", s, b, {200})

    s, b = req("GET", f"{NEW}/api/Family/Me")
    check("NEW GET /api/Family/Me no token", s, b, {401})

    if doc_tok:
        s, b = req("GET", f"{NEW}/api/Family/Me", token=doc_tok)
        check("NEW GET /api/Family/Me doctor JWT (Old-API token on New-API)", s, b, {200, 401, 400})
        s, b = req("GET", f"{NEW}/api/AdminAcl/me", token=doc_tok)
        check("NEW GET /api/AdminAcl/me doctor", s, b, {200, 401, 403})
        s, b = req("POST", f"{OLD}/api/Account/Logout", token=doc_tok)
        check("OLD POST /api/Account/Logout", s, b, {200, 204})

    if acc_tok:
        s, b = req("GET", f"{NEW}/api/Family/Me", token=acc_tok)
        check("NEW GET /api/Family/Me account role (expect 403 or 401)", s, b, {401, 403})

    if pharm_tok:
        s, b = req("GET", f"{NEW}/api/Family", token=pharm_tok)
        check("NEW GET /api/Family pharmacy role (expect 403 or 401)", s, b, {401, 403})

    s, b = req("POST", f"{NEW}/api/Account/ForgotPassword", {"email": "not-an-email"})
    check("NEW ForgotPassword unknown (generic 200 anti-enumeration)", s, b, {200})

    s, b = req("POST", f"{NEW}/api/Enquiry", {"name": "", "email": "", "message": ""})
    check("NEW POST /api/Enquiry empty", s, b, {400})

    s, b = req("GET", f"{NEW}/swagger/v1/swagger.json")
    check("NEW swagger JSON", s, b, {200})

    failed = [r for r in rows if r[2] == "FAIL"]
    print(f"\n{len(rows)-len(failed)}/{len(rows)} PASS")
    for r in failed:
        print("  FAIL", r[0], r[1], r[3][:160])


if __name__ == "__main__":
    main()
