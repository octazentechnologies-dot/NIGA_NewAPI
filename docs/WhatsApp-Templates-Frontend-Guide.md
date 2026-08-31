# WhatsApp Templates & Messaging — Frontend API Guide

**Base URL:** `{API_HOST}/api/WhatsApp`  
**Authentication:** All endpoints require JWT Bearer token.

```
Authorization: Bearer {your_jwt_token}
Content-Type: application/json
```

---

## Table of contents

1. [Quick overview](#1-quick-overview)
2. [Template categories](#2-template-categories)
3. [Placeholders](#3-placeholders)
4. [Standard response shapes](#4-standard-response-shapes)
5. [Template management APIs](#5-template-management-apis)
6. [Sending messages by category](#6-sending-messages-by-category)
7. [Recommended frontend flow](#7-recommended-frontend-flow)
8. [Common rules & errors](#8-common-rules--errors)
9. [Multi-language templates (English & Marathi)](#9-multi-language-templates-english--marathi)
10. [Cursor Prompt — Frontend Implementation](#cursor-prompt--frontend-implementation-copy-to-another-cursor-instance)
11. [Cursor Prompt — Multi-Language Update](#cursor-prompt--multi-language-update-copy-to-another-cursor-instance)

---

## 1. Quick overview

| What | How |
|------|-----|
| **Store message templates** | `GET/POST` Template APIs → saved in `WhatsAppTemplateMaster` |
| **Pick template on UI** | Call `GetTemplates?templateCategory=...` → use `templateID` in send APIs |
| **Send to one patient** | `individual: true` + `patientID` or `patientContactNumber` |
| **Send to all opted-in patients** | `bulk: true` → queued in background; response returns immediately |
| **Patient must allow WhatsApp** | `isWhatsAppOptIn: true` on patient record |

**Three message categories** map to three UI modules:

| UI module | `templateCategory` / `messageCategory` |
|-----------|----------------------------------------|
| Hospital Services | `HospitalService` |
| Offers & Discounts | `OffersDiscount` |
| Health Tips | `HealthTips` |

---

## 2. Template categories

| Category value | Use for |
|----------------|---------|
| `HospitalService` | Clinic services, announcements, appointments info |
| `OffersDiscount` | Coupons, discounts, festival/consultation offers |
| `HealthTips` | Daily tips, disease awareness, seasonal/doctor tips |

> Category strings are **case-insensitive** in validation but stored normalized as above.

### Default templates (after DB seed)

After running `WhatsAppTemplateMaster_SeedDefaults.sql`, you typically get:

| TemplateID* | TemplateName | Category |
|-------------|--------------|----------|
| 1 | Hospital Service Default | `HospitalService` |
| 2 | Offers Default | `OffersDiscount` |
| 3 | Health Tips Default | `HealthTips` |

\*IDs depend on your database — **always load IDs from `GetTemplates`**, do not hardcode in production.

---

## 3. Placeholders

Write these inside `templateBody` (or use seeded defaults). They are replaced per patient when sending.

| Placeholder | Filled from send request field |
|-------------|-------------------------------|
| `{{PatientName}}` | Patient name from DB |
| `{{DoctorName}}` | `doctorName` (or doctor profile) |
| `{{HospitalName}}` | `hospitalName` (default: `"Homeo Centrum"`) |
| `{{Date}}` | `date` (default: today, format `dd-MMM-yyyy`) |
| `{{Message}}` | `message` |
| `{{Offer}}` | `offer` |
| `{{HealthTip}}` | `healthTip` |
| `{{AppointmentDate}}` | `appointmentDate` |
| `{{AppointmentTime}}` | `appointmentTime` |

Legacy aliases also work: `{{patient_name}}`, `{{doctor_name}}`, etc.

### Example template body (Hospital)

```text
Dear {{PatientName}},

Greetings from {{HospitalName}}.

{{Message}}

Warm Regards,
{{DoctorName}}
```

---

## 4. Standard response shapes

### Success — single object (`ApiResponse<T>`)

Used by: send APIs, `GetTemplateById`, `AddTemplate`, `UpdateTemplate`, etc.

```json
{
  "success": true,
  "message": "WhatsApp template created successfully.",
  "resultObject": { }
}
```

### Failure — single object (may still include partial data on send)

```json
{
  "success": false,
  "message": "Failed to send WhatsApp message: ...",
  "resultObject": {
    "totalSent": 0,
    "totalFailed": 1,
    "results": [ ]
  }
}
```

### Success — paginated list (`PaginatedApiResponse<T>`)

Used by: `GetTemplates`, `GetMessageHistory`, `GetCampaignHistory`.

```json
{
  "success": true,
  "message": "WhatsApp templates retrieved successfully.",
  "pageNumber": 1,
  "pageSize": 10,
  "totalRecords": 3,
  "totalPages": 1,
  "resultObject": [ ]
}
```

### Paginated failure

```json
{
  "success": false,
  "message": "Invalid template category..."
}
```

---

## 5. Template management APIs

### 5.1 List templates (for dropdown by category)

**`GET /api/WhatsApp/GetTemplates`**

| Query param | Type | Description |
|-------------|------|-------------|
| `templateCategory` | string | Optional: `HospitalService`, `OffersDiscount`, `HealthTips` |
| `languageId` | int | Optional: filter by language (e.g. `1` = English, `2` = Marathi) |
| `isActive` | bool | Optional: `true` / `false` |
| `pageNumber` | int | Default `1` |
| `pageSize` | int | Default `10`, max `100` |

**Example request**

```http
GET /api/WhatsApp/GetTemplates?templateCategory=OffersDiscount&languageId=2&isActive=true&pageNumber=1&pageSize=20
```

**Example response**

```json
{
  "success": true,
  "message": "WhatsApp templates retrieved successfully.",
  "pageNumber": 1,
  "pageSize": 20,
  "totalRecords": 2,
  "totalPages": 1,
  "resultObject": [
    {
      "templateID": 2,
      "templateName": "Offers Default",
      "templateCategory": "OffersDiscount",
      "metaTemplateName": null,
      "languageId": 2,
      "languageName": "Marathi",
      "description": "Default offers and discounts template",
      "isActive": true,
      "enteredDate": "2026-06-03T10:00:00",
      "changedDate": null
    }
  ]
}
```

> List response does **not** include `templateBody`. Use `GetTemplateById` for edit screen.

---

### 5.2 Get template detail (edit / preview)

**`GET /api/WhatsApp/GetTemplateById/{id}`**

**Example response**

```json
{
  "success": true,
  "message": "WhatsApp template retrieved successfully.",
  "resultObject": {
    "templateID": 2,
    "templateName": "Offers Default",
    "templateCategory": "OffersDiscount",
    "metaTemplateName": null,
    "templateBody": "Dear {{PatientName}},\n\n{{HospitalName}} has an exclusive offer:\n\n{{Offer}}\n\nValid until {{Date}}.",
    "languageId": 1,
    "languageName": "English",
    "description": "Default offers and discounts template",
    "isActive": true,
    "enteredBy": "System",
    "enteredDate": "2026-06-03T10:00:00",
    "changedBy": null,
    "changedDate": null
  }
}
```

---

### 5.3 Create template

**`POST /api/WhatsApp/AddTemplate`**

**Request body**

| Field | Required | Description |
|-------|----------|-------------|
| `templateName` | Yes | Unique per category |
| `templateCategory` | Yes | `HospitalService` / `OffersDiscount` / `HealthTips` |
| `templateBody` | Yes | Message with placeholders |
| `languageId` | No | Language from `GetLanguages` (defaults to English if omitted) |
| `metaTemplateName` | No | Meta-approved template name (Business Manager) |
| `description` | No | Admin notes |
| `isActive` | No | Default `true` |
| `enteredBy` | No | Audit |

**Example request — Health Tips**

```json
{
  "templateName": "Monsoon Health Tip",
  "templateCategory": "HealthTips",
  "languageId": 2,
  "templateBody": "प्रिय {{PatientName}},\n\n{{DoctorName}} कडून टिप:\n{{HealthTip}}\n\n- {{HospitalName}}",
  "description": "Seasonal monsoon tip",
  "isActive": true,
  "enteredBy": "dr_sharma"
}
```

**Example response**

```json
{
  "success": true,
  "message": "WhatsApp template created successfully.",
  "resultObject": {
    "templateID": 4,
    "templateName": "Monsoon Health Tip",
    "templateCategory": "HealthTips",
    "templateBody": "Dear {{PatientName}},\n\nTip from {{DoctorName}}:\n{{HealthTip}}\n\n- {{HospitalName}}",
    "isActive": true,
    "enteredDate": "2026-06-03T14:30:00"
  }
}
```

---

### 5.4 Update template

**`POST /api/WhatsApp/UpdateTemplate`**

**Request body**

| Field | Required |
|-------|----------|
| `templateID` | Yes |
| `templateName` | Yes |
| `templateCategory` | Yes |
| `templateBody` | Yes |
| `languageId` | No (defaults to English) |
| `metaTemplateName` | No |
| `description` | No |
| `isActive` | No |
| `changedBy` | No |

**Example request**

```json
{
  "templateID": 4,
  "templateName": "Monsoon Health Tip",
  "templateCategory": "HealthTips",
  "templateBody": "Dear {{PatientName}},\n\n{{HealthTip}}\n\nStay safe this monsoon!\n{{DoctorName}}",
  "isActive": true,
  "changedBy": "dr_sharma"
}
```

**Example response** — same shape as Add (full `WhatsAppTemplateDetailModel` in `resultObject`).

---

## 6. Sending messages by category

### Send result object (all send APIs)

```json
{
  "messageId": "wamid.xxx",
  "bulkJobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "campaignID": 12,
  "totalQueued": 0,
  "totalSent": 1,
  "totalFailed": 0,
  "results": [
    {
      "patientID": 42,
      "patientName": "Rahul Patel",
      "mobileNumber": "919876543210",
      "success": true,
      "messageId": "wamid.xxx",
      "errorMessage": null
    }
  ]
}
```

| Field | Individual send | Bulk send |
|-------|-----------------|-----------|
| `messageId` | Last successful Meta message ID | Often `null` initially |
| `bulkJobId` | `null` | GUID when queued |
| `campaignID` | Optional | Set when campaign created |
| `totalQueued` | `0` | Number of recipients queued |
| `totalSent` / `totalFailed` | Per-request sync results | `0` on queue response; check history later |
| `results` | Per-patient breakdown | Empty on queue; populated in background |

---

### Category 1: Hospital Services

**Dedicated endpoint:** `POST /api/WhatsApp/SendHospitalServiceMessage`  
**Category applied automatically:** `HospitalService`

#### Individual — Hospital Services

Rules:
- `individual: true`, `bulk: false`
- `patientContactNumber` **required** (10+ digit mobile)
- Patient must be linked to `doctorID` and `isWhatsAppOptIn: true`

```json
{
  "doctorID": 1,
  "languageId": 1,
  "templateID": 1,
  "individual": true,
  "bulk": false,
  "patientContactNumber": "9876543210",
  "patientName": "Rahul Patel",
  "doctorName": "Dr. Sharma",
  "hospitalName": "Homeo Centrum",
  "date": "2026-06-15",
  "message": "We now offer online consultation every Saturday.",
  "imageBase64": null
}
```

**Success response**

```json
{
  "success": true,
  "message": "WhatsApp message sent successfully.",
  "resultObject": {
    "messageId": "wamid.HBgM...",
    "bulkJobId": null,
    "campaignID": null,
    "totalQueued": 0,
    "totalSent": 1,
    "totalFailed": 0,
    "results": [
      {
        "patientID": 42,
        "patientName": "Rahul Patel",
        "mobileNumber": "919876543210",
        "success": true,
        "messageId": "wamid.HBgM...",
        "errorMessage": null
      }
    ]
  }
}
```

#### Bulk — Hospital Services

Rules:
- `individual: false`, `bulk: true`
- Sends to **all** patients for doctor with `isWhatsAppOptIn: true` and valid mobile
- **Async** — returns queue summary immediately

```json
{
  "doctorID": 1,
  "languageId": 2,
  "templateID": 4,
  "individual": false,
  "bulk": true,
  "doctorName": "Dr. Sharma",
  "hospitalName": "Homeo Centrum",
  "date": "2026-06-15",
  "message": "Free health camp on Sunday 9 AM - 1 PM.",
  "imageBase64": null
}
```

**Bulk queue response**

```json
{
  "success": true,
  "message": "Bulk HospitalService messages queued for 85 recipient(s).",
  "resultObject": {
    "messageId": null,
    "bulkJobId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "campaignID": 5,
    "totalQueued": 85,
    "totalSent": 0,
    "totalFailed": 0,
    "results": []
  }
}
```

> Poll `GET /api/WhatsApp/GetMessageHistory?doctorID=1&messageCategory=HospitalService` to see delivery status.

---

### Category 2: Offers & Discounts

**Dedicated endpoint:** `POST /api/WhatsApp/SendOfferMessage`  
**Category:** `OffersDiscount`  
**Key placeholder:** `{{Offer}}` — pass value in `offer` field

#### Individual — Offers (by Patient ID)

```json
{
  "doctorID": 1,
  "templateID": 2,
  "individual": true,
  "bulk": false,
  "patientID": 42,
  "doctorName": "Dr. Sharma",
  "hospitalName": "Homeo Centrum",
  "date": "2026-06-30",
  "offer": "20% off first consultation + free follow-up within 7 days",
  "message": "Limited time festival offer!",
  "imageBase64": null
}
```

#### Individual — Offers (by mobile)

Use `patientContactNumber` instead of `patientID`:

```json
{
  "doctorID": 1,
  "templateID": 2,
  "individual": true,
  "bulk": false,
  "patientContactNumber": "9876543210",
  "offer": "Flat Rs. 200 off on package booking",
  "date": "2026-07-31"
}
```

#### Bulk — Offers

```json
{
  "doctorID": 1,
  "templateID": 2,
  "individual": false,
  "bulk": true,
  "doctorName": "Dr. Sharma",
  "hospitalName": "Homeo Centrum",
  "date": "2026-06-30",
  "offer": "Diwali special: 15% off all chronic care packages",
  "message": "Book before 30 June to avail.",
  "imageBase64": "data:image/jpeg;base64,/9j/4AAQ..."
}
```

**Notes for offers UI:**
- `offer` → maps to `{{Offer}}` in template
- `message` → maps to `{{Message}}` (optional extra line)
- Image: JPEG/PNG/WEBP, max **5 MB**, as Base64 or data-URI

---

### Category 3: Health Tips

**Dedicated endpoint:** `POST /api/WhatsApp/SendHealthTipMessage`  
**Category:** `HealthTips`  
**Key placeholder:** `{{HealthTip}}`

#### Individual — Health Tips

```json
{
  "doctorID": 1,
  "templateID": 3,
  "individual": true,
  "bulk": false,
  "patientID": 42,
  "doctorName": "Dr. Sharma",
  "hospitalName": "Homeo Centrum",
  "healthTip": "Drink warm water in the morning and avoid cold drinks during seasonal change.",
  "message": "Your weekly wellness tip"
}
```

#### Bulk — Health Tips

```json
{
  "doctorID": 1,
  "templateID": 3,
  "individual": false,
  "bulk": true,
  "doctorName": "Dr. Sharma",
  "hospitalName": "Homeo Centrum",
  "healthTip": "Wash hands frequently during flu season and maintain 7-8 hours of sleep.",
  "imageBase64": null
}
```

---

### Generic send APIs (optional)

#### `POST /api/WhatsApp/SendIndividualMessage`

Hospital-style individual only. Uses `patientID` (not mobile).

```json
{
  "doctorID": 1,
  "patientID": 42,
  "templateID": 1,
  "message": "Your appointment is confirmed.",
  "doctorName": "Dr. Sharma",
  "hospitalName": "Homeo Centrum",
  "appointmentDate": "2026-06-10",
  "appointmentTime": "10:30 AM"
}
```

#### `POST /api/WhatsApp/SendBulkMessage`

Generic bulk for **any** category. Requires `campaignName` + `messageCategory`.

```json
{
  "doctorID": 1,
  "campaignName": "June Health Awareness",
  "messageCategory": "HealthTips",
  "templateID": 3,
  "healthTip": "Eat seasonal fruits and stay hydrated.",
  "doctorName": "Dr. Sharma",
  "hospitalName": "Homeo Centrum"
}
```

**Bulk response** — same queue pattern (`bulkJobId`, `totalQueued`, `campaignID`).

---

## 7. Recommended frontend flow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Login → store JWT                                        │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. GET GetLanguages → doctor picks English or Marathi     │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. User picks module: Hospital | Offers | Health Tips       │
│    → GET GetTemplates?templateCategory={cat}&languageId={}  │
│    → Populate template dropdown (templateID, templateName)  │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Optional: Admin → AddTemplate / UpdateTemplate           │
│    → GET GetTemplateById/{id} for editor                    │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Compose screen: message, offer, healthTip, date, image   │
│    Preview: show templateBody with placeholders highlighted │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Send mode:                                               │
│    Individual → patient picker (patientID or mobile)        │
│    Bulk → confirm count (opted-in patients only)            │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. Call category send API OR SendBulkMessage                │
│    Individual → show results[] per patient                  │
│    Bulk → show "Queued N messages" + campaignID / bulkJobId   │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ 8. History: GET GetMessageHistory?doctorID=&messageCategory=│
│    Campaigns: GET GetCampaignHistory?doctorID=              │
└─────────────────────────────────────────────────────────────┘
```

### Which send API to use?

| Screen | Individual | Bulk |
|--------|------------|------|
| Hospital Services | `SendHospitalServiceMessage` (`patientContactNumber`) | `SendHospitalServiceMessage` |
| Offers | `SendOfferMessage` (`patientID` or `patientContactNumber`) | `SendOfferMessage` |
| Health Tips | `SendHealthTipMessage` (`patientID` or `patientContactNumber`) | `SendHealthTipMessage` |
| Generic / custom campaign name | `SendIndividualMessage` | `SendBulkMessage` |

---

## 8. Common rules & errors

### Patient consent

Only patients with **`isWhatsAppOptIn: true`** receive messages. Set via Patient save API:

```json
{
  "patientID": 42,
  "isWhatsAppOptIn": true
}
```

If not opted in, send returns error e.g. *"Patient has not provided WhatsApp consent"* or *"not opted in"*.

### Individual vs Bulk flags

| Rule | Value |
|------|-------|
| Exactly one mode | `individual` **XOR** `bulk` (one `true`, one `false`) |
| Wrong combo | Error: *"Either Individual or Bulk must be true, but not both."* |

### Template resolution priority

1. `templateID` → load `templateBody` from DB (if active; must match `languageId` when both sent)
2. Else `messageBody` on request (inline override)
3. Else DB default template for `category` + `languageId`
4. Else built-in code default for category + language

### Language rules

| Rule | Behavior |
|------|----------|
| `languageId` omitted on send | Defaults to **English** |
| `languageId` + `templateID` | Template must belong to that language or API returns error |
| Template list | Pass `languageId` to `GetTemplates` so dropdown shows only that language |
| Doctor content fields | Doctor types `message` / `offer` / `healthTip` in the **selected language** |
| Languages API | `GET /api/mastersAPI/GetLanguages` — same `LanguageMaster` table as rest of app |

### Image (`imageBase64`)

- Optional on all send endpoints
- Formats: JPEG, PNG, WEBP
- Max size: **5 MB**
- Allowed formats: raw Base64 or `data:image/jpeg;base64,...`

### Typical error messages

| Message | Frontend action |
|---------|-----------------|
| `WhatsApp Meta API configuration is missing` | Contact backend — server config |
| `Doctor not found or has been deleted` | Invalid `doctorID` |
| `Template not found or is inactive` | Refresh template list |
| `No opted-in patients...` | Show empty state; prompt consent |
| `A template with the same name already exists for this category and language` | Same name allowed in another language; change name or language |
| `Selected template does not match the chosen language` | Re-fetch templates after language change |
| `Invalid language ID` | Reload languages from `GetLanguages` |
| `Invalid template category` | Use exact category enum values |

### Reporting (related endpoints)

| Endpoint | Purpose |
|----------|---------|
| `GET GetMessageHistory` | Filter by `messageCategory`, `doctorID`, `patientID`, dates |
| `GET GetCampaignHistory` | Bulk campaigns list |
| `GET GetCampaignDetails/{campaignId}` | Campaign + recent messages |
| `GET GetDashboard` | Totals and analytics |

**History filter example**

```http
GET /api/WhatsApp/GetMessageHistory?doctorID=1&messageCategory=OffersDiscount&pageNumber=1&pageSize=20
```

---

## Appendix: Field cheat sheet by category

| Field | Hospital | Offers | Health Tips |
|-------|:--------:|:------:|:-----------:|
| `templateID` | ✓ | ✓ | ✓ |
| `languageId` | ✓ | ✓ | ✓ |
| `message` / `{{Message}}` | ✓ | ✓ | ✓ |
| `offer` / `{{Offer}}` | — | ✓ | — |
| `healthTip` / `{{HealthTip}}` | — | — | ✓ |
| `appointmentDate` / `appointmentTime` | ✓ | ✓ | ✓ |
| `imageBase64` | ✓ | ✓ | ✓ |
| `patientID` | Offers/Tips/Generic | ✓ | ✓ |
| `patientContactNumber` | Hospital, Offers, Tips | ✓ | ✓ |
| `campaignName` | — (auto on bulk) | — | — |
| Use `SendBulkMessage` | Optional | Optional | Optional |

---

## Cursor Prompt — Frontend Implementation (copy to another Cursor instance)

Use this section when opening the **frontend repository** in a separate Cursor chat. Copy everything inside the block below into a new Cursor prompt so the agent implements doctor-side and admin-side WhatsApp features against the existing HomeoCentrum API.

---

### How to use

1. Open the **frontend** project in Cursor (not this API repo, unless frontend lives here).
2. Start a new Agent chat.
3. Copy the entire prompt below (from `BEGIN CURSOR PROMPT` to `END CURSOR PROMPT`).
4. Attach or reference this file: `Docs/WhatsApp-Templates-Frontend-Guide.md` (or paste the API base URL + JWT auth pattern from your env).
5. Let the agent read existing auth, routing, and API client patterns in the frontend codebase first, then implement.

---

```text
BEGIN CURSOR PROMPT

# HomeoCentrum — WhatsApp Messaging Frontend (Doctor + Admin)

You are implementing the WhatsApp Messaging module for HomeoCentrum (doctor portal + admin portal). The backend API is already complete. Your source of truth for endpoints, request/response JSON, categories, individual vs bulk behavior, and placeholders is the document:

**WhatsApp-Templates-Frontend-Guide.md** (sections 1–8 and Appendix)

Read that document fully before writing code. Match existing frontend stack, folder structure, API client, auth (JWT Bearer), toast/error handling, and UI component library already used in the project.

---

## API basics

- Base path: `{API_BASE_URL}/api/WhatsApp`
- Header on every call: `Authorization: Bearer {token}` from login
- Standard success: `{ success, message, resultObject }`
- Paginated success: `{ success, message, pageNumber, pageSize, totalRecords, totalPages, resultObject[] }`
- Send failure may still return `resultObject` with `results[]` per patient — show partial success UI

---

## Template categories (3 modules)

| Module UI label     | API value `templateCategory` / `messageCategory` |
|---------------------|--------------------------------------------------|
| Hospital Services   | `HospitalService`                                |
| Offers & Discounts  | `OffersDiscount`                                 |
| Health Tips         | `HealthTips`                                     |

Placeholders in templates: `{{PatientName}}`, `{{DoctorName}}`, `{{HospitalName}}`, `{{Date}}`, `{{Message}}`, `{{Offer}}`, `{{HealthTip}}`, `{{AppointmentDate}}`, `{{AppointmentTime}}`.

---

## Role split: Admin vs Doctor

### ADMIN side (template management + global visibility)

Implement for users with admin/super-admin role (use existing role guard in app):

1. **Template list** — per category tab or filter
   - `GET /api/WhatsApp/GetTemplates?templateCategory={cat}&isActive=true&pageNumber=1&pageSize=20`
   - Table/cards: templateName, category, isActive, enteredDate
   - Actions: Edit, Activate/Deactivate (via UpdateTemplate)

2. **Template create**
   - `POST /api/WhatsApp/AddTemplate`
   - Form: templateName, templateCategory (dropdown), templateBody (textarea with placeholder helper text), description, isActive, metaTemplateName (optional advanced)
   - Show live preview substituting sample placeholder values

3. **Template edit**
   - `GET /api/WhatsApp/GetTemplateById/{id}` — load templateBody
   - `POST /api/WhatsApp/UpdateTemplate` — save (POST not PUT)

4. **Optional admin dashboards**
   - `GET /api/WhatsApp/GetDashboard` — charts: totalMessages, delivered, failed, by category, monthly
   - `GET /api/WhatsApp/GetCampaignHistory` — all doctors or filter by doctorID if admin selects doctor
   - `GET /api/WhatsApp/GetMessageHistory` — global filter by category, date, status

Admin does **not** need to send messages unless product requires it; focus on template CRUD and reporting.

---

### DOCTOR side (send messages + own history)

Use `doctorID` from logged-in doctor profile (same as other doctor modules). Implement:

1. **Three send screens** (or one screen with category tabs):
   - Hospital Services → `POST /api/WhatsApp/SendHospitalServiceMessage`
   - Offers & Discounts → `POST /api/WhatsApp/SendOfferMessage`
   - Health Tips → `POST /api/WhatsApp/SendHealthTipMessage`

2. **Shared send UI pattern per category**
   - Load templates: `GET GetTemplates?templateCategory={cat}&isActive=true`
   - Dropdown: templateID + templateName; on select optionally preview templateBody from `GetTemplateById`
   - Fields by category (see Appendix in guide):
     - Hospital: message, date, doctorName, hospitalName, optional image
     - Offers: offer (required for placeholder), message, date, optional image
     - Health Tips: healthTip (required), message, optional image
   - Toggle: **Individual** vs **Bulk** (mutually exclusive; exactly one true)
   - Individual: patient picker — support patientID (preferred) and/or mobile for Hospital use patientContactNumber
   - Bulk: confirmation modal — "Send to all WhatsApp opted-in patients for this doctor?" — no patient picker
   - Image upload → convert to Base64 (max 5MB, jpeg/png/webp) → `imageBase64`
   - Submit → show result: individual = per-patient results table; bulk = show totalQueued, bulkJobId, campaignID + link to history

3. **Patient WhatsApp consent**
   - On patient profile/edit (existing patient form): checkbox `isWhatsAppOptIn` + show `whatsAppOptInDate` if API returns it
   - Before send: if individual patient not opted in, block with clear message (match API error text)
   - Optional: badge on patient list "WhatsApp ✓" for opted-in patients

4. **Message history (doctor scoped)**
   - `GET /api/WhatsApp/GetMessageHistory?doctorID={id}&messageCategory={cat}&pageNumber&pageSize`
   - Columns: patientName, mobileNumber, sendStatus, createdDate, errorMessage, messageCategory
   - Row click → `GET /api/WhatsApp/GetMessageById/{id}` detail drawer (finalMessage, templateMessage, metaMessageId)

5. **Campaign history (doctor scoped)**
   - `GET /api/WhatsApp/GetCampaignHistory?doctorID={id}`
   - Row click → `GET /api/WhatsApp/GetCampaignDetails/{campaignId}` with recentMessages

6. **Optional doctor dashboard widget**
   - `GET /api/WhatsApp/GetDashboard?doctorID={id}` — small stats cards on doctor home

---

## Send API quick reference (doctor)

### Individual — Hospital
POST SendHospitalServiceMessage
{ doctorID, templateID, individual: true, bulk: false, patientContactNumber, message, date, doctorName, hospitalName, imageBase64? }

### Bulk — Hospital
{ doctorID, templateID, individual: false, bulk: true, message, date, ... }

### Individual — Offers
POST SendOfferMessage
{ doctorID, templateID, individual: true, bulk: false, patientID OR patientContactNumber, offer, date, message?, imageBase64? }

### Bulk — Offers
{ individual: false, bulk: true, offer, ... }

### Individual — Health Tips
POST SendHealthTipMessage
{ doctorID, templateID, individual: true, bulk: false, patientID OR patientContactNumber, healthTip, message? }

### Bulk — Health Tips
{ individual: false, bulk: true, healthTip, ... }

Alternative generic bulk: POST SendBulkMessage with campaignName + messageCategory + templateID.

---

## UX requirements

- Disable send button while request in flight; prevent double submit on bulk
- After bulk queue success, toast: "Queued {totalQueued} messages" — do not wait for all sends
- Poll or refresh message history to show delivery status (sendStatus true/false)
- Validate: individual XOR bulk; required patient field for individual; offer/healthTip by category
- Template preview panel showing resolved sample text (fake placeholder values)
- Error display: show API `message` + per-row `errorMessage` from results[]
- Responsive layout consistent with existing doctor/admin layouts
- i18n only if project already uses it; otherwise English labels as in this doc

---

## Implementation order (suggested)

1. API service module: `whatsappApi.ts` (or equivalent) with typed methods for all endpoints in the guide
2. Admin: template list + add/edit
3. Doctor: one category send screen end-to-end (Hospital) — then clone pattern for Offers and Health Tips
4. Doctor: message history + campaign history
5. Patient consent checkbox on patient form
6. Admin dashboard (optional)

---

## Do NOT

- Invent new API endpoints or field names — use exact casing from guide (e.g. templateID, doctorID, patientID)
- Use PUT for template update — use POST UpdateTemplate
- Send bulk synchronously expecting immediate totalSent — bulk returns totalQueued
- Skip JWT on WhatsApp routes
- Hardcode template IDs — always load from GetTemplates

---

## Before finishing

- Wire routes/menus: Admin → "WhatsApp Templates" + "WhatsApp Reports"; Doctor → "Send WhatsApp" (3 sub-menus) + "Message History"
- Test individual send with opted-in patient; test bulk queue response; test template CRUD on admin
- Align TypeScript interfaces with JSON property names in WhatsApp-Templates-Frontend-Guide.md

END CURSOR PROMPT
```

---

## 9. Multi-language templates (English & Marathi)

WhatsApp templates now support **multiple languages** via `LanguageId` on `WhatsAppTemplateMaster`, linked to the existing `LanguageMaster` table.

### Language API

**`GET /api/mastersAPI/GetLanguages`**

```json
[
  { "languageId": 1, "languageName": "English", "description": "English", "isDeleted": false },
  { "languageId": 2, "languageName": "Marathi", "description": "Marathi", "isDeleted": false }
]
```

> Always load `languageId` values from this API — do not hardcode `1` / `2` in production.

### How templates work per language

- Same `templateName` + `templateCategory` can exist **once per language** (e.g. "Hospital Service Default" in English and Marathi are separate rows with different `templateID`).
- `GetTemplates?templateCategory=HospitalService&languageId=2` returns **Marathi only**.
- Each `templateID` is already language-specific; `languageId` on send is for validation and defaults.

### Doctor send flow (updated)

```
1. GET /api/mastersAPI/GetLanguages
2. Doctor selects Language (English / Marathi)
3. GET /api/WhatsApp/GetTemplates?templateCategory={cat}&languageId={selected}
4. Doctor selects template → preview via GetTemplateById
5. Doctor fills message/offer/healthTip in selected language
6. Send with languageId + templateID on all send APIs
```

### Send request — all category APIs

Add `languageId` to every send body (`SendHospitalServiceMessage`, `SendOfferMessage`, `SendHealthTipMessage`, `SendIndividualMessage`, `SendBulkMessage`):

```json
{
  "doctorID": 1,
  "languageId": 2,
  "templateID": 4,
  "individual": true,
  "bulk": false,
  "patientID": 42,
  "message": "तुमची भेट निश्चित झाली आहे."
}
```

If `languageId` is **omitted**, backend defaults to **English**.

### Admin template CRUD

- **AddTemplate** / **UpdateTemplate**: include `languageId` (optional; defaults to English).
- Create Marathi template: same `templateName` as English version is allowed when `languageId` differs.
- Admin list can show `languageName` column from `GetTemplates` response.

### Message history

`GetMessageHistory` and `GetMessageById` now return `languageId` and `languageName` on each log row.

### Database scripts (backend / DevOps)

Run in order on existing databases:

1. `Database/Scripts/WhatsAppTemplateMaster_Alter_Language.sql`
2. `Database/Scripts/WhatsAppMessageLog_Alter_LanguageId.sql`
3. `Database/Scripts/WhatsAppTemplateMaster_SeedMarathiDefaults.sql`

---

## Cursor Prompt — Multi-Language Update (copy to another Cursor instance)

Use this when the **frontend already has WhatsApp messaging** and you only need to add **English + Marathi language selection**. Copy everything inside the block into a new Cursor Agent chat in the **frontend** repository.

---

### How to use

1. Open the **frontend** project in Cursor.
2. Start a new Agent chat.
3. Copy the entire prompt below (`BEGIN MULTI-LANGUAGE PROMPT` → `END MULTI-LANGUAGE PROMPT`).
4. Attach or reference: `docs/WhatsApp-Templates-Frontend-Guide.md` (sections 5, 6, 8, 9).

---

```text
BEGIN MULTI-LANGUAGE PROMPT

# HomeoCentrum — WhatsApp Multi-Language (English + Marathi)

The backend API now supports multi-language WhatsApp templates. Your source of truth is **WhatsApp-Templates-Frontend-Guide.md** (sections 5, 6, 8, 9).

Read existing WhatsApp module code first (API client, send screens, admin template CRUD). Extend it — do not rewrite from scratch unless missing.

---

## What changed on the API

| Area | Change |
|------|--------|
| Languages | `GET /api/mastersAPI/GetLanguages` → `{ languageId, languageName, description }` |
| Template list | `GET /api/WhatsApp/GetTemplates` accepts `languageId` query param |
| Template list/detail | Response includes `languageId`, `languageName` |
| Add/Update template | Request accepts optional `languageId` (defaults to English) |
| All send APIs | Request accepts optional `languageId` (defaults to English) |
| Message history | Response includes `languageId`, `languageName` |

Same template name allowed in different languages (separate `templateID` per language).

---

## API field names (exact casing)

- `languageId` — number (from GetLanguages)
- `templateID` — number (from GetTemplates filtered by languageId)
- `doctorID`, `patientID`, `templateCategory`, `messageCategory` — unchanged

---

## Doctor UI — required changes

### 1. Language dropdown (all 3 send screens: Hospital, Offers, Health Tips)

- On screen load: `GET /api/mastersAPI/GetLanguages`
- Show dropdown: **English**, **Marathi** (use `languageName` label, store `languageId` value)
- Default selection: **English** (or first language from API where `languageName` is English)
- When language changes:
  - Clear selected `templateID`
  - Re-fetch templates: `GET /api/WhatsApp/GetTemplates?templateCategory={cat}&languageId={selected}&isActive=true`
  - Clear preview panel

### 2. Template dropdown

- Populate only from templates returned for selected `languageId`
- On select: `GET /api/WhatsApp/GetTemplateById/{id}` for preview (body is already in that language)

### 3. Compose fields hint

- When Marathi selected, placeholder/helper text: doctor should type `message`, `offer`, or `healthTip` in **Marathi**
- Template provides Marathi structure; doctor fills content in same language

### 4. Send payload

Include `languageId` on every send call:

```json
{
  "doctorID": 1,
  "languageId": 2,
  "templateID": 4,
  "individual": true,
  "bulk": false,
  "patientID": 42,
  "offer": "२०% सूट..."
}
```

Apply to: `SendHospitalServiceMessage`, `SendOfferMessage`, `SendHealthTipMessage`, `SendIndividualMessage`, `SendBulkMessage`.

If frontend omits `languageId`, API defaults to English — but **always send explicit `languageId`** from dropdown for clarity.

### 5. Validation before send

- If `templateID` set, it must come from list filtered by current `languageId`
- On API error `Selected template does not match the chosen language` → reset template dropdown and show toast

### 6. Message history

- Add **Language** column using `languageName` from history API
- Optional filter by language later (not required for v1)

---

## Admin UI — required changes

### 1. Template list

- Add **Language** column (`languageName` from `GetTemplates`)
- Optional filter dropdown: All | English | Marathi → pass `languageId` to `GetTemplates`

### 2. Template create / edit form

- Add **Language** dropdown (`GET /api/mastersAPI/GetLanguages`)
- Required on create; on edit load `languageId` from `GetTemplateById`
- Pass `languageId` in `AddTemplate` / `UpdateTemplate` body
- Same `templateName` + `templateCategory` allowed when `languageId` differs (e.g. create Marathi copy of English default)

### 3. Admin workflow for Marathi defaults

Backend seeds Marathi defaults after SQL scripts. Admin can also manually add Marathi templates with `languageId` = Marathi.

---

## TypeScript interfaces (add / extend)

```typescript
interface LanguageMaster {
  languageId: number;
  languageName: string;
  description?: string;
  isDeleted?: boolean;
}

// Extend existing WhatsApp template types:
interface WhatsAppTemplateListItem {
  // ...existing fields
  languageId: number;
  languageName?: string;
}

// Extend all send request types:
interface WhatsAppSendRequestBase {
  // ...existing fields
  languageId?: number;
}
```

---

## API service methods to add/update

```typescript
getLanguages(): Promise<LanguageMaster[]>
  // GET /api/mastersAPI/GetLanguages

getTemplates(params: { templateCategory?: string; languageId?: number; isActive?: boolean; pageNumber?: number; pageSize?: number })
  // add languageId query param

// All send methods: include languageId in POST body
```

---

## UX rules

- Language dropdown **above** template dropdown on send screens
- Changing language resets template selection and preview
- Do not hardcode template IDs — always load per language
- Do not hardcode language IDs in production — use `GetLanguages`; English/Marathi IDs may vary by DB
- Bulk send: pass same `languageId` as individual
- Show API error messages verbatim for language/template mismatch

---

## Testing checklist

- [ ] English: load templates → send individual Hospital message
- [ ] Marathi: load templates → send individual Offer message with Marathi `offer` text
- [ ] Switch language mid-form → template list refreshes, old templateID cleared
- [ ] Bulk send with `languageId: 2` queues successfully
- [ ] Admin: create Marathi template with same name as English template
- [ ] History shows `languageName` column

---

## Do NOT

- Invent new endpoints — use `GetLanguages` and existing WhatsApp routes only
- Use one global template list without `languageId` filter on doctor send screens
- Send English `templateID` with `languageId: 2` (API will reject)
- Auto-translate doctor message content — doctor types in chosen language

END MULTI-LANGUAGE PROMPT
```

---

*Document version: 1.2 — HomeoCentrum WhatsApp API (multi-language)*  
*Source: `Niga-Web/Controllers/WhatsAppController.cs`, `Niga-Domain/DTOs/WhatsAppModels.cs`, `Niga-Web/Controllers/MastersAPIController.cs`*
