/*
008 — Seed important reference data from Dev (idempotent natural keys).
MenuNameMarathi and other string NOT NULL cols use N'' when empty.
*/
SET NOCOUNT ON;
GO

PRINT N'ModuleMaster ensure AdminPortal...';
IF OBJECT_ID(N'dbo.ModuleMaster',N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.ModuleMaster WHERE ModuleId=5)
BEGIN
  SET IDENTITY_INSERT dbo.ModuleMaster ON;
  INSERT INTO dbo.ModuleMaster (ModuleId, ModuleName, ModuleMarathiName, ModuleIcon, ModuleAreaName, Seqno, IsDirectNode, ModuleUrl, EnteredBy, EnteredDate, DeleteStatus)
  VALUES (5, N'AdminPortal', N'', N'ri-shield-user-line', N'Admin', 700, 1, N'/dashboard', N'Seed008', GETDATE(), 0);
  SET IDENTITY_INSERT dbo.ModuleMaster OFF;
END
GO


PRINT N'SmsTemplate...';
IF OBJECT_ID(N'dbo.SmsTemplate',N'U') IS NOT NULL BEGIN
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'APPT_CANCEL') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'APPT_CANCEL',N'Your Homeocentrum appointment was cancelled.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Your Homeocentrum appointment was cancelled.', IsActive=1 WHERE Code=N'APPT_CANCEL';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'APPT_CONFIRM') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'APPT_CONFIRM',N'Your Homeocentrum appointment is confirmed.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Your Homeocentrum appointment is confirmed.', IsActive=1 WHERE Code=N'APPT_CONFIRM';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'APPT_PROPOSED') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'APPT_PROPOSED',N'The clinic proposed {date} at {time}. Open the app to accept or decline.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'The clinic proposed {date} at {time}. Open the app to accept or decline.', IsActive=1 WHERE Code=N'APPT_PROPOSED';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'APPT_REMIND') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'APPT_REMIND',N'Reminder: Homeocentrum appointment with {doctor} on {date} at {time}. See you soon.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Reminder: Homeocentrum appointment with {doctor} on {date} at {time}. See you soon.', IsActive=1 WHERE Code=N'APPT_REMIND';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'APPT_REQUEST') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'APPT_REQUEST',N'Your appointment request for {date} at {time} was received. The clinic will confirm the time.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Your appointment request for {date} at {time} was received. The clinic will confirm the time.', IsActive=1 WHERE Code=N'APPT_REQUEST';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'APPT_RESCHEDULE') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'APPT_RESCHEDULE',N'Your appointment moved from {oldSlot} to {newSlot}.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Your appointment moved from {oldSlot} to {newSlot}.', IsActive=1 WHERE Code=N'APPT_RESCHEDULE';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'CHECK_IN') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'CHECK_IN',N'You are checked in for today''s visit with {doctor}. Status: {status}.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'You are checked in for today''s visit with {doctor}. Status: {status}.', IsActive=1 WHERE Code=N'CHECK_IN';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'FOLLOW_UP') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'FOLLOW_UP',N'Your follow-up with {doctor} is due on {date}. Open the app to book.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Your follow-up with {doctor} is due on {date}. Open the app to book.', IsActive=1 WHERE Code=N'FOLLOW_UP';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'MEDICINE_ORDER') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'MEDICINE_ORDER',N'Your medicine order update: {status}. Open the app for details.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Your medicine order update: {status}. Open the app for details.', IsActive=1 WHERE Code=N'MEDICINE_ORDER';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'OTP') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'OTP',N'Your Homeocentrum code is {otp}.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Your Homeocentrum code is {otp}.', IsActive=1 WHERE Code=N'OTP';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'PAY_LINK') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'PAY_LINK',N'Pay for your Homeocentrum visit: {url}',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Pay for your Homeocentrum visit: {url}', IsActive=1 WHERE Code=N'PAY_LINK';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'PAYEE_OTP') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'PAYEE_OTP',N'Homeocentrum bank-change code: {otp}. Payee #{payeeId}. Valid {minutes} minutes. Do not share.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Homeocentrum bank-change code: {otp}. Payee #{payeeId}. Valid {minutes} minutes. Do not share.', IsActive=1 WHERE Code=N'PAYEE_OTP';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'PAYMENT') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'PAYMENT',N'Payment of {amount} for your Homeocentrum visit is recorded.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Payment of {amount} for your Homeocentrum visit is recorded.', IsActive=1 WHERE Code=N'PAYMENT';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'PAYOUT_OTP') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'PAYOUT_OTP',N'Homeocentrum payout approval code: {otp}. Amount {amount}. Valid for {minutes} minutes. Do not share this code.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Homeocentrum payout approval code: {otp}. Amount {amount}. Valid for {minutes} minutes. Do not share this code.', IsActive=1 WHERE Code=N'PAYOUT_OTP';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'PHARMACY_OTP') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'PHARMACY_OTP',N'Homeocentrum pharmacy accept code: {otp}. Order #{orderId}. Valid {minutes} minutes. Do not share.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Homeocentrum pharmacy accept code: {otp}. Order #{orderId}. Valid {minutes} minutes. Do not share.', IsActive=1 WHERE Code=N'PHARMACY_OTP';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'PRESCRIPTION') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'PRESCRIPTION',N'Your Homeocentrum prescription is ready. Open the app to view it.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Your Homeocentrum prescription is ready. Open the app to view it.', IsActive=1 WHERE Code=N'PRESCRIPTION';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'REMINDER') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'REMINDER',N'Reminder from Homeocentrum: {title}. Due {due}.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Reminder from Homeocentrum: {title}. Due {due}.', IsActive=1 WHERE Code=N'REMINDER';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'TELE_READY') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'TELE_READY',N'Your tele consultation is ready. Room {room}. Open the waiting room in the app to join.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Your tele consultation is ready. Room {room}. Open the waiting room in the app to join.', IsActive=1 WHERE Code=N'TELE_READY';
IF NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code=N'WAITLIST_OFFER') INSERT INTO dbo.SmsTemplate (Code,Body,IsActive) VALUES (N'WAITLIST_OFFER',N'Hi {name}, a slot opened on {when}. Open the app to book. This offer does not reserve the slot.',1); ELSE UPDATE dbo.SmsTemplate SET Body=N'Hi {name}, a slot opened on {when}. Open the app to book. This offer does not reserve the slot.', IsActive=1 WHERE Code=N'WAITLIST_OFFER';
END
GO

