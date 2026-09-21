# Homeocentrum role matrix (S1/S2 second pass)

Database: HomeoCentrum_Dev (`localhost\MSSQLSERVER25`)
Clinic login: Old-API `POST /api/Account/Login` (`http://127.0.0.1:5001`)
Password for Tufan_* rows: `123456`
Evidence date: 21-Sep-2026

Uniqueness: `tufanpowar@gmail.com` / `7768046064` is on **Tufan_Patient** UserMaster (and Reception staff Email/Contact, which is not UserMaster). Other Tufan_* logins keep unique emails/mobiles so LoginWithOtp / ForgotPassword stay unambiguous.

| Role | Login | Dashboard | Feature access | Restricted features | API access |
| ---- | ----- | --------- | -------------- | ------------------- | ---------- |
| Admin (`Tufan_Admin`) | Old-API 200, role `Admin`. Formik Sign in → `/dashboard` (TOTAL REVENUE / APPOINTMENTS / PATIENTS). | Admin portal home `/dashboard`. Direct `/doctordashboard` redirects to `/dashboard`. | Enquiries inbox, OTP audit, consent admin audit, clinical masters (AdminPortal). | Doctor case-taking, Account money, Pharmacy console. | AdminPortal JWT. GetMenuByRole: RoleDetails seeded by `19_DEV_Seed_Role_Menus.sql` (200 list, SPA nested nav). Empty RoleDetails still 200 `[]`. |
| Doctor (`Tufan_Doctore`) | Old-API 200, role `Doctor`. UI → `/doctordashboard` (Welcome TUFAN DOCTORE, AVAILABILITY / TELE QUEUE / UNPAID). | Doctor chrome. Direct `/accountdashboard` and `/dashboard` redirect to `/doctordashboard`. | Reception-staff Add/Edit/Disable, profile, availability, COG, board backup, complaints GET for owned patients. | Admin enquiries, Account/Pharmacy, Patient family PII. `/apps-todo` (Velzon) is not a production route. | Doctor JWT + DoctorID. Family 200 for own clinic patients via clinical APIs; Family/Me is patient-portal (money roles 403). |
| Reception (`Tufan_Reception`) | Old-API 200, role `Reception`, DoctorId 1010. UI → `/doctordashboard` (Welcome TUFAN RECEPTION). Staff row email/mobile = shared test contact. | Shared doctor dashboard until Phase 5. Patient stats chart failed to load in this pass (dashboard still usable). | Appointments / patients chrome shared with doctor. | `/doctor/patientboard` and `/doctor/reception-staff` redirect to `/doctordashboard`. COG 403. Board backup 403 after DoctorOnly authorization-filter deploy. | Reception JWT. Case-taking APIs 403. |
| Account (`Tufan_Account`) | Old-API 200, role `Account`. UI → `/accountdashboard` (Welcome TUFAN ACCOUNT, Coming Soon tiles). | Account horizontal nav: Home, Ledger, Earnings, Payouts, Invoices, Reports (stubs). | Money/OTP audit. | `/doctordashboard` and `/enquiries` redirect to `/accountdashboard`. Family/Me 403. | Account JWT. Family/PII 403 (`ForbidMoneyRoles`). Otp/Audit 200. |
| Pharmacy (`Tufan_Pharmacy`) | Old-API 200, role `PharmacyPartner`. UI → `/pharmacydashboard`. | Pharmacy Home + Coming Soon (orders/quotes/inventory/onboarding/prescriptions). | HomeoMeds stub console. | `/doctordashboard` stays on pharmacy home. Family 403. | PharmacyPartner JWT. Family 403. |
| Patient (`Tufan_Patient`) | Old-API 200, role `Patient`. LoginWithOtp on `7768046064` resolves this user. UI → `/family`. | Family + Caregiver nav. Profile email `tufanpowar@gmail.com`, mobile `7768046064`. | Family CRUD (owner from JWT), caregiver grant with OTP, portal home, privacy consent. | `/doctordashboard` redirects to `/family`. Otp/Audit 403. | Patient JWT. Family/Me 200 (ownerPatientId 3046). Caregiver ListMine 200. |
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
| Anonymous | `/book` | 9 doctors, in-clinic + tele fees |
| Anonymous | `/pricing` | Doctor SaaS plans — not patient consult fees |
| After logout | `/doctordashboard` | `/login` |
