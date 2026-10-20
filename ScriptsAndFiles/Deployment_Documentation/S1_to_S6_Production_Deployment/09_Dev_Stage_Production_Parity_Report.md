# 09 — Dev / Stage / Production Parity Report

| Check | Dev | Stage | Production | Result |
|-------|-----|-------|------------|--------|
| Portable parity markers | PASS | PASS | PASS | PARITY_VERIFIED |
| Table names | 290 | 290 | 290 | PARITY_VERIFIED |
| Column signatures | 2559 | 2559 | 2559 | PARITY_VERIFIED |

## Expected environment differences

- Patient/doctor/user/transactional **data** differs by environment (EXPECTED).
- Location master **row counts** may differ if seed volumes differ historically (schema present on all three).
- Index *names* for some system-generated PKs may differ while keys exist (EXPECTED_ENVIRONMENT_DIFFERENCE).
