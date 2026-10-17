/*
S5 Week 5 — step 3 of 3. Run after 02_S5_Week5_Menus.sql.
Seed SMS templates used by appointment and OTP events.
Does not send a message and does not store a provider key.
*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.SmsTemplate', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'OTP')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'OTP', N'Your Homeocentrum verification code is {otp}. It is valid for {minutes} minutes. Do not share this code.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'PAYOUT_OTP')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'PAYOUT_OTP', N'Homeocentrum payout approval code: {otp}. Amount {amount}. Valid for {minutes} minutes. Do not share this code.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'APPT_CONFIRM')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'APPT_CONFIRM', N'Your Homeocentrum appointment with {doctor} is confirmed for {date} at {time}.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'APPT_CANCEL')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'APPT_CANCEL', N'Your appointment on {slot} was cancelled{reason}. Contact the clinic if you need a new slot.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'APPT_RESCHEDULE')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'APPT_RESCHEDULE', N'Your appointment moved from {oldSlot} to {newSlot}.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'APPT_REQUEST')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'APPT_REQUEST', N'Your appointment request for {date} at {time} was received. The clinic will confirm the time.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'APPT_PROPOSED')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'APPT_PROPOSED', N'The clinic proposed {date} at {time}. Open the app to accept or decline.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'WAITLIST_OFFER')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'WAITLIST_OFFER', N'Hi {name}, a slot opened on {when}. Open the app to book. This offer does not reserve the slot.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'TELE_READY')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'TELE_READY', N'Your tele consultation is ready. Room {room}. Open the waiting room in the app to join.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'FOLLOW_UP')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'FOLLOW_UP', N'Your follow-up with {doctor} is due on {date}. Open the app to book.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'PRESCRIPTION')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'PRESCRIPTION', N'Your Homeocentrum prescription is ready. Open the app to view it.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'MEDICINE_ORDER')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'MEDICINE_ORDER', N'Your medicine order update: {status}. Open the app for details.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'PAYMENT')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'PAYMENT', N'Payment of {amount} for your Homeocentrum visit is recorded.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'REMINDER')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'REMINDER', N'Reminder from Homeocentrum: {title}. Due {due}.');
    IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = N'CHECK_IN')
        INSERT INTO dbo.SmsTemplate (Code, Body) VALUES (N'CHECK_IN', N'You are checked in for today''s visit with {doctor}. Status: {status}.');
END
GO
