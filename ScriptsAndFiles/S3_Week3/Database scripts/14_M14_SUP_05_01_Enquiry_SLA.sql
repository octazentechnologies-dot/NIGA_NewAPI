/*
SUP-05.01 — Enquiry inbox SLA.
No new table: first-response due is EnquiryDate + 24 hours, returned as SlaDueAt on GET /api/Enquiry (API).
Run is idempotent (no DDL).
*/
SET NOCOUNT ON;
PRINT 'SUP-05.01 enquiry SLA is computed in API EnquiryController (EnquiryDate + 24h).';
GO
