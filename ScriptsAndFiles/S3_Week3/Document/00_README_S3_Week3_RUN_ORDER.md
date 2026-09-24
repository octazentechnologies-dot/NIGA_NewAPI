# S3 Week 3 — SQL run order

Scripts live in **New-API only**:

`NIGA_NewAPI/ScriptsAndFiles/S3_Week3/Database scripts`

Run on `HomeoCentrum_Dev` (`localhost\MSSQLSERVER25`). Both scripts are already applied on Dev.

| # | File | Why |
|---|------|-----|
| 1 | `01_S3_Week3_Schema.sql` | Appointment cancel/payment columns, schedule break, reception IsActive, case-paper role, waitlist, tele, support, help. Does not rewrite old VisitType rows and does not touch Razorpay. |
| 2 | `02_S3_Week3_Menus.sql` | Menu URLs for schedule, support, tele, and reception. |
| 3 | `03_DEV_Seed_S3_Static.sql` | Dev only. Looks up Tufan_Doctor / Tufan_Patient / Tufan_Admin by UserName (Dev happens to be 1010 / 3046 / 10030). Static help, waitlist, schedule with a break, appointment token S3STATIC01, support ticket, instant consult. Do not run on production. |

## After SQL

1. New HTTP is New-API `http://127.0.0.1:5038`. Login stays Old-API `http://127.0.0.1:5001`.
2. API contract: `../S3_Week3_API_DOC.txt`
3. Mobile contract (same URLs): `../S3_Week3_MOBILE_API_DOC.txt`
4. Live check: `../S3_Week3_Live.py` (63 passed, 22-Sep-2026).
5. Second pass: `../S3_Week3_SecondPass.py` (149 passed, 22-Sep-2026).

No SMS, WhatsApp, waitlist auto-offer, or Razorpay in this week.
