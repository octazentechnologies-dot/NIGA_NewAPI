"""S3 Week 3 live checks. No SMS, WhatsApp, or Razorpay calls."""
from __future__ import annotations

import json
import urllib.error
import urllib.request
from datetime import date, timedelta

OLD = "http://127.0.0.1:5001"
NEW = "http://127.0.0.1:5038"
DOCTOR_ID = 1010
PATIENT_ID = 3065
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
        return ex.code, ex.read()[:2000].decode("utf-8", "replace")
    except Exception as ex:
        return 0, str(ex)


def token_from(body: str) -> str:
    try:
        payload = json.loads(body)
    except json.JSONDecodeError:
        return ""
    data = payload.get("data") or payload
    if isinstance(data, dict):
        return data.get("token") or data.get("Token") or ""
    return ""


def check(name, status, body, expect):
    ok = status in expect
    ROWS.append((name, status, "PASS" if ok else "FAIL", body[:220].replace("\n", " ")))
    print(f"{'PASS' if ok else 'FAIL'}  {status:4}  {name}")
    if not ok:
        print("      ", body[:300].replace("\n", " "))


def login(user):
    status, body = req("POST", f"{OLD}/api/Account/Login", {"userName": user, "password": "123456"})
    check(f"OLD login {user}", status, body, {200})
    return token_from(body)


