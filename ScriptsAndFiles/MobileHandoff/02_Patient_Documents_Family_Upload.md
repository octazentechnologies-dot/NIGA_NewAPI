# Patient App — Documents upload (self + family)

API base: `https://devapi.homeocentrum.com/` (same host as Patient App `newBaseApi`).

## Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/api/Patient/Documents` | Upload (multipart) |
| `GET` | `/api/Patient/Documents?patientId={id}` | List for self or family member |
| `GET` | `/api/Patient/Documents/{documentId}` | Download bytes |
| `DELETE` | `/api/Patient/Documents/{documentId}` | Delete |

Auth: `Authorization: Bearer <token>` (existing `prepareAuthHeaders`).

---

## POST upload — request body (`multipart/form-data`)

Not JSON. Use `FormData`.

| Field | Required | Type | Notes |
|-------|----------|------|--------|
| `file` | **Yes** | binary | `.pdf`, `.jpg`, `.jpeg`, `.png` · max **10 MB** |
| `patientId` | No | number | Omit / self id = logged-in patient. Pass **family member `patientId`** to attach the file to that member. |
| `documentType` | No | string | `Report` · `Prescription` · `LabReport` · `Invoice` · `Other` (default **Other**). Aliases: `Priscription`/`Rx` → Prescription; `Lab` → LabReport. |
| `description` | No | string | Optional note, max **500** chars. |

### Example (React Native / fetch)

```ts
const form = new FormData();
form.append('file', {
  uri: localUri,
  name: 'blood-report.pdf',
  type: 'application/pdf',
} as any);
form.append('patientId', String(selectedMemberPatientId)); // self or family
form.append('documentType', 'Report');
form.append('description', 'CBC from City Lab'); // optional

await fetch(`${API_BASE_URL}api/Patient/Documents`, {
  method: 'POST',
  headers: { Authorization: `Bearer ${token}` }, // do NOT set Content-Type
  body: form,
});
```

### curl

```bash
curl -X POST 'https://devapi.homeocentrum.com/api/Patient/Documents' \
  -H 'Authorization: Bearer YOUR_TOKEN' \
  -F 'file=@/path/to/report.pdf;type=application/pdf' \
  -F 'patientId=4042' \
  -F 'documentType=Report' \
  -F 'description=CBC from City Lab'
```

### Success response (shape)

```json
{
  "success": true,
  "data": {
    "documentId": 123,
    "patientId": 4042,
    "path": "patients/4042/documents/....pdf",
    "fileName": "report.pdf",
    "mime": "application/pdf",
    "size": 204800,
    "documentType": "Report",
    "description": "CBC from City Lab",
    "uploadedAt": "2026-11-03T09:30:00Z",
    "downloadUrl": "/api/Patient/Documents/123"
  }
}
```

---

## GET list — filter by member

```http
GET /api/Patient/Documents
GET /api/Patient/Documents?patientId=4042
```

Each item includes:

- `documentId`, `patientId`, `fileName`, `mime`
- `documentType`, `description`
- `uploadedAt` (and `createdAt` alias)
- `downloadUrl`

**UI rule:** when user selects Self → call without `patientId` (or with self id). When user selects a family member → always pass that member’s `patientId` on **both** list and upload.

---

## RTK Query changes (Patient App)

File: `src/store/api/new/recordsApi.ts`

1. Extend `ApiPatientDocument` with `patientId?`, `documentType?`, `description?`, `uploadedAt?`.
2. `uploadDocument` mutation: build `FormData` with `file`, optional `patientId`, `documentType`, `description` (do not set JSON `Content-Type`).
3. `getDocuments` already supports `patientId` query — pass selected member id from the health-records controller.

Controller: when uploading, include `patientId` of the currently selected member (same id used for list).

---

## Allowed `documentType` values

`Report` | `Prescription` | `LabReport` | `Invoice` | `Other`
