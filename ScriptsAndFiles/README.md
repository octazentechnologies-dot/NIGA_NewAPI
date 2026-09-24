# ScriptsAndFiles — Week 1 / 2 / 3 (source of truth)

All SQL, API docs, mobile docs, and live test helpers for **S1 Week 1**, **S2 Week 2**, and **S3 Week 3** live here:

| Week | Folder | SQL | Contracts |
|------|--------|-----|-----------|
| 1 | `S1_Week1/` | `Database scripts/` + run order in `Document/00_README_S1_Week1_RUN_ORDER.md` | `S1_Week1_API_DOC.txt`, `S1_Week1_MOBILE_API_DOC.txt` |
| 2 | `S2_Week2/` | `Database scripts/` + `Document/00_README_S2_Week2_RUN_ORDER.md` | `S2_Week2_API_DOC.txt`, `S2_Week2_MOBILE_API_DOC.txt` |
| 3 | `S3_Week3/` | `Database scripts/` + `Document/00_README_S3_Week3_RUN_ORDER.md` | `S3_Week3_API_DOC.txt`, `S3_Week3_MOBILE_API_DOC.txt` |

## Rules

1. **Use this folder only** for Week 1–3 deploy / UAT / handoff.
2. Do **not** treat `NIGA_NewAPI/Database/Scripts` as the Week 1–3 run path. Tracker ticket scripts (M05 / M06 / M12 / M14, Tufan seed 15–28) are mirrored under the week folders above.
3. Run SQL on `HomeoCentrum_Dev` with SSMS / `sqlcmd`, in the order in each week’s README.
4. After SQL: New-API `http://127.0.0.1:5038`, Old-API login `http://127.0.0.1:5001`.
5. Combined catalog (when present): `Homeocentrum_All_New_And_Updated+APIs.xlsx`.
6. Cross-week live samples: `S*_Week*/S1_S2_S3_LIVE_REQUEST_RESPONSE.txt`.

## Typical Dev seed logins

| UserName | Role | Password |
|----------|------|----------|
| `Tufan_Doctor` | Doctor | `123456` |
| `Tufan_Reception` | Reception | `123456` |
| `Tufan_Patient` | Patient | `123456` |
| `Tufan_Admin` | Admin | `123456` |

Email / mobile on seed rows: `tufanpowar001@gmail.com` / `7768046064`. Numeric DoctorId / PatientId differ by database — use login response ids in API samples.
