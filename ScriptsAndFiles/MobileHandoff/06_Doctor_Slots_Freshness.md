# Patient App — Doctor Slots freshness (client v1)

**Backend status:** DONE. Recycle New-API after deploy.

## API

`GET /api/Public/Doctors/{doctorId}/Slots?date=YYYY-MM-DD`

### Response headers (required for acceptance)

```
Cache-Control: no-store, no-cache, must-revalidate
Pragma: no-cache
Vary: Authorization
```

Do **not** assume CDN/proxy may cache this GET. Network traces should show `no-store`.

### Additive `data` field

| Key | Type | Meaning |
|-----|------|---------|
| `scheduleUpdatedAt` | `string \| null` (ISO) | Last write time of that day’s `DoctorDailySchedule`; `null` when `hasSchedule === false` |

Existing fields unchanged: `doctorId`, `appointmentDate`, `hasSchedule`, `intervalMinutes`, `slots[]`.

## Client v1 contract (no polling)

Keep event-driven sync only:

- PTR / day re-tap / leave–re-enter / AppState focus → refetch Slots for active `{ doctorId, date }`
- RTK cache key per `{ doctorId, date }` is correct
- Optional: if `scheduleUpdatedAt` equals last seen for that key, skip redundant UI churn (still OK to replace slots from network)

**Not required for v1:** websocket / FCM `schedule.updated`.

## Acceptance

1. Admin upserts **today** hours → patient PTR or re-tap today → slots appear (no app kill).
2. Admin upserts **tomorrow** only → tomorrow has slots; today empty/correct.
3. Slots GET shows `Cache-Control: no-store`.
