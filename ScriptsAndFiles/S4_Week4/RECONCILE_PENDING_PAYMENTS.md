# Reconcile pending consult payments (Razorpay already captured)

Use when Razorpay shows **captured** but `PaymentOrder` is still `PENDING` and the doctor board shows **Pending**.

## Endpoint

`POST /api/Payments/ReconcilePending`  
**Auth:** Admin or Account Bearer token  
**Keys:** same live `Razorpay:KeyId` / `KeySecret` as checkout

### Dry run (recommended first)

```json
{
  "dryRun": true,
  "doctorId": 2022,
  "from": "2026-10-26",
  "to": "2026-10-29",
  "limit": 50
}
```

Or by explicit ids (Mayekar-style stuck orders):

```json
{
  "dryRun": true,
  "paymentOrderIds": [10004, 10005, 10006, 10007, 10008, 10009]
}
```

### Apply capture

Same body with `"dryRun": false`.

## What it does

1. Loads PENDING/AWAITING_GATEWAY CONSULT `PaymentOrder` rows with a `GatewayOrderId`.
2. Calls Razorpay `GET /v1/orders/{orderId}/payments`.
3. If a payment is `captured` / `authorized`, runs the same capture path as Verify:
   - `PaymentOrder` → `CAPTURED` + `GatewayPaymentId`
   - `PatientAppointment` → `PAID`
   - `ConsultPayment` + ledger (no patient push on reconcile)

## Outcomes in response

| outcome | meaning |
|---------|---------|
| `would_capture` | dry run — would mark PAID |
| `captured` | written |
| `already_captured` | skipped |
| `no_razorpay_payment` | order has no payments at gateway |
| `not_captured_at_gateway` | payment exists but not captured |
| `apply_failed` | often `AMOUNT_MISMATCH` — check `PaymentException` |

## After deploy

Redeploy New-API, call dry run, then apply. Doctor web / Doctor App will show **Paid** from DB with no client changes.
