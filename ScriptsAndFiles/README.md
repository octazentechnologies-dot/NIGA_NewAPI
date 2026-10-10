# ScriptsAndFiles — Week 1 / 2 / 3 / 4 (source of truth)

## SQL + seed

| Week | Folder |
|------|--------|
| 1 | `S1_Week1/Database scripts/` |
| 2 | `S2_Week2/Database scripts/` |
| 3 | `S3_Week3/Database scripts/` |
| 4 | `S4_Week4/Database scripts/` (`01` schema, `02` notification outbox, `03` menus, `04` demo data, then address `05` schema, `06`–`08` country/state/India data, `09` Other, `10` EnteredBy, `11` user location ids) |

Run on `HomeoCentrum_Dev` in numbered order.

## API documentation (single file)

**Only file to share for APIs:**

`NIGA_API/ScriptsAndFiles/Homeocentrum_All_New_And_Updated+APIs.xlsx`

- Regenerated from API controllers (+ Old-API rows kept where marked).
- Includes Sample request / Sample Real request / Sample response columns.
- Sample Real request: 3 copy-paste examples with real HomeoCentrum_Dev ids (no `:id`).
- Samples in the Excel use API `https://devapi2.homeocentrum.com/api` and Old API `https://devapi1.homeocentrum.com/api`
- Rebuild catalog: `python ScriptsAndFiles/_rebuild_api_catalog_xlsx.py`
- Enrich real samples: `python ScriptsAndFiles/_enrich_sample_real_requests.py`
- Mobile-only endpoints (`/api/MobilePatient`, `/api/MobileDoctor`) and the `API_Audience_Map` sheet: run `python ScriptsAndFiles/_mobile_controller_split.py` after any rebuild. The shared-endpoint list comes from `Homeocentrum.Niga.API/Hosting/ApiAudience.cs`.

Do not maintain separate `.md` / `.txt` API documentation copies for handoff.

## Dev seed logins

| UserName | Role | Password |
|----------|------|----------|
| `Tufan_Doctor` | Doctor | `123456` |
| `Tufan_Reception` | Reception | `123456` |
| `Tufan_Patient` | Patient | `123456` |
| `Tufan_Admin` | Admin | `123456` |

Swagger: `Homeocentrum_Developer` / `HomeocentrumDeveloper@12345`
