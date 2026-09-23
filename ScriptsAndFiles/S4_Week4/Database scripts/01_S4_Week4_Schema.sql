/*
================================================================================
Author       : Tufan Powar
Created      : 22-09-2026
Script       : 01_S4_Week4_Schema.sql
Purpose      : S4 Week 4 tables for fees, payments, ledger, trust, eRx, and medicine orders.
               Does not store Razorpay keys. Does not rewrite existing VisitType rows.
Use          : HomeoCentrum_Dev. Idempotent. Run before calling the new HTTP routes.
================================================================================
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.ConsultFeeConfig', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsultFeeConfig
    (
        ConsultFeeConfigId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConsultFeeConfig PRIMARY KEY,
        DoctorId INT NOT NULL,
        InClinicFee DECIMAL(18,2) NOT NULL,
        TeleFee DECIMAL(18,2) NOT NULL,
        InstantSurcharge DECIMAL(18,2) NOT NULL CONSTRAINT DF_ConsultFeeConfig_Instant DEFAULT (0),
        Currency CHAR(3) NOT NULL CONSTRAINT DF_ConsultFeeConfig_Currency DEFAULT ('INR'),
        PayAtClinicEnabled BIT NOT NULL CONSTRAINT DF_ConsultFeeConfig_PayAtClinic DEFAULT (1),
        EffectiveFrom DATE NOT NULL,
        CreatedBy BIGINT NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ConsultFeeConfig_CreatedAt DEFAULT (GETDATE())
    );
    CREATE INDEX IX_ConsultFeeConfig_Doctor ON dbo.ConsultFeeConfig (DoctorId, EffectiveFrom DESC, ConsultFeeConfigId DESC);
END
GO

IF OBJECT_ID(N'dbo.PaymentOrder', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentOrder
    (
        PaymentOrderId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentOrder PRIMARY KEY,
        Stream NVARCHAR(20) NOT NULL,
        PatientAppId INT NULL,
        MedicineOrderId INT NULL,
        DoctorId INT NULL,
        PatientId INT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Currency CHAR(3) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        Method NVARCHAR(30) NULL,
        GatewayOrderId NVARCHAR(80) NULL,
        GatewayPaymentId NVARCHAR(80) NULL,
        IdempotencyKey NVARCHAR(80) NULL,
        CorrelationId NVARCHAR(40) NOT NULL,
        CreatedBy BIGINT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PaymentOrder_CreatedAt DEFAULT (GETDATE())
    );
    CREATE UNIQUE INDEX UX_PaymentOrder_Idempotency ON dbo.PaymentOrder (IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;
    CREATE INDEX IX_PaymentOrder_Appointment ON dbo.PaymentOrder (PatientAppId, CreatedAt DESC);
    CREATE INDEX IX_PaymentOrder_GatewayOrder ON dbo.PaymentOrder (GatewayOrderId);
END
GO

IF OBJECT_ID(N'dbo.PaymentEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentEvent
    (
        PaymentEventId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentEvent PRIMARY KEY,
        PaymentOrderId BIGINT NULL,
        GatewayEventId NVARCHAR(80) NOT NULL,
        EventType NVARCHAR(60) NOT NULL,
        RawJson NVARCHAR(MAX) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_PaymentEvent_At DEFAULT (GETDATE()),
        CONSTRAINT UX_PaymentEvent_GatewayEventId UNIQUE (GatewayEventId)
    );
END
GO

IF OBJECT_ID(N'dbo.ConsultPayment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsultPayment
    (
        ConsultPaymentId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConsultPayment PRIMARY KEY,
        PaymentOrderId BIGINT NOT NULL,
        PatientAppId INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Method NVARCHAR(30) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        GstAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_ConsultPayment_Gst DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ConsultPayment_CreatedAt DEFAULT (GETDATE())
    );
    CREATE INDEX IX_ConsultPayment_Appointment ON dbo.ConsultPayment (PatientAppId, CreatedAt DESC);
END
GO

IF OBJECT_ID(N'dbo.Refund', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Refund
    (
        RefundId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Refund PRIMARY KEY,
        PaymentOrderId BIGINT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Reason NVARCHAR(500) NOT NULL,
        Policy NVARCHAR(20) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        GatewayRefundId NVARCHAR(80) NULL,
        ByUserId BIGINT NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_Refund_At DEFAULT (GETDATE())
    );
    CREATE INDEX IX_Refund_Order ON dbo.Refund (PaymentOrderId, At DESC);
END
GO

IF OBJECT_ID(N'dbo.Invoice', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoice
    (
        InvoiceId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Invoice PRIMARY KEY,
        Number NVARCHAR(40) NOT NULL,
        Series NVARCHAR(20) NOT NULL,
        Stream NVARCHAR(20) NOT NULL,
        PaymentOrderId BIGINT NOT NULL,
        GstBreakup NVARCHAR(500) NULL,
        PdfPath NVARCHAR(400) NULL,
        SecureDocumentId BIGINT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Invoice_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT UX_Invoice_Number UNIQUE (Number),
        CONSTRAINT UX_Invoice_PaymentOrder UNIQUE (PaymentOrderId)
    );
END
GO

IF OBJECT_ID(N'dbo.LedgerEntry', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LedgerEntry
    (
        LedgerEntryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LedgerEntry PRIMARY KEY,
        Stream NVARCHAR(20) NOT NULL,
        Direction NVARCHAR(10) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Gst DECIMAL(18,2) NOT NULL CONSTRAINT DF_LedgerEntry_Gst DEFAULT (0),
        Commission DECIMAL(18,2) NOT NULL CONSTRAINT DF_LedgerEntry_Commission DEFAULT (0),
        SplitSeller DECIMAL(18,2) NULL,
        SplitPlatform DECIMAL(18,2) NULL,
        SplitDelivery DECIMAL(18,2) NULL,
        EntityType NVARCHAR(40) NOT NULL,
        EntityId NVARCHAR(40) NOT NULL,
        PaymentOrderId BIGINT NULL,
        CorrelationId NVARCHAR(40) NULL,
        SettlementRunId BIGINT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_LedgerEntry_At DEFAULT (GETDATE())
    );
    CREATE INDEX IX_LedgerEntry_StreamAt ON dbo.LedgerEntry (Stream, At DESC);
    CREATE INDEX IX_LedgerEntry_Entity ON dbo.LedgerEntry (EntityType, EntityId, At DESC);
END
GO

IF OBJECT_ID(N'dbo.SettlementRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SettlementRun
    (
        SettlementRunId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SettlementRun PRIMARY KEY,
        Status NVARCHAR(20) NOT NULL,
        CreatedBy BIGINT NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_SettlementRun_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.SettlementLine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SettlementLine
    (
        SettlementLineId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SettlementLine PRIMARY KEY,
        SettlementRunId BIGINT NOT NULL,
        PayeeType NVARCHAR(20) NOT NULL,
        PayeeId INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        LedgerEntryId BIGINT NULL
    );
    CREATE INDEX IX_SettlementLine_Run ON dbo.SettlementLine (SettlementRunId);
END
GO

IF OBJECT_ID(N'dbo.Payout', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payout
    (
        PayoutId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payout PRIMARY KEY,
        PayeeType NVARCHAR(20) NOT NULL,
        PayeeId INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        SettlementRunId BIGINT NULL,
        RejectReason NVARCHAR(500) NULL,
        DecidedBy BIGINT NULL,
        DecidedAt DATETIME NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Payout_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.PaymentException', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentException
    (
        PaymentExceptionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentException PRIMARY KEY,
        PaymentOrderId BIGINT NULL,
        Kind NVARCHAR(40) NOT NULL,
        Detail NVARCHAR(1000) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_PaymentException_Status DEFAULT (N'OPEN'),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PaymentException_CreatedAt DEFAULT (GETDATE()),
        ResolvedBy BIGINT NULL,
        ResolvedAt DATETIME NULL,
        ResolutionNote NVARCHAR(500) NULL
    );
    CREATE INDEX IX_PaymentException_Status ON dbo.PaymentException (Status, CreatedAt DESC);
END
GO

IF OBJECT_ID(N'dbo.TaxConfig', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaxConfig
    (
        TaxConfigId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaxConfig PRIMARY KEY,
        GstRate DECIMAL(6,2) NOT NULL,
        TreatmentExempt BIT NOT NULL
    );
    INSERT INTO dbo.TaxConfig (GstRate, TreatmentExempt) VALUES (0, 1);
END
GO

IF OBJECT_ID(N'dbo.Payee', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payee
    (
        PayeeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payee PRIMARY KEY,
        PayeeType NVARCHAR(20) NOT NULL,
        DoctorId INT NULL,
        PharmacyId INT NULL,
        AccountName NVARCHAR(200) NULL,
        BankAccount NVARCHAR(40) NULL,
        Ifsc NVARCHAR(20) NULL,
        Pan NVARCHAR(20) NULL,
        KycStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_Payee_Kyc DEFAULT (N'PENDING'),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_Payee_UpdatedAt DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.DoctorVerificationEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorVerificationEvent
    (
        DoctorVerificationEventId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DoctorVerificationEvent PRIMARY KEY,
        DoctorId INT NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        Note NVARCHAR(500) NULL,
        ByUserId BIGINT NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_DoctorVerificationEvent_At DEFAULT (GETDATE())
    );
    CREATE INDEX IX_DoctorVerificationEvent_Doctor ON dbo.DoctorVerificationEvent (DoctorId, At DESC);
END
GO

IF OBJECT_ID(N'dbo.Review', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Review
    (
        ReviewId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Review PRIMARY KEY,
        PatientAppId INT NOT NULL,
        DoctorId INT NOT NULL,
        PatientId INT NOT NULL,
        Rating INT NOT NULL,
        Text NVARCHAR(1000) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Review_Status DEFAULT (N'APPROVED'),
        At DATETIME NOT NULL CONSTRAINT DF_Review_At DEFAULT (GETDATE()),
        CONSTRAINT UX_Review_Appointment UNIQUE (PatientAppId)
    );
    CREATE INDEX IX_Review_Doctor ON dbo.Review (DoctorId, Status, At DESC);
END
GO

IF OBJECT_ID(N'dbo.ReviewAppeal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReviewAppeal
    (
        ReviewAppealId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReviewAppeal PRIMARY KEY,
        ReviewId INT NOT NULL,
        DoctorId INT NOT NULL,
        Reason NVARCHAR(1000) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_ReviewAppeal_Status DEFAULT (N'OPEN'),
        Resolution NVARCHAR(500) NULL,
        At DATETIME NOT NULL CONSTRAINT DF_ReviewAppeal_At DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.RankingWeight', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RankingWeight
    (
        RankingWeightId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RankingWeight PRIMARY KEY,
        VerifiedWeight DECIMAL(6,2) NOT NULL,
        RatingWeight DECIMAL(6,2) NOT NULL,
        FeeWeight DECIMAL(6,2) NOT NULL
    );
    INSERT INTO dbo.RankingWeight (VerifiedWeight, RatingWeight, FeeWeight) VALUES (40, 40, 20);
END
GO

IF OBJECT_ID(N'dbo.PotencyMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PotencyMaster
    (
        PotencyId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PotencyMaster PRIMARY KEY,
        Code NVARCHAR(20) NOT NULL,
        SortOrder INT NOT NULL,
        CONSTRAINT UX_PotencyMaster_Code UNIQUE (Code)
    );
    INSERT INTO dbo.PotencyMaster (Code, SortOrder) VALUES
        (N'6C', 1), (N'30C', 2), (N'200C', 3), (N'1M', 4), (N'10M', 5), (N'CM', 6), (N'Q', 7);
END
GO

IF COL_LENGTH(N'dbo.PrescriptionRemedyDetail', N'PotencyId') IS NULL
    ALTER TABLE dbo.PrescriptionRemedyDetail ADD PotencyId INT NULL;
IF COL_LENGTH(N'dbo.PrescriptionRemedyDetail', N'Frequency') IS NULL
    ALTER TABLE dbo.PrescriptionRemedyDetail ADD Frequency NVARCHAR(80) NULL;
IF COL_LENGTH(N'dbo.PrescriptionRemedyDetail', N'Duration') IS NULL
    ALTER TABLE dbo.PrescriptionRemedyDetail ADD Duration NVARCHAR(80) NULL;
IF COL_LENGTH(N'dbo.PrescriptionRemedyDetail', N'Instructions') IS NULL
    ALTER TABLE dbo.PrescriptionRemedyDetail ADD Instructions NVARCHAR(500) NULL;
GO

IF OBJECT_ID(N'dbo.ErxSnapshot', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ErxSnapshot
    (
        ErxSnapshotId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ErxSnapshot PRIMARY KEY,
        PatientAppId INT NOT NULL,
        PatientId INT NOT NULL,
        DoctorId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        SignedAt DATETIME NULL,
        SignedBy BIGINT NULL,
        LabOrdersJson NVARCHAR(MAX) NULL,
        CONSTRAINT UX_ErxSnapshot_Appointment UNIQUE (PatientAppId)
    );
END
GO

IF OBJECT_ID(N'dbo.ErxSnapshotItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ErxSnapshotItem
    (
        ErxSnapshotItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ErxSnapshotItem PRIMARY KEY,
        ErxSnapshotId INT NOT NULL,
        RemedyId INT NOT NULL,
        RemedyCode NVARCHAR(20) NOT NULL,
        RemedyName NVARCHAR(200) NOT NULL,
        PotencyCode NVARCHAR(20) NULL,
        Dose NVARCHAR(80) NULL,
        Frequency NVARCHAR(80) NULL,
        Duration NVARCHAR(80) NULL,
        Instructions NVARCHAR(500) NULL
    );
    CREATE INDEX IX_ErxSnapshotItem_Snapshot ON dbo.ErxSnapshotItem (ErxSnapshotId);
END
GO

IF OBJECT_ID(N'dbo.RefillRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefillRequest
    (
        RefillRequestId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefillRequest PRIMARY KEY,
        ErxSnapshotId INT NOT NULL,
        PatientId INT NOT NULL,
        DoctorId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_RefillRequest_Status DEFAULT (N'PENDING'),
        Reason NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_RefillRequest_CreatedAt DEFAULT (GETDATE()),
        DecidedAt DATETIME NULL
    );
END
GO

IF OBJECT_ID(N'dbo.PharmacyPartner', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PharmacyPartner
    (
        PharmacyPartnerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PharmacyPartner PRIMARY KEY,
        UserId BIGINT NULL,
        Name NVARCHAR(200) NOT NULL,
        Mobile NVARCHAR(20) NOT NULL,
        Area NVARCHAR(120) NULL,
        Status NVARCHAR(20) NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PharmacyPartner_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.PharmacyLicence', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PharmacyLicence
    (
        PharmacyLicenceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PharmacyLicence PRIMARY KEY,
        PharmacyPartnerId INT NOT NULL,
        LicenceNumber NVARCHAR(80) NOT NULL,
        ExpiryDate DATE NOT NULL,
        Status NVARCHAR(20) NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.SellerRoutingRule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SellerRoutingRule
    (
        SellerRoutingRuleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SellerRoutingRule PRIMARY KEY,
        PharmacyPartnerId INT NOT NULL,
        Area NVARCHAR(120) NULL,
        OpenTime TIME(0) NULL,
        CloseTime TIME(0) NULL,
        Capacity INT NOT NULL CONSTRAINT DF_SellerRoutingRule_Capacity DEFAULT (20)
    );
END
GO

IF OBJECT_ID(N'dbo.MedicineOrder', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicineOrder
    (
        MedicineOrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicineOrder PRIMARY KEY,
        ErxSnapshotId INT NOT NULL,
        PatientId INT NOT NULL,
        PharmacyPartnerId INT NULL,
        Status NVARCHAR(30) NOT NULL,
        ConsentGranted BIT NOT NULL CONSTRAINT DF_MedicineOrder_Consent DEFAULT (0),
        QuoteAmount DECIMAL(18,2) NULL,
        PayMode NVARCHAR(20) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_MedicineOrder_CreatedAt DEFAULT (GETDATE())
    );
    CREATE INDEX IX_MedicineOrder_Patient ON dbo.MedicineOrder (PatientId, CreatedAt DESC);
END
GO

IF OBJECT_ID(N'dbo.MedicineOrderItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicineOrderItem
    (
        MedicineOrderItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicineOrderItem PRIMARY KEY,
        MedicineOrderId INT NOT NULL,
        RemedyCode NVARCHAR(20) NOT NULL,
        RemedyName NVARCHAR(200) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.MedicineQuote', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicineQuote
    (
        MedicineQuoteId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicineQuote PRIMARY KEY,
        MedicineOrderId INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Note NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_MedicineQuote_At DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.MedicineOrderEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicineOrderEvent
    (
        MedicineOrderEventId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicineOrderEvent PRIMARY KEY,
        MedicineOrderId INT NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        Detail NVARCHAR(400) NULL,
        At DATETIME NOT NULL CONSTRAINT DF_MedicineOrderEvent_At DEFAULT (GETDATE())
    );
    CREATE INDEX IX_MedicineOrderEvent_Order ON dbo.MedicineOrderEvent (MedicineOrderId, At);
END
GO

IF OBJECT_ID(N'dbo.FollowUpPlan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FollowUpPlan
    (
        FollowUpPlanId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FollowUpPlan PRIMARY KEY,
        PatientAppId INT NOT NULL,
        PatientId INT NOT NULL,
        DoctorId INT NOT NULL,
        Note NVARCHAR(1000) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_FollowUpPlan_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.FollowUpTask', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FollowUpTask
    (
        FollowUpTaskId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FollowUpTask PRIMARY KEY,
        FollowUpPlanId INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        DueDate DATE NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_FollowUpTask_Status DEFAULT (N'OPEN'),
        CompletedAt DATETIME NULL
    );
END
GO

IF OBJECT_ID(N'dbo.SymptomDiary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SymptomDiary
    (
        SymptomDiaryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SymptomDiary PRIMARY KEY,
        PatientId INT NOT NULL,
        EntryDate DATE NOT NULL,
        Severity INT NOT NULL,
        Note NVARCHAR(1000) NULL,
        DeleteStatus BIT NOT NULL CONSTRAINT DF_SymptomDiary_Delete DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_SymptomDiary_CreatedAt DEFAULT (GETDATE())
    );
    CREATE INDEX IX_SymptomDiary_Patient ON dbo.SymptomDiary (PatientId, EntryDate DESC);
END
GO

IF OBJECT_ID(N'dbo.DataRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DataRequest
    (
        DataRequestId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DataRequest PRIMARY KEY,
        PatientId INT NOT NULL,
        RequestType NVARCHAR(20) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_DataRequest_Status DEFAULT (N'OPEN'),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_DataRequest_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.PatientHealthBasics', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientHealthBasics
    (
        PatientId INT NOT NULL CONSTRAINT PK_PatientHealthBasics PRIMARY KEY,
        BloodGroup NVARCHAR(8) NULL,
        Allergies NVARCHAR(500) NULL,
        ChronicConditions NVARCHAR(500) NULL,
        EmergencyContactName NVARCHAR(120) NULL,
        EmergencyContactMobile NVARCHAR(20) NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_PatientHealthBasics_UpdatedAt DEFAULT (GETDATE())
    );
END
GO

IF COL_LENGTH(N'dbo.PatientAppointment', N'CorrelationId') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD CorrelationId NVARCHAR(40) NULL;
GO