PRINT N'HelpArticle...';
IF OBJECT_ID(N'dbo.HelpArticle',N'U') IS NOT NULL BEGIN
IF NOT EXISTS (SELECT 1 FROM dbo.HelpArticle WHERE Title=N'Booking a visit') INSERT INTO dbo.HelpArticle (Title,Slug,Body,IsPublished,CreatedAt) VALUES (N'Booking a visit',N'booking-a-visit',N'Choose a doctor, an open slot, and in-clinic or tele. A full day can take a waitlist join. Joining does not reserve the slot.',1,GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.HelpArticle WHERE Title=N'Draft hidden article') INSERT INTO dbo.HelpArticle (Title,Slug,Body,IsPublished,CreatedAt) VALUES (N'Draft hidden article',N'draft-hidden-article',N'This article stays hidden until it is published.',0,GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.HelpArticle WHERE Title=N'Cancel a visit') INSERT INTO dbo.HelpArticle (Title,Slug,Body,IsPublished,CreatedAt) VALUES (N'Cancel a visit',N's3-static-cancel',N'Cancel before the visit starts. Reason Other needs a short note. A cancelled visit does not hold the slot.',1,GETDATE());
END
GO

PRINT N'MenuMaster...';
IF OBJECT_ID(N'dbo.MenuMaster',N'U') IS NOT NULL BEGIN
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/home' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Account Home',N'',N'Menu',NULL,N'/account/home',N'Account stub (M08 screens later)',N'ri-home-line',N'',N'',1,1,100,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/ledger' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Ledger',N'',N'Menu',NULL,N'/account/ledger',N'Account stub (M08 screens later)',N'ri-book-line',N'',N'',1,1,110,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/earnings' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Doctor Earnings',N'',N'Menu',NULL,N'/account/earnings',N'Account stub (M08 screens later)',N'ri-money-dollar-circle-line',N'',N'',1,1,120,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/payouts' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Payouts',N'',N'Menu',NULL,N'/account/payouts',N'Account stub (M08 screens later)',N'ri-bank-card-line',N'',N'',1,1,130,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/invoices' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Invoices',N'',N'Menu',NULL,N'/account/invoices',N'Account stub (M08 screens later)',N'ri-file-list-3-line',N'',N'',1,1,140,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/reports' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Reports',N'',N'Menu',NULL,N'/account/reports',N'Account stub (M08 screens later)',N'ri-bar-chart-line',N'',N'',1,1,150,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/pharmacy/home' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (2,N'Pharmacy Home',N'',N'Menu',NULL,N'/pharmacy/home',N'HomeoMeds stub',N'ri-capsule-line',N'',N'',1,1,200,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/family' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (3,N'Family',N'',N'Menu',NULL,N'/family',N'Patient app (M16/M17)',N'ri-group-line',N'',N'',1,1,100,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/caregiver' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (3,N'Caregiver',N'',N'Menu',NULL,N'/caregiver',N'Patient app (M16/M17)',N'ri-user-heart-line',N'',N'',1,1,110,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctordashboard' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Dashboard',N'',N'Menu',NULL,N'/doctordashboard',N'Clinic doctor SPA (ADM-B04)',N'ri-dashboard-2-line',N'',N'',1,1,100,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/patientboard' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Patient Board',N'',N'Menu',NULL,N'/doctor/patientboard',N'Clinic doctor SPA (ADM-B04)',N'ri-user-heart-line',N'',N'',1,1,110,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/anatomy' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Anatomy',N'',N'Menu',NULL,N'/doctor/anatomy',N'Clinic doctor SPA (ADM-B04)',N'ri-body-scan-line',N'',N'',1,1,120,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/reception-staff' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Reception Staff',N'',N'Menu',NULL,N'/doctor/reception-staff',N'S2 Week 2 doctor SPA',N'ri-user-star-line',N'',N'',1,1,140,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/profile' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Profile',N'',N'Menu',NULL,N'/profile',N'S2 Week 2 doctor SPA',N'ri-user-settings-line',N'',N'',1,1,150,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/dashboard' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Dashboard',N'',N'Menu',NULL,N'/dashboard',N'Role menu seed S2-19',N'ri-dashboard-2-line',N'',N'',1,1,10,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/enquiries' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Enquiries',N'',N'Menu',NULL,N'/admin/enquiries',N'Role menu seed S2-19',N'ri-mail-line',N'',N'',1,1,20,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/existancequestions' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Existance Questions',N'',N'Menu',NULL,N'/admin/nav/existancequestions',N'Role menu seed S2-19',N'ri-question-line',N'',N'',0,1,100,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/clinicalpatterns' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Clinical Patterns',N'',N'Menu',NULL,N'/admin/nav/clinicalpatterns',N'Role menu seed S2-19',N'ri-bar-chart-horizontal-fill',N'',N'',0,1,200,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/pharmacy/onboarding' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (2,N'Onboarding',N'',N'Menu',NULL,N'/pharmacy/onboarding',N'Role menu seed S2-19',N'ri-user-add-line',N'',N'',1,1,210,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/pharmacy/orders' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (2,N'Orders',N'',N'Menu',NULL,N'/pharmacy/orders',N'Role menu seed S2-19',N'ri-shopping-bag-3-line',N'',N'',1,1,220,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/pharmacy/quotes' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (2,N'Quotes',N'',N'Menu',NULL,N'/pharmacy/quotes',N'Role menu seed S2-19',N'ri-file-list-3-line',N'',N'',1,1,230,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/repertory' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Repertory',N'',N'Menu',NULL,N'/admin/nav/repertory',N'Role menu seed S2-19',N'ri-book-3-line',N'',N'',0,1,300,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/materiamedica' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Materia Medica',N'',N'Menu',NULL,N'/admin/nav/materiamedica',N'Role menu seed S2-19',N'ri-hail-line',N'',N'',0,1,400,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/adverseeffect' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Adverse Effect',N'',N'Menu',NULL,N'/admin/nav/adverseeffect',N'Role menu seed S2-19',N'ri-body-scan-fill',N'',N'',0,1,500,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/deepanalytics' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Deep Analytics',N'',N'Menu',NULL,N'/admin/nav/deepanalytics',N'Role menu seed S2-19',N'ri-user-search-line',N'',N'',0,1,600,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/businessmanagement' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'BU Mgmt.',N'',N'Menu',NULL,N'/admin/nav/businessmanagement',N'Role menu seed S2-19',N'ri-customer-service-2-line',N'',N'',0,1,700,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/3dbodypart' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'3D Parts',N'',N'Menu',NULL,N'/admin/nav/3dbodypart',N'Role menu seed S2-19',N'ri-body-scan-line',N'',N'',0,1,800,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/rubricintelligence' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Rubric Intelligence',N'',N'Menu',NULL,N'/admin/nav/rubricintelligence',N'Role menu seed S2-19',N'ri-brain-line',N'',N'',0,1,900,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listexistance' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Existance',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/existancequestions' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listexistance',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,101,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listquestiongroup' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Question Group',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/existancequestions' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listquestiongroup',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,102,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listsubquestiongroup' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Sub Question Group',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/existancequestions' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listsubquestiongroup',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,103,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listclinicalquestion' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Clinical Questions',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/existancequestions' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listclinicalquestion',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,104,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listdiagnosissystem' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Diagnosis System',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/clinicalpatterns' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listdiagnosissystem',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,201,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listdiagnosistherapeuticsdetails' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Diagnosis Therapeutics',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/clinicalpatterns' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listdiagnosistherapeuticsdetails',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,202,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listdiagnosisconditions' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Diagnosis & Conditions',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/clinicalpatterns' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listdiagnosisconditions',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,203,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listsection' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Section',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/repertory' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listsection',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,301,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listsubsection' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Sub Section',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/repertory' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listsubsection',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,302,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listrubrics' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Add Remedies',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/repertory' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listrubrics',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,303,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listremedialrubrics' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Remedial Rubrics',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/repertory' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listremedialrubrics',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,304,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listlanguage' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Language',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/repertory' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listlanguage',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,305,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listbodyparts' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Body Parts',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/repertory' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listbodyparts',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,306,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listintensity' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Intensity',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/repertory' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listintensity',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,307,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listremedy' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Remedy',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/repertory' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listremedy',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,308,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listremedygrade' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Remedy Grade',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/repertory' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listremedygrade',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,309,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listauthor' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Author',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/materiamedica' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listauthor',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,401,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listmateriamedicaremedies' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Materia Medica Remedies',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/materiamedica' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listmateriamedicaremedies',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,402,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listmateriamedica' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Materia Medica List',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/materiamedica' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listmateriamedica',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,403,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listhead' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Head',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/materiamedica' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listhead',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,404,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listdrugsystem' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Drug System',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/adverseeffect' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listdrugsystem',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,501,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listdruggroup' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Drug Group',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/adverseeffect' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listdruggroup',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,502,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listallopathicdrug' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Allopathic Drug',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/adverseeffect' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listallopathicdrug',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,503,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/commingsoon' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Deep Analytics Home',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/deepanalytics' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/commingsoon',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,601,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listpackage' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Packages',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/businessmanagement' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listpackage',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,701,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listqualification' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Qualifications',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/businessmanagement' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listqualification',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,702,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listrole' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Roles',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/businessmanagement' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listrole',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,703,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listusers' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Users',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/businessmanagement' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listusers',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,704,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listblog' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Blogs',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/businessmanagement' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listblog',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,705,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listnews' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'News',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/businessmanagement' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listnews',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,706,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listlabsimaging' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Labs & Imaging',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/businessmanagement' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listlabsimaging',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,707,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listmeshkeymaster' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Mesh Key Master',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/3dbodypart' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listmeshkeymaster',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,801,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/list3dsectionmaster' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Section Master',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/3dbodypart' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/list3dsectionmaster',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,802,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/list3dhotspots' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Hotspots',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/3dbodypart' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/list3dhotspots',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,803,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listrubricmetaphors' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Metaphors',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/rubricintelligence' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listrubricmetaphors',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,901,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listrubricaliases' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Aliases',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/rubricintelligence' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/listrubricaliases',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,902,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/rubric-intelligence-benchmark' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Benchmark',N'',N'Menu',(SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/rubricintelligence' AND ISNULL(DeleteStatus,0)=0 ORDER BY MenuId),N'/admin/rubric-intelligence-benchmark',N'Role menu seed S2-19',N'ri-list-check',N'',N'',1,1,903,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/schedule' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Schedule',N'',N'Menu',NULL,N'/doctor/schedule',N'S3 Week 3',N'ri-calendar-2-line',N'',N'',1,1,160,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/support' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Support',N'',N'Menu',NULL,N'/doctor/support',N'S3 Week 3',N'ri-customer-service-2-line',N'',N'',1,1,170,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/tele' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Teleconsult',N'',N'Menu',NULL,N'/doctor/tele',N'S3 Week 3',N'ri-vidicon-line',N'',N'',1,1,180,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/reception' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Reception',N'',N'Menu',NULL,N'/reception',N'S3 Week 3',N'ri-dashboard-2-line',N'',N'',1,1,10,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/reception/schedule' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Schedule',N'',N'Menu',NULL,N'/reception/schedule',N'S3 Week 3',N'ri-calendar-2-line',N'',N'',1,1,20,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/reception/case-paper' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Case paper',N'',N'Menu',NULL,N'/reception/case-paper',N'S3 Week 3',N'ri-file-list-3-line',N'',N'',1,1,30,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/support-tickets' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Support tickets',N'',N'Menu',NULL,N'/admin/support-tickets',N'S3 Week 3',N'ri-ticket-2-line',N'',N'',1,1,900,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/trust-queue' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Trust queue',N'',N'Menu',NULL,N'/admin/trust-queue',N'S4 Week 4 menu',N'ri-shield-check-line',N'',N'',1,1,55,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/homemeds-exceptions' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'HomeoMeds exceptions',N'',N'Menu',NULL,N'/admin/homemeds-exceptions',N'S4 Week 4 menu',N'ri-capsule-line',N'',N'',1,1,56,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/pharmacy-partners' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Pharmacy partners',N'',N'Menu',NULL,N'/admin/pharmacy-partners',N'S4 Week 4 menu',N'ri-store-3-line',N'',N'',1,1,57,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/consult-payments' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Consult payments',N'',N'Menu',NULL,N'/admin/consult-payments',N'S4 Week 4 menu',N'ri-money-rupee-circle-line',N'',N'',1,1,58,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/consult-recon' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Consultation recon',N'',N'Menu',NULL,N'/account/consult-recon',N'S4 Week 4 menu',N'ri-exchange-line',N'',N'',1,1,112,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/medicine-ledger' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Medicine ledger',N'',N'Menu',NULL,N'/account/medicine-ledger',N'S4 Week 4 menu',N'ri-capsule-line',N'',N'',1,1,115,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/patient/continuity' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (3,N'Care continuity',N'',N'Menu',NULL,N'/patient/continuity',N'S4 Week 4 menu',N'ri-heart-pulse-line',N'',N'',1,1,120,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/consult-fees' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Consult fees',N'',N'Menu',NULL,N'/doctor/consult-fees',N'S4 Week 4 menu',N'ri-money-rupee-circle-line',N'',N'',1,1,125,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/earnings' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Earnings',N'',N'Menu',NULL,N'/doctor/earnings',N'S4 Week 4 menu',N'ri-line-chart-line',N'',N'',1,1,126,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/erx' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Sign eRx',N'',N'Menu',NULL,N'/doctor/erx',N'S4 Week 4 menu',N'ri-file-list-3-line',N'',N'',1,1,128,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/patient/medicine-orders' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (3,N'Medicine orders',N'',N'Menu',NULL,N'/patient/medicine-orders',N'S4 Week 4 menu',N'ri-capsule-line',N'',N'',1,1,130,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/refunds' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Refunds',N'',N'Menu',NULL,N'/account/refunds',N'S4 Week 4 menu',N'ri-refund-2-line',N'',N'',1,1,135,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/settlements' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Settlements',N'',N'Menu',NULL,N'/account/settlements',N'S4 Week 4 menu',N'ri-shake-hands-line',N'',N'',1,1,136,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/exceptions' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Exceptions',N'',N'Menu',NULL,N'/account/exceptions',N'S4 Week 4 menu',N'ri-error-warning-line',N'',N'',1,1,137,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/tax' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'GST & tax',N'',N'Menu',NULL,N'/account/tax',N'S4 Week 4 menu',N'ri-percent-line',N'',N'',1,1,138,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/payees' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Payees',N'',N'Menu',NULL,N'/account/payees',N'S4 Week 4 menu',N'ri-bank-line',N'',N'',1,1,139,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/clinic-collections' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (1,N'Clinic collections',N'',N'Menu',NULL,N'/account/clinic-collections',N'S4 Week 4 menu',N'ri-store-2-line',N'',N'',1,1,145,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/reports' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (5,N'Clinic reports',N'',N'Menu',NULL,N'/admin/reports',N'S5 Week 5 menu',N'ri-bar-chart-box-line',N'',N'',1,1,59,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/follow-ups' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (4,N'Follow-ups due',N'',N'Menu',NULL,N'/doctor/follow-ups',N'S5 Week 5 menu',N'ri-calendar-check-line',N'',N'',1,1,129,N'0',NULL,GETDATE(),0);
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/patient/notifications' AND ISNULL(DeleteStatus,0)=0)
INSERT INTO dbo.MenuMaster (ModuleId,MenuName,MenuNameMarathi,MenuType,ParentMenuId,MenuUrl,Description,MenuIcon,ActionName,ControllerName,IsLeaf,ShowInMainMenu,SeqNo,FirmIds,EnteredBy,EnteredDate,DeleteStatus)
VALUES (3,N'Notifications',N'',N'Menu',NULL,N'/patient/notifications',N'S5 Week 5 menu',N'ri-notification-3-line',N'',N'',1,1,140,N'0',NULL,GETDATE(),0);
END
GO

PRINT N'RoleDetails...';
IF OBJECT_ID(N'dbo.RoleDetails',N'U') IS NOT NULL BEGIN
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/dashboard' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/dashboard' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/dashboard' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/enquiries' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/enquiries' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/enquiries' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/existancequestions' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/nav/existancequestions' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/nav/existancequestions' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/clinicalpatterns' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/nav/clinicalpatterns' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/nav/clinicalpatterns' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/repertory' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/nav/repertory' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/nav/repertory' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/materiamedica' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/nav/materiamedica' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/nav/materiamedica' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/adverseeffect' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/nav/adverseeffect' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/nav/adverseeffect' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/deepanalytics' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/nav/deepanalytics' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/nav/deepanalytics' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/businessmanagement' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/nav/businessmanagement' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/nav/businessmanagement' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/3dbodypart' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/nav/3dbodypart' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/nav/3dbodypart' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/nav/rubricintelligence' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/nav/rubricintelligence' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/nav/rubricintelligence' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listexistance' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listexistance' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listexistance' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listquestiongroup' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listquestiongroup' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listquestiongroup' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listsubquestiongroup' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listsubquestiongroup' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listsubquestiongroup' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listclinicalquestion' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listclinicalquestion' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listclinicalquestion' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listdiagnosissystem' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listdiagnosissystem' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listdiagnosissystem' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listdiagnosistherapeuticsdetails' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listdiagnosistherapeuticsdetails' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listdiagnosistherapeuticsdetails' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listdiagnosisconditions' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listdiagnosisconditions' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listdiagnosisconditions' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listsection' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listsection' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listsection' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listsubsection' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listsubsection' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listsubsection' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listrubrics' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listrubrics' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listrubrics' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listremedialrubrics' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listremedialrubrics' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listremedialrubrics' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listlanguage' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listlanguage' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listlanguage' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listbodyparts' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listbodyparts' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listbodyparts' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listintensity' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listintensity' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listintensity' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listremedy' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listremedy' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listremedy' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listremedygrade' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listremedygrade' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listremedygrade' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listauthor' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listauthor' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listauthor' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listmateriamedicaremedies' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listmateriamedicaremedies' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listmateriamedicaremedies' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listmateriamedica' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listmateriamedica' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listmateriamedica' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listhead' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listhead' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listhead' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listdrugsystem' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listdrugsystem' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listdrugsystem' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listdruggroup' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listdruggroup' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listdruggroup' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listallopathicdrug' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listallopathicdrug' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listallopathicdrug' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/commingsoon' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/commingsoon' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/commingsoon' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listpackage' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listpackage' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listpackage' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listqualification' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listqualification' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listqualification' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listrole' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listrole' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listrole' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listusers' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listusers' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listusers' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listblog' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listblog' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listblog' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listnews' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listnews' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listnews' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listlabsimaging' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listlabsimaging' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listlabsimaging' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listmeshkeymaster' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listmeshkeymaster' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listmeshkeymaster' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/list3dsectionmaster' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/list3dsectionmaster' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/list3dsectionmaster' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/list3dhotspots' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/list3dhotspots' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/list3dhotspots' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listrubricmetaphors' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listrubricmetaphors' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listrubricmetaphors' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/listrubricaliases' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/listrubricaliases' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/listrubricaliases' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/rubric-intelligence-benchmark' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/rubric-intelligence-benchmark' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/rubric-intelligence-benchmark' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/support-tickets' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/support-tickets' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/support-tickets' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/trust-queue' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/trust-queue' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/trust-queue' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/homemeds-exceptions' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/homemeds-exceptions' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/homemeds-exceptions' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/pharmacy-partners' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/pharmacy-partners' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/pharmacy-partners' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/consult-payments' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/consult-payments' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/consult-payments' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/admin/reports' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=1 AND m.MenuUrl=N'/admin/reports' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 1, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/admin/reports' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctordashboard' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/doctordashboard' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctordashboard' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/patientboard' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/doctor/patientboard' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctor/patientboard' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/anatomy' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/doctor/anatomy' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctor/anatomy' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/reception-staff' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/doctor/reception-staff' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctor/reception-staff' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/profile' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/profile' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/profile' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/schedule' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/doctor/schedule' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctor/schedule' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/support' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/doctor/support' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctor/support' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/tele' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/doctor/tele' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctor/tele' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/consult-fees' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/doctor/consult-fees' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctor/consult-fees' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/earnings' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/doctor/earnings' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctor/earnings' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/erx' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/doctor/erx' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctor/erx' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctor/follow-ups' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=3 AND m.MenuUrl=N'/doctor/follow-ups' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 3, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctor/follow-ups' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/doctordashboard' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=4 AND m.MenuUrl=N'/doctordashboard' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 4, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/doctordashboard' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/profile' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=4 AND m.MenuUrl=N'/profile' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 4, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/profile' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/reception' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=4 AND m.MenuUrl=N'/reception' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 4, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/reception' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/reception/schedule' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=4 AND m.MenuUrl=N'/reception/schedule' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 4, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/reception/schedule' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/reception/case-paper' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=4 AND m.MenuUrl=N'/reception/case-paper' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 4, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/reception/case-paper' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/family' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=5 AND m.MenuUrl=N'/family' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 5, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/family' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/caregiver' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=5 AND m.MenuUrl=N'/caregiver' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 5, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/caregiver' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/patient/continuity' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=5 AND m.MenuUrl=N'/patient/continuity' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 5, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/patient/continuity' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/patient/medicine-orders' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=5 AND m.MenuUrl=N'/patient/medicine-orders' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 5, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/patient/medicine-orders' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/patient/notifications' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=5 AND m.MenuUrl=N'/patient/notifications' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 5, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/patient/notifications' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/home' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/home' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/home' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/ledger' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/ledger' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/ledger' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/earnings' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/earnings' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/earnings' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/payouts' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/payouts' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/payouts' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/invoices' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/invoices' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/invoices' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/reports' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/reports' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/reports' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/consult-recon' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/consult-recon' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/consult-recon' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/medicine-ledger' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/medicine-ledger' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/medicine-ledger' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/refunds' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/refunds' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/refunds' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/settlements' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/settlements' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/settlements' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/exceptions' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/exceptions' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/exceptions' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/tax' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/tax' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/tax' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/payees' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/payees' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/payees' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/account/clinic-collections' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=6 AND m.MenuUrl=N'/account/clinic-collections' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 6, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/account/clinic-collections' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/pharmacy/home' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=7 AND m.MenuUrl=N'/pharmacy/home' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 7, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/pharmacy/home' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/pharmacy/onboarding' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=7 AND m.MenuUrl=N'/pharmacy/onboarding' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 7, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/pharmacy/onboarding' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/pharmacy/orders' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=7 AND m.MenuUrl=N'/pharmacy/orders' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 7, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/pharmacy/orders' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
IF EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl=N'/pharmacy/quotes' AND ISNULL(DeleteStatus,0)=0)
AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails rd INNER JOIN dbo.MenuMaster m ON m.MenuId=rd.MenuId WHERE rd.RoleId=7 AND m.MenuUrl=N'/pharmacy/quotes' AND ISNULL(m.DeleteStatus,0)=0)
INSERT INTO dbo.RoleDetails (RoleId,MenuId,IsView,IsAdd,IsModify,IsDelete)
SELECT TOP 1 7, m.MenuId, 1, 0, 0, 0 FROM dbo.MenuMaster m WHERE m.MenuUrl=N'/pharmacy/quotes' AND ISNULL(m.DeleteStatus,0)=0 ORDER BY m.MenuId;
END
GO

SELECT 'SmsTemplate' T, COUNT(*) C FROM dbo.SmsTemplate
UNION ALL SELECT 'MenuMaster', COUNT(*) FROM dbo.MenuMaster WHERE ISNULL(DeleteStatus,0)=0
UNION ALL SELECT 'RoleDetails', COUNT(*) FROM dbo.RoleDetails
UNION ALL SELECT 'HelpArticle', COUNT(*) FROM dbo.HelpArticle;
PRINT N'008 seed complete';
GO
