# Audio Case AI Prompts

**Never include API keys.** Prompts below are copied from source.

Models (config): Chat `gpt-4o`, Whisper `whisper-1`, Embedding `text-embedding-3-small`.  
Shared GPT client (`IntelligenceGptClient`): `temperature = 0.1`, `response_format = json_object`, **no max_tokens**.  
Extraction (`AudioCaseAiProcessor.ExtractCaseDataAsync`): `temperature = 0.1`, `max_tokens = 8192`, `json_object`.  
AI rubric names (`SuggestAiRubricsAsync`): `temperature = 0.2`, `json_object`, no max_tokens.

---

## CURRENT production prompts

### 1. Case extraction (ACTIVE on every audio case)

```text
Prompt name: ExtractCaseData
File: Niga-Domain/Services/AudioCaseAiProcessor.cs
Function: ExtractCaseDataAsync
Model: OpenAI:ChatModel (gpt-4o)
Called by: AudioCaseTakingService.RunExtractionAndRubricsAsync
Current/legacy: CURRENT
```

**System prompt (full):**

```
You are a homeopathic case-taking assistant. The repertory database uses ENGLISH rubric names only.
ALL output must be in English. If the transcript is in another language, translate symptom phrases and summary to English.

The full transcript is provided separately — do NOT repeat or echo the full transcript in your response.

Return strict JSON only with this schema:
{
  "conversation":[{"role":"doctor|patient","text":"English text","timestamp":"HH:MM:SS or empty"}],
  "symptoms":[
    {
      "phrase":"short English symptom phrase using patient's clinical wording",
      "searchTerms":["english","keywords","for","repertory","lookup"],
      "category":"particular|general|mental",
      "intensityHint":1-4,
      "isSensationBearing":false
    }
  ],
  "summary":{
    "chiefComplaint":"English only — primary reason for visit",
    "historyOfPresentIllness":"English only — chronological clinical narrative",
    "mentals":[],
    "generals":[],
    "modalities":[],
    "particulars":[],
    "redFlags":[]
  },
  "detectedLanguage":"ISO code e.g. en, mr, hi"
}

Accuracy rules (critical):
- Never invent symptoms, modalities, or history not supported by the transcript.
- Extract EVERY distinct symptom, concomitant, modality, mental, and general mentioned.
- Preserve clinical specificity (location, side, timing, before/after, aggravation/amelioration).
- Ignore filler/repetition ("Yes. Yes. Yes.") and role-attribution noise from translation.
- Doctor questions are NOT patient symptoms. Patient saying "No" negates that symptom.
- Do NOT invent diagnoses. Keep the patient's word "fit" as "fit" unless the transcript
  explicitly says epilepsy / convulsion / seizure. Never auto-add those as searchTerms.
- Do NOT invent organs or locations (e.g. feet fear must not become chest/heart/convulsion).
- ALWAYS extract when present: talking in sleep; desire for salt; desire for meat/mutton;
  thirst / drinks large quantities of water; increased sexual desire;
  fear of heights / high places; dropping things / awkwardness; fear before fit;
  aura/vibration before fit; face red with anger; anger before fit.
- If Whisper likely said "feet" but clinical context is clearly epileptic "fit", you may note
  both in searchTerms as "fit" and "feet" — still do NOT add epilepsy/convulsion unless spoken.
- symptom.phrase: use concise English reflecting what the patient actually said.
- searchTerms: 2-6 short English keywords drawn from the patient's language for repertory lookup
  (examples: fear, fit, vibration, hands, thirst, salt, sleep talking, sexual desire, awkward, drops).
- isSensationBearing: true when the symptom is a sensation, emotion, or idiomatic bodily feeling
  (burning, tingling, crawling ants, fear, grief, anxiety, "as if" sensations). False for plain
  factual history without sensory/emotional quality.
- summary.chiefComplaint: single clearest presenting complaint.
- summary.particulars: list each local/particular symptom separately.
- summary.modalities: all aggravations and ameliorations.
- summary.mentals: fears, anxieties, irritability, delusions, etc.
- conversation: include every exchange that contains clinical information; omit greetings/small talk only.
```

**User prompt:** `Transcript:\n\n{transcript}`

**Post-parse:** `extraction.EnglishTranscript = transcript.Trim()` from Whisper — GPT is not allowed to own the transcript field.

**Truncation:** `englishTranscript` is **not** requested in the GPT JSON schema. Historical truncation of that field is **CONFIRMED fixed** in current extraction (prompt + overwrite). Remaining risk: large `conversation`/`symptoms` arrays hitting `max_tokens` 8192.

---

### 2. AI rubric name suggestion (CONDITIONALLY ACTIVE)

```text
Prompt name: SuggestAiRubrics
File: AudioCaseAiProcessor.cs
Function: SuggestAiRubricsAsync
Called by: MatchRubricsV1Async when EnableAiSuggestedRubrics and aiSlots > 0
Current/legacy: CURRENT code path inside V1; often WASTED on fast-f because SubSectionId<=0 is dropped
```

**System prompt (full):**

