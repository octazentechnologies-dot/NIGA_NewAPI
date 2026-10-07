/*
  11 - Versioned consent notices and guardian consent for minors (DPDP Act 2023, s.5, s.6, s.9).

  ConsentNotice   One row per notice text (consent type + version + language). The text and its SHA-256 can never
                  change once published (trigger); a new wording is a new version. IsCurrent marks the version shown
                  to users. RequiresReconsent = 1 means consents given before this version must be given again.

  ConsentRecord   New columns record which notice was agreed to (id, version, language, SHA-256 of the text) and,
                  for a patient under 18, who gave consent on the child's behalf and how that adult was verified.

  Safe to run more than once. Notice text is ASCII so its SHA-256 matches the API (UTF-8) hash.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.ConsentNotice', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsentNotice
    (
        ConsentNoticeId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConsentNotice PRIMARY KEY,
        ConsentTypeId     INT            NOT NULL CONSTRAINT FK_ConsentNotice_ConsentType REFERENCES dbo.ConsentType (ConsentTypeId),
        Version           NVARCHAR(20)   NOT NULL,
        Language          NVARCHAR(10)   NOT NULL CONSTRAINT DF_ConsentNotice_Language DEFAULT (N'en'),
        Title             NVARCHAR(200)  NOT NULL,
        Body              NVARCHAR(MAX)  NOT NULL,
        BodySha256        CHAR(64)       NOT NULL,
        EffectiveFrom     DATETIME2(0)   NOT NULL,
        RequiresReconsent BIT            NOT NULL CONSTRAINT DF_ConsentNotice_Reconsent DEFAULT (0),
        IsCurrent         BIT            NOT NULL CONSTRAINT DF_ConsentNotice_IsCurrent DEFAULT (0),
        CreatedByUserId   BIGINT         NULL,
        CreatedAt         DATETIME2(0)   NOT NULL CONSTRAINT DF_ConsentNotice_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UX_ConsentNotice_Type_Version_Language UNIQUE (ConsentTypeId, Version, Language)
    );
    CREATE UNIQUE INDEX UX_ConsentNotice_Current ON dbo.ConsentNotice (ConsentTypeId, Language) WHERE IsCurrent = 1;
END
GO

CREATE OR ALTER TRIGGER dbo.TR_ConsentNotice_NoDelete
ON dbo.ConsentNotice
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51010, 'ConsentNotice rows are permanent. Publish a new version instead.', 1;
END
GO

CREATE OR ALTER TRIGGER dbo.TR_ConsentNotice_TextImmutable
ON dbo.ConsentNotice
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(Body) OR UPDATE(BodySha256) OR UPDATE(Version) OR UPDATE(Language) OR UPDATE(Title)
       OR UPDATE(ConsentTypeId) OR UPDATE(EffectiveFrom) OR UPDATE(RequiresReconsent)
        THROW 51011, 'A published consent notice cannot be changed. Publish a new version instead.', 1;
END
GO

IF COL_LENGTH(N'dbo.ConsentRecord', N'ConsentNoticeId') IS NULL
    ALTER TABLE dbo.ConsentRecord ADD
        ConsentNoticeId            INT           NULL CONSTRAINT FK_ConsentRecord_ConsentNotice REFERENCES dbo.ConsentNotice (ConsentNoticeId),
        NoticeVersion              NVARCHAR(20)  NULL,
        NoticeLanguage             NVARCHAR(10)  NULL,
        NoticeSha256               CHAR(64)      NULL,
        GrantedForMinor            BIT           NOT NULL CONSTRAINT DF_ConsentRecord_GrantedForMinor DEFAULT (0),
        GuardianUserId             BIGINT        NULL,
        GuardianName               NVARCHAR(150) NULL,
        GuardianRelationship       NVARCHAR(50)  NULL,
        GuardianVerificationMethod NVARCHAR(30)  NULL,
        GuardianVerificationRef    NVARCHAR(100) NULL,
        GuardianMobileMasked       NVARCHAR(30)  NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ConsentRecord_GuardianRef' AND object_id = OBJECT_ID(N'dbo.ConsentRecord'))
    CREATE INDEX IX_ConsentRecord_GuardianRef ON dbo.ConsentRecord (GuardianVerificationRef) WHERE GuardianVerificationRef IS NOT NULL;
GO

/* ---- Version 1.0 notices (English). Review the wording with the clinic and counsel before go-live. ---- */
DECLARE @nl CHAR(1) = CHAR(10);
DECLARE @notices TABLE (Code NVARCHAR(50), Title NVARCHAR(200), Body NVARCHAR(MAX));

