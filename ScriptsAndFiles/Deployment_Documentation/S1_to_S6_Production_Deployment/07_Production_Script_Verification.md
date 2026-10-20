# 07 — Production Script Verification

## Independent checks (post-deploy)

| Check | Result |
|-------|--------|
| `@@SERVERNAME` / `DB_NAME()` | DESKTOP-I7AMAS6 / HomeoCentrum_Production |
| User table count | 290 |
| Portable pack `005_Verify_Parity_Markers.sql` | **PASS** (0 missing) |
| Column signature vs Dev | 2559 = 2559 |
| Table set vs Dev | identical |
| Table set vs Stage | identical |

S6 `05_Verify_S6_Objects.sql` executed in-run with exit 0.
