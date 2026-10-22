# Homeocentrum — FCM / Device Push APIs (Mobile handoff)

Use this file to wire Android / iOS (and Website) push with Cursor or any client.

**Auth:** all endpoints below require  
`Authorization: Bearer <JWT>`  
unless marked otherwise.

**Base URL examples**

| Environment | Base |
|-------------|------|
| IIS (same origin) | `http://<host>/new-api/api` |
| Direct New API | `http://<host>:5002/api` |

Replace `<host>` with the deployed server. Paths below are relative to that base (after `/api`).

---

## Device types

| `deviceType` value | Use when |
|--------------------|----------|
| `Android` | Native Android (FCM) |
| `iOS` | Native iOS (FCM) |
| `Website` | Web / PWA (FCM + VAPID) |

Also accepted (normalized server-side): `android`, `ios`, `web`, `browser`, `mobile`, `iphone`, `ipad`.

One user may register **many devices**. Same `token` or same (`userId` + `deviceType` + `deviceId`) → **update**; first time → **insert**.

---

## 1. Register FCM token

Call **after login** and whenever FCM refreshes the token.

### `POST /Device/Register`

**Headers**

```http
Authorization: Bearer <JWT>
Content-Type: application/json
```

### Request — Android

```json
{
  "token": "<FCM_REGISTRATION_TOKEN>",
  "userId": 10020,
  "deviceType": "Android",
  "deviceId": "android-stable-uuid-or-installation-id"
}
```

### Request — iOS

```json
{
  "token": "<FCM_REGISTRATION_TOKEN>",
  "userId": 10020,
  "deviceType": "iOS",
  "deviceId": "ios-stable-uuid-or-installation-id"
}
```

### Request — Website

```json
{
  "token": "<FCM_WEB_TOKEN>",
  "userId": 10020,
  "deviceType": "Website",
  "deviceId": "web-homeocentrum.example.com"
}
```

### Request fields

| Field | Type | Required | Notes |
|-------|------|----------|--------|
| `token` | string | **Yes** | FCM token (max 512) |
| `userId` | long | No | Must match JWT user if sent |
| `deviceType` | string | Recommended | `Android` \| `iOS` \| `Website` (default `Website`) |
| `deviceId` | string | Recommended | Stable per device so token refresh updates the same row |
| `platform` | string | No | Legacy; ignored when `deviceType` is set |

### Success response `200`

```json
{
  "success": true,
  "data": {
    "devicePushTokenId": 12,
    "userId": 10020,
    "deviceType": "Android",
    "deviceId": "android-stable-uuid-or-installation-id",
    "updatedAt": "2026-10-18T10:30:00Z"
  }
}
```

### Error responses

```json
{ "success": false, "message": "Token is required." }
```
`400`

```json
{ "success": false, "message": "Sign in required." }
```
`401`

```json
{ "success": false, "message": "userId must match the signed-in user." }
```
`403`

---

## 2. Register FCM token (alias)

Same body and behavior as `#1`. Prefer `/Device/Register`.

### `POST /Devices/Register`

Request/response identical to **Register FCM token** above.

---

## 3. Unregister one device token

Use when removing push for **this device only** (not full logout).

### `POST /Device/Unregister`

**Headers**

```http
Authorization: Bearer <JWT>
Content-Type: application/json
```

### Request — by token

```json
{
  "token": "<FCM_REGISTRATION_TOKEN>"
}
```

### Request — by id

```json
{
  "devicePushTokenId": 12
}
```

### Success response `200`

```json
{
  "success": true,
  "message": "Unregistered."
}
```

### Error responses

```json
{ "success": false, "message": "Token or DevicePushTokenId is required." }
```
`400`

```json
{ "success": false, "message": "Device token not found." }
```
`404`

---

## 4. List my registered devices

### `GET /Device/Mine`

**Headers**

```http
Authorization: Bearer <JWT>
```

### Request

No body.

### Success response `200`

```json
{
  "success": true,
  "data": [
    {
      "devicePushTokenId": 12,
      "userId": 10020,
      "deviceType": "Android",
      "platform": "FCM",
      "deviceId": "android-stable-uuid-or-installation-id",
      "updatedAt": "2026-10-18T10:30:00Z"
    },
    {
      "devicePushTokenId": 13,
      "userId": 10020,
      "deviceType": "iOS",
      "platform": "FCM",
      "deviceId": "ios-stable-uuid-or-installation-id",
      "updatedAt": "2026-10-18T09:00:00Z"
    },
    {
      "devicePushTokenId": 14,
      "userId": 10020,
      "deviceType": "Website",
      "platform": "FCM",
      "deviceId": "web-homeocentrum.example.com",
      "updatedAt": "2026-10-18T08:00:00Z"
    }
  ]
}
```

---

## 5. Logout (clears ALL FCM tokens for this user)

Call on logout. Server soft-deletes every active `DevicePushToken` for the JWT user.

### `POST /Account/Logout`

**Headers**

```http
Authorization: Bearer <JWT>
```

### Request

No body (empty body is fine).

### Success response `200`

