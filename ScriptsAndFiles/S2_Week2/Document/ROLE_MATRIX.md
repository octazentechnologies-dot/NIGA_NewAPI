# Homeocentrum role matrix (S1/S2 second pass)

Database: HomeoCentrum_Dev (`localhost\MSSQLSERVER25`)
Clinic login: Old-API `POST /api/Account/Login` (`http://127.0.0.1:5001`)
Password for Tufan_* rows: `123456`
Evidence date: 21-Sep-2026

Uniqueness: `tufanpowar001@gmail.com` / `7768046064` is on **Tufan_Patient** UserMaster (and Reception staff Email/Contact, which is not UserMaster). Other Tufan_* logins keep unique emails/mobiles so LoginWithOtp / ForgotPassword stay unambiguous.

| Role | Login | Dashboard | Feature access | Restricted features | API access |
| ---- | ----- | --------- | -------------- | ------------------- | ---------- |
| Admin (`Tufan_Admin`) | Old-API 200, role `Admin`. Formik Sign in → `/dashboard` (TOTAL REVENUE / APPOINTMENTS / PATIENTS). | Admin portal home `/dashboard`. Direct `/doctordashboard` redirects to `/dashboard`. Nav from GetMenuByRole (49 menus after script 19). | Enquiries inbox, OTP audit, consent admin audit, clinical masters (AdminPortal). | Doctor case-taking, Account money, Pharmacy console. | AdminPortal JWT. GetMenuByRole 200 list. Empty RoleDetails is 200 `[]` (Tufan_NoMenu). Hardcoded nav only if GetMenuByRole fails. |
| Doctor (`Tufan_Doctor`) | Old-API 200, role `Doctor`. UI → `/doctordashboard` (Welcome TUFAN DOCTOR, AVAILABILITY / TELE QUEUE / UNPAID). | Doctor chrome. Direct `/accountdashboard` redirects home. **Extra UserDetails menus other doctors do not have:** Enquiries, Family, Caregiver, Packages, Qualifications, Roles, Users, Labs. | Reception-staff Add/Edit/Disable, profile, availability, COG, board backup, plus those extra screens. | Account/Pharmacy consoles. `/apps-todo` (Velzon) is not a production route. | Doctor JWT + DoctorID. Dev privileged doctor also passes AdminPortal policy. |
| Reception (`Tufan_Reception`) | Old-API 200, role `Reception`, DoctorId 1010, DoctorUserId 10032. UI → `/doctordashboard` (Welcome TUFAN RECEPTION). Staff row email/mobile = shared test contact. | Shared doctor dashboard until Phase 5. GetMenuByRole 1 item (`/doctordashboard`). Patient stats 200 zeros (“No patient stats to show”). | Appointments / patients chrome shared with doctor. SPA does not call PatientBoardBackup. | `/doctor/patientboard`, `/patientboard`, `/admin/listqualification` redirect home. COG 403. Board backup API 403 if called. | Reception JWT Role=Reception. Case-taking APIs 403. Dashboard reader OK. |
| Account (`Tufan_Account`) | Old-API 200, role `Account`. UI → `/accountdashboard` (Welcome TUFAN ACCOUNT, Coming Soon tiles). | Account horizontal nav: Home, Ledger, Earnings, Payouts, Invoices, Reports (stubs). | Money/OTP audit. | `/doctordashboard` and `/enquiries` redirect to `/accountdashboard`. Family/Me 403. | Account JWT. Family/PII 403 (`ForbidMoneyRoles`). Otp/Audit 200. |
| Pharmacy (`Tufan_Pharmacy`) | Old-API 200, role `PharmacyPartner`. UI → `/pharmacydashboard`. | Pharmacy Home + Coming Soon (orders/quotes/inventory/onboarding/prescriptions). | HomeoMeds stub console. | `/doctordashboard` stays on pharmacy home. Family 403. | PharmacyPartner JWT. Family 403. |
| Patient (`Tufan_Patient`) | Old-API 200, role `Patient`. LoginWithOtp on `7768046064` resolves this user. UI → `/family`. | Family + Caregiver nav. Profile email `tufanpowar001@gmail.com`, mobile `7768046064`. | Family CRUD (owner from JWT), caregiver grant with OTP, portal home, privacy consent. | `/doctordashboard` redirects to `/family`. Otp/Audit 403. | Patient JWT. Family/Me 200 (ownerPatientId 3046). Caregiver ListMine 200. |
| Caregiver (`Tufan_Caregiver`) | Old-API 200, role `Patient` (caregiver is a Patient user with CaregiverAuth). | Same patient portal (`/family`). | ListActingFor 200 for patient 3046. | Cannot use Admin/Doctor/Account routes. | Patient JWT + caregiver rows. Grant still requires OTP. |

## Direct URL checks (this pass)

| Actor | URL | Result |
| ----- | --- | ------ |
| Doctor | `/doctordashboard` | Dashboard |
| Doctor | `/dashboard`, `/accountdashboard` | Redirect home |
| Doctor | `/doctor/reception-staff` | Staff CRUD |
| Doctor | `/logout` then `/doctordashboard` | Login |
| Admin | `/dashboard` | Admin metrics |
| Admin | `/doctordashboard` | Redirect `/dashboard` |
| Account | `/accountdashboard` | Account home |
| Account | `/doctordashboard`, `/enquiries` | Redirect account home |
| Pharmacy | `/pharmacydashboard` | Pharmacy home |
| Pharmacy | `/doctordashboard` | Stay on pharmacy home |
| Patient | `/family`, `/caregiver` | Family/caregiver screens |
| Patient | `/doctordashboard` | Redirect `/family` |
| Reception | `/doctordashboard` | Shared doctor dashboard |
| Reception | `/doctor/patientboard` | Redirect `/doctordashboard` |
| Reception | `/doctor/reception-staff` | Redirect `/doctordashboard` |
| Reception | `/admin/listqualification` | Redirect `/doctordashboard` |
| Patient | `/accountdashboard`, `/admin/listqualification` | Redirect `/family` |
| Anonymous | `/book` | 9 doctors, in-clinic + tele fees |
| Anonymous | `/pricing` | “Doctor SaaS plans — not patient consult fees” |
| After logout | `/doctordashboard` | `/login` |
