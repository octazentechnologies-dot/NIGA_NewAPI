/*
    HomeoCentrum - Default WhatsApp templates (run after WhatsAppTemplateMaster_CreateTable.sql)
*/

DECLARE @EnglishLanguageId INT =
(
    SELECT TOP 1 languageId
    FROM dbo.LanguageMaster
    WHERE (IsDeleted = 0 OR IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(languageName))) IN (N'ENGLISH', N'EN')
    ORDER BY languageId
);

IF @EnglishLanguageId IS NULL
    SET @EnglishLanguageId = 1;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.WhatsAppTemplateMaster
    WHERE TemplateName = N'Hospital Service Default'
      AND TemplateCategory = N'HospitalService'
      AND LanguageId = @EnglishLanguageId
      AND DeleteStatus = 0
)
BEGIN
    INSERT INTO dbo.WhatsAppTemplateMaster
        (TemplateName, TemplateCategory, MetaTemplateName, TemplateBody, LanguageId, Description, IsActive, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (
            N'Hospital Service Default',
            N'HospitalService',
            NULL,
            N'Dear {{PatientName}},

Greetings from {{HospitalName}}.

We are pleased to inform you about our healthcare services under the guidance of {{DoctorName}}.

📅 Effective Date:
{{Date}}

📢 Important Information:
{{Message}}

For appointments and inquiries, please contact our clinic.

Thank you for choosing us for your healthcare journey.

Warm Regards,
{{DoctorName}}
{{HospitalName}}',
            @EnglishLanguageId,
            N'Default hospital services message template',
            1,
            N'System',
            GETDATE(),
            0
        );
END
GO

DECLARE @EnglishLanguageId2 INT =
(
    SELECT TOP 1 languageId
    FROM dbo.LanguageMaster
    WHERE (IsDeleted = 0 OR IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(languageName))) IN (N'ENGLISH', N'EN')
    ORDER BY languageId
);

IF @EnglishLanguageId2 IS NULL
    SET @EnglishLanguageId2 = 1;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.WhatsAppTemplateMaster
    WHERE TemplateName = N'Offers Default'
      AND TemplateCategory = N'OffersDiscount'
      AND LanguageId = @EnglishLanguageId2
      AND DeleteStatus = 0
)
BEGIN
    INSERT INTO dbo.WhatsAppTemplateMaster
        (TemplateName, TemplateCategory, MetaTemplateName, TemplateBody, LanguageId, Description, IsActive, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (
            N'Offers Default',
            N'OffersDiscount',
            NULL,
            N'Dear {{PatientName}},

{{HospitalName}} has an exclusive offer for you:

{{Offer}}

Valid until {{Date}}.

Contact {{DoctorName}} for details.

Warm Regards,
{{HospitalName}}',
            @EnglishLanguageId2,
            N'Default offers and discounts template',
            1,
            N'System',
            GETDATE(),
            0
        );
END
GO

DECLARE @EnglishLanguageId3 INT =
(
    SELECT TOP 1 languageId
    FROM dbo.LanguageMaster
    WHERE (IsDeleted = 0 OR IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(languageName))) IN (N'ENGLISH', N'EN')
    ORDER BY languageId
);

IF @EnglishLanguageId3 IS NULL
    SET @EnglishLanguageId3 = 1;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.WhatsAppTemplateMaster
    WHERE TemplateName = N'Health Tips Default'
      AND TemplateCategory = N'HealthTips'
      AND LanguageId = @EnglishLanguageId3
      AND DeleteStatus = 0
)
BEGIN
    INSERT INTO dbo.WhatsAppTemplateMaster
        (TemplateName, TemplateCategory, MetaTemplateName, TemplateBody, LanguageId, Description, IsActive, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (
            N'Health Tips Default',
            N'HealthTips',
            NULL,
            N'Dear {{PatientName}},

Health Tip from {{DoctorName}} at {{HospitalName}}:

{{HealthTip}}

Stay healthy!

Warm Regards,
{{DoctorName}}',
            @EnglishLanguageId3,
            N'Default health tips template',
            1,
            N'System',
            GETDATE(),
            0
        );
END
GO