INSERT INTO @notices (Code, Title, Body) VALUES
(N'Privacy', N'Privacy notice', CONCAT(
    N'Homeocentrum processes your personal data to provide homeopathic consultation and treatment.', @nl, @nl,
    N'What we collect: name, mobile number, email, address, date of birth, gender and photo; health details such as complaints, case history, prescriptions, lab results and uploaded reports; payment details for your bills.', @nl, @nl,
    N'Why: to register you, book and run consultations, keep your case record, prescribe and dispense medicines, send appointment and payment messages, bill you, and keep the service secure.', @nl, @nl,
    N'Who receives it: your doctor and the clinic staff, and service providers that work for us (payments, SMS, WhatsApp and email delivery, video consultation, hosting). They may use it only for these purposes.', @nl, @nl,
    N'How long: for as long as your care needs it and the law requires medical records to be kept. After that it is erased or anonymised.', @nl, @nl,
    N'Your rights: you can see a summary of your data, correct it, ask for it to be erased, withdraw this consent at any time as easily as you gave it, nominate someone to act for you, and raise a grievance. Withdrawal does not undo processing already done, and some records must be kept by law.', @nl, @nl,
    N'Children: for anyone under 18, a parent or legal guardian must give this consent.', @nl, @nl,
    N'Contact: our grievance officer, through Help and Privacy in the app or at the clinic. If you are not satisfied, you may complain to the Data Protection Board of India.')),
(N'Booking', N'Appointment booking', CONCAT(
    N'We use your name, mobile number and the details you enter to book and manage your appointments and to send reminders.', @nl,
    N'You can withdraw this consent at any time; existing appointments may then need to be handled at the clinic.')),
(N'TeleRecording', N'Recording of online consultations', CONCAT(
    N'Your video or audio consultation may be recorded and stored in your case record to support your treatment.', @nl,
    N'This is optional. If you refuse or withdraw, the consultation continues without recording.')),
(N'PharmacyShare', N'Sharing with a pharmacy', CONCAT(
    N'Your prescription, name, mobile number and delivery address are shared with the partner pharmacy you choose so it can prepare and deliver your medicines.', @nl,
    N'You can withdraw this consent at any time; you can still collect medicines from the clinic.')),
(N'Marketing', N'Health tips and offers', CONCAT(
    N'We may send you health tips and offers by SMS, WhatsApp or email.', @nl,
    N'This is optional and you can withdraw at any time. It does not affect your treatment.')),
(N'Caregiver', N'Caregiver access', CONCAT(
    N'The person you authorise can view your appointments and act for you within the scope you choose.', @nl,
    N'You can revoke this at any time.'));

INSERT INTO dbo.ConsentNotice (ConsentTypeId, Version, Language, Title, Body, BodySha256, EffectiveFrom, RequiresReconsent, IsCurrent)
SELECT t.ConsentTypeId, N'1.0', N'en', n.Title, n.Body,
       LOWER(CONVERT(CHAR(64), HASHBYTES('SHA2_256', CAST(n.Body AS VARCHAR(MAX))), 2)),
       SYSUTCDATETIME(), 0,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.ConsentNotice c WHERE c.ConsentTypeId = t.ConsentTypeId AND c.Language = N'en' AND c.IsCurrent = 1) THEN 0 ELSE 1 END
FROM @notices n
JOIN dbo.ConsentType t ON t.Code = n.Code
WHERE NOT EXISTS (SELECT 1 FROM dbo.ConsentNotice c WHERE c.ConsentTypeId = t.ConsentTypeId AND c.Version = N'1.0' AND c.Language = N'en');

/* Consents recorded before notices existed were given against this wording. */
UPDATE r
SET r.ConsentNoticeId = n.ConsentNoticeId, r.NoticeVersion = n.Version, r.NoticeLanguage = n.Language, r.NoticeSha256 = n.BodySha256
FROM dbo.ConsentRecord r
JOIN dbo.ConsentNotice n ON n.ConsentTypeId = r.ConsentTypeId AND n.Version = N'1.0' AND n.Language = N'en'
WHERE r.ConsentNoticeId IS NULL;

SELECT t.Code, n.Version, n.Language, n.IsCurrent, n.BodySha256
FROM dbo.ConsentNotice n JOIN dbo.ConsentType t ON t.ConsentTypeId = n.ConsentTypeId
ORDER BY t.Code, n.Version;
GO
