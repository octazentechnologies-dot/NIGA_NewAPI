# Cursor prompt — Doctor App: verification resubmit (NeedsInfo / Rejected → Pending)

Copy everything below the line into the **Doctor-App** Cursor agent chat on the mobile developer machine.

---

## Context (API already shipped — do not re-implement register)

Admin Trust queue and New-API already support:

| Status | Meaning |
|--------|---------|
| `Pending` | Under review (first submit **or** after doctor re-uploaded) |
| `NeedsInfo` | Admin clicked **Request more information** (note required) |
| `Rejected` | Admin rejected credentials (note required) |
| `Verified` | Approved |

**Product rule:** Doctor never re-registers after the account exists. They only re-upload documents. After a successful re-upload while status is `Rejected` or `NeedsInfo`, the API sets status back to **`Pending`**.

### APIs to use (authenticated JWT)

1. **`GET api/Trust/MyStatus`** — enriched response:

```json
{
  "success": true,
  "data": {
    "doctorId": 123,
    "verificationStatus": "NeedsInfo",
    "isVerified": false,
    "canResubmitDocuments": true,
    "latestNote": "MD certificate is blurry — please re-upload",
    "latestDecisionStatus": "NeedsInfo",
    "latestDecisionAt": "...",
    "lastResubmittedAt": null
  }
}
```

- `canResubmitDocuments` is `true` only for `NeedsInfo` and `Rejected`.
- `latestNote` is the last **admin** decision note (not the system “Documents resubmitted” event).

2. **`POST api/Profile/Me/CredentialDocuments`** (multipart) — upload/re-upload a file.  
   Response now includes:

```json
{
  "success": true,
  "data": { "...document..." },
  "verificationStatus": "Pending",
  "statusResetToPending": true
}
```

When the doctor was `Rejected` / `NeedsInfo`, `statusResetToPending` is `true` and `verificationStatus` becomes **`Pending`**.

3. **`PUT api/Profile/Me/CredentialDocuments/{id}`** — same status reset when replacing the file.

4. **Do NOT** call `POST api/users/RegisterDoctorWithDocuments` again after signup. Same email/username → `isUserAlreadyRegistered` / “User already exists”.

Admin reviews again at web **Trust & Verification** (`/admin/trust-queue`) under the **Pending** tab after resubmit.

---

## What is broken today in Doctor-App (fix this)

Repo: `Doctor-App` (Expo SDK 57, MVC: controllers → views, RTK Query).

1. After **Reject**, login sends doctor to credentials `loginGate` with “re-upload” copy, but **Submit for Review** still calls `useRegisterDoctorWithDocumentsMutation` → fails once account exists.
2. Local file pick on credentials gate does not hit `POST api/Profile/Me/CredentialDocuments`.
3. **`NeedsInfo`** is not handled as its own status (often falls through to Pending / locked uploads).
4. Practice Status may show local “submitted” after upload, but must refetch `Trust/MyStatus` so UI matches server (`Pending`).

Follow existing rules:

- `newBaseApi` / inject endpoints in `src/store/api/new/<screen>Api.ts`
- Controllers own API hooks; views only render view models
- Copy via `t()` / localization (`en.ts`, `mr.ts`, `types.ts`)
- Read Expo v57 docs if touching Expo APIs

---

## Implement (mobile only)

### 1) Types / RTK — `practiceStatusApi.ts` (and any Trust status mapping)

Extend `TrustMyStatusData`:

- `canResubmitDocuments?: boolean`
- `latestNote?: string | null`
- `latestDecisionStatus?: string | null`
- `latestDecisionAt?: string | null`
- `lastResubmittedAt?: string | null`

Parse them in `transformResponse` for `getTrustMyStatus`.

Extend upload mutation response typing with `verificationStatus` and `statusResetToPending`.

### 2) Normalize verification status everywhere

Treat these as first-class (case-insensitive):