```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

---

## 6. In-app notification list (web bell + mobile inbox)

Every `SendToUserAsync` FCM send also inserts a per-user row in `dbo.AppNotification` (same `UserId` as the JWT login). The web header bell (`GET /Notifications`) and mobile inbox both read this table.

The row is written even when FCM is skipped (no device token) so the bell still shows confirm / cancel / reschedule / reminder / tele / medicine / pay-link / missed follow-up / admin send.

Clinic staff also get a row keyed to **their own login id** (doctor `UserMaster.UserId`, reception `ReceptionStaffId`, pharmacy `PharmacyPartner.UserId`). Patient rows use the patient portal `UserId`. Coverage: book / cancel / reschedule, appointment request, CareLink, refill, medicine status, diary reminder.

### `GET /Notifications`

**Query (optional paging)**

| Query | Type | Default |
|-------|------|---------|
| `pageNumber` | int | 1 |
| `pageSize` | int | 20 |

### `GET /Notifications?pageNumber=1&pageSize=20`

**Headers**

```http
Authorization: Bearer <JWT>
```

### Success response `200` (shape)

```json
{
  "success": true,
  "data": [
    {
      "appNotificationId": 1,
      "title": "Appointment booked",
      "body": "Your appointment with Dr. …",
      "isRead": false,
      "pushStatus": "SENT",
      "createdAt": "2026-10-18T10:30:00",
      "type": "APPOINTMENT",
      "deepLink": "/appointments"
    }
  ]
}
```

---

## 7. Unread notification count (optional)

### `GET /Notifications/UnreadCount`

**Headers**

```http
Authorization: Bearer <JWT>
```

### Success response `200`

```json
{
  "success": true,
  "data": {
    "unread": 3
  }
}
```

---

## 8. Mark notification read (optional)

### `PATCH /Notifications/{id}?isRead=true`

**Headers**

```http
Authorization: Bearer <JWT>
```

### Example

`PATCH /Notifications/1?isRead=true`

### Success response `200` (shape)

```json
{
  "success": true
}
```

---

## 9. Admin/Doctor send test push (optional — not for patient app)

### `POST /Notifications/Send`

Requires Doctor or Admin JWT.

```json
{
  "userId": 10020,
  "title": "Test notification",
  "body": "Hello from Homeocentrum"
}
```

### Success response `200` (shape)

```json
{
  "success": true,
  "pushStatus": "SENT"
}
```

`pushStatus` may be `SENT` | `LOGGED` | `SKIPPED` | `FAILED`.

---

## Mobile client flow (bind this)

```text
1. Login → store JWT + userId
2. Get FCM token from Firebase SDK (Android / iOS)
3. POST /Device/Register
   { token, userId, deviceType: "Android"|"iOS", deviceId }
4. On FCM token refresh → call Register again (same deviceId)
5. On logout → POST /Account/Logout
   (optional) POST /Device/Unregister for this device only
```

### cURL — Android register

```bash
curl -X POST "http://<host>/new-api/api/Device/Register" \
  -H "Authorization: Bearer <JWT>" \
  -H "Content-Type: application/json" \
  -d "{\"token\":\"<FCM_TOKEN>\",\"userId\":10020,\"deviceType\":\"Android\",\"deviceId\":\"device-uuid-1\"}"
```

### cURL — iOS register

```bash
curl -X POST "http://<host>/new-api/api/Device/Register" \
  -H "Authorization: Bearer <JWT>" \
  -H "Content-Type: application/json" \
  -d "{\"token\":\"<FCM_TOKEN>\",\"userId\":10020,\"deviceType\":\"iOS\",\"deviceId\":\"device-uuid-2\"}"
```

### cURL — logout

```bash
curl -X POST "http://<host>/new-api/api/Account/Logout" \
  -H "Authorization: Bearer <JWT>"
```

---

## Incoming FCM payload (server → device)

Server sends to **all** active tokens for that `userId` after successful actions (appointment book, cancel, remind, tele, waitlist, etc.).

### Notification (system tray)

| Field | Example |
|-------|---------|
| `title` | `Appointment booked` |
| `body` | Rendered SMS-style message text |

### Data map (for deep link / routing)

| Key | Example values |
|-----|----------------|
| `action` | `appointment_confirm` \| `appointment_reschedule` \| `appointment_cancel` \| `appointment_remind` \| `tele_ready` \| `waitlist_offer` \| `general` |
| `date` | `18 Oct 2026` (when relevant) |
| `time` | `10:30` (when relevant) |
| `room` | tele room id (when `tele_ready`) |

### Example data payload

```json
{
  "action": "appointment_confirm",
  "date": "18 Oct 2026",
  "time": "10:30"
}
```

Handle `data.action` in the app to open the correct screen.

---

## Quick API index

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/Device/Register` | Save / update FCM token |
| `POST` | `/Devices/Register` | Same (alias) |
| `POST` | `/Device/Unregister` | Clear one token |
| `GET` | `/Device/Mine` | List my tokens |
| `POST` | `/Account/Logout` | Logout + clear all tokens |
| `GET` | `/Notifications` | In-app notification list |
| `GET` | `/Notifications/UnreadCount` | Unread count |
| `PATCH` | `/Notifications/{id}?isRead=true` | Mark read |
| `POST` | `/Notifications/Send` | Doctor/Admin test push |

---

## Firebase project (client SDK)

| Setting | Value |
|---------|--------|
| Project ID | `homeocentrum-mobile` |
| Messaging Sender ID | `991452928656` |

Use the Firebase config / `google-services.json` / `GoogleService-Info.plist` for the correct app (`HelloHomeo Doctor` / `HelloHomeo Patient`). Do **not** embed the server service-account JSON in the mobile app.
