# FND-01.01 — Shared keys and domain events

Homeocentrum is one identity: a patient, appointment, prescription or payment created on web or mobile must be the same row everywhere.

## Canonical keys (live)

| Key | Where it lives | Notes |
|-----|----------------|-------|
| `UserId` | `UserMaster.UserId`, JWT `NameIdentifier` | Every signed-in person |
| `DoctorId` | `Doctor.DoctorID`, JWT `DoctorID` | Clinic doctor. Reception tokens also carry the parent `DoctorId` |
| `PatientId` | `Patient.PatientId` | Clinical person. Family members are real `Patient` rows |
| `PatientAppId` | `PatientAppointment.PatientAppId` | Appointment / visit |
| `CaseId` | Case / board payload | Clinical case on the Patient Board |
| `RoleId` / `RoleName` | `RoleMaster`, JWT `role` | Admin, Doctor, Reception, Patient, Account, PharmacyPartner |

## Keys reserved for later money / pharmacy phases (do not invent a second table in S1/S2)

| Key | Phase | Status |
|-----|-------|--------|
| `ErxId` | eRx (S4) | Not a separate S1/S2 table. Today Rx rows stay on classic prescription APIs |
| `LedgerTxnId` | Payments (S4) | Not created in S1/S2. Dashboard badges are nullable placeholders |
| `MedicineOrderId` | HomeoMeds (S4/S5) | Not created in S1/S2. `PackageEntryDetail` is SaaS subscription only — never reuse for consult or medicine |

## Events (created / rescheduled / cancelled / paid / signed / accepted)

| Event | S1/S2 | Where |
|-------|-------|-------|
| Appointment created / rescheduled / cancelled | Existing Old-API appointment APIs | `PatientAppointment` |
| Paid | Placeholder until Phase 6 | Public booking creates an appointment; Razorpay consult pay stays Old-API |
| Signed (eRx) | Classic Rx write stays Old-API | Do not dual-write |
| Accepted (pharmacy quote) | Out of S1/S2 | Pharmacy portal is a stub |

## Dual-API rule (FND-01.03)

- New HTTP → New-API `nigahomeoAPI` `:5038`
- Existing URLs stay on the host the live UI already calls
- Clinic login, Rx-write, Razorpay stay Old-API `:5001`
- Do not silently switch hosts
- Doctor mobile must not call case-taking (clipboard, Center of Gravity, audio, lab mutate)
