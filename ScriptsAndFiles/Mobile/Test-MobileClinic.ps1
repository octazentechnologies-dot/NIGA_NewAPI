# Mobile clinic API smoke test

Run against a local NIGA_NewAPI process. Does not print tokens.

```powershell
$base = 'http://127.0.0.1:5198'
# POST /api/Account/Login with a doctor and a patient JSON body, then:
# GET  /api/Public/AppConfig                         anonymous
# GET  /api/Doctor/PracticeStats                     doctor token, expect 200
# GET  /api/Doctor/FollowUps                         doctor token, expect 200
# GET  /api/Doctor/Activity                          doctor token, expect 200
# GET  /api/Doctor/BookingRules                      doctor token, expect 200
# GET  /api/Availability/Me                          doctor token, gapMinutes null until saved
# GET  /api/MobileDoctor/Registration/CheckAvailability?userName=unused_name_example
# GET  /api/Patient/Visits                           patient token, consultFee from doctor row
# GET  /api/Patient/Progress                         patient token
# GET  /api/Reviews/Mine                             patient token
# GET  /api/Notifications/Preferences                either token
# GET  /api/Doctor/PracticeStats                     no token, expect 401
```

Pharmacy seller price fields are null because `PharmacyPartner` does not store price, ETA, or distance.
`yearsExperience`, `registrationValidUntil`, `issuingCouncil`, `adherencePercent`, and `punctualityPct` are null because those facts are not stored.
