# 08 — Dev / Stage / Production Schema Comparison

**Last verified:** 2026-11-01

| Kind | Dev | Stage | Production |
|------|----:|------:|-----------:|
| Tables | 293 | 293 | 293 |
| Column signatures | 2607 | 2607 | 2607 |
| Indexes | 491 | 491 | 491 |

Missing-in-Stage vs Dev: **none**  
Missing-in-Production vs Dev: **none** (applied `07_PushNotificationOutbox.sql` on 2026-11-01)  
Extra-in-Production vs Dev: **none**

Business row data intentionally not synchronized.
