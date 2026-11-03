# Patient / Doctor App — Appointment OVERDUE → 24h grace → EXPIRED

## Lifecycle

| Status | Meaning | Reschedule | Refund |
|--------|---------|------------|--------|
| Normal (`WALK-IN` / `E-CONSULT` / …) | Upcoming or active | Yes if slot in future | Cancel policy |
| **`OVERDUE`** | Slot time passed; **24h grace** | **Yes** until `graceEndsAt` | No cash refund in grace (reschedule keeps paid value) |
| **`EXPIRED`** | Grace ended | **No** | **No** |

## Visit detail fields (API)

`GET` visit / patient visits payloads now include:

- `lifecycleStatus`: `upcoming` | `overdue_grace` | `expired` | `open`
- `overdueAt`, `graceEndsAt`, `expiredAt`, `graceHoursRemaining`
- `actions.canReschedule`, `actions.canCancel`, `actions.canRefund`

## Mobile UX

1. If `lifecycleStatus === 'overdue_grace'`: show banner “Reschedule within {graceHoursRemaining}h” + CTA Reschedule. Paid copy: payment kept only if rescheduled before expiry.
2. If `EXPIRED`: disable reschedule/cancel/refund; prompt “Book new appointment”.
3. Listen for push/inbox types: `APPT_T15`, `APPT_T0`, `APPT_OVERDUE`, `APPT_GRACE_SOON`, `APPT_EXPIRED`.

## Feature flag

`FeatureFlags:EnableAppointmentGraceLifecycle` (default true). Requires API recycle after change.
