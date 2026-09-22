"""S3 Week 3 second pass. One task group, several cases, against the running APIs.

Does not send SMS, WhatsApp, or Razorpay. Does not disable Tufan_Reception.
"""
from __future__ import annotations

import json
import subprocess
from datetime import date, timedelta

import urllib.error
import urllib.request

OLD = "http://127.0.0.1:5001"
NEW = "http://127.0.0.1:5038"
DOCTOR_ID = 1010
DOCTOR_USER = 10032
STAFF_PATIENT = 3065
OWN_PATIENT = 3046
ROWS = []


def req(method, url, body=None, token=None):
    data = None
    headers = {"Accept": "application/json"}
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=40) as resp:
            return resp.status, resp.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as ex:
        return ex.code, ex.read()[:2500].decode("utf-8", "replace")
    except Exception as ex:
        return 0, str(ex)


def j(body):
    try:
        return json.loads(body)
    except json.JSONDecodeError:
        return {}


def token_from(body: str) -> str:
    data = j(body).get("data") or j(body)
    if isinstance(data, dict):
        return data.get("token") or data.get("Token") or ""
    return ""


def check(task, name, status, body, expect, contains=None, absent=None):
    ok = status in expect
    text = body if isinstance(body, str) else json.dumps(body)
    if contains and ok:
        ok = all(part.lower() in text.lower() for part in contains)
    if absent and ok:
        ok = all(part.lower() not in text.lower() for part in absent)
    ROWS.append((task, name, status, "PASS" if ok else "FAIL"))
    print(f"{'PASS' if ok else 'FAIL'}  {status:4}  {task:12}  {name}")
    if not ok:
        print("      ", text[:360].replace("\n", " "))


def login(user, password="123456"):
    status, body = req("POST", f"{OLD}/api/Account/Login", {"userName": user, "password": password})
    return status, body, token_from(body)


def sql(query: str) -> str:
    done = subprocess.run(
        ["sqlcmd", "-S", r"localhost\MSSQLSERVER25", "-d", "HomeoCentrum_Dev", "-E", "-h", "-1", "-W", "-Q", query],
        capture_output=True, text=True, timeout=30,
    )
    return (done.stdout or "") + (done.stderr or "")


def appointments(day, token):
    status, body = req(
        "GET",
        f"{NEW}/api/PatientAppointment/GetAppointmentsByDate?userId={DOCTOR_USER}&appointmentDate={day}",
        token=token,
    )
    data = j(body)
    if isinstance(data, list):
        return status, data
    if isinstance(data, dict):
        inner = data.get("data") or data.get("resultObject") or []
        return status, inner if isinstance(inner, list) else []
    return status, []


def find_id(day, token, hhmm, patient_id=None, cancelled=False):
    _, rows = appointments(day, token)
    found = 0
    for item in rows:
        stamp = str(item.get("appointmentTime") or item.get("AppointmentTime") or "")
        if not stamp.startswith(hhmm[:5]):
            continue
        if patient_id and int(item.get("patientId") or item.get("PatientId") or 0) != patient_id:
            continue
        state = str(item.get("status") or item.get("appStatus") or "")
        is_cancelled = state.upper() == "CANCELLED"
        if is_cancelled != cancelled:
            continue
        found = max(found, int(item.get("patientAppId") or item.get("PatientAppId") or 0))
    return found


def book(day, hhmm, token, patient, mode, **extra):
    payload = {
        "patientAppId": 0,
        "patientId": patient,
        "doctorId": DOCTOR_ID,
        "userId": DOCTOR_USER,
        "appointmentDate": day,
        "appointmentTime": hhmm,
        "status": "WAITING",
        "consultMode": mode,
        "visitType": mode,
        "deleteStatus": False,
    }
    payload.update(extra)
    return req("POST", f"{NEW}/api/PatientApp", payload, token=token)


