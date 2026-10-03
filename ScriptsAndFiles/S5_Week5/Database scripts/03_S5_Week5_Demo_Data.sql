/*
S5 Week 5 — step 3 of 3. Run after 02_S5_Week5_Menus.sql.
Seed SMS templates used by appointment and OTP events.
Does not send a message and does not store a provider key.
*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.SmsTemplate', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'APPT_CONFIRM')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'APPT_CONFIRM', N'Your Homeocentrum appointment is confirmed.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'APPT_CANCEL')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'APPT_CANCEL', N'Your Homeocentrum appointment was cancelled.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'OTP')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'OTP', N'Your Homeocentrum code is {otp}.');
END
GO
