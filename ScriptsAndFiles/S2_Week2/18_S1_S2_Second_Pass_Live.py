"""S1+S2 second-pass live checks against running Old-API :5001 and New-API :5038.

Uses Tufan_* Dev logins. Does not invent endpoints. Records evidence for the audit.
"""
from __future__ import annotations

import json
import urllib.error
import urllib.request

OLD = "http://127.0.0.1:5001"
NEW = "http://127.0.0.1:5038"

USERS = [
    ("Tufan_Admin", "123456", "Admin"),
    ("Tufan_Doctore", "123456", "Doctor"),
    ("Tufan_Reception", "123456", "Reception"),
    ("Tufan_Account", "123456", "Account"),
    ("Tufan_Pharmacy", "123456", "PharmacyPartner"),
    ("Tufan_Patient", "123456", "Patient"),
    ("Tufan_Caregiver", "123456", "Patient"),
]


def req(method, url, body=None, token=None, timeout=30):
    data = None
    headers = {"Accept": "application/json"}
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bearer {token}"
    r = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(r, timeout=timeout) as resp:
            raw = resp.read()[:6000]
            return resp.status, raw.decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        raw = e.read()[:3000].decode("utf-8", "replace")
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


def role_from(body: str) -> str:
    try:
        j = json.loads(body)
    except json.JSONDecodeError:
        return ""
    data = j.get("data") or j
    if isinstance(data, dict):
        return str(data.get("role") or data.get("Role") or data.get("roleName") or "")
    return ""


