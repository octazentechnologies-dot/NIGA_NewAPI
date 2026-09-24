# S3 Week 3 — SQL run order

**Source of truth for Week 3 scripts and docs:**

`NIGA_NewAPI/ScriptsAndFiles/S3_Week3/`

Do **not** run week scripts from `NIGA_NewAPI/Database/Scripts` — those copies are legacy; everything required for Week 1–3 live under `ScriptsAndFiles`.

Run on `HomeoCentrum_Dev` (`localhost\MSSQLSERVER25`) via SSMS / `sqlcmd`. Idempotent unless noted.

## Execution order

| # | File | Why |
|---|------|-----|
| 1 | `01_S3_Week3_Schema.sql` | Cancel/payment/queue/booking-channel/CalledAt, schedule break, reception IsActive, case-paper role, waitlist, tele, support, help. Does not rewrite old VisitType and does not touch Razorpay. |
| 1b | `01b_M05_APT_02_01.sql` | APT-02.01 — add VisitType + ConsultMode; blank VisitType → First. Alias: `M05_APT_02_01.sql`. |
| 2 | `02_S3_Week3_Menus.sql` | Menu URLs for schedule, support, tele, reception. |
| 3 | `03_DEV_Seed_S3_Static.sql` | Dev only. Tufan_* lookup by UserName. Static help, waitlist, schedule break, token S3STATIC01, support, instant consult. **Not for production.** |
| 4 | `04_M05_APT_09_01.sql` | APT-09.01 — PaymentStatus column + unpaid defaults. |
| 5 | `05_M06_REC_08_01.sql` | REC-08.01 — CallNext / CalledAt contract proof. |
| 5b | `05b_M06_REC_02_01.sql` | REC-02.01 — reception Profile/Me reuses DoctorReceptionStaff (no new table). Alias: `M06_REC_02_01.sql`. |
| 6a | `06_M12_TEL_01_01.sql` | TEL-01 — TeleAvailability. |
| 6b | `06_M12_TEL_07_01.sql` | TEL-07 — Instant consult request. |
| 6c | `06_M12_TEL_12_01.sql` | TEL-12 — recording consent. |
| 7 | `07_M12_TEL_03_01.sql` | TEL-03 — TeleSession + events. |
| 8a | `08_M12_TEL_02_01.sql` | TEL-02 — tele queue / join-time columns. |
| 8b | `08_M12_TEL_06_01.sql` | TEL-06 — TeleChatMessage. |
| 9 | `09_M12_TEL_10_01.sql` | TEL-10 — TeleSummary. |
| 10a | `10_M12_TEL_11_01.sql` | TEL-11 — summary access rules. |
| 10b | `10_M14_SUP_01_01.sql` | SUP-01 — SupportTicket. |
| 11 | `11_M14_SUP_03_01.sql` | SUP-03 — ticket status transitions. |
| 12 | `12_M14_SUP_06_01.sql` | SUP-06 — HelpTopic. |
| 13 | `13_M14_SUP_07_01.sql` | SUP-07 — AssistedRequest / AssistedBook support. |

Tracker aliases (same content as the numbered files above) also sit in this folder as `M05_*.sql`, `M06_*.sql`, `M12_*.sql`, `M14_*.sql` for search by ticket id. Prefer the numbered run order when applying.

Scripts 3 is Dev/test data only. Do **not** run it on production.

On another database, numeric ids (Doctor 1010, Patient 3046) differ. Seeds look up `Tufan_Doctor` / `Tufan_Patient` / `Tufan_Admin` by UserName.

## After SQL

1. New HTTP is New-API `http://127.0.0.1:5038`. Login stays Old-API `http://127.0.0.1:5001`.
2. API contract: `../S3_Week3_API_DOC.txt` (copy also in this `Document` folder).
3. Mobile contract: `../S3_Week3_MOBILE_API_DOC.txt`.
4. Live samples: `../S1_S2_S3_LIVE_REQUEST_RESPONSE.txt`.
5. Live check: `../S3_Week3_Live.py` / `../S3_Week3_SecondPass.py`.
6. SMS / WhatsApp / Tele video keys: `MESSAGING_AND_TELE_VIDEO.md` (Stub until you fill `Sms` / `TeleVideo` / WhatsApp Meta).

Waitlist auto-book and Razorpay refunds are still out of scope for this week. SMS/WhatsApp notices on cancel/reschedule/OTP/tele-ready are wired (Stub-safe).
