# Patient App — Order Details API enrichment (PAT-MED-OD-API)

**Do not invent money or dates.** Bind to Tracking / History keys below. API is additive JSON.

## Primary source

`GET /api/MedicineOrders/{medicineOrderId}/Tracking`

### New / important `data` keys

| Key | Use |
|-----|-----|
| `deliveredAt` | Banner “Delivered on …”; `null` if not delivered — **never** use static i18n date |
| `subtotalMedicines` | Subtotal line; `null` if unknown |
| `deliveryFee` | Delivery charges; `null` = unknown → show “Included”/hide; `0` = known free |
| `totalPaid` | Prefer over `quoteAmount` when set |
| `currency` | Usually `"INR"` |
| `payMode` | `ONLINE` / `COD` |
| `paymentReference` | Optional gateway id |
| `paymentOrderId` | Invoice ByPayment |
| `erxSnapshotId`, `rxLabel`, `doctorName` | Fulfilled-against |
| `pharmacyName`, `pharmacyArea`, `pharmacyLicenceNumber`, `pharmacyLicensed`, `pharmacyContactMasked` | Pharmacy card |
| `invoiceId`, `invoiceNumber`, `invoicePdfAvailable` | Tax invoice / Download enable |
| `canRequestRefill` | Optional gate for refill CTA |
| `lines[].lineAmount` | Optional; often `null` |

Money rule: **never coerce missing fee/subtotal to `0`.**

## List / history (handoff consistency)

- `GET /api/Patient/MedicineOrders/History` — same enrichment keys per order  
- `GET /api/Patient/MedicineOrders` — lightweight: `pharmacyName`, `deliveredAt`, `deliveryFee`, `paymentOrderId`

## Invoice download

1. If `invoicePdfAvailable` and `paymentOrderId` → `GET /api/Invoices/ByPayment/{paymentOrderId}` then SecureDocument PDF  
2. If payment captured but no invoice → `POST /api/Invoices/ByPayment/{paymentOrderId}` then download  
3. If `paymentOrderId` null → hide Download

## Unchanged

- `POST /api/Erx/Refills` `{ erxSnapshotId }`  
- Prefer `canRequestRefill` + `erxSnapshotId` from Tracking when present

## FE work (Patient-App only)

Bind `useOrderDetailsController` / `OrderDetailsView` + RTK types in `medicinesApi.ts`. Remove static `orderDetailDeliveredOn` date. Hide invoice when `invoicePdfAvailable === false`.
