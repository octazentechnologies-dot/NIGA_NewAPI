# FND-02.04 — Five web portals

PDF count is **five web portals**. Pharmacy console is HomeoMeds, not a sixth portal in that count.

| Portal | Role | Landing URL | Layout |
|--------|------|-------------|--------|
| Patient Website | Public + Patient | `/` marketing, `/family` when signed in as Patient | Public landing + patient chrome |
| Doctor Web Portal | Doctor | `/doctordashboard` | Doctor layout |
| Reception Portal | Reception | `/doctordashboard` until Phase 5 splits chrome | Same doctor chrome, no case-taking |
| Admin Portal | Admin | `/dashboard` | Admin horizontal layout |
| Account Department | Account | `/accountdashboard` | Account stub (ledger/payouts later) |
| HomeoMeds console (not a 6th PDF portal) | PharmacyPartner | `/pharmacydashboard` | Pharmacy stub |

## Route ACL (FND-02.02 / SEC-04.02)

- New portal routes declare `allowedRoles` and deny other roles (redirect to that role’s home).
- Admin `/dashboard` and `/admin/*` use `AdminProtected`.
- Doctor case-taking `/doctor/patientboard` is Doctor only (not Reception).
- Velzon demo URLs (`/apps-*`, `/dashboard-crm`, …) are unregistered unless `REACT_APP_SHOW_VELZON_DEMO=true`.