- `Pending` / `pending` / `submitted` → **pending** (under review, uploads locked)
- `NeedsInfo` / `needsinfo` → **needsInfo** (action required, uploads open)
- `Rejected` / `rejected` → **rejected** (action required, uploads open)
- `Verified` / `approved` → **verified**

Update:

- `useSignInController.ts` (gate routing)
- `useRegisterCredentialsController.ts` (`normalizeVerificationStatus`, banner, `primaryAction`, `uploadsLocked`)
- `useHomeController.ts` Practice Status preview mapping

Sign-in routing (keep existing Verified → home/deviceTrust logic):

- `Verified` + continued + device → home  
- `Pending` | `NeedsInfo` | `Rejected` | first Verified → **credentials loginGate**

### 3) Credentials gate — `useRegisterCredentialsController` + `RegisterCredentialsView`

When `entry === 'loginGate'` and status is **`NeedsInfo` or `Rejected`**:

| UI | Behavior |
|----|----------|
| Banner | NeedsInfo: “More information needed” + show `latestNote`. Rejected: existing rejected banner + `latestNote` |
| Uploads | **Unlocked** |
| Primary CTA | **“Resubmit documents”** (not “Submit for Review” / not Contact Support only) |
| On CTA | Upload each new local file via **`uploadCredentialDocument`** (`POST Profile/Me/CredentialDocuments`) with correct `documentType` (`Qualification` / `Registration` / `Experience` / `Other`). Then refetch `getTrustMyStatus`. Expect **`Pending`**. Set local verification to Pending, lock uploads, show submitted banner. |
| Never | Call `registerDoctor` / `RegisterDoctorWithDocuments` on this path |

When status is **`Pending`**: keep current behavior — uploads locked, Contact Support / wait.

When **`Verified`**: Continue as today.

Add localization keys (en + mr + types), e.g.:

- `regCredBannerNeedsInfoTitle` / `Body`
- `regCredResubmit` (“Resubmit documents”)
- `regCredResubmitSuccess` (“Documents submitted. Verification pending again.”)
- Optional: show `latestNote` with a `psAdminNote` style label

### 4) Practice Status — `PracticeStatusView` / `useHomeController`

- Map `NeedsInfo` to its own preview (or reuse rejected-style “action required” with different copy).
- Show `latestNote` from Trust/MyStatus.
- On successful `uploadCredentialDocument`, if response `statusResetToPending` or `verificationStatus === 'Pending'`, set preview to **submitted/pending** and invalidate/refetch TrustStatus.
- Do not leave the UI stuck on “rejected” after a successful re-upload.

### 5) Optional but recommended

- If `canResubmitDocuments === false` and status is Pending, do not offer re-upload on credentials gate.
- After resubmit success toast, stay on gate with submitted banner (do not invent Verified).
- Keep MVC: no fetch inside views.

---

## Acceptance tests (mobile)

1. Fresh register → Pending → credentials gate locked → OK.  
2. Admin **NeedsInfo** with note → doctor logs in → sees NeedsInfo banner + note → re-uploads → API returns Pending → UI shows Pending/submitted → uploads lock.  
3. Admin **Reject** with note → same re-upload path → Pending.  
4. Credentials gate **must not** call Register again (no “User already exists”).  
5. Admin Trust queue shows doctor under **Pending** after resubmit; Approve → app shows Verified / continue.  
6. Marathi strings present for new keys.

---

## Out of scope for mobile

- Admin Trust queue UI (already updated)
- Changing Decide API (`Approve` / `Reject` / `NeedsInfo`)
- Full re-registration wizard for existing users

---

## Key files (Doctor-App)

- `src/store/api/new/practiceStatusApi.ts`
- `src/controllers/useRegisterCredentialsController.ts`
- `src/views/RegisterCredentialsView.tsx`
- `src/controllers/useSignInController.ts`
- `src/controllers/useHomeController.ts`
- `src/views/PracticeStatusView.tsx`
- `src/localization/en.ts`, `mr.ts`, `types.ts`
- `src/navigation/AppNavigator.tsx` (only if gate entry needs adjustment)

Implement with minimal diff; match existing patterns; no Expo Router.
