# Stage S6 rollback notes

**Preferred recovery:** restore from the pre-S6 Stage full backup (path/time to be recorded at deploy time).

S6 scripts are additive (CREATE TABLE IF NOT EXISTS / ADD COLUMN IF NULL). Manual reverse DDL is only for emergency and must be reviewed:

| Object | Reverse risk |
|--------|----------------|
| dbo.SmsOtpVerify | DROP TABLE only if empty and unused |
| dbo.FirebaseNotificationLog | DROP TABLE only if empty |
| DevicePushToken.DeviceType | DROP COLUMN breaks clients writing DeviceType |
| CareLinkRequest / CareLinkInfoRequest | DROP only if empty |
| AppNotification.Type / DeepLink | DROP COLUMN may break app notification payloads |
| InstantConsultRequest.PatientAppId / TeleSessionId | DROP COLUMN may break instant consult accept flow |

Never DROP on Production. Never DROP with residual rows without archival.
