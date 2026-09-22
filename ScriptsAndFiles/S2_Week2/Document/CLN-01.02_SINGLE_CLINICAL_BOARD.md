# CLN-01.02 — One clinical board; no case-taking on doctor mobile

Canonical for UI readers: `NIGAHomeopathy_UI/docs/CLN-01.02_SINGLE_CLINICAL_BOARD.md`.

Case taking is one web Patient Board. `/patientboard` is a legacy alias of `/doctor/patientboard`, not a second app. Doctor mobile must not call clipboard, Center of Gravity, audio mutate, lab mutate, or Rx-write. There is no doctor mobile app in this workspace.

Server `[DoctorOnly]` blocks Reception/Patient JWTs. It does not distinguish phone vs browser for a Doctor JWT — do not ship those screens in the doctor app.
