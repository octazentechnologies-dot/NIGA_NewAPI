"""Live checks: role menus, listed bugs, CON-01 family, ADM ACL/host samples."""
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
            raw = resp.read()[:50000]
            return resp.status, raw.decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        raw = e.read()[:4000].decode("utf-8", "replace")
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


def user_id_from(body: str) -> int:
    try:
        j = json.loads(body)
    except json.JSONDecodeError:
        return 0
    data = j.get("data") or j
    if isinstance(data, dict):
        return int(data.get("userId") or data.get("UserId") or 0)
    return 0


def as_list(body: str):
    try:
        j = json.loads(body)
    except json.JSONDecodeError:
        return []
    if isinstance(j, list):
        return j
    if isinstance(j, dict):
        for key in ("$values", "data", "Data", "resultObject"):
            val = j.get(key)
            if isinstance(val, list):
                return val
            if isinstance(val, dict) and isinstance(val.get("$values"), list):
                return val["$values"]
    return []


def urls_of(rows):
    out = []
    for row in rows:
        if not isinstance(row, dict):
            continue
        out.append(str(row.get("menuUrl") or row.get("MenuUrl") or ""))
    return out


def main():
    rows = []

    def check(sub, name, status, body, expect, extra_ok=True):
        ok = status in expect and extra_ok
        snippet = body.replace("\n", " ")[:180]
        rows.append((sub, name, status, "PASS" if ok else "FAIL", snippet))
        print(f"{'PASS' if ok else 'FAIL'}  {status:4}  {sub:12}  {name}  {snippet[:90]}")

    tokens = {}
    user_ids = {}
    for user, pwd, _role in USERS:
        s, b = req("POST", f"{OLD}/api/Account/Login", {"userName": user, "password": pwd})
        check("SEC-01.02", f"OLD login {user}", s, b, {200})
        tokens[user] = token_from(b)
        user_ids[user] = user_id_from(b)

    # BUG-S1-01 / ADM-B04.02 menus
    expected = {
        "Tufan_Admin": ["/dashboard", "/admin/enquiries", "/admin/listqualification"],
        "Tufan_Doctore": ["/doctordashboard", "/doctor/patientboard"],
        "Tufan_Reception": ["/doctordashboard"],
        "Tufan_Account": ["/account/home", "/account/ledger"],
        "Tufan_Pharmacy": ["/pharmacy/home", "/pharmacy/onboarding"],
        "Tufan_Patient": ["/family", "/caregiver"],
    }
    forbidden = {
        "Tufan_Doctore": ["/enquiries", "/admin/enquiries"],
        "Tufan_Reception": ["/doctor/patientboard", "/enquiries", "/admin/enquiries"],
        "Tufan_Account": ["/family"],
        "Tufan_Pharmacy": ["/family"],
    }
    for user, needles in expected.items():
        tok = tokens.get(user) or ""
        s, b = req("GET", f"{NEW}/api/mastersAPI/GetMenuByRole", token=tok)
        menu_urls = urls_of(as_list(b))
        if not menu_urls:
            menu_urls = [u for u in expected[user] + forbidden.get(user, []) if u in b]
        blob = " ".join(menu_urls) if menu_urls else b
        has = all(n in b for n in needles)
        check("ADM-B04.02", f"GetMenuByRole {user} count={len(menu_urls)}", s, b, {200}, has)
        for bad in forbidden.get(user, []):
            check("ADM-B04.02", f"GetMenuByRole {user} no {bad}", s, blob, {200}, bad not in blob)

    rec = tokens.get("Tufan_Reception") or ""
    rec_uid = user_ids.get("Tufan_Reception") or 0
    doc_uid = user_ids.get("Tufan_Doctore") or 0
    s, b = req("POST", f"{NEW}/api/PatientBoardBackup/Save", {}, token=rec)
    check("BUG-S2-01", "Reception backup empty 403", s, b, {403})
    s, b = req(
        "POST",
        f"{NEW}/api/PatientBoardBackup/Save",
        {"backupPayload": "{}", "patientCount": 0, "schemaVersion": 1},
        token=rec,
    )
    check("BUG-S2-01", "Reception backup payload 403", s, b, {403})

    s, b = req("GET", f"{NEW}/api/doctorDashBoard/GetPatientStatsCharts?userId={doc_uid or rec_uid}&period=ALL", token=rec)
    check("BUG-S2-03", "Reception GetPatientStatsCharts", s, b, {200})
    s, b = req("GET", f"{NEW}/api/doctorDashBoard/GetPatientStats?userId={doc_uid or rec_uid}", token=rec)
    check("BUG-S2-03", "Reception GetPatientStats", s, b, {200})

    pat = tokens.get("Tufan_Patient") or ""
    s, b = req("GET", f"{NEW}/api/Family/Relations", token=pat)
    rels = as_list(b)
    if not rels and b.strip().startswith("{"):
        try:
            j = json.loads(b)
            rels = as_list(json.dumps(j.get("data") or j))
            if isinstance(j.get("data"), list):
                rels = j["data"]
        except json.JSONDecodeError:
            pass
    check("CON-01.02", "Family Relations", s, b, {200}, len(rels) > 0 or "Spouse" in b or "Father" in b)
    s, b = req("POST", f"{NEW}/api/Family", {"patientName": "NameOnlyBug"}, token=pat)
    check("CON-01.03", "Family name-only 400", s, b, {400})
    s, b = req(
        "POST",
        f"{NEW}/api/Family",
        {"patientName": "Anita Live", "relation": "Spouse"},
        token=pat,
    )
    check("CON-01.02", "Family create with relation", s, b, {200})

    admin = tokens.get("Tufan_Admin") or ""
    doctor = tokens.get("Tufan_Doctore") or ""
    s, b = req("GET", f"{NEW}/api/qualification/GetQualificationList?PageNumber=1&PageSize=5", token=admin)
    check("ADM-B01.03", "Qualifications list New-API Admin", s, b, {200})
    s, b = req("POST", f"{NEW}/api/qualification/AddQualification", {"qualificationName": ""}, token=doctor)
    check("ADM-B01.02", "Doctor qualification mutate forbidden", s, b, {401, 403})
    s, b = req("GET", f"{NEW}/api/threeDBodyPartMeshKeyMaster/GetThreeDBodyPartMeshKeyMasterList?PageNumber=1&PageSize=5", token=admin)
    check("ADM-3D1.03", "3D mesh list New-API", s, b, {200, 404})
    s, b = req("POST", f"{NEW}/api/threeDBodyPartMeshKeyMaster/AddThreeDBodyPartMeshKeyMaster", {}, token=doctor)
    check("ADM-3D1.02", "Doctor 3D mesh mutate forbidden", s, b, {401, 403})
    s, b = req("GET", f"{OLD}/api/package?PageNumber=1&PageSize=5", token=admin)
    check("ADM-B03.03", "Packages still Old-API", s, b, {200, 404})
    s, b = req("POST", f"{OLD}/api/package", {"packageName": ""}, token=doctor)
    check("ADM-B03.02", "Doctor package mutate forbidden", s, b, {401, 403})
    s, b = req("GET", f"{OLD}/api/diagnosis/GetDiagnosis?PageNumber=1&PageSize=5", token=admin)
    check("ADM-B02.03", "Admin diagnosis list Old-API", s, b, {200, 404})
    s, b = req("POST", f"{OLD}/api/diagnosis", {"diagnosisName": ""}, token=doctor)
    check("ADM-B02.02", "Doctor diagnosis mutate forbidden", s, b, {401, 403})
    s, b = req("GET", f"{OLD}/api/AllopathicDrug/GetAllopathicDrug?PageNumber=1&PageSize=5", token=admin)
    check("ADM-B02.03", "Admin allopathic drug list Old-API", s, b, {200, 404})
    s, b = req("POST", f"{OLD}/api/AllopathicDrug", {"allopathicDrugName": ""}, token=doctor)
    check("ADM-B02.02", "Doctor allopathic mutate forbidden", s, b, {401, 403})

    # BUG-S1-01 empty vs missing vs unauth
    s, b = req("GET", f"{NEW}/api/mastersAPI/GetMenuByRole")
    check("BUG-S1-01", "GetMenuByRole unauthenticated 401", s, b, {401})
    s, b = req("GET", f"{NEW}/api/mastersAPI/GetMenuByRole?userId=999999999", token=admin)
    check("BUG-S1-01", "Admin inspect missing user 404", s, b, {404})
    s, b = req(
        "POST", f"{OLD}/api/Account/Login",
        {"userName": "Tufan_NoMenu", "password": "123456"},
    )
    check("BUG-S1-01", "OLD login Tufan_NoMenu", s, b, {200})
    empty_tok = token_from(b)
    s, b = req("GET", f"{NEW}/api/mastersAPI/GetMenuByRole", token=empty_tok)
    empty_list = as_list(b)
    check("BUG-S1-01", "Tufan_NoMenu GetMenuByRole 200 []", s, b, {200}, s == 200 and len(empty_list) == 0)

    acct = tokens.get("Tufan_Account") or ""
    admin_uid = user_ids.get("Tufan_Admin") or 0
    s, b = req("GET", f"{NEW}/api/mastersAPI/GetMenuByRole?userId={admin_uid}", token=acct)
    check(
        "BUG-S1-01",
        "Account cannot IDOR Admin menus",
        s,
        b,
        {200},
        "/dashboard" not in b and "/admin/enquiries" not in b and "/account/home" in b,
    )

    # BUG-S2-01 remaining cases
    s, b = req("POST", f"{NEW}/api/PatientBoardBackup/Save", {})
    check("BUG-S2-01", "Backup unauthenticated 401", s, b, {401})
    s, b = req("POST", f"{NEW}/api/PatientBoardBackup/Save", {}, token="not-a-jwt")
    check("BUG-S2-01", "Backup invalid JWT 401", s, b, {401})
    s, b = req("POST", f"{NEW}/api/PatientBoardBackup/Save", {}, token=acct)
    check("BUG-S2-01", "Account backup empty 403", s, b, {403})
    s, b = req("POST", f"{NEW}/api/PatientBoardBackup/Save", {}, token=pat)
    check("BUG-S2-01", "Patient backup empty 403", s, b, {403})
    s, b = req("POST", f"{NEW}/api/PatientBoardBackup/Save", {}, token=doctor)
    check("BUG-S2-01", "Doctor backup empty 400", s, b, {400})
    s, b = req(
        "POST",
        f"{NEW}/api/PatientBoardBackup/Save",
        {"backupPayload": "{\"patients\":[]}", "patientCount": 0, "schemaVersion": 1},
        token=doctor,
    )
    check("BUG-S2-01", "Doctor backup valid 200", s, b, {200})

    # BUG-S2-03 / DOC-02.02 other roles
    s, b = req("GET", f"{NEW}/api/doctorDashBoard/GetPatientStatsCharts?userId={doc_uid or 1}&period=ALL", token=doctor)
    check("BUG-S2-03", "Doctor GetPatientStatsCharts", s, b, {200}, "pieChart" in b or "PieChart" in b)
    s, b = req("GET", f"{NEW}/api/doctorDashBoard/GetPatientStatsCharts?userId={doc_uid or 1}&period=ALL", token=acct)
    check("BUG-S2-03", "Account GetPatientStatsCharts 403", s, b, {403})
    s, b = req("GET", f"{NEW}/api/doctorDashBoard/GetPatientStatsCharts?period=ALL", token=rec)
    check("BUG-S2-03", "Reception charts without userId", s, b, {200, 400})

    # CON-01 remaining
    s, b = req("POST", f"{NEW}/api/Family", {"relation": "Spouse"}, token=pat)
    check("CON-01.03", "Family relation-only 400", s, b, {400})
    s, b = req("POST", f"{NEW}/api/Family", {}, token=pat)
    check("CON-01.03", "Family empty 400", s, b, {400})
    s, b = req("GET", f"{NEW}/api/Family", token=acct)
    check("CON-01.02", "Account Family 403", s, b, {401, 403})
    pharm = tokens.get("Tufan_Pharmacy") or ""
    s, b = req("GET", f"{NEW}/api/Family", token=pharm)
    check("CON-01.02", "Pharmacy Family 403", s, b, {401, 403})
    s, b = req("GET", f"{NEW}/api/Family", token=pat)
    check("CON-01.01", "Patient Family list", s, b, {200})
    s, b = req("GET", f"{NEW}/api/PatientBoardBackup/Summary", token=rec)
    check("BUG-S2-01", "Reception backup summary 403", s, b, {403})
    s, b = req("GET", f"{NEW}/api/mastersAPI/GetMenuByRole", token="not-a-jwt")
    check("BUG-S1-01", "GetMenuByRole invalid JWT 401", s, b, {401})

    # ADM mutate more hosts
    s, b = req("GET", f"{NEW}/api/qualification/GetQualificationList?PageNumber=1&PageSize=5", token=doctor)
    check("ADM-B01.03", "Doctor qualifications list", s, b, {200, 401, 403})
    s, b = req("POST", f"{NEW}/api/qualification/AddQualification", {"qualificationName": ""}, token=admin)
    check("ADM-B01.02", "Admin empty qualification name", s, b, {400, 200})
    s, b = req("GET", f"{OLD}/api/package?PageNumber=1&PageSize=5", token=doctor)
    check("ADM-B03.03", "Doctor packages Old-API", s, b, {200, 401, 403, 404})

    out = __file__.replace(".py", "_results.tsv")
    with open(out, "w", encoding="utf-8") as f:
        f.write("sub\tname\tstatus\tresult\tsnippet\n")
        for row in rows:
            f.write("\t".join(str(x) for x in row) + "\n")
    failed = sum(1 for r in rows if r[3] == "FAIL")
    print(f"Wrote {out}  FAIL={failed}/{len(rows)}")
    return failed


if __name__ == "__main__":
    raise SystemExit(main())
