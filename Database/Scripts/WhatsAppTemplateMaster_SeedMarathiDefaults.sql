/*
    HomeoCentrum - Marathi default WhatsApp templates
    Run after WhatsAppTemplateMaster_Alter_Language.sql and WhatsAppTemplateMaster_SeedDefaults.sql
*/

DECLARE @MarathiLanguageId INT =
(
    SELECT TOP 1 languageId
    FROM dbo.LanguageMaster
    WHERE (IsDeleted = 0 OR IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(languageName))) IN (N'MARATHI', N'MR', N'MAR')
    ORDER BY languageId
);

IF @MarathiLanguageId IS NULL
BEGIN
    RAISERROR('Marathi language not found in LanguageMaster. Add Marathi before running this script.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1
    FROM dbo.WhatsAppTemplateMaster
    WHERE TemplateName = N'Hospital Service Default'
      AND TemplateCategory = N'HospitalService'
      AND LanguageId = @MarathiLanguageId
      AND DeleteStatus = 0
)
BEGIN
    INSERT INTO dbo.WhatsAppTemplateMaster
        (TemplateName, TemplateCategory, MetaTemplateName, TemplateBody, Description, LanguageId, IsActive, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (
            N'Hospital Service Default',
            N'HospitalService',
            NULL,
            N'प्रिय {{PatientName}},

{{HospitalName}} कडून शुभेच्छा.

{{DoctorName}} यांच्या मार्गदर्शनाखाली आमच्या आरोग्य सेवांबद्दल माहिती:

📅 दिनांक:
{{Date}}

📢 महत्वाची माहिती:
{{Message}}

भेटीसाठी कृपया आमच्या क्लिनिकशी संपर्क साधा.

आम्हाला निवडल्याबद्दल धन्यवाद.

सादर,
{{DoctorName}}
{{HospitalName}}',
            N'Default hospital services message template (Marathi)',
            @MarathiLanguageId,
            1,
            N'System',
            GETDATE(),
            0
        );
END
GO

DECLARE @MarathiLanguageId2 INT =
(
    SELECT TOP 1 languageId
    FROM dbo.LanguageMaster
    WHERE (IsDeleted = 0 OR IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(languageName))) IN (N'MARATHI', N'MR', N'MAR')
    ORDER BY languageId
);

IF NOT EXISTS (
    SELECT 1
    FROM dbo.WhatsAppTemplateMaster
    WHERE TemplateName = N'Offers Default'
      AND TemplateCategory = N'OffersDiscount'
      AND LanguageId = @MarathiLanguageId2
      AND DeleteStatus = 0
)
BEGIN
    INSERT INTO dbo.WhatsAppTemplateMaster
        (TemplateName, TemplateCategory, MetaTemplateName, TemplateBody, Description, LanguageId, IsActive, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (
            N'Offers Default',
            N'OffersDiscount',
            NULL,
            N'प्रिय {{PatientName}},

{{HospitalName}} कडून तुमच्यासाठी खास ऑफर:

{{Offer}}

{{Date}} पर्यंत वैध.

तपशीलांसाठी {{DoctorName}} यांच्याशी संपर्क साधा.

सादर,
{{HospitalName}}',
            N'Default offers and discounts template (Marathi)',
            @MarathiLanguageId2,
            1,
            N'System',
            GETDATE(),
            0
        );
END
GO

DECLARE @MarathiLanguageId3 INT =
(
    SELECT TOP 1 languageId
    FROM dbo.LanguageMaster
    WHERE (IsDeleted = 0 OR IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(languageName))) IN (N'MARATHI', N'MR', N'MAR')
    ORDER BY languageId
);

IF NOT EXISTS (
    SELECT 1
    FROM dbo.WhatsAppTemplateMaster
    WHERE TemplateName = N'Health Tips Default'
      AND TemplateCategory = N'HealthTips'
      AND LanguageId = @MarathiLanguageId3
      AND DeleteStatus = 0
)
BEGIN
    INSERT INTO dbo.WhatsAppTemplateMaster
        (TemplateName, TemplateCategory, MetaTemplateName, TemplateBody, Description, LanguageId, IsActive, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (
            N'Health Tips Default',
            N'HealthTips',
            NULL,
            N'प्रिय {{PatientName}},

{{HospitalName}} मधील {{DoctorName}} यांचा आरोग्य टिप:

{{HealthTip}}

निरोगी राहा!

सादर,
{{DoctorName}}',
            N'Default health tips template (Marathi)',
            @MarathiLanguageId3,
            1,
            N'System',
            GETDATE(),
            0
        );
END
GO
