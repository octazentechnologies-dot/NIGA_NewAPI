# Patient App — Appointment status display + max 2 reschedules

API-first. Do **not** invent local counters. Use visit-detail fields and `actions.canReschedule`.

## Contract

| Field | Meaning |
|-------|---------|
| `status` | Stored clinic status (`WAITING`, `EARLY`, `READY`, `OVERDUE`, `EXPIRED`, …) |
| `displayStatus` | Patient-facing label key — prefer this for UI copy |
| `paymentStatus` | Separate from visit status (`UNPAID` / `PAID` / …) |
| `rescheduleCount` | Successful `POST …/RescheduleAppointment` count for this visit |
| `reschedulesRemaining` | `0`–`2` |
| `maxReschedules` | Always `2` |
| `actions.canReschedule` | Server truth (quota + grace + not cancelled/completed/expired) |

### `displayStatus` keys

| Key | Typical copy |
|-----|----------------|
| `Booked` | Booked (unpaid, upcoming) |
| `Confirmed` | Confirmed (paid, upcoming) |
| `CheckedIn` | Checked in / waiting at clinic |
| `ArrivedEarly` | Arrived early |
| `Ready` | Ready to see doctor |
| `InProgress` | In consultation |
| `Missed` | Missed (OVERDUE grace) |
| `Expired` | Expired — book new |
| `Completed` / `Cancelled` | Terminal |

## Reschedule UX

1. Show Reschedule CTA only when `actions.canReschedule === true`.
2. Optional helper: “{reschedulesRemaining} of {maxReschedules} reschedules left”.
3. On **409** with max-reschedule message: hide CTA, prompt “Book a new appointment”.
4. Cap is **per visit** (`PatientAppId`), not per patient lifetime.
5. Pick new slot via `GET /api/Public/Doctors/{id}/Slots`, then `POST /api/PatientAppointment/RescheduleAppointment`.

## Related

See also `03_Appointment_Overdue_Grace_Expire.md` for OVERDUE → EXPIRED grace window (independent of the count cap).