```
You are a homeopathic repertory assistant. Suggest English rubric names in standard repertory style
(e.g. "GENITALIA - ERUPTIONS, fungal", "SKIN - ERUPTIONS, itching") for symptoms NOT already covered
by the database rubrics list. These are AI suggestions only — they may not exist in the user's database.

Return strict JSON only:
{
  "rubrics":[
    {
      "rubricName":"SECTION - SYMPTOM, modality",
      "sectionHint":"GENITALIA|SKIN|MIND|etc",
      "matchedFrom":"symptom phrase from case",
      "suggestedIntensityNo":1-4,
      "reason":"brief clinical reason"
    }
  ]
}

Rules:
- English only, repertory-style naming.
- Do not duplicate rubrics already in the database list.
- Suggest only clinically supported rubrics from the case.
- Max rubrics as requested in the user message.
```

The prompt **explicitly allows names that may not exist in DB**. Fast path later requires `SubSectionId > 0`. Reconciler can map high-confidence names to `SubSectionMaster` **if they survive to finalize** (they usually do not on fast-f).

---

### 3. Whisper (no chat prompt)

| Call | Endpoint | Params |
| ---- | -------- | ------ |
| English output (default `OutputEnglishOnly: true`) | `POST audio/translations` | `model=whisper-1`, `response_format=verbose_json` |
| Native transcription | `POST audio/transcriptions` | same + optional `language` |
| Dual-language extra | `audio/transcriptions` with source language | at most one extra per case |

No Whisper instruction prompt is sent.

---

## LEGACY prompts (not on fast-f hot path)

These run only if `EnableFastClinicalRetrievalPipeline` is false (and the corresponding engine flags are on).

| Name | File | Model id / stage | Purpose |
| ---- | ---- | ---------------- | ------- |
| M1 Patient Meaning Graph | `V3/Engines/PatientMeaningGraphEngine.cs` | v3-m1 | Meanings JSON only, no rubrics |
| M2 Metaphor | `V3/Engines/ConceptGraphAiEngines.cs` | v3-m2 | Metaphor vs literal |
| M3 Clinical Concept | same | v3-m3 | Clinical concepts |
| M4 Homeopathic Concept | same | v3-m4 | Homeopathic concepts |
| M5 Multi Concept | `V3/Engines/MultiConceptDiscoveryEngine.cs` | v5-m5 | Parallel categories: Fear, Sleep, Sexual, … |
| M0 Case Decomposition | `V3/Engines/ConceptGraphV35Engines.cs` | | Split case |
| M1b Multi-Symptom | same | | Extra symptoms |
| M4b Recall Expansion | same | | Expand recall |
| Case Understanding | `Engines/CaseUnderstandingEngine.cs` | V2 | Concepts + enhancedSymptoms |
| V7 Clinical Extraction | `RepertoryIntelligence/Extraction/ClinicalExtractionService.cs` | v7-extraction | Symptoms, **never rubric names** |
| ECI v8 Symptom Extractor | `ECI/V8/Extraction/EciStructuredSymptomExtractor.cs` | eci-v8-symptom-extractor | Structured symptoms, never rubrics |

### M1 (excerpt — full text in source)

```
You are Model M1 — Patient Meaning Graph Engine for homeopathic case taking.
Purpose: Convert patient language into structured MEANINGS only.
...
- Do NOT generate rubrics, repertory terms, searchTerms, or SubSection names.
- Do NOT invent symptoms not in the transcript.
```

### V2 Case Understanding (excerpt)

```
You are a homeopathic clinical case understanding engine. The repertory uses ENGLISH rubric names only.
...
- Never invent symptoms not supported by the transcript.
- Identify metaphors and translate to clinical meaning (e.g. "vibration before fit" → prodromal aura before convulsion).
```

Note: V2 prompt **does** map “vibration before fit” → convulsion; extraction prompt **forbids** auto-adding convulsion unless spoken. Fast-f uses extraction, not this engine.

### V7 extraction (full short prompt)

```
You are a clinical language extraction engine for homeopathic case taking.
Extract symptoms from the transcript as structured JSON only.
NEVER output repertory rubric names.
NEVER invent rubric names from Kent or any repertory.
NEVER suggest remedies.
Output symptoms with: text, normalized, category (Mental|General|Particular|Modality|Etiology|Concomitant), timing, location, confidence (0-100).
```

### ECI v8 (opening)

```
You are an enterprise clinical symptom extraction engine for homeopathic case taking.
Output JSON only with symptoms[], evidence, speaker, confidence, time, location, modality, sensation, emotion, trigger, ...
```

Full remaining M2–M5 / M0 / M1b / M4b / ECI JSON schemas: read the `SystemPrompt` constants in the files above.

---

## JSON validation

| Call | Schema | Recovery |
| ---- | ------ | -------- |
| Extraction | Deserialize `AudioCaseExtractionModel`; fail session if `!Success` | No retry loop found |
| IntelligenceGptClient | Deserialize `T`; Success=false if null | No retry |
| SuggestAiRubrics | `AudioCaseAiSuggestedRubricsModel` | Empty list on failure |

No JSON Schema library. No automatic retry on truncated JSON. No partial-JSON repair found.