def main():
    doctor = login("Tufan_Doctor")
    reception = login("Tufan_Reception")
    admin = login("Tufan_Admin")
    patient = login("Tufan_Patient")

    status, body = req("GET", f"{NEW}/api/Help")
    check("help published list", status, body, {200})
    check("help hides draft", status, body, {200} if "draft-hidden" not in body else set())

    status, body = req("GET", f"{NEW}/api/Help/draft-hidden-article")
    check("unpublished help 404", status, body, {404})

    status, body = req("GET", f"{NEW}/api/Help/booking-a-visit")
    check("published help 200", status, body, {200})

    status, body = req("POST", f"{NEW}/api/Waitlist/Join", {
        "doctorId": DOCTOR_ID,
        "patientId": PATIENT_ID,
        "requestedDate": "2026-12-01",
        "consultMode": "In-clinic",
        "contactName": "Week3 Wait",
        "contactMobile": "9000000991",
    })
    check("waitlist join no offer", status, body, {200})

    status, body = req("GET", f"{NEW}/api/Waitlist?doctorId={DOCTOR_ID}", token=doctor)
    check("doctor sees waitlist", status, body, {200})

    status, body = req("GET", f"{NEW}/api/Waitlist?doctorId=4", token=doctor)
    check("doctor cannot read other waitlist", status, body, {403})

    status, body = req("POST", f"{NEW}/api/PatientApp", {
        "patientAppId": 0,
        "patientId": PATIENT_ID,
        "doctorId": DOCTOR_ID,
        "userId": 10032,
        "appointmentDate": "2026-11-03",
        "appointmentTime": "10:00:00",
        "status": "WAITING",
        "consultMode": "Tele",
        "visitType": "Tele",
        "deleteStatus": False,
    }, token=doctor)
    check("create without schedule rejected", status, body, {400})

    day = None
    for offset in range(0, 40):
        candidate = (date(2027, 8, 1) + timedelta(days=offset)).isoformat()
        status, body = req("POST", f"{NEW}/api/PatientAppointment/SaveDailySchedule", {
            "doctorId": DOCTOR_ID,
            "scheduleDate": candidate,
            "slotIntervalMinutes": 15,
            "workStartTime": "09:00:00",
            "workEndTime": "12:00:00",
            "breakStartTime": "10:30:00",
            "breakEndTime": "10:45:00",
            "createdByUserId": 10032,
        }, token=doctor)
        if status == 200:
            day = candidate
            check(f"save schedule {day}", status, body, {200})
            break
    if not day:
        check("save schedule", 0, "no free date", {200})
        print(f"\n{sum(1 for r in ROWS if r[2]=='PASS')} passed, {sum(1 for r in ROWS if r[2]=='FAIL')} failed")
        return

    status, body = req("POST", f"{NEW}/api/PatientAppointment/SaveDailySchedule", {
        "doctorId": DOCTOR_ID,
        "scheduleDate": day,
        "slotIntervalMinutes": 15,
        "workStartTime": "09:00:00",
        "workEndTime": "12:00:00",
        "breakStartTime": "08:00:00",
        "breakEndTime": "08:15:00",
        "createdByUserId": 10032,
    }, token=doctor)
    check("break outside hours rejected", status, body, {400})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/SaveDailySchedule", {
        "doctorId": DOCTOR_ID,
        "scheduleDate": "2026-12-21",
        "slotIntervalMinutes": 15,
        "workStartTime": "09:00:00",
        "workEndTime": "12:00:00",
        "createdByUserId": 10032,
    }, token=reception)
    check("reception cannot edit schedule", status, body, {403})

    status, body = req("GET", f"{NEW}/api/PatientAppointment/GetAppointmentSlots?doctorId={DOCTOR_ID}&appointmentDate={day}", token=doctor)
    open_times = []
    try:
        payload = json.loads(body)
        for slot in payload.get("slots") or payload.get("Slots") or []:
            if str(slot.get("status") or slot.get("Status") or "").lower() == "available":
                open_times.append(slot.get("time") or slot.get("Time"))
    except json.JSONDecodeError:
        pass
    check("open slots from the one calculator", status, body, {200} if len(open_times) >= 3 else set())
    first, clash, later = (open_times + ["", "", ""])[:3]

    status, body = req("POST", f"{NEW}/api/PatientApp", {
        "patientAppId": 0,
        "patientId": PATIENT_ID,
        "doctorId": DOCTOR_ID,
        "userId": 10032,
        "appointmentDate": day,
        "appointmentTime": first,
        "status": "WAITING",
        "consultMode": "Tele",
        "visitType": "Tele",
        "deleteStatus": False,
    }, token=doctor)
    check("create tele appointment", status, body, {200})

    status, body = req("GET", f"{NEW}/api/PatientAppointment/GetAppointmentsByDate?userId=10032&appointmentDate={day}", token=doctor)
    check("list includes new appointment", status, body, {200})
    check("list carries payment status", status, body, {200} if "paymentStatus" in body or "PaymentStatus" in body else set())
    app_id = 0
    try:
        items = json.loads(body)
        if isinstance(items, list):
            for item in items:
                stamp = str(item.get("appointmentTime") or item.get("AppointmentTime") or "")
                if stamp.startswith(str(first)[:5]):
                    app_id = item.get("patientAppId") or item.get("PatientAppId") or 0
    except json.JSONDecodeError:
        pass
    print("      appointment", app_id, first)
    if not app_id:
        check("parsed new appointment id", 0, body[:500], {200})

    if app_id:
        status, body = req("POST", f"{NEW}/api/PatientApp", {
            "patientAppId": 0,
            "patientId": 3064,
            "doctorId": DOCTOR_ID,
            "userId": 10032,
            "appointmentDate": day,
            "appointmentTime": clash,
            "status": "WAITING",
            "consultMode": "InClinic",
            "deleteStatus": False,
        }, token=doctor)
        check("second appointment for clash", status, body, {200})

        status, body = req("POST", f"{NEW}/api/PatientAppointment/RescheduleAppointment", {
            "patientAppId": app_id,
            "appointmentDate": day,
            "appointmentTime": clash,
        }, token=doctor)
        check("reschedule onto taken slot 409", status, body, {409})

        status, body = req("POST", f"{NEW}/api/PatientAppointment/RescheduleAppointment", {
            "patientAppId": app_id,
            "appointmentDate": day,
            "appointmentTime": later,
            "reason": "clinic move",
        }, token=doctor)
        check("reschedule to open slot", status, body, {200})

        status, body = req("POST", f"{NEW}/api/PatientAppointment/RescheduleAppointment", {
            "patientAppId": app_id,
            "appointmentDate": day,
            "appointmentTime": later,
        }, token=patient)
        check("patient cannot reschedule this visit", status, body, {403})

        status, body = req("GET", f"{NEW}/api/PatientAppointment/ChangeLog/{app_id}", token=doctor)
        check("change log written", status, body, {200})

        status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
            "patientAppId": app_id,
            "reasonCode": "Other",
        }, token=doctor)
        check("cancel other without text 400", status, body, {400})

        status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
            "patientAppId": app_id,
            "reasonCode": "PatientRequest",
        }, token=doctor)
        check("cancel frees slot", status, body, {200})

        status, body = req("POST", f"{NEW}/api/Support/AssistedBook", {
            "doctorId": DOCTOR_ID,
            "patientId": PATIENT_ID,
            "appointmentDate": day,
            "appointmentTime": first,
            "consultMode": "InClinic",
        }, token=reception)
        check("reception assisted book", status, body, {200})
        assisted_id = 0
        status, listed = req("GET", f"{NEW}/api/PatientAppointment/GetAppointmentsByDate?userId=10032&appointmentDate={day}", token=doctor)
        try:
            for item in json.loads(listed):
                stamp = str(item.get("appointmentTime") or "")
                state = str(item.get("status") or item.get("appStatus") or "")
                if stamp.startswith(str(first)[:5]) and state.upper() != "CANCELLED":
                    assisted_id = item.get("patientAppId") or 0
        except json.JSONDecodeError:
            assisted_id = 0
        if assisted_id:
            status, body = req("POST", f"{NEW}/api/PatientAppointment/CancelAppointment", {
                "patientAppId": assisted_id,
                "reasonCode": "Duplicate",
            }, token=reception)
            check("reception cancels assisted booking", status, body, {200})
        check("cancel does not mention razorpay refund action", status, body, {200} if "refund was sent" not in body.lower() or "no refund" in body.lower() else set())

        status, body = req("POST", f"{NEW}/api/PatientApp/UpdateAppointmentStatus", {
            "patientAppId": app_id,
            "status": "CANCELLED",
        }, token=doctor)
        check("status API refuses CANCELLED", status, body, {400})

        status, body = req("POST", f"{NEW}/api/Tele/Sessions", {"patientAppId": 27159}, token=doctor)
        check("create tele session", status, body, {200})
        session_id = 0
        try:
            session_id = json.loads(body).get("teleSessionId") or 0
        except json.JSONDecodeError:
            pass
        if session_id:
            status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Token", token=doctor)
            check("stub tele token", status, body, {200})
            status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Rejoin", token=doctor)
            check("rejoin before start is 409", status, body, {409})
            status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Start", token=doctor)
            check("start session", status, body, {200})
            status, body = req("POST", f"{NEW}/api/Tele/Consent", {
                "teleSessionId": session_id,
                "accepted": False,
            }, token=doctor)
            check("decline recording still logs", status, body, {200})
            status, body = req("POST", f"{NEW}/api/Tele/Chat", {
                "sessionId": session_id,
                "body": "hello from the room",
            }, token=doctor)
            check("session chat", status, body, {200})
            status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/End", token=doctor)
            check("end session", status, body, {200})
            status, body = req("POST", f"{NEW}/api/Tele/Sessions/{session_id}/Rejoin", token=doctor)
            check("rejoin after end is 409", status, body, {409})

    status, body = req("POST", f"{NEW}/api/Tele/Availability", {"isOnline": True}, token=doctor)
    check("doctor online", status, body, {200})

    status, body = req("GET", f"{NEW}/api/Tele/Availability/{DOCTOR_ID}")
    check("public availability", status, body, {200})

    status, body = req("POST", f"{NEW}/api/Tele/Instant", {
        "patientId": PATIENT_ID,
        "contactName": "Instant",
        "contactMobile": "9000000992",
    }, token=patient)
    check("instant consult offer", status, body, {200})
    instant_id = 0
    try:
        instant_id = json.loads(body).get("instantConsultRequestId") or 0
    except json.JSONDecodeError:
        pass

    if instant_id:
        status, body = req("POST", f"{NEW}/api/Tele/Instant/{instant_id}/Accept", token=doctor)
        check("doctor accepts instant", status, body, {200})

    status, body = req("POST", f"{NEW}/api/Support/Tickets", {
        "category": "booking",
        "subject": "Week 3 ticket",
        "body": "Need a slot",
        "reporterRole": "Doctor",
    }, token=doctor)
    check("doctor ticket", status, body, {200})
    ticket_id = 0
    try:
        ticket_id = json.loads(body).get("supportTicketId") or 0
    except json.JSONDecodeError:
        pass

    status, body = req("GET", f"{NEW}/api/Support/Tickets", token=doctor)
    check("doctor cannot open admin queue", status, body, {403})

    status, body = req("GET", f"{NEW}/api/Support/Tickets", token=admin)
    check("admin ticket queue", status, body, {200})

    if ticket_id:
        status, body = req("POST", f"{NEW}/api/Support/Tickets/{ticket_id}/Messages", {
            "body": "Looking at it",
        }, token=admin)
        check("admin replies", status, body, {200})
        status, body = req("GET", f"{NEW}/api/Support/Tickets/{ticket_id}/Messages", token=patient)
        check("patient cannot read doctor ticket", status, body, {403})

    status, body = req("GET", f"{NEW}/api/Tele/DeviceCheck")
    check("device check contract", status, body, {200})

    status, body = req("GET", f"{NEW}/api/Public/Doctors/{DOCTOR_ID}/Slots?date={day}")
    check("public slots use the shared engine", status, body, {200})
    check("public day has a schedule", status, body, {200} if '"hasSchedule":true' in body.replace(" ", "") else set())

    status, body = req("GET", f"{NEW}/api/Reception/Profile", token=doctor)
    check("doctor cannot open reception profile", status, body, {403})

    status, body = req("GET", f"{NEW}/api/PatientAppointment/GetDailySchedule?doctorId={DOCTOR_ID}&scheduleDate={day}", token=reception)
    check("reception reads schedule", status, body, {200})

    status, body = req("GET", f"{NEW}/api/Reception/Profile", token=reception)
    check("reception profile", status, body, {200})
    check("reception profile has no fee", status, body, {200} if "consultFee" not in body else set())

    status, body = req("PUT", f"{NEW}/api/Profile/Me", {
        "consultFeeInClinic": 9999,
    }, token=reception)
    check("reception cannot write doctor fee", status, body, {403})

    status, body = req("GET", f"{NEW}/api/Refill", token=doctor)
    check("refill list empty", status, body, {200})

    status, body = req("POST", f"{NEW}/api/Refill/1/Reject", {"reason": ""}, token=doctor)
    check("refill reject needs reason", status, body, {400})

    status, body = req("POST", f"{NEW}/api/Reception/CasePaper", {
        "patientId": PATIENT_ID,
        "chiefComplaint": "Headache since morning",
    }, token=reception)
    check("reception case paper", status, body, {200})

    status, body = req("POST", f"{NEW}/api/Reception/CasePaper", {
        "patientId": PATIENT_ID,
        "chiefComplaint": "Doctor should not write this path",
    }, token=doctor)
    check("doctor blocked from reception case paper", status, body, {403})

    status, body = req("GET", f"{NEW}/api/DoctorMobile/Context/27159", token=doctor)
    check("doctor context card", status, body, {200})

    status, body = req("GET", f"{NEW}/api/DoctorMobile/Context/27159", token=patient)
    check("patient cannot read doctor context", status, body, {403})

    status, body = req("POST", f"{NEW}/api/PatientAppointment/CallNext", token=doctor)
    check("call next waiting patient", status, body, {200, 404})

    status, body = req("GET", f"{NEW}/api/PatientAppointment/Queue", token=reception)
    check("reception queue", status, body, {200})

    failed = [row for row in ROWS if row[2] == "FAIL"]
    print(f"\n{len(ROWS) - len(failed)} passed, {len(failed)} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
