/*
  S5 — patient rating of a delivered medicine order (MED-15.01). One review per order.
  Idempotent: safe to run more than once.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.MedicineOrder', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.MedicineOrder is missing. Run the S4 Week 4 schema script first.', 16, 1);
    RETURN;
END
GO

IF OBJECT_ID(N'dbo.MedicineOrderReview', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicineOrderReview
    (
        MedicineOrderReviewId int            IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicineOrderReview PRIMARY KEY,
        MedicineOrderId       int            NOT NULL,
        PatientId             int            NOT NULL,
        Rating                tinyint        NOT NULL,
        Comment               nvarchar(1000) NULL,
        CreatedAt             datetime       NOT NULL CONSTRAINT DF_MedicineOrderReview_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT UQ_MedicineOrderReview_Order UNIQUE (MedicineOrderId),
        CONSTRAINT CK_MedicineOrderReview_Rating CHECK (Rating BETWEEN 1 AND 5),
        CONSTRAINT FK_MedicineOrderReview_Order FOREIGN KEY (MedicineOrderId) REFERENCES dbo.MedicineOrder (MedicineOrderId)
    );
END
GO

PRINT N'08_S5_Medicine_Order_Review applied.';
GO