def main():
    rows = []

    def check(sub, name, status, body, expect):
        ok = status in expect
        snippet = body.replace("\n", " ")[:160]
        rows.append((sub, name, status, "PASS" if ok else "FAIL", snippet))
        print(f"{'PASS' if ok else 'FAIL'}  {status:4}  {sub:12}  {name}  {snippet[:80]}")

    tokens = {}
    for user, pwd, expect_role in USERS:
        s, b = req("POST", f"{OLD}/api/Account/Login", {"userName": user, "password": pwd})
        check("SEC-01.02", f"OLD login {user}", s, b, {200})
        tok = token_from(b)
        tokens[user] = tok
        got_role = role_from(b)
        role_ok = expect_role.lower() in got_role.lower() or (
            expect_role == "PharmacyPartner" and "pharmacy" in got_role.lower()
        )
        check("SEC-01.02", f"role claim {user} expect {expect_role} got {got_role}", 200 if role_ok else 0, got_role, {200})

    s, b = req("POST", f"{OLD}/api/Account/Login", {"userName": "Tufan_Doctore", "password": "wrong"})
    check("SEC-01.02", "OLD login wrong password", s, b, {400, 401, 404})

    s, b = req("POST", f"{OLD}/api/Account/Login", {"userName": "", "password": ""})
    check("SEC-01.02", "OLD login empty", s, b, {400, 401, 404})

    doc = tokens.get("Tufan_Doctore") or ""
    rec = tokens.get("Tufan_Reception") or ""
    acc = tokens.get("Tufan_Account") or ""
    pharm = tokens.get("Tufan_Pharmacy") or ""
    pat = tokens.get("Tufan_Patient") or ""
    admin = tokens.get("Tufan_Admin") or ""
    cg = tokens.get("Tufan_Caregiver") or ""

    s, b = req("GET", f"{NEW}/api/Family/Me")
    check("CON-01.02", "Family/Me no token", s, b, {401})

    if pat:
        s, b = req("GET", f"{NEW}/api/Family/Me", token=pat)
        check("CON-01.02", "Family/Me patient", s, b, {200})
        s, b = req("GET", f"{NEW}/api/Family/Relations", token=pat)
        check("PAT-06.02", "Family/Relations", s, b, {200})
        s, b = req("GET", f"{NEW}/api/Caregiver/ListMine", token=pat)
        check("CON-02.02", "Caregiver/ListMine patient", s, b, {200})
        s, b = req("POST", f"{NEW}/api/Caregiver/Grant", {"patientId": 3046, "caregiverUserId": 10031}, token=pat)
        check("CON-02.04", "Caregiver/Grant without OTP", s, b, {400})
        s, b = req("GET", f"{NEW}/api/PatientPortal/Home", token=pat)
        check("PAT-08.02", "PatientPortal/Home", s, b, {200, 404})
        s, b = req("GET", f"{NEW}/api/Consent/PrivacyStatus", token=pat)
        check("PAT-05.02", "Consent/PrivacyStatus", s, b, {200})
        s, b = req("GET", f"{NEW}/api/PatientProfile/Me", token=pat)
        check("PAT-04.02", "PatientProfile/Me", s, b, {200, 404})
        s, b = req("GET", f"{NEW}/api/Otp/Audit", token=pat)
        check("SEC-07.03", "Otp/Audit as patient (admin/account only)", s, b, {401, 403})

    if cg:
        s, b = req("GET", f"{NEW}/api/Caregiver/ListActingFor", token=cg)
        check("CON-02.02", "Caregiver/ListActingFor", s, b, {200})

    if acc:
        s, b = req("GET", f"{NEW}/api/Family/Me", token=acc)
        check("SEC-04.01", "Family/Me account 403", s, b, {401, 403})
        s, b = req("GET", f"{NEW}/api/Otp/Audit", token=acc)
        check("SEC-07.03", "Otp/Audit account", s, b, {200})

    if pharm:
        s, b = req("GET", f"{NEW}/api/Family", token=pharm)
        check("SEC-04.01", "Family pharmacy 403", s, b, {401, 403})

    if rec:
        s, b = req("POST", f"{NEW}/api/Repertorization/CenterOfGravity", {"rubrics": []}, token=rec)
        check("CLN-02.02", "COG reception blocked", s, b, {403})
        s, b = req("POST", f"{NEW}/api/PatientBoardBackup/Save", {"backupPayload": "{}"}, token=rec)
        check("CLN-19.02", "board backup reception blocked", s, b, {403})

    if doc:
        s, b = req("POST", f"{NEW}/api/Repertorization/CenterOfGravity", {"rubrics": []}, token=doc)
        check("CLN-13.02", "COG doctor empty clipboard", s, b, {200})
        s, b = req("GET", f"{NEW}/api/Profile/Me", token=doc)
        check("DOC-10.02", "Profile/Me doctor", s, b, {200})
        s, b = req("GET", f"{NEW}/api/Availability/Me", token=doc)
        check("DMO-05.02", "Availability/Me", s, b, {200})
        s, b = req("GET", f"{NEW}/api/ReceptionStaff/GetReceptionStaffList", token=doc)
        check("DOC-09.01", "ReceptionStaff list", s, b, {200})
        s, b = req("GET", f"{NEW}/api/PatientBoardBackup/Summary", token=doc)
        check("CLN-19.03", "PatientBoardBackup/Summary", s, b, {200, 404})
        s, b = req("GET", f"{NEW}/api/patient/GetComplaints/1", token=doc)
        check("CLN-16.02", "GetComplaints", s, b, {200, 403, 404})
        s, b = req("POST", f"{NEW}/api/Account/ConfirmMobile", {"mobileNo": "9000000202"}, token=doc)
        check("DMO-02.02", "ConfirmMobile", s, b, {200, 400})
        s, b = req("POST", f"{NEW}/api/Device/Register", {"platform": "FCM", "deviceId": "s2-audit-doc", "token": "tok-audit"}, token=doc)
        check("DMO-06.02", "Device/Register FCM", s, b, {200, 201})
        s, b = req("GET", f"{NEW}/api/mastersAPI/GetMenuByRole", token=doc)
        check("ADM-B04.02", "GetMenuByRole doctor", s, b, {200})
        s, b = req("GET", f"{NEW}/api/patient/ExportCaseToPdf/1/1", token=doc)
        check("CLN-18.01", "ExportCaseToPdf", s, b, {200, 403, 404})

    if admin:
        s, b = req("GET", f"{NEW}/api/Enquiry", token=admin)
        check("WEB-06.02", "Enquiry list admin", s, b, {200})
        s, b = req("GET", f"{NEW}/api/mastersAPI/GetMenuByRole", token=admin)
        check("ADM-B04.02", "GetMenuByRole admin", s, b, {200})
        s, b = req("GET", f"{NEW}/api/Otp/Audit", token=admin)
        check("SEC-07.03", "Otp/Audit admin", s, b, {200})
        s, b = req("GET", f"{NEW}/api/Consent/AdminAudit", token=admin)
        check("SEC-06.02", "Consent/AdminAudit", s, b, {200, 404})

    s, b = req("GET", f"{NEW}/api/Public/Doctors?pageNumber=1&pageSize=5")
    check("WEB-03.02", "Public/Doctors", s, b, {200})
    s, b = req("GET", f"{NEW}/api/Public/Doctors/999999")
    check("WEB-03.02", "Public/Doctors missing", s, b, {404, 400})
    s, b = req("GET", f"{NEW}/api/Public/Policies/Privacy")
    check("WEB-07.01", "Policies/Privacy", s, b, {200})
    s, b = req("GET", f"{NEW}/api/Public/Policies/Terms")
    check("WEB-08.01", "Policies/Terms", s, b, {200})
    s, b = req("GET", f"{NEW}/api/Public/Articles")
    check("WEB-12.01", "Public/Articles", s, b, {200})
    s, b = req("GET", f"{NEW}/api/Welcome/Patient")
    check("PAT-02.02", "Welcome/Patient", s, b, {200})
    s, b = req("GET", f"{NEW}/api/mastersAPI/GetLanguages")
    check("PAT-01.02", "GetLanguages", s, b, {200})
    s, b = req("GET", f"{NEW}/api/PatientPortal/CareCategories")
    check("PAT-09.02", "CareCategories", s, b, {200})

    s, b = req("POST", f"{NEW}/api/PatientAuth/RequestOtp", {"mobile": "7768046064"})
    check("WEB-04.02", "PatientAuth/RequestOtp 7768046064", s, b, {200})
    try:
        j = json.loads(b)
        code = (j.get("devCode") or (j.get("data") or {}).get("devCode")) if isinstance(j, dict) else None
        cid = j.get("otpChallengeId") or (j.get("data") or {}).get("otpChallengeId")
    except json.JSONDecodeError:
        code, cid = None, None
    if code and cid:
        s, b = req("POST", f"{NEW}/api/PatientAuth/VerifyOtp", {"mobile": "7768046064", "code": code, "otpChallengeId": cid})
        check("WEB-04.02", "PatientAuth/VerifyOtp", s, b, {200})

    s, b = req("POST", f"{NEW}/api/Otp/RequestOtp", {
        "action": "Login", "entityType": "Mobile", "entityId": "7768046064", "destination": "7768046064"
    })
    check("PAT-03.02", "Otp/RequestOtp Login", s, b, {200})
    try:
        j = json.loads(b)
        data = j.get("data") or {}
        code = data.get("devCode")
        cid = data.get("otpChallengeId")
    except json.JSONDecodeError:
        code, cid = None, None
    if code and cid:
        s, b = req("POST", f"{NEW}/api/Account/LoginWithOtp", {
            "otpChallengeId": cid, "code": code, "mobileNo": "7768046064"
        })
        check("PAT-03.02", "LoginWithOtp Tufan_Patient mobile", s, b, {200})
        otp_role = role_from(b)
        check("PAT-03.02", f"LoginWithOtp role {otp_role}", 200 if "patient" in otp_role.lower() else 0, otp_role, {200})

    s, b = req("POST", f"{NEW}/api/Account/ForgotPassword", {"email": "tufanpowar@gmail.com"})
    check("SEC-02.02", "ForgotPassword Tufan_Patient email", s, b, {200})
    s, b = req("POST", f"{NEW}/api/Account/ForgotPassword", {"email": "nobody-audit@example.invalid"})
    check("SEC-02.02", "ForgotPassword unknown generic 200", s, b, {200})
    s, b = req("POST", f"{NEW}/api/Enquiry", {"name": "", "email": "", "message": ""})
    check("WEB-06.02", "Enquiry empty 400", s, b, {400})
    s, b = req("POST", f"{NEW}/api/Enquiry", {
        "enquiryName": "S2 Audit", "emailId": "tufanpowar@gmail.com", "mobileNo": "7768046064",
        "enquiryDetails": "Second pass audit enquiry"
    })
    check("WEB-06.02", "Enquiry create", s, b, {200, 201, 400})

    s, b = req("GET", f"{NEW}/swagger/v1/swagger.json")
    check("FND-01.03", "New-API swagger", s, b, {200})

    if doc:
        s, b = req("POST", f"{OLD}/api/Account/Logout", token=doc)
        check("SEC-03.01", "OLD Logout doctor", s, b, {200, 204})
        s, b = req("GET", f"{NEW}/api/Profile/Me", token=doc)
        check("SEC-03.01", "Profile/Me after logout (denylist or still valid until expiry)", s, b, {200, 401})

    failed = [r for r in rows if r[3] == "FAIL"]
    print(f"\n{len(rows) - len(failed)}/{len(rows)} PASS")
    for r in failed:
        print("  FAIL", r[0], r[1], r[2], r[4][:180])
    out = __file__.replace(".py", "_results.tsv")
    with open(out, "w", encoding="utf-8") as f:
        f.write("SubID\tName\tStatus\tResult\tBody\n")
        for r in rows:
            f.write("\t".join(str(x).replace("\t", " ") for x in r) + "\n")
    print("wrote", out)


if __name__ == "__main__":
    main()
