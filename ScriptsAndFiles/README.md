# ScriptsAndFiles — Week 1 / 2 / 3 / 4 (source of truth)

## SQL + seed

| Week | Folder |
|------|--------|
| 1 | `S1_Week1/Database scripts/` |
| 2 | `S2_Week2/Database scripts/` |
| 3 | `S3_Week3/Database scripts/` |

Run on `HomeoCentrum_Dev` in numbered order.

## API documentation (single file)

**Only file to share for APIs:**

`NIGA_NewAPI/ScriptsAndFiles/Homeocentrum_All_New_And_Updated+APIs.xlsx`

- Regenerated from New-API controllers (+ Old-API rows kept where marked).
- Includes Sample request / Sample Real request / Sample response columns.
- Sample Real request: 3 copy-paste examples with real HomeoCentrum_Dev ids (no `:id`).
- New-API: `http://127.0.0.1:5002` — Old-API: `http://127.0.0.1:5001`
- Rebuild catalog: `python ScriptsAndFiles/_rebuild_api_catalog_xlsx.py`
- Enrich real samples: `python ScriptsAndFiles/_enrich_sample_real_requests.py`

Do not maintain separate `.md` / `.txt` API documentation copies for handoff.

## Dev seed logins

| UserName | Role | Password |
|----------|------|----------|
| `Tufan_Doctor` | Doctor | `123456` |
| `Tufan_Reception` | Reception | `123456` |
| `Tufan_Patient` | Patient | `123456` |
| `Tufan_Admin` | Admin | `123456` |

Swagger: `Homeocentrum_Developer` / `HomeocentrumDeveloper@12345`
