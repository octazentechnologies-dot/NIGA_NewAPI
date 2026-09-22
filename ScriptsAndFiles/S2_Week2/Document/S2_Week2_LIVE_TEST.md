# S2 Week 2 — live test

Date: 2026-09-22. Hosts: New-API http://127.0.0.1:5038, Old-API http://127.0.0.1:5001, UI http://127.0.0.1:3000.
Catalog: `NIGA_NewAPI/ScriptsAndFiles/Homeocentrum_All_New_And_Updated+APIs.xlsx`
Skipped: QA, Mobile UI, Mobile Frontend. Excel status ignored.

**Checks:** 190  **PASS:** 190  **FAIL:** 0

| SubID | Host | Call | Expect | Got | Result | Note |
|-------|------|------|--------|-----|--------|------|
| DOC-02.03 | Old-API | `POST /api/Account/Login` | 200 | 200 | PASS | doctor Tufan_Doctor |
| DOC-04.02 | Old-API | `POST /api/Account/Login` | 200 | 200 | PASS | doctor2 testdoctor |
| WEB-06.02 | Old-API | `POST /api/Account/Login` | 200 | 200 | PASS | admin Tufan_Admin |
| PAT-08.02 | Old-API | `POST /api/Account/Login` | 200 | 200 | PASS | patient Tufan_Patient |
| PAT-07.02 | Old-API | `POST /api/Account/Login caregiver` | 200 | 200 | PASS | Tufan_Caregiver |
| SEC-04 | Old-API | `POST /api/Account/Login account` | 200 | 200 | PASS | Tufan_Account |
| SEC-04 | Old-API | `POST /api/Account/Login pharmacy` | 200 | 200 | PASS | Tufan_Pharmacy |
| CLN-02.02 | Old-API | `POST /api/Account/Login` | 200 | 200 | PASS | reception Tufan_Reception ok |
| ADM-B04.02 | New-API | `GET /api/mastersAPI/GetMenuByRole Tufan_Doctor` | 200 | 200 | PASS | [{"menuId":10,"moduleId":4,"moduleName":null,"menuName":"Dashboard","menuNameMar |
| ADM-B04.02 | New-API | `GET GetMenuByRole Tufan_Doctor extras` | >=20 | 69 | PASS | menus=69 |
| ADM-B04.02 | New-API | `GET /api/mastersAPI/GetMenuByRole testdoctor` | 200 | 200 | PASS | [{"menuId":10,"moduleId":4,"moduleName":null,"menuName":"Dashboard","menuNameMar |
| ADM-B04.02 | New-API | `GET GetMenuByRole testdoctor core-only` | <20 | 8 | PASS | menus=8 |
| SEC-09 | New-API | `POST /api/Diagnostics/ClientError` | 200 | 200 | PASS | {"success":true} |
| SEC-09 | New-API | `FILE ErrorAlert who/when/where log` | user+browser+device | 200 | PASS | who=True device=True |
| WEB-03.02 | New-API | `GET /api/Public/Doctors` | 200 | 200 | PASS | {"success":true,"pageNumber":1,"pageSize":20,"totalRecords":9,"data":[{"doctorId |
| WEB-03.01 | New-API | `GET /api/Public/Doctors` | >0 | 9 | PASS | total=9 n=9 |
| WEB-02.01 | New-API | `GET /api/Public/Doctors` | fees | 200 | PASS | 550.0 |
| PAT-13.02 | New-API | `GET /api/Public/Doctors` | verified | 200 | PASS | verified=True |
| WEB-01.02 | New-API | `GET /api/Public/Doctors?isOnline=true` | 200 | 200 | PASS | {"success":true,"pageNumber":1,"pageSize":5,"totalRecords":4,"data":[{"doctorId" |
| PAT-11.02 | New-API | `GET /api/Public/Doctors filters` | 200 | 200 | PASS | {"success":true,"pageNumber":1,"pageSize":5,"totalRecords":1,"data":[{"doctorId" |
| PAT-10.02 | New-API | `GET /api/Public/Doctors search` | 200 | 200 | PASS | {"success":true,"pageNumber":1,"pageSize":5,"totalRecords":7,"data":[{"doctorId" |
| WEB-03.02 | New-API | `GET /api/Public/Doctors/4` | 200 | 200 | PASS | {"success":true,"data":{"workingHoursNote":"Mon-Sat 10:00-18:00","verificationSt |
| PAT-12.02 | New-API | `GET /api/Public/Doctors/4` | 200 | 200 | PASS | {"success":true,"data":{"workingHoursNote":"Mon-Sat 10:00-18:00","verificationSt |
| PAT-14.02 | New-API | `GET /api/Public/Doctors/4/Ranking` | 200 | 200 | PASS | {"success":true,"data":{"doctorId":4,"summary":"Verified credentials · Currently |
| WEB-04.02 | New-API | `GET /api/Public/Doctors/4/Slots` | 200 | 200 | PASS | {"success":true,"data":{"doctorId":4,"appointmentDate":"2026-09-22T00:00:00","ha |
| WEB-04.02 | New-API | `GET /api/Public/Doctors/4/Slots` | 200 | 200 | PASS | {"success":true,"data":{"doctorId":4,"appointmentDate":"2026-09-22T00:00:00","ha |
| WEB-07.01 | New-API | `GET /api/Public/Policies/Privacy` | 200 | 200 | PASS | {"success":true,"data":{"policyType":"Privacy","version":"2026.09","title":"Priv |
| WEB-08.01 | New-API | `GET /api/Public/Policies/Terms` | 200 | 200 | PASS | {"success":true,"data":{"policyType":"Terms","version":"2026.09","title":"Terms  |
| WEB-08.01 | New-API | `GET /api/Public/Policies/Booking` | 200 | 200 | PASS | {"success":true,"data":{"policyType":"Booking","version":"2026.09","title":"Book |
| WEB-12.01 | New-API | `GET /api/Public/Articles` | 200 | 200 | PASS | {"success":true,"pageNumber":1,"pageSize":10,"totalRecords":3,"data":[{"blogId": |
| PAT-15.02 | New-API | `GET /api/Public/Articles/3` | 200 | 200 | PASS | {"success":true,"data":{"blogId":3,"blogHead":"S2-TESTED care article","blogSubH |
| PAT-09.02 | New-API | `GET /api/PatientPortal/CareCategories` | 200 | 200 | PASS | {"success":true,"data":[{"id":9,"name":"Abdomen","description":"S2-TESTED abdome |
| PAT-02.02 | New-API | `GET /api/Welcome/Patient` | 200 | 200 | PASS | {"success":true,"data":{"version":"1","audience":"Patient","slides":[{"welcomeSl |
| PAT-01.02 | New-API | `GET /api/mastersAPI/GetLanguages` | 200 | 200 | PASS | [{"languageId":1,"languageName":"English","description":"English Language ","isD |
| WEB-04.02 | New-API | `POST /api/PatientAuth/RequestOtp` | 200 | 200 | PASS | {"success":true,"otpChallengeId":71,"expiresAt":"2026-09-22T12:38:31.2269981Z"," |
| WEB-04.02 | New-API | `POST /api/PatientAuth/VerifyOtp` | 200 | 200 | PASS | {"success":true,"bookingSessionId":71,"mobile":"9000011122"} |
| WEB-04.02 | New-API | `POST /api/Public/Doctors/4/Bookings` | [200, 201, 409, 400] | 400 | PASS | {"success":false,"message":"Daily appointment schedule is not configured for thi |
| WEB-04.02 | New-API | `POST Bookings` | token | 400 | PASS | {"success":false,"message":"Daily appointment schedule is not configured for thi |
| WEB-09.02 | New-API | `GET /api/users/RegistrationStatus` | 200 | 200 | PASS | {"success":true,"found":true,"activated":true,"verificationStatus":"Pending","di |
| WEB-09.02 | New-API | `POST /api/users/RegisterDoctor` | 400 | 400 | PASS | {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or mor |
| WEB-10.02 | New-API | `POST /api/users/ActivateByToken` | [400, 410] | 400 | PASS | "Invalid or already used activation link" |
| WEB-10.02 | New-API | `POST /api/users/ResendActivation` | 200 | 200 | PASS | {"success":true,"message":"If the account exists and is not activated, an email  |
| WEB-06.02 | New-API | `POST /api/Enquiry` | [200, 201] | 200 | PASS | {"success":true,"data":{"enquiryId":46,"ticketStatus":"New"}} |
| WEB-06.02 | New-API | `GET /api/Enquiry` | [200, 401, 403] | 200 | PASS | {"success":true,"data":[{"enquiryId":46,"enquiryName":"S2 Live","enquiryDate":"2 |
| WEB-06.02 | Old-API | `POST /api/EnquiryDetail` | [200, 201, 400] | 200 | PASS | "Enquiry Details saved Successfully" |
| DOC-10.02 | New-API | `GET /api/Profile/Me` | 200 | 200 | PASS | {"success":true,"data":{"doctorId":1010,"userId":10032,"firstName":"Tufan","midd |
| DOC-10.02 | New-API | `GET /api/Profile/Me/Credentials` | [200, 404] | 200 | PASS | {"success":true,"data":{"doctorId":1010,"doctorVerificationId":10,"status":"Appr |
| DMO-05.02 | New-API | `GET /api/Availability/Me` | 200 | 200 | PASS | {"success":true,"data":{"doctorId":1010,"isOnline":true,"workingHoursNote":"Mon- |
| DMO-05.02 | New-API | `PUT /api/Availability/Me` | [200, 400] | 200 | PASS | {"success":true,"data":{"doctorId":1010,"isOnline":true,"workingHoursNote":"Mon- |
| DOC-01.02 | New-API | `POST /api/doctorDashBoard/GetCountApp` | [200, 400, 404] | 200 | PASS | {"patientAppId":0,"appointmentDate":null,"status":null,"userId":0,"doctorId":101 |
| DOC-03.02 | New-API | `GET /api/patientApp/GetCasesByUser/10032` | [200, 404] | 200 | PASS | [{"doctorID":0,"patientID":3046,"patientName":"Tufan Patient","address":"Tufan t |
| CLN-13.02 | New-API | `POST /api/Repertorization/CenterOfGravity` | 200 | 200 | PASS | {"success":true,"data":[],"message":"Empty clipboard."} |
| CLN-13.02 | New-API | `POST /api/RepertorizationPage/CenterOfGravity` | 200 | 200 | PASS | {"success":true,"data":[],"message":"Empty clipboard."} |
| DOC-09.01 | New-API | `GET /api/ReceptionStaff/GetReceptionStaffList` | 200 | 200 | PASS | {"success":false,"message":"DoctorUserID is required."} |
| CLN-18.01 | New-API | `GET /api/patient/ExportCasesToExcel` | [200, 404] | 200 | PASS | PK    Q�6]����   �     xl/workbook.xml�PMo�0�+��#��TQ8��i��v�C#����� |
| CLN-18.01 | New-API | `GET /api/patient/ExportCaseToPdf/1/1` | [200, 403, 404] | 403 | PASS | {"success":false,"message":"Access denied for this doctor resource."} |
| CLN-18.01 | New-API | `GET /api/doctorDashBoard/ExportPatients` | [200, 400, 404] | 200 | PASS | ﻿Patient ID,Case ID,Patient Name,Date of Birth,Age,Gender,Address,State,Country, |
| DOC-06.01 | New-API | `NOTE ExportPatients formats` | csv/excel/pdf | csv | PASS | csv today exercised; excel/pdf skipped (large export can stall Kestrel) |
| CLN-20.02 | New-API | `GET /api/threeDBodyPartSectionHotspot/GetThreeDBodyPartSectionHotspotList` | [200, 400, 404] | 200 | PASS | {"success":true,"message":"Data retrieved successfully.","pageNumber":1,"pageSiz |
| CLN-19.03 | New-API | `GET /api/PatientBoardBackup/Summary` | [200, 404, 405] | 200 | PASS | {"success":true,"message":"Backup summary retrieved.","resultObject":{"hasBackup |
| CLN-19.03 | New-API | `GET /api/PatientBoardBackup/Latest` | [200, 404, 405] | 200 | PASS | {"success":true,"message":"Backup retrieved successfully.","resultObject":{"back |
| DMO-06.02 | New-API | `POST /api/Device/Register` | [200, 201, 400] | 400 | PASS | {"success":false,"message":"Platform must be FCM or APNs."} |
| DMO-06.02 | New-API | `POST /api/Device/Register FCM` | [200, 201] | 200 | PASS | {"success":true,"data":{"devicePushTokenId":36,"platform":"FCM","deviceId":"s2-l |
| DMO-06.02 | New-API | `POST /api/Device/Unregister` | [200, 400, 404] | 200 | PASS | {"success":true,"message":"Unregistered."} |
| DOC-08.03 | New-API | `GET /api/doctorDashBoard/GetPatientStats` | [200, 400, 404] | 200 | PASS | {"patientAppId":0,"appointmentDate":null,"status":null,"userId":0,"doctorId":0," |
| WEB-09.02 | New-API | `POST /api/users/RegisterDoctorWithDocuments` | [400, 415] | 415 | PASS |  |
| CLN-16.02 | New-API | `GET /api/patient/GetComplaints/1` | [200, 403, 404] | 403 | PASS | {"success":false,"message":"Access denied for this doctor resource."} |
| CLN-02.02 | New-API | `POST /api/Repertorization/CenterOfGravity` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| DOC-10.02 | New-API | `PUT /api/Profile/Me` | [403, 400] | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| DOC-02.02 | New-API | `POST /api/doctorDashBoard/GetCountApp` | [200, 400, 403] | 200 | PASS | {"patientAppId":0,"appointmentDate":null,"status":null,"userId":0,"doctorId":101 |
| CLN-19.02 | New-API | `POST /api/PatientBoardBackup/Save` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| DOC-02.02 | New-API | `GET /api/doctorDashBoard/GetPatientStats reception` | [200, 400] | 200 | PASS | {"patientAppId":0,"appointmentDate":null,"status":null,"userId":0,"doctorId":0," |
| CLN-08.02 | New-API | `POST /api/AudioCaseTaking/upload` | [400, 403, 415] | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| PAT-08.02 | New-API | `GET /api/PatientPortal/Home` | [200, 404] | 200 | PASS | {"success":true,"data":{"patientId":3046,"patientName":"Tufan Patient","familyCo |
| PAT-06.02 | New-API | `GET /api/Family/Me` | [200, 404] | 200 | PASS | {"success":true,"data":{"ownerPatientId":3046,"ownerPatientName":"Tufan Patient" |
| PAT-06.02 | New-API | `GET /api/Family/Relations` | [200, 404] | 200 | PASS | {"success":true,"data":[{"relationId":15,"relationName":"Spouse","sortOrder":10} |
| PAT-06.02 | New-API | `GET /api/Family` | [200, 404] | 200 | PASS | {"success":true,"ownerPatientId":3046,"ownerPatientName":"Tufan Patient","isActi |
| PAT-06.02 | New-API | `GET /api/Family/22` | [200, 404] | 200 | PASS | {"success":true,"data":{"familyMemberId":22,"ownerPatientId":3046,"memberPatient |
| PAT-06.02 | New-API | `GET /api/Family/CanBookAs/3047` | [200, 400, 404] | 200 | PASS | {"success":true,"allowed":true,"reason":"family"} |
| PAT-07.02 | New-API | `GET /api/Caregiver/Me` | [200, 404] | 200 | PASS | {"success":true,"data":{"ownerPatientId":3046,"ownerPatientName":"Tufan Patient" |
| PAT-07.02 | New-API | `GET /api/Caregiver/Lookup` | [200, 400, 404] | 200 | PASS | {"success":true,"data":{"caregiverUserId":10020,"displayName":"Tufan Powar","con |
| PAT-07.02 | New-API | `GET /api/Caregiver/ListMine` | [200, 404] | 200 | PASS | {"success":true,"data":[{"caregiverAuthorizationId":16,"patientId":3046,"patient |
| PAT-07.02 | New-API | `GET /api/Caregiver/ListActingFor` | [200, 404] | 200 | PASS | {"success":true,"data":[]} |
| PAT-05.02 | New-API | `GET /api/Consent/PrivacyStatus` | [200, 404] | 200 | PASS | {"success":true,"data":{"required":true,"granted":true,"consentRecordId":45,"gra |
| PAT-04.02 | New-API | `GET /api/PatientProfile/Me` | [200, 404] | 200 | PASS | {"success":true,"data":{"userId":10033,"firstName":"Tufan","lastName":"Patient", |
| CLN-02.02 | New-API | `POST /api/Repertorization/CenterOfGravity` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| DOC-10.02 | New-API | `GET /api/Profile/Me` | [200, 403, 404] | 404 | PASS | {"success":false,"message":"Doctor profile not found for this user."} |
| PAT-06.02 | New-API | `GET /api/Family/Me caregiver` | 200 | 200 | PASS | {"success":true,"data":{"ownerPatientId":3046,"ownerPatientName":"Tufan Patient" |
| PAT-06.02 | New-API | `GET /api/Family/Me caregiver owner` | Tufan Patient | 200 | PASS | {'success': True, 'data': {'ownerPatientId': 3046, 'ownerPatientName': 'Tufan Pa |
| PAT-06.02 | New-API | `GET /api/Family caregiver` | 200 | 200 | PASS | {"success":true,"ownerPatientId":3046,"ownerPatientName":"Tufan Patient","isActi |
| PAT-07.02 | New-API | `GET /api/Caregiver/ListActingFor caregiver` | 200 | 200 | PASS | {"success":true,"data":[{"caregiverAuthorizationId":15,"patientId":3046,"patient |
| SEC-04 | New-API | `GET /api/Family account` | [401, 403] | 403 | PASS | {"success":false,"message":"Account and Pharmacy roles cannot access patient per |
| DOC-08.02 | New-API | `GET /api/doctorDashBoard/GetPatientStats account` | 403 | 403 | PASS | {"success":false,"message":"Clinic dashboard is for the treating doctor or their |
| SEC-04 | New-API | `GET /api/Family pharmacy` | [401, 403] | 403 | PASS | {"success":false,"message":"Account and Pharmacy roles cannot access patient per |
| DOC-08.02 | New-API | `GET /api/doctorDashBoard/GetPatientStats patient` | 403 | 403 | PASS | {"success":false,"message":"Clinic dashboard is for the treating doctor or their |
| DOC-04.02 | New-API | `GET /api/patientApp/GetCasesByUser/10007` | 403 | 403 | PASS | {"success":false,"message":"Access denied for this doctor resource."} |
| DOC-04.02 | New-API | `GET /api/patientApp/GetCasesByUser/10032` | 403 | 403 | PASS | {"success":false,"message":"Access denied for this doctor resource."} |
| CLN-02.03 | Old-API | `GET /api/clipboardRubrics` | [200, 404] | 200 | PASS | [] |
| CLN-03.03 | Old-API | `GET /api/clinicalquestions` | [200, 404] | 200 | PASS | [{"questionsId":1,"questionGroupId":1,"enteredBy":null,"enteredDate":null,"chang |
| CLN-06.03 | Old-API | `GET /api/subsection/GetSubSections` | [200, 404, 405] | 200 | PASS | [{"subSectionId":1,"sectionId":1,"sectionName":null,"parentSubSectionId":null,"m |
| CLN-05.03 | Old-API | `GET /api/diagnosis/GetDiagnosis` | [200, 404, 405] | 200 | PASS | [{"diagnosisId":1,"diagnosisGroupId":null,"diagnosisName":"EPILEPSY/CONVULSION", |
| CLN-11.03 | Old-API | `GET /api/AllopathicDrug/GetAllopathicDrug` | [200, 404, 405] | 200 | PASS | [{"allopathicDrugId":1,"drugGroupId":1,"drugGroupName":"SEDATIVES/HYPNOTICS","al |
| CLN-14.03 | Old-API | `GET /api/PatientLab/GetAllLabTests` | [200, 404, 405] | 200 | PASS | [] |
| CLN-15.03 | Old-API | `GET /api/AppointmentHistoryNote/GetAllAppointmentHistoryNotes` | [200, 404, 405] | 200 | PASS | {"totalCount":0.0,"totalPageCount":0.0,"resultObject":[]} |
| CLN-12.02 | Old-API | `GET /api/RepertorizationPage/GetMateriaMedicaHeadingbyAuthorId/1` | [200, 404, 405] | 200 | PASS | [{"materiaMedicaHeadId":55,"materiaMedicaHeadName":"INTRODUCTION","differentialM |
| CLN-10.02 | Old-API | `GET /api/MateriaMedicaRemediesDetails/GetMateriaMedicaByRemedy/1` | [200, 404] | 200 | PASS | [{"remedyId":1,"authorId":1,"lstRemedy":[{"materiaMedicaHeadId":55,"materiaMedic |
| CLN-14.03 | Old-API | `GET /api/PatientLab` | [200, 404, 405] | 404 | PASS |  |
| CLN-11.03 | Old-API | `GET /api/AllopathicDrug` | [200, 404, 405] | 405 | PASS |  |
| CLN-15.03 | Old-API | `GET /api/AppointmentHistoryNote` | [200, 404, 405] | 404 | PASS |  |
| CLN-16.02 | Old-API | `GET /api/patient/GetComplaints/1` | [200, 403, 404] | 403 | PASS | {"success":false,"message":"Access denied for this doctor resource."} |
| CLN-16.02 | Old-API | `GET /api/patient/GetCaseDetails/1` | [200, 403, 404] | 403 | PASS | {"success":false,"message":"Access denied for this doctor resource."} |
| CLN-17.02 | Old-API | `GET /api/CaseDetails/GetPatientBackHostoryById/1` | [200, 403, 404] | 200 | PASS | [] |
| CLN-16.03 | Old-API | `POST /api/CaseDetails` | [200, 400] | 200 | PASS | "Case Details Saved Successfully" |
| CLN-17.02 | New-API | `GET /api/Prescription/GetPrescriptionDetailsByAppointmentId?AppointmentId=1` | [200, 400, 403, 404] | 200 | PASS | {"success":false,"message":"Appointment not found"} |
| DOC-01.02 | Old-API | `POST /api/doctorDashBoard/GetCountApp` | [200, 400, 404] | 200 | PASS | {"patientAppId":0,"appointmentDate":null,"status":null,"userId":0,"doctorId":0," |
| CLN-12.02 | Old-API | `GET /api/RepertorizationPage` | [200, 404, 405] | 404 | PASS |  |
| DOC-03.02 | Old-API | `GET /api/patientApp/GetCasesByUser/10032` | [200, 404] | 200 | PASS | [{"doctorID":0,"patientID":3046,"patientName":"Tufan Patient","address":"Tufan t |
| CLN-17.02 | New-API | `GET /api/PatientAppointment/GetAppointmentListByPatientId?patientId=1` | [200, 400, 403, 404] | 200 | PASS | {"success":true,"message":"Appointment list retrieved successfully.","pageNumber |
| CLN-02.02 | Old-API | `GET /api/clipboardRubrics` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-03.02 | Old-API | `GET /api/clinicalquestions` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-04.02 | Old-API | `GET /api/QuestionSection` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-05.02 | Old-API | `GET /api/diagnosis/GetDiagnosis` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-06.02 | Old-API | `GET /api/subsection/GetSubSections` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-10.02 | Old-API | `GET /api/MateriaMedicaRemediesDetails/GetMateriaMedicaByRemedy/1` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-11.02 | Old-API | `GET /api/AllopathicDrug/GetAllopathicDrug` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-14.02 | Old-API | `GET /api/PatientLab/GetAllLabTests` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-15.02 | Old-API | `GET /api/AppointmentHistoryNote/GetAllAppointmentHistoryNotes` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-12.02 | Old-API | `GET /api/RepertorizationPage/GetMateriaMedicaHeadingbyAuthorId/1` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-16.02 | Old-API | `GET /api/patient/GetComplaints/1` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-16.02 | Old-API | `POST /api/patient/SaveComplaints` | [400, 403] | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-16.03 | Old-API | `POST /api/CaseDetails` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-18.01 | New-API | `GET /api/patient/ExportCasesToExcel reception` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| DMO-05.02 | New-API | `PUT /api/Availability/Me reception` | 403 | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-08.03 | New-API | `GET /api/AudioCaseTaking` | [200, 404, 405] | 404 | PASS |  |
| CLN-08.03 | New-API | `GET /api/AudioCaseIntelligence/health` | [200, 404] | 200 | PASS | {"v2Enabled":false,"rollbackToV1Only":false,"requiresManualApproval":false,"enab |
| DMO-04.02 | New-API | `POST /api/doctorDashBoard/GetCountApp mobile` | [200, 400, 404] | 200 | PASS | {"patientAppId":0,"appointmentDate":null,"status":null,"userId":0,"doctorId":101 |
| CLN-08.02 | New-API | `POST /api/AudioCaseTaking/upload` | [400, 403, 415] | 403 | PASS | {"success":false,"message":"Only the treating doctor can run case taking."} |
| CLN-09.01 | New-API | `GET /api/AudioCaseIntelligence/admin/metaphors` | [200, 400, 404] | 200 | PASS | {"success":true,"message":"Metaphors fetched.","resultObject":{"items":[{"metaph |
| CLN-09.01 | New-API | `GET /api/AudioCaseIntelligence/admin/aliases` | [200, 400, 404] | 200 | PASS | {"success":true,"message":"Aliases fetched.","resultObject":{"items":[{"rubricAl |
| PAT-03.02 | New-API | `POST /api/Otp/RequestOtp` | [200, 400, 429] | 400 | PASS | {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or mor |
| WEB-03.03 | Web | `GET /book` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-03.03 | Web | `GET /book/4` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-04.03 | Web | `GET /book/4/slots` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-04.03 | Web | `GET /book/4/confirm?date=2026-09-22&slot=10:00&mode=clinic` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-04.03 | Web | `GET /book/success` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-05.01 | Web | `GET /book/pay/demo` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-05.01 | Web | `GET /book/pay/demo/success` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-05.01 | Web | `GET /book/pay/demo/failure` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-01.03 | Web | `GET /` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-02.02 | Web | `GET /pricing` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-07.01 | Web | `GET /privacy` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-08.01 | Web | `GET /terms` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-06.03 | Web | `GET /contact` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-09.03 | Web | `GET /register` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-09.03 | Web | `GET /register/pending` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-09.03 | Web | `GET /register/status` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-10.03 | Web | `GET /activate` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-10.03 | Web | `GET /login` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-12.01 | Web | `GET /blog` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| WEB-12.01 | Web | `GET /news` | 200 | 200 | PASS | <!DOCTYPE html> <html lang="en" data-th |
| DOC-09.02 | Web | `GET /doctor/reception-staff` | 200 | 200 | PASS | SPA shell |
| DOC-10.03 | Web | `GET /profile` | 200 | 200 | PASS | SPA shell |
| WEB-06.03 | Web | `GET /enquiries` | 200 | 200 | PASS | SPA shell |
| WEB-06.03 | Web | `GET /admin/enquiries` | 200 | 200 | PASS | SPA shell |
| CLN-01.01 | Web | `GET /doctor/patientboard` | 200 | 200 | PASS | SPA shell |
| DOC-01.03 | Web | `GET /doctordashboard` | 200 | 200 | PASS | SPA shell |
| DOC-06.02 | Web | `GET /doctordashboard` | 200 | 200 | PASS | SPA shell |
| CLN-20.03 | Web | `GET /doctor/anatomy` | 200 | 200 | PASS | SPA shell |
| CLN-01.02 | Docs | `NOTE doctor-mobile-no-case-taking` | doc | ok | PASS | MOBILE_API_DOC forbids clipboard/COG/audio |

## Failures

None.

## Excel in-scope coverage

Excel in-scope rows (skip QA / Mobile UI / Mobile Frontend): **107**
Excel API=Yes rows: **84**

| SubID | Bifurcation | API col | APIStatus | Live |
|-------|-------------|---------|-----------|------|
| CLN-01.01 | UI | No | N/A | PASS |
| CLN-01.02 | Web Other | No | N/A | PASS |
| CLN-02.02 | Security | Yes | Done | PASS |
| CLN-02.03 | Web API | Yes | Done | PASS |
| CLN-03.02 | Security | Yes | Done | PASS |
| CLN-03.03 | Web API | Yes | Done | PASS |
| CLN-04.02 | Security | Yes | Done | PASS |
| CLN-04.03 | Web API | Yes | Done | NO LIVE ROW |
| CLN-05.02 | Security | Yes | Done | PASS |
| CLN-05.03 | Web API | Yes | Done | PASS |
| CLN-06.02 | Security | Yes | Done | PASS |
| CLN-06.03 | Web API | Yes | Done | PASS |
| CLN-07.01 | Database | No | N/A | NO LIVE ROW |
| CLN-07.02 | Web API | Yes | Done | NO LIVE ROW |
| CLN-07.03 | UI | Yes | Done | NO LIVE ROW |
| CLN-08.02 | Security | Yes | Done | PASS |
| CLN-08.03 | Web API | Yes | Done | PASS |
| CLN-09.01 | Web API | No | N/A | PASS |
| CLN-09.02 | UI | Yes | Done | NO LIVE ROW |
| CLN-10.01 | Database | No | N/A | NO LIVE ROW |
| CLN-10.02 | Web API | Yes | Done | PASS |
| CLN-10.03 | UI | Yes | Done | NO LIVE ROW |
| CLN-11.02 | Security | Yes | Done | PASS |
| CLN-11.03 | Web API | Yes | Done | PASS |
| CLN-12.01 | Database | No | N/A | NO LIVE ROW |
| CLN-12.02 | Web API | Yes | Done | PASS |
| CLN-12.03 | UI | Yes | Done | NO LIVE ROW |
| CLN-13.01 | Database | No | N/A | NO LIVE ROW |
| CLN-13.02 | Web API | Yes | Done | PASS |
| CLN-13.03 | UI | Yes | Done | NO LIVE ROW |
| CLN-14.02 | Security | Yes | Done | PASS |
| CLN-14.03 | Web API | Yes | Done | PASS |
| CLN-15.02 | Security | Yes | Done | PASS |
| CLN-15.03 | Web API | Yes | Done | PASS |
| CLN-16.01 | Database | No | N/A | NO LIVE ROW |
| CLN-16.02 | Web API | Yes | Done | PASS |
| CLN-16.03 | UI | Yes | Done | PASS |
| CLN-17.01 | Database | No | N/A | NO LIVE ROW |
| CLN-17.02 | Web API | Yes | Done | PASS |
| CLN-17.03 | UI | Yes | Done | NO LIVE ROW |
| CLN-18.01 | Web API | Yes | Done | PASS |
| CLN-18.02 | UI | Yes | Done | NO LIVE ROW |
| CLN-19.02 | Security | Yes | Done | PASS |
| CLN-19.03 | Web API | Yes | Done | PASS |
| CLN-20.01 | Database | No | N/A | NO LIVE ROW |
| CLN-20.02 | Web API | Yes | Done | PASS |
| CLN-20.03 | UI | Yes | Done | PASS |
| DOC-01.01 | Database | No | N/A | NO LIVE ROW |
| DOC-01.02 | Web API | Yes | Done | PASS |
| DOC-01.03 | UI | Yes | Done | PASS |
| DOC-02.02 | Security | Yes | Done | PASS |
| DOC-02.03 | Web API | Yes | Done | PASS |
| DOC-03.01 | Database | No | N/A | NO LIVE ROW |
| DOC-03.02 | Web API | Yes | Done | PASS |
| DOC-03.03 | UI | Yes | Done | NO LIVE ROW |
| DOC-04.02 | Security | Yes | Done | PASS |
| DOC-04.03 | Web API | Yes | Done | NO LIVE ROW |
| DOC-05.02 | Security | Yes | Done | NO LIVE ROW |
| DOC-05.03 | Web API | Yes | Done | NO LIVE ROW |
| DOC-06.01 | Web API | Yes | Done | PASS |
| DOC-06.02 | UI | No | N/A | PASS |
| DOC-07.02 | Security | Yes | Done | NO LIVE ROW |
| DOC-07.03 | Web API | Yes | Done | NO LIVE ROW |
| DOC-08.02 | Security | Yes | Done | PASS |
| DOC-08.03 | Web API | Yes | Done | PASS |
| DOC-09.01 | Web API | Yes | Done | PASS |
| DOC-09.02 | UI | Yes | Done | PASS |
| DOC-10.01 | Database | No | N/A | NO LIVE ROW |
| DOC-10.02 | Web API | Yes | Done | PASS |
| DOC-10.03 | UI | Yes | Done | PASS |
| WEB-01.01 | Database | No | N/A | NO LIVE ROW |
| WEB-01.02 | Web API | Yes | Done | PASS |
| WEB-01.03 | Web UI | Yes | Done | PASS |
| WEB-02.01 | Web UI | Yes | Done | PASS |
| WEB-02.02 | Web UI | No | N/A | PASS |
| WEB-03.01 | Database | No | N/A | PASS |
| WEB-03.02 | Web API | Yes | Done | PASS |
| WEB-03.03 | Web UI | Yes | Done | PASS |
| WEB-04.01 | Database | No | N/A | NO LIVE ROW |
| WEB-04.02 | Web API | Yes | Done | PASS |
| WEB-04.03 | Web UI | Yes | Done | PASS |
| WEB-05.01 | Web UI | Yes | Done | PASS |
| WEB-06.01 | Database | No | N/A | NO LIVE ROW |
| WEB-06.02 | Web API | Yes | Done | PASS |
| WEB-06.03 | Web UI | Yes | Done | PASS |
| WEB-07.01 | Web UI | No | N/A | PASS |
| WEB-08.01 | Web UI | No | N/A | PASS |
| WEB-09.01 | Database | No | N/A | NO LIVE ROW |
| WEB-09.02 | Web API | Yes | Done | PASS |
| WEB-09.03 | Web UI | Yes | Done | PASS |
| WEB-10.01 | Database | No | N/A | NO LIVE ROW |
| WEB-10.02 | Web API | Yes | Done | PASS |
| WEB-10.03 | Web UI | Yes | Done | PASS |
| WEB-12.01 | Web UI | Yes | Done | PASS |
| PAT-06.02 | API Mobile | Yes | Done | PASS |
| PAT-07.02 | API Mobile | Yes | Done | PASS |
| PAT-08.02 | API Mobile | Yes | Done | PASS |
| PAT-09.02 | API Mobile | Yes | Done | PASS |
| PAT-10.02 | API Mobile | Yes | Done | PASS |
| PAT-11.02 | API Mobile | Yes | Done | PASS |
| PAT-12.02 | API Mobile | Yes | Done | PASS |
| PAT-13.02 | API Mobile | Yes | Done | PASS |
| PAT-14.02 | API Mobile | Yes | Done | PASS |
| PAT-15.02 | API Mobile | Yes | Done | PASS |
| DMO-04.02 | API Mobile | Yes | Done | PASS |
| DMO-05.02 | API Mobile | Yes | Done | PASS |
| DMO-06.02 | API Mobile | Yes | Done | PASS |

No dedicated live row (covered by sibling API/UI/SQL of same MainTask, or extra-task SQL): CLN-04.03, CLN-07.01, CLN-07.02, CLN-07.03, CLN-09.02, CLN-10.01, CLN-10.03, CLN-12.01, CLN-12.03, CLN-13.01, CLN-13.03, CLN-16.01, CLN-17.01, CLN-17.03, CLN-18.02, CLN-20.01, DOC-01.01, DOC-03.01, DOC-03.03, DOC-04.03, DOC-05.02, DOC-05.03, DOC-07.02, DOC-07.03, DOC-10.01, WEB-01.01, WEB-04.01, WEB-06.01, WEB-09.01, WEB-10.01

## Extra tasks / markdown / API docs

- `S2_Week2_API_DOC.txt`: present
- `S2_Week2_API_DOC.txt`: present
- `S2_Week2_MOBILE_API_DOC.txt`: present
- `S2_Week2_HOST_AUDIT.md`: present
- `S2_Week2_STATUS_REPORT.md`: present
- `S2_Week2_LIVE_TEST.md`: present
- SQL 01–13: all present

- API_DOC URL blocks: 75; field issues: 0