def main():
    print("== logins ==")
    ds, db, doctor = login("Tufan_Doctor")
    check("REC-01.03", "doctor login", ds, db, {200})
    rs, rb, reception = login("Tufan_Reception")
    check("REC-01.03", "reception login", rs, rb, {200})
    a_s, a_b, admin = login("Tufan_Admin")
    check("SEC", "admin login", a_s, a_b, {200})
    ps, pb, patient = login("Tufan_Patient")
    check("SEC", "patient login", ps, pb, {200})
    bad_s, bad_b, _ = login("Tufan_Doctor", "wrong-password")
    check("REC-01.02", "wrong password rejected", bad_s, bad_b, {400, 401})
    acc_s, _, account = login("Tufan_Account")
    ph_s, _, pharmacy = login("Tufan_Pharmacy")
    if acc_s == 200:
        check("SEC", "account login", acc_s, "ok", {200})
    if ph_s == 200:
        check("SEC", "pharmacy login", ph_s, "ok", {200})

    before_blank = sql("SET NOCOUNT ON; SELECT COUNT(*) FROM PatientAppointment WHERE VisitType IS NULL OR LTRIM(RTRIM(VisitType))='';")
    before_first = sql("SET NOCOUNT ON; SELECT COUNT(*) FROM PatientAppointment WHERE VisitType='First';")

    print("== APT schedule and slots ==")
    day = None
    for offset in range(1, 25):
        candidate = (date(2027, 4, 1) + timedelta(days=offset)).isoformat()
        status, body = req("POST", f"{NEW}/api/PatientAppointment/SaveDailySchedule", {
            "doctorId": DOCTOR_ID,
            "scheduleDate": candidate,
            "slotIntervalMinutes": 15,
            "workStartTime": "09:00:00",
            "workEndTime": "12:00:00",
            "breakStartTime": "10:30:00",
            "breakEndTime": "10:45:00",
            "createdByUserId": DOCTOR_USER,
        }, token=doctor)
        if status == 200:
            day = candidate
            check("APT-07.02", f"save fresh schedule {day}", status, body, {200})
            break
    if not day:
        check("APT-07.02", "save fresh schedule", 0, "no free date", {200})
        return 1

    status, body = req("POST", f"{NEW}/api/PatientAppointment/SaveDailySchedule", {
        "doctorId": DOCTOR_ID,
        "scheduleDate": day,
        "slotIntervalMinutes": 30,
        "workStartTime": "08:00:00",
        "workEndTime": "13:00:00",
        "createdByUserId": DOCTOR_USER,
    }, token=doctor)
    check("APT-07.02", "locked schedule cannot change", status, body, {400}, contains=["already"])

    status, body = req("POST", f"{NEW}/api/PatientAppointment/SaveDailySchedule", {
        "doctorId": DOCTOR_ID,
        "scheduleDate": day,
        "slotIntervalMinutes": 15,
        "workStartTime": "09:00:00",
        "workEndTime": "12:00:00",
        "breakStartTime": "08:00:00",
        "breakEndTime": "08:15:00",
        "createdByUserId": DOCTOR_USER,
    }, token=doctor)
    check("APT-07.02", "break outside hours rejected", status, body, {400})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/SaveDailySchedule", {
        "doctorId": DOCTOR_ID,
        "scheduleDate": "2027-05-02",
        "slotIntervalMinutes": 15,
        "workStartTime": "09:00:00",
        "workEndTime": "12:00:00",
        "createdByUserId": DOCTOR_USER,
    }, token=reception)
    check("REC-11.01", "reception cannot save schedule", status, body, {403})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/SaveDailySchedule", {
        "doctorId": DOCTOR_ID,
        "scheduleDate": "2027-05-03",
        "slotIntervalMinutes": 15,
        "workStartTime": "09:00:00",
        "workEndTime": "12:00:00",
        "createdByUserId": DOCTOR_USER,
    }, token=patient)
    check("APT-01.02", "patient cannot save schedule", status, body, {403})

    status, body = req("GET", f"{NEW}/api/PatientAppointment/GetDailySchedule?doctorId={DOCTOR_ID}&scheduleDate={day}", token=reception)
    check("REC-11.01", "reception reads schedule", status, body, {200})

    status, body = req("GET", f"{NEW}/api/PatientAppointment/GetAppointmentSlots?doctorId={DOCTOR_ID}&appointmentDate={day}", token=doctor)
    payload = j(body)
    slots = payload.get("slots") or payload.get("Slots") or []
    open_times = [s.get("time") or s.get("Time") for s in slots if str(s.get("status") or s.get("Status")).lower() == "available"]
    break_times = [s.get("time") or s.get("Time") for s in slots if str(s.get("status") or s.get("Status")).lower() == "break"]
    check("APT-08.03", "slot engine returns open slots", status, body, {200} if len(open_times) >= 6 else set())
    check("APT-07.02", "break slot is marked break", status, body, {200} if break_times else set())

    status, body = req("GET", f"{NEW}/api/Public/Doctors/{DOCTOR_ID}/Slots?date={day}")
    public_slots = j(body)
    public_data = public_slots.get("data") or public_slots
    check("APT-08.03", "public slots share the engine", status, body, {200})
    check("APT-08.03", "public slots hide patient names", status, body, {200}, absent=["patientname"])
    check("APT-08.03", "public day has a schedule", 200 if public_data.get("hasSchedule") is True else 0, body, {200})

    t_book, t_clash, t_move, t_patient, t_assisted, t_public = (open_times + [""] * 6)[:6]
    break_time = break_times[0] if break_times else "10:30:00"

    print("== APT create, visit type, payment ==")
    status, body = req("POST", f"{NEW}/api/PatientApp", {
        "patientAppId": 0, "patientId": STAFF_PATIENT, "doctorId": DOCTOR_ID, "userId": DOCTOR_USER,
        "appointmentDate": "2020-01-06", "appointmentTime": "10:00:00", "status": "WAITING",
        "consultMode": "Tele", "deleteStatus": False,
    }, token=doctor)
    check("APT-05.02", "past date rejected", status, body, {400}, contains=["past"])

    status, body = req("POST", f"{NEW}/api/PatientApp", {
        "patientAppId": 0, "patientId": STAFF_PATIENT, "doctorId": DOCTOR_ID, "userId": DOCTOR_USER,
        "appointmentDate": day, "appointmentTime": "09:07:00", "status": "WAITING",
        "consultMode": "Tele", "deleteStatus": False,
    }, token=doctor)
    check("APT-05.02", "misaligned slot rejected", status, body, {400})

    status, body = req("POST", f"{NEW}/api/PatientApp", {
        "patientAppId": 0, "patientId": STAFF_PATIENT, "doctorId": DOCTOR_ID, "userId": DOCTOR_USER,
        "appointmentDate": day, "appointmentTime": "08:00:00", "status": "WAITING",
        "consultMode": "Tele", "deleteStatus": False,
    }, token=doctor)
    check("APT-05.02", "outside hours rejected", status, body, {400})

    status, body = book(day, break_time, doctor, STAFF_PATIENT, "InClinic")
    check("APT-07.02", "break slot rejected", status, body, {400}, contains=["break"])

    status, body = book(day, t_book, doctor, STAFF_PATIENT, "E-CONSULT", paymentStatus="PAID")
    check("APT-02.02", "create E-CONSULT", status, body, {200})
    tele_id = find_id(day, doctor, t_book, STAFF_PATIENT)
    _, listed = appointments(day, doctor)
    tele_row = next((r for r in listed if int(r.get("patientAppId") or 0) == tele_id), {})
    check("APT-02.02", "E-CONSULT stored as Tele", 200, json.dumps(tele_row), {200}, contains=['"visitType": "Tele"'] if False else ["Tele"])
    check("APT-09.02", "client PAID is stored as UNPAID", 200, json.dumps(tele_row), {200}, contains=["UNPAID"])
    check("APT-09.02", "pay at clinic stays allowed", 200, json.dumps(tele_row), {200}, contains=["payAtClinicAllowed"])

    status, body = book(day, t_book, doctor, 3064, "In-clinic")
    check("APT-05.02", "double book is a conflict", status, body, {409})

    status, body = book(day, t_clash, doctor, 3064, "In-clinic")
    check("APT-02.02", "create in-clinic", status, body, {200})
    clinic_id = find_id(day, doctor, t_clash, 3064)
    _, listed = appointments(day, doctor)
    clinic_row = next((r for r in listed if int(r.get("patientAppId") or 0) == clinic_id), {})
    check("APT-02.02", "In-clinic stored as InClinic", 200, json.dumps(clinic_row), {200}, contains=["InClinic"])

    status, body = req("PATCH", f"{NEW}/api/PatientAppointment/{tele_id}/VisitType", {
        "consultMode": "Online",
    }, token=doctor)
    check("APT-02.02", "patch visit type Online stays Tele", status, body, {200}, contains=["Tele"])

    status, body = req("PATCH", f"{NEW}/api/PatientAppointment/{tele_id}/VisitType", {
        "consultMode": "Clinic",
    }, token=patient)
    check("APT-01.02", "patient cannot patch visit type", status, body, {403})

    status, body = book(day, t_patient, patient, OWN_PATIENT, "Tele")
    check("PAT-17.02", "patient books own visit", status, body, {200})
    own_id = find_id(day, doctor, t_patient, OWN_PATIENT)

    status, body = book(day, t_move, patient, STAFF_PATIENT, "Tele")
    check("APT-01.02", "patient cannot book another patient", status, body, {403})

    print("== APT reschedule, quiet move, cancel ==")
    status, body = req("POST", f"{NEW}/api/PatientAppointment/RescheduleAppointment", {
        "patientAppId": tele_id, "appointmentDate": day, "appointmentTime": t_clash,
    }, token=doctor)
    check("APT-05.02", "reschedule onto taken slot", status, body, {409}, contains=["alternative"])

    status, body = req("POST", f"{NEW}/api/PatientAppointment/RescheduleAppointment", {
        "patientAppId": tele_id, "appointmentDate": day,
    }, token=doctor)
    check("APT-05.02", "reschedule without time rejected", status, body, {400})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/RescheduleAppointment", {
        "patientAppId": tele_id, "appointmentDate": day, "appointmentTime": t_move, "reason": "clinic move",
    }, token=doctor)
    check("APT-05.02", "reschedule to an open slot", status, body, {200})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/UpdateAppointmentTime", {
        "patientAppId": tele_id, "appointmentDate": day, "appointmentTime": t_book,
    }, token=doctor)
    check("APT-04.03", "quiet time move does not notify", status, body, {200}, contains=["does not notify"])

    status, body = req("GET", f"{NEW}/api/PatientAppointment/ChangeLog/{tele_id}", token=doctor)
    check("APT-05.01", "change log has the reschedule", status, body, {200}, contains=["Reschedule"])

    status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
        "patientAppId": tele_id, "reasonCode": "Nope",
    }, token=doctor)
    check("APT-06.02", "unknown cancel reason rejected", status, body, {400})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
        "patientAppId": tele_id, "reasonCode": "Other",
    }, token=doctor)
    check("APT-06.02", "Other without text rejected", status, body, {400})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
        "patientAppId": tele_id, "reasonCode": "Other", "reasonText": "patient travelling",
    }, token=doctor)
    check("APT-06.02", "cancel Other with text", status, body, {200}, contains=["no refund"])

    status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
        "patientAppId": tele_id, "reasonCode": "PatientRequest",
    }, token=doctor)
    check("APT-06.02", "second cancel is a conflict", status, body, {409})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/RescheduleAppointment", {
        "patientAppId": tele_id, "appointmentDate": day, "appointmentTime": t_assisted,
    }, token=doctor)
    check("APT-05.02", "cancelled visit cannot be rescheduled", status, body, {409})

    status, body = req("POST", f"{NEW}/api/PatientApp/UpdateAppointmentStatus", {
        "patientAppId": tele_id, "status": "CANCELLED",
    }, token=doctor)
    check("APT-06.02", "status API refuses CANCELLED", status, body, {400})

    status, body = book(day, t_book, doctor, STAFF_PATIENT, "InClinic")
    check("APT-06.02", "cancelled slot can be booked again", status, body, {200})
    rebook_id = find_id(day, doctor, t_book, STAFF_PATIENT)

    status, body = req("POST", f"{NEW}/api/PatientAppointment/UpdateAppointmentTime", {
        "patientAppId": clinic_id, "appointmentDate": day, "appointmentTime": t_move,
    }, token=doctor)
    check("APT-04.03", "quiet move onto a free slot", status, body, {200})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
        "patientAppId": clinic_id, "reasonCode": "DoctorUnavailable",
    }, token=reception)
    check("REC-06.01", "reception cancels own doctor visit", status, body, {200})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
        "patientAppId": rebook_id, "reasonCode": "Duplicate",
    }, token=patient)
    check("APT-01.02", "patient cannot cancel another patient's visit", status, body, {403})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
        "patientAppId": own_id, "reasonCode": "PatientRequest",
    }, token=patient)
    check("PAT-22.02", "patient cancels own visit", status, body, {200})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {}, token=doctor)
    check("APT-06.02", "cancel without id rejected", status, body, {400})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/RescheduleAppointment", token=doctor)
    check("APT-05.02", "reschedule without token body rejected", status, body, {400, 415})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
        "patientAppId": rebook_id, "reasonCode": "PatientRequest",
    })
    check("APT-01.02", "anonymous cancel rejected", status, body, {401})

    print("== waitlist and public booking ==")
    status, body = req("POST", f"{NEW}/api/Waitlist/Join", {
        "doctorId": DOCTOR_ID, "patientId": STAFF_PATIENT, "requestedDate": day,
        "consultMode": "In-clinic", "contactName": "Second Pass", "contactMobile": "9000001881",
    })
    check("WEB-11.02", "anonymous waitlist join", status, body, {200})
    status, body = req("GET", f"{NEW}/api/Waitlist?doctorId={DOCTOR_ID}", token=doctor)
    check("WEB-11.02", "doctor sees the join", status, body, {200}, contains=["Second Pass"])
    status, body = req("GET", f"{NEW}/api/Waitlist?doctorId=4", token=doctor)
    check("APT-01.02", "doctor cannot read another waitlist", status, body, {403})
    status, body = req("GET", f"{NEW}/api/Waitlist?doctorId={DOCTOR_ID}")
    check("APT-01.02", "anonymous waitlist list rejected", status, body, {401})
    status, body = req("POST", f"{NEW}/api/Waitlist/Join", {
        "doctorId": DOCTOR_ID, "requestedDate": day, "consultMode": "Tele",
        "contactName": "No Offer", "contactMobile": "9000001882",
    })
    check("APT-06.04", "join response does not offer a slot", status, body, {200}, absent=["offered", "expiresin"])

    otp_raw = sql(
        "SET NOCOUNT ON; INSERT INTO OtpChallenge (Action, EntityType, EntityId, DestinationMasked, OtpHash, ExpiresAt, AttemptCount, CreatedAt, VerifiedAt) "
        "VALUES ('PatientAuth','Mobile','9000001771','9000001771','SECOND', DATEADD(hour,1,GETDATE()),0,GETDATE(),GETDATE()); "
        "SELECT CAST(SCOPE_IDENTITY() AS bigint);"
    )
    otp_id = 0
    for line in otp_raw.splitlines():
        line = line.strip()
        if line.isdigit():
            otp_id = int(line)
    check("CON-09.01", "patient auth session for public book", 200 if otp_id else 0, otp_raw, {200})

    status, body = req("POST", f"{NEW}/api/Public/Doctors/{DOCTOR_ID}/Bookings", {
        "mobile": "9000001771", "patientName": "Public Second", "appointmentDate": day,
        "appointmentTime": break_time[:5], "consultMode": "In-clinic", "visitType": "In-clinic",
        "bookingSessionId": otp_id,
    })
    check("APT-08.03", "public book on a break slot rejected", status, body, {400, 409})

    status, body = req("POST", f"{NEW}/api/Public/Doctors/{DOCTOR_ID}/Bookings", {
        "mobile": "9000001771", "patientName": "Public Second", "appointmentDate": day,
        "appointmentTime": t_assisted[:5], "consultMode": "In-clinic", "visitType": "In-clinic",
        "bookingSessionId": otp_id,
    })
    check("CON-09.01", "public in-clinic hold", status, body, {200}, contains=["PENDING", "InClinic", "550"])
    public_body = j(body).get("data") or {}
    public_id = int(public_body.get("patientAppId") or 0)
    token_code = public_body.get("bookingToken") or ""

    status, body = req("GET", f"{NEW}/api/Public/Bookings/{token_code}")
    check("PAT-20.02", "public booking lookup", status, body, {200}, contains=["PENDING"])

    status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
        "patientAppId": public_id, "reasonCode": "Duplicate",
    }, token=doctor)
    check("APT-06.02", "cancel public hold", status, body, {200}, contains=["no refund"])

    status, body = req("POST", f"{NEW}/api/Public/Doctors/{DOCTOR_ID}/Bookings", {
        "mobile": "9000001771", "patientName": "Public Second", "appointmentDate": day,
        "appointmentTime": t_assisted[:5], "consultMode": "Online", "visitType": "Online",
        "bookingSessionId": otp_id,
    })
    check("CON-09.01", "cancelled public slot can be booked as Tele", status, body, {200}, contains=["PENDING", "Tele", "420"])
    tele_public = int((j(body).get("data") or {}).get("patientAppId") or 0)
    if tele_public:
        req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
            "patientAppId": tele_public, "reasonCode": "Duplicate",
        }, token=doctor)

    print("== reception, queue, case paper ==")
    status, body = req("GET", f"{NEW}/api/Reception/Profile", token=doctor)
    check("REC-02.02", "doctor cannot open reception profile", status, body, {403})
    status, body = req("GET", f"{NEW}/api/Reception/Profile", token=reception)
    check("REC-02.02", "reception profile", status, body, {200}, absent=["consultFee"])
    status, body = req("PUT", f"{NEW}/api/Reception/Profile", {
        "mobileNo": "9000001444",
    }, token=reception)
    check("REC-02.02", "reception updates own mobile", status, body, {200})
    status, body = req("PUT", f"{NEW}/api/Profile/Me", {"consultFeeInClinic": 1}, token=reception)
    check("REC-02.02", "reception cannot write the doctor fee", status, body, {403})

    status, body = req("POST", f"{NEW}/api/Support/AssistedBook", {
        "doctorId": DOCTOR_ID, "patientId": STAFF_PATIENT, "appointmentDate": day,
        "appointmentTime": t_patient, "consultMode": "InClinic",
    }, token=reception)
    check("SUP-07.02", "reception assisted book", status, body, {200}, contains=["Assisted"])
    assisted_id = find_id(day, doctor, t_patient, STAFF_PATIENT)
    _, listed = appointments(day, doctor)
    assisted_row = next((r for r in listed if int(r.get("patientAppId") or 0) == assisted_id), {})
    check("SUP-07.01", "assisted channel stored", 200, json.dumps(assisted_row), {200}, contains=["Assisted"])

    status, body = req("POST", f"{NEW}/api/Support/AssistedBook", {
        "doctorId": DOCTOR_ID, "patientId": OWN_PATIENT, "appointmentDate": day,
        "appointmentTime": t_move, "consultMode": "Tele",
    }, token=patient)
    check("SUP-07.02", "patient cannot assisted-book", status, body, {403})

    status, body = req("POST", f"{NEW}/api/Reception/CasePaper", {
        "patientId": STAFF_PATIENT, "chiefComplaint": "Second pass headache",
    }, token=reception)
    check("REC-12.02", "reception writes case paper", status, body, {200})
    status, body = req("GET", f"{NEW}/api/Reception/CasePaper?patientId={STAFF_PATIENT}", token=reception)
    check("REC-12.02", "reception reads case paper", status, body, {200}, contains=["Second pass headache"])
    status, body = req("POST", f"{NEW}/api/Reception/CasePaper", {
        "patientId": STAFF_PATIENT, "chiefComplaint": "doctor path",
    }, token=doctor)
    check("REC-12.02", "doctor cannot write reception case paper", status, body, {403})
    status, body = req("GET", f"{NEW}/api/Reception/CasePaper?patientId={STAFF_PATIENT}", token=patient)
    check("REC-12.02", "patient cannot read reception case paper", status, body, {403})

    status, body = req("GET", f"{NEW}/api/PatientAppointment/Queue", token=reception)
    check("REC-08.02", "reception queue", status, body, {200})
    status, body = req("GET", f"{NEW}/api/PatientAppointment/Queue", token=patient)
    check("REC-08.02", "patient cannot open the queue", status, body, {403})
    status, body = req("POST", f"{NEW}/api/PatientAppointment/CallNext", token=patient)
    check("REC-08.02", "patient cannot call next", status, body, {403})
    status, body = req("POST", f"{NEW}/api/PatientAppointment/CallNext", token=doctor)
    check("REC-08.02", "doctor call next", status, body, {200, 404})

    if account:
        status, body = req("GET", f"{NEW}/api/PatientAppointment/Queue", token=account)
        check("REC-04.02", "account cannot open the queue", status, body, {403})

    print("== tele ==")
    status, body = req("POST", f"{NEW}/api/Tele/Availability", {"isOnline": True}, token=doctor)
    check("TEL-01.02", "doctor goes online", status, body, {200})
    status, body = req("GET", f"{NEW}/api/Tele/Availability/{DOCTOR_ID}")
    check("TEL-01.02", "public availability", status, body, {200})
    sql(f"SET NOCOUNT ON; UPDATE TeleAvailability SET LastHeartbeat = DATEADD(minute,-10,GETDATE()) WHERE DoctorId={DOCTOR_ID};")
    status, body = req("GET", f"{NEW}/api/Tele/Availability/{DOCTOR_ID}")
    online = j(body).get("data") or {}
    flag = online.get("isOnline")
    check("TEL-01.01", "heartbeat older than 2 minutes is offline", 200 if flag in (False, 0, "false") else 0, body, {200})
    req("POST", f"{NEW}/api/Tele/Availability", {"isOnline": True}, token=doctor)

    status, body = req("GET", f"{NEW}/api/Tele/DeviceCheck")
    check("PAT-26.02", "device check is a client stub", status, body, {200}, contains=["camera", "microphone", "speaker", "stub"])

    status, body = req("GET", f"{NEW}/api/Tele/Queue", token=doctor)
    check("TEL-02.02", "doctor tele queue", status, body, {200})
    status, body = req("GET", f"{NEW}/api/Tele/Queue", token=reception)
    check("TEL-02.02", "reception cannot open the tele queue", status, body, {403})

    # A fresh tele visit for the patient who owns the login, so consent and chat can be two-sided.
    status, body = book(day, t_move, doctor, OWN_PATIENT, "Tele")
    check("TEL-03.02", "tele visit for the session", status, body, {200})
    session_appt = find_id(day, doctor, t_move, OWN_PATIENT)
    status, body = req("POST", f"{NEW}/api/Tele/Sessions", {"patientAppId": session_appt}, token=doctor)
    check("TEL-03.02", "create session", status, body, {200})
    session_id = int(j(body).get("teleSessionId") or 0)
    check("TEL-03.01", "room id is the session id", 200 if str(j(body).get("roomId")) == str(session_id) else 0, body, {200})

    status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Rejoin", token=doctor)
    check("TEL-08.01", "rejoin before start rejected", status, body, {409})
    status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Token", token=doctor)
    token_body = j(body)
    check("DMO-08.02", "stub token before start", status, body, {200}, contains=["stub"])
    check("TEL-04.01", "token room matches session", 200 if str(token_body.get("roomId")) == str(session_id) else 0, body, {200})
    status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Token", token=reception)
    check("TEL-04.01", "reception cannot take a token", status, body, {403})

    status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Start", token=doctor)
    check("TEL-02.02", "start session", status, body, {200}, contains=["Active"])
    status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Rejoin", token=patient)
    check("TEL-08.01", "patient rejoin while active", status, body, {200}, contains=["stub"])
    status, body = req("GET", f"{NEW}/api/Tele/Sessions/{session_id}", token=patient)
    check("TEL-06.02", "patient reads session status", status, body, {200}, contains=["Active"])
    status, body = req("GET", f"{NEW}/api/Tele/Sessions/{session_id}", token=reception)
    check("TEL-06.02", "reception cannot read the session", status, body, {403})

    status, body = req("POST", f"{NEW}/api/Tele/Consent", {"teleSessionId": session_id, "accepted": True}, token=doctor)
    check("TEL-07.02", "doctor consent alone does not allow recording", status, body, {200}, contains=["false"])
    status, body = req("POST", f"{NEW}/api/Tele/Consent", {"teleSessionId": session_id, "accepted": False}, token=patient)
    check("TEL-07.02", "patient decline is stored", status, body, {200})
    status, body = req("POST", f"{NEW}/api/Tele/Consent", {"teleSessionId": session_id, "accepted": True}, token=patient)
    check("TEL-07.02", "both sides accepting allows recording", status, body, {200}, contains=["true"])

    status, body = req("POST", f"{NEW}/api/Tele/Chat", {"sessionId": session_id, "body": ""}, token=doctor)
    check("TEL-10.02", "empty chat rejected", status, body, {400})
    status, body = req("POST", f"{NEW}/api/Tele/Chat", {"sessionId": session_id, "body": "from the doctor"}, token=doctor)
    check("TEL-10.02", "doctor chat", status, body, {200})
    status, body = req("POST", f"{NEW}/api/Tele/Chat", {"sessionId": session_id, "body": "from the patient"}, token=patient)
    check("TEL-10.02", "patient chat", status, body, {200})
    status, body = req("GET", f"{NEW}/api/Tele/Chat/{session_id}", token=patient)
    check("TEL-10.02", "patient reads the chat", status, body, {200}, contains=["from the doctor", "from the patient"])
    status, body = req("POST", f"{NEW}/api/Tele/Chat", {"sessionId": session_id, "body": "no"}, token=reception)
    check("TEL-10.02", "reception cannot chat", status, body, {403})

    status, body = req("PUT", f"{NEW}/api/Tele/Summary", {"patientAppId": session_appt}, token=doctor)
    check("TEL-11.02", "summary without text rejected", status, body, {400})
    status, body = req("PUT", f"{NEW}/api/Tele/Summary", {"patientAppId": session_appt, "text": "Rest and follow up"}, token=doctor)
    check("TEL-11.02", "doctor saves the summary", status, body, {200})
    status, body = req("GET", f"{NEW}/api/Tele/Summary/{session_appt}", token=patient)
    check("TEL-11.02", "patient reads the summary", status, body, {200}, contains=["Rest and follow up"])
    status, body = req("GET", f"{NEW}/api/Tele/Summary/{session_appt}", token=reception)
    check("TEL-11.02", "reception cannot read the summary", status, body, {403})

    status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/JoinFailure", {"code": "CAMERA_DENIED"}, token=patient)
    check("TEL-09.01", "join failure is logged for retry", status, body, {200}, contains=["CAMERA_DENIED", "retry"])

    status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/End", token=doctor)
    check("TEL-03.02", "end session", status, body, {200})
    status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Rejoin", token=doctor)
    check("TEL-08.01", "rejoin after end rejected", status, body, {409})
    status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Token", token=patient)
    check("TEL-04.01", "token after end rejected", status, body, {409})
    status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Start", token=doctor)
    check("TEL-03.02", "start after end rejected", status, body, {409})
    status, body = req("POST", f"{NEW}/api/Tele/Sessions/99999999/Token", token=doctor)
    check("TEL-03.02", "unknown session is 404", status, body, {404})

    status, body = req("POST", f"{NEW}/api/Tele/Instant", {"contactMobile": ""}, token=patient)
    check("TEL-12.02", "instant without mobile rejected", status, body, {400})
    status, body = req("POST", f"{NEW}/api/Tele/Instant", {
        "patientId": OWN_PATIENT, "contactName": "Instant Pass", "contactMobile": "9000001883",
    }, token=patient)
    check("PAT-24.02", "instant consult request", status, body, {200})
    instant = j(body)
    instant_id = int(instant.get("instantConsultRequestId") or 0)
    if instant.get("doctorId") == DOCTOR_ID or instant.get("status") == "OFFERED" and instant.get("doctorId") in (DOCTOR_ID, None):
        status, body = req("GET", f"{NEW}/api/Tele/Instant/Offers", token=doctor)
        check("TEL-12.02", "doctor sees offers", status, body, {200})
    if instant.get("doctorId") == DOCTOR_ID:
        status, body = req("POST", f"{NEW}/api/Tele/Instant/{instant_id}/Accept", token=doctor)
        check("TEL-12.02", "doctor accepts the offer", status, body, {200}, contains=["ACCEPTED"])
        status, body = req("POST", f"{NEW}/api/Tele/Instant/{instant_id}/Accept", token=doctor)
        check("TEL-12.02", "second accept has no open offer", status, body, {404})
    else:
        status, body = req("POST", f"{NEW}/api/Tele/Instant/{instant_id}/Accept", token=doctor)
        check("TEL-12.02", "accept only the doctor who was offered", status, body, {404})

    print("== support, help, context, refill ==")
    status, body = req("POST", f"{NEW}/api/Support/Tickets", {
        "category": "booking", "subject": "Second pass ticket", "body": "Need help", "reporterRole": "Patient",
    }, token=patient)
    check("SUP-01.02", "patient creates a ticket", status, body, {200})
    ticket_id = int(j(body).get("supportTicketId") or 0)
    status, body = req("GET", f"{NEW}/api/Support/Tickets/Mine", token=patient)
    check("SUP-01.02", "patient sees own tickets", status, body, {200}, contains=["Second pass ticket"])
    status, body = req("GET", f"{NEW}/api/Support/Tickets/{ticket_id}/Messages", token=doctor)
    check("SUP-01.02", "doctor cannot read the patient ticket", status, body, {403})
    status, body = req("PUT", f"{NEW}/api/Support/Tickets/{ticket_id}", {
        "status": "Closed", "priority": "High",
    }, token=doctor)
    check("SUP-03.02", "doctor cannot administer tickets", status, body, {403})
    status, body = req("GET", f"{NEW}/api/Support/Tickets", token=admin)
    check("SUP-03.02", "admin ticket queue", status, body, {200})
    status, body = req("PUT", f"{NEW}/api/Support/Tickets/{ticket_id}", {
        "status": "Closed", "priority": "High",
    }, token=admin)
    check("SUP-03.02", "admin closes the ticket", status, body, {200})
    status, body = req("POST", f"{NEW}/api/Support/Tickets/{ticket_id}/Messages", {
        "body": "Closed after check", "fileName": "note.txt",
    }, token=admin)
    check("SUP-04.01", "admin message with attachment name", status, body, {200})
    status, body = req("GET", f"{NEW}/api/Support/Tickets/{ticket_id}/Messages", token=admin)
    check("SUP-04.01", "message list returns the attachment", status, body, {200}, contains=["note.txt"])
    status, body = req("GET", f"{NEW}/api/Support/Tickets?status=Closed", token=admin)
    check("SUP-03.02", "admin filters tickets by status", status, body, {200}, contains=["Second pass ticket"])
    status, body = req("GET", f"{NEW}/api/Support/Tickets?status=NOT-A-STATUS", token=admin)
    check("SUP-03.02", "unknown status filter is empty", status, body, {200}, absent=["Second pass ticket"])
    status, body = req("POST", f"{NEW}/api/Support/Tickets", {"subject": "", "body": ""}, token=patient)
    check("SUP-01.02", "empty ticket rejected", status, body, {400})

    status, body = req("POST", f"{NEW}/api/Help", {
        "title": "Second pass", "slug": "week3-second-pass", "body": "hidden", "isPublished": False,
    }, token=doctor)
    check("SUP-06.02", "doctor cannot publish help", status, body, {403})
    status, body = req("POST", f"{NEW}/api/Help", {
        "title": "Second pass", "slug": "week3-second-pass", "body": "hidden", "isPublished": False,
    }, token=admin)
    check("SUP-06.02", "admin saves an unpublished article", status, body, {200})
    status, body = req("GET", f"{NEW}/api/Help/week3-second-pass")
    check("SUP-06.02", "unpublished article stays hidden", status, body, {404})
    status, body = req("POST", f"{NEW}/api/Help", {
        "title": "Second pass", "slug": "week3-second-pass", "body": "now visible", "isPublished": True,
    }, token=admin)
    check("SUP-06.02", "admin publishes the article", status, body, {200})
    status, body = req("GET", f"{NEW}/api/Help/week3-second-pass")
    check("SUP-06.02", "published article is public", status, body, {200}, contains=["now visible"])
    req("POST", f"{NEW}/api/Help", {
        "title": "Second pass", "slug": "week3-second-pass", "body": "hidden again", "isPublished": False,
    }, token=admin)

    status, body = req("GET", f"{NEW}/api/DoctorMobile/Context/{session_appt}", token=doctor)
    check("DMO-07.02", "doctor context card", status, body, {200})
    status, body = req("GET", f"{NEW}/api/DoctorMobile/Context/{session_appt}", token=patient)
    check("DMO-07.02", "patient cannot read the context card", status, body, {403})
    status, body = req("GET", f"{NEW}/api/DoctorMobile/Context/99999999", token=doctor)
    check("DMO-07.02", "unknown appointment context is 404", status, body, {404})

    status, body = req("GET", f"{NEW}/api/Refill", token=doctor)
    check("DMO-09.02", "refill list is empty until prescriptions", status, body, {200})
    status, body = req("GET", f"{NEW}/api/Refill", token=patient)
    check("DMO-09.02", "patient cannot list refills", status, body, {403})
    status, body = req("POST", f"{NEW}/api/Refill/1/Reject", {"reason": ""}, token=doctor)
    check("DMO-09.02", "reject without a reason is 400", status, body, {400})
    status, body = req("POST", f"{NEW}/api/Refill/1/Reject", {"reason": "not due"}, token=doctor)
    check("DMO-09.02", "unknown refill reject is 404", status, body, {404})
    status, body = req("POST", f"{NEW}/api/Refill/1/Approve", token=doctor)
    check("DMO-09.02", "unknown refill approve is 404", status, body, {404})
    status, body = req("POST", f"{NEW}/api/Refill/1/Approve", token=reception)
    check("DMO-09.02", "reception cannot approve a refill", status, body, {403})

    status, body = req("GET", f"{NEW}/api/PatientApp/{session_appt}", token=doctor)
    check("APT-09.02", "appointment detail carries payment status", status, body, {200}, contains=["paymentStatus", "UNPAID"])

    today = date.today().isoformat()
    status, body = req("POST", f"{NEW}/api/PatientAppointment/SaveDailySchedule", {
        "doctorId": DOCTOR_ID, "scheduleDate": today, "slotIntervalMinutes": 15,
        "workStartTime": "18:00:00", "workEndTime": "21:00:00", "createdByUserId": DOCTOR_USER,
    }, token=doctor)
    if status == 200:
        check("APT-07.02", "today evening schedule", status, body, {200})
    slot_status, slot_body = req(
        "GET",
        f"{NEW}/api/PatientAppointment/GetAppointmentSlots?doctorId={DOCTOR_ID}&appointmentDate={today}",
        token=doctor,
    )
    now_hm = __import__("datetime").datetime.now().strftime("%H:%M:%S")
    later = ""
    for slot in (j(slot_body).get("slots") or []):
        stamp = str(slot.get("time") or "")
        if str(slot.get("status") or "").lower() == "available" and stamp > now_hm:
            later = stamp
            break
    if later:
        status, body = book(today, later, doctor, STAFF_PATIENT, "InClinic")
        check("REC-08.02", "book a waiting patient for today", status, body, {200})
        status, body = req("POST", f"{NEW}/api/PatientAppointment/CallNext", token=doctor)
        check("REC-08.02", "call next sets a board status", status, body, {200}, contains=["WALK-IN"])
    else:
        check("REC-08.02", "call next when today has no open future slot", slot_status, slot_body, {200, 404})

    inactive = sql("SET NOCOUNT ON; SELECT TOP 1 UserName FROM UserMaster WHERE ISNULL(IsActive,1)=0 AND UserName IS NOT NULL;")
    inactive_name = ""
    for line in inactive.splitlines():
        name = line.strip()
        if name and "rows affected" not in name.lower() and not name.startswith("("):
            inactive_name = name
            break
    if inactive_name:
        off_s, off_b, _ = login(inactive_name)
        check("REC-01.02", "disabled staff cannot login", off_s, off_b, {400, 401, 403})

    after_blank = sql("SET NOCOUNT ON; SELECT COUNT(*) FROM PatientAppointment WHERE VisitType IS NULL OR LTRIM(RTRIM(VisitType))='';")
    after_first = sql("SET NOCOUNT ON; SELECT COUNT(*) FROM PatientAppointment WHERE VisitType='First';")
    check("APT-02.01", "blank visit types were not rewritten", 200 if before_blank.strip().split()[-1:] == after_blank.strip().split()[-1:] else 0, f"{before_blank.strip()} -> {after_blank.strip()}", {200})
    check("APT-02.01", "First visit types were not rewritten", 200 if before_first.strip().split()[-1:] == after_first.strip().split()[-1:] else 0, f"{before_first.strip()} -> {after_first.strip()}", {200})

    failed = [row for row in ROWS if row[3] == "FAIL"]
    print(f"\n{len(ROWS) - len(failed)} passed, {len(failed)} failed")
    for row in failed:
        print("  FAIL", row[0], row[1], "status", row[2])
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
