# Voice Copilot product specification

Status: D01 review candidate. This document defines required behavior, not
verified compatibility or completed implementation. D02 must produce technical
probe evidence; D08 must approve the complete design before production work.
Epic D is [issue #1](https://github.com/vojtadtenis-personal/voice-copilot/issues/1);
D01 is [issue #2](https://github.com/vojtadtenis-personal/voice-copilot/issues/2).

The final product decisions and parent-supplied approved 44-ticket extract are
the baseline. Their final free-only policy supersedes earlier cost assumptions;
voice AND manual transcript scrolling supersede the earlier MVP exclusion.
[backlog.md](backlog.md) owns ticket dependencies and acceptance gates. Requirement
IDs below are stable: amend their text when necessary, never recycle an ID.

## Purpose and release boundaries

A Windows desktop front end lets a user operate real Copilot SDK coding sessions
by continuous English voice input and independently by mouse and keyboard.
It presents terminal-style output without embedding the Copilot TUI.

| Boundary | Required outcome |
| --- | --- |
| Design baseline, D01-D08 | Requirements, verified feasibility, architecture, behavior, UI, tests and delivery reviewed before production implementation. |
| Internal MVP, M01-M17 | Packaged real create/select/dictate/finish/review/execute/stop loop; normal prompt editing and submission; concurrent sessions, basic saved-session access, recovery, voice AND manual scrolling. Not the first complete v1 release. |
| Mandatory complete v1, V01-V08 | MVP plus file references through all required inputs, coding-model switching, dictionary and complete input/settings parity. These cannot move to optional E tickets. |
| Release qualification, R01-R05 | Reliability, performance, migration, installation and exact-artifact qualification; publication of the first complete product. Testing already applies throughout M and V. |
| Optional follow-ons, E01-E06 | Only after R05, each with its own specification and tests. Never replace, delay or expand the mandatory initial product implicitly. |

## Glossary

| Term | Meaning |
| --- | --- |
| Session ID / session number | Permanent persisted identity / user-facing number of one coding conversation. Neither changes when tabs move or close. |
| Tab number | Current positional number among OPEN tabs, not a permanent identity. Resolve it to a session ID before asynchronous work. |
| Saved session | History record that may be opened, resumed or selected without automatic prompt execution. |
| Working folder | Neutral, persistent per-session execution location; no mandatory project/entity abstraction. |
| Draft | Unsent text, revisions and attachment references owned by one session. |
| Dictation target | Stable session ID bound when capture begins; independent of later active-tab changes. |
| Command mode | Speech interpreted by a tool-free cloud model against the app action catalog. |
| Prompt mode | Literal dictated prompt text with only intended-ending detection, not general action interpretation. |
| Ending / explicit submission | Ending stops prompt capture for review; submission is a separate actual user instruction to send. Both may occur in the same utterance. |
| Cleanup | Light correction of speech-owned text, not task rewriting or permission to submit. |
| Turn / busy | One coding execution / session with an in-flight turn, independently of other sessions. |
| Effective / pending model | Model actually used / requested model deferred until the current turn finishes. |
| Tool-free inference | Separate control, ending or cleanup context with no repository, shell or coding-session tools. |
| Independent review | Evidence from a reviewer other than the PR author, tied to the current candidate; not fabricated self-approval. |

## Required behavior

### Desktop and sessions

**VC-001 - Product surface (MVP).** Implement C#/.NET WPF with a terminal-style
UI and actual Copilot SDK integration. Use tabs resembling Windows Terminal,
not an embedded TUI or a fake production conversation adapter.

**VC-002 - Identity and tabs (MVP).** Session IDs and numbers are permanent.
Tab numbers are positional among open tabs; opening, reordering and closing
updates positions without renumbering sessions. Display/resolve the distinction
and reject stale positional targets.

**VC-003 - Session lifecycle (MVP).** Create, open, resume, select, rename and
close sessions; preserve saved sessions in history and basic access to them.
Opening/resuming restores conversation access, never pending execution.
No mandatory project/entity selection or organization step is required.

**VC-004 - Working folders (MVP).** Every session has a neutral persistent
working folder, not a transient current-tab or project-derived location.
Creation, selection and restoration keep its association explicit; D03 defines
ownership and safe defaults using D02 evidence.

**VC-005 - Concurrent execution and closing (MVP).** Multiple sessions may run
concurrently. Closing a busy tab aborts its current turn and preserves its
history and draft. Stop/close races and late callbacks must not corrupt another
session or restart the closed turn.

**VC-006 - First-class normal input (MVP, completed in v1).** Every feature has
usable mouse and keyboard operation: prompts, session lifecycle, attachments,
models, dictionary, settings and mic controls. Users can type, edit and submit
without speech/cloud-control inference. Typed composer text is prompt text,
not app commands. Voice and normal inputs call one shared action backend.

**VC-007 - Focus and editor behavior (MVP, audited in v1).** Support ordinary
focus, text selection, copy/paste, undo/redo and scrolling. Streaming, capture,
background sessions, cleanup, status updates and failures must not steal focus
or overwrite selection/newer input. Design keyboard bindings and accessibility
IDs in D05, and audit complete parity in V07.

### Listening and interpretation

**VC-008 - Local speech recognition (MVP).** Prefer local English-only ASR that
can run on CPU. Continuously process captured speech with device handling,
VAD/pre-roll, bounded buffers and explicit initialization/failure. Pin and
integrity-check chosen assets. D02 selects verified versions and supported
resources; do not assume a GPU, RAM capacity or universal performance.

**VC-009 - Startup and listening policy (MVP).** Every launch starts LISTENING
in COMMAND mode, regardless of the previous mute/mode state. Global listening
when unmuted is the default; a persisted foreground-only setting is an
alternative. Never persist or restore mute state. Unavailable microphone/ASR
must be shown as a real failure, not falsely shown as usable listening.

**VC-010 - Local controls and silence (MVP).** Local mute and emergency
stop/abort controls work independently of cloud availability. No wake phrase,
spoken feedback, TTS or audio notifications. Silence is not a command, an ending
that authorizes submission, or a restart trigger.

**VC-011 - Command interpretation (MVP).** In command mode, a tool-free cloud
LLM uses a bounded app action catalog to infer validated ORDERED actions.
Natural intent, not a fixed phrase whitelist, controls sessions, draft
entry/exit/basic editing, explicit submit, stop and scrolling; v1 extends the
catalog for its mandatory features. An unresolved app command/target produces
a visible nonblocking failure, not invented targets or an interview.

**VC-012 - Literal prompt mode and endings (MVP).** Prompt mode does NOT run
the general command interpreter. Only capture literal text and detect an
intended ending. Other apparent commands are text; quoted/reported endings and
negated ending intent remain text. "Start prompt" and "end prompt" are examples,
not mandatory strings. Ending detection may use a narrow LLM schema WITHOUT
the action catalog; it must not reinterpret the prompt as general commands.

**VC-013 - Compound speech and explicit execution (MVP).** One utterance may
cross commands -> prompt -> commands using ordered spans, including an intended
end AND explicit execute instruction in that same utterance. Ending alone
never submits. Only actual user voice, keyboard or click instruction may
authorize sending; silence, cleanup completion, retries and restart do not.

**VC-014 - Stable targets and stale work (MVP).** Bind dictation to its stable
target session. Switching tabs does not redirect captured text, cleanup,
scrolling or queued execution. Resolve spoken positional references at the
defined action boundary and reject stale/closed targets, superseded inference,
duplicate utterances and delayed callbacks rather than acting elsewhere.

### Drafts and execution

**VC-015 - Append and explicit editing (MVP).** Dictation APPENDS to an existing
unsent draft. Explicit clear/replace supports undo. Preserve original dictated
text and normal manual editing/undo/redo; do not silently replace a draft.

**VC-016 - Light semantic-preserving cleanup (MVP).** Automatically clean only
dictated portions: fillers, hesitations, repetition, spelling, punctuation and
domain normalization. Never rewrite/restructure quantities, negations,
identifiers, paths or requirements. Preserve uncertain words rather than
fabricating replacements. The user may review the draft before submitting.

**VC-017 - Revision reconciliation (MVP).** Track draft revisions and
speech-owned changes. Delayed cleanup never overwrites manually typed content
or newer edits; reconcile against the applicable revision or retire stale work
with an explicit outcome. Keep original text/undo available, including when
cleanup fails or overlaps manual edits.

**VC-018 - Finalize, queue and correlate sends (MVP).** Enter/Send during active
dictation first finalizes capture and applicable cleanup, then sends only under
that explicit instruction. Cleanup failure is visible and never becomes
submission authorization. Explicit sends to a busy session are queued in order
with stable draft/message correlation. Distinguish acknowledged, rejected and
uncertain delivery; never blindly retry a prompt that might already be accepted.

**VC-019 - Coding models (base inheritance in MVP, switching in v1).** First
session uses the configured default; new sessions inherit the prior active
session's coding-model choice. Persist per-session choices. Offer
mouse/keyboard/voice selection with effective versus pending status. Busy
changes apply after the current turn and before the next applicable queued send;
failed changes are not shown as effective. Sessions and control-model fallback
are isolated. D04 specifies ordering if the inherited choice is pending.

**VC-020 - Isolated control inference (MVP).** Command control, ending and
cleanup contexts are SEPARATE and TOOL-FREE, without repository/shell access.
Discover available models; prefer an available compatible Haiku, otherwise
automatically try compatible alternatives and make fallback visible in UI.
Bound each context and validate output schema. Ending context has no action
catalog. Exhausted alternatives/timeouts are failures, not invented success.

**VC-021 - AI usage and authorization (MVP).** Track coding, control, ending
and cleanup usage separately; no hard app budget initially and no guessed
account allowance amounts. Real tests may use the explicitly authorized
existing Copilot account allowance outside chat. Authorized conversation-agent
credits do not authorize extra purchases. Never buy capacity, enable paid
overage or create new charges; report exhausted allowance/authorization blocks.

**VC-022 - Coding runtime authority (MVP).** Coding sessions use actual SDK
autopilot and allow-all, without app approval prompts. Enforced organization,
runtime, credential and consent restrictions cannot be bypassed: surface
refusal/blocked/errors accurately. Coding permissions never grant tools to
control, ending or cleanup contexts.

**VC-023 - Task ownership (MVP).** Copilot owns task reasoning and questions.
The app forwards prompts and displays responses; it does not add task
clarification, generated forms, or an app interview. App-command failures
remain separate from Copilot's conversation.

### Mandatory v1 additions

**VC-024 - Attachment references and normal inputs (v1).** Support picker,
drag/drop and clipboard files/images, reference removal and draft association.
Forward references through the SDK; adding references never submits. No app
OCR, document parser, file whitelist or content analysis. The runtime owns
supported formats; disclose tested limitations and missing-reference failures,
not guessed compatibility. Do not copy or modify original user files.

**VC-025 - Clipboard image ownership (v1).** A raw clipboard bitmap has no path:
save it as an app-owned image and forward that path. Persist unsent-draft
references and retain referenced clipboard assets across restart. Cleanup
deletes only unreferenced app-owned assets, never user originals.

**VC-026 - Voice attachments (v1).** Accept explicit spoken paths or
known-folder names/relative references, including ordered add/remove and
explicit submit. Do not guess across the disk. Resolve against a stable session
and known location or fail nonblockingly. Attachment-like speech in prompt
mode remains literal text unless it follows a genuine mode ending.

**VC-027 - Dictionary editing and data (v1).** Provide global entries and
per-session overrides, normal editor and versioned import. Entries include
canonical names, aliases, acronyms and short meanings; define validation,
collision/override precedence and atomic import/persistence. Entries are data,
not instructions. No local-only exclusion feature is required.

**VC-028 - Dictionary integration (v1).** Use hints where the verified ASR
supports them and bounded dictionary context for cloud interpretation/cleanup.
Prove domain-term recognition and semantic preservation using owned/synthetic
recordings and real model evaluation, not just adapter argument tests.

### Persistence, quality and delivery

**VC-029 - Passive persistence (MVP, extended in v1).** Persist stable IDs,
session/tab order, saved history, drafts/revisions, references, dictionary,
working folders, settings and model choices with versioned atomic writes.
Do NOT persist mute state or raw audio. Restoration does not autoreplay audio,
autosend drafts, execute queues or restart tasks. Preserve referenced assets
and user data across migrations and interrupted writes/upgrades.

**VC-030 - Bounded recovery (MVP, hardened for release).** Bound audio/text
buffers, inference contexts and dispatch backlogs; expose overload, mic loss,
cloud/SDK outage, invalid output, policy refusal and persistence errors.
Cancel/retire stale work and prevent duplicates. Keep normal UI and local
mute/stop responsive; no broad catch that pretends failure succeeded.

**VC-031 - Transcript scrolling (MVP).** Voice in command mode AND normal
mouse/keyboard scrolling support up/down and return-to-latest. Preserve
user scroll position while output streams; do not force bottom anchoring.
Use the intended stable session, without focus theft. Scroll-like speech in
prompt mode is text.

**VC-032 - Clean architecture (design and all implementation).** Use
Core/Application/Infrastructure/Desktop dependency direction: Core independent,
Application depends on Core, outer adapters depend inward, and only the
composition root wires concrete dependencies. Core/Application do not depend
on WPF or SDK. Favor cohesive encapsulated objects, immutable values and
composition; one shared use-case backend, practical test seams, no speculative
interfaces, duplicate implementations or unnecessary comments.

**VC-033 - Automated verification (all tickets).** Appropriate focused tests
and failure cases accompany every behavior change. Critical invariants use
transition/property tests and focused mutation testing; target at least 90%
Core/Application branch coverage for meaningful handwritten logic, without
trivial tests, excluding real logic or weakening cases to fake metrics.
Separate deterministic fakes/replay from actual ASR/SDK/model and packaged
unattended Windows UI qualification. No routine human listening/clickthrough.

**VC-034 - Evidence before technical claims (design through release).** D02
verifies selected .NET/WPF, released SDK/runtime pairing, authentication,
create/resume, streaming/completion, abort, autopilot/permissions, model listing,
tool-free inference, local ASR and unattended Windows UI environment. Record
versions, reproducible probes, platforms, measured resources, limitations and
alternatives. No unrun probe, fake or missing credential is a successful test.

**VC-035 - Reviewed autonomous delivery (all tickets).** Use isolated ticket
worktrees, feature branches and linked PRs; README is the only main bootstrap.
D08 is a prerequisite to production. Require passing applicable CI AND
independent review evidence/check for the current candidate; authors cannot
approve their own PR. Personal-account delivery uses serialized merges and
latest-base validation, not merge queue. Design credential/retry/conflict/stale
review/duplicate-dispatch handling in D07; unavailable external authority is a
block, not routine human intervention or a bypass. Record default-token
workflow event-trigger limitations and a free authorized solution in D07.

**VC-036 - Free-only infrastructure (all tickets).** Only this authorized
PUBLIC repository and standard free public GitHub runners. No purchases, paid
services, new charges, overages, Actions artifact upload/download, Actions
cache or implicit caching, Packages/GHCR, LFS, Codespaces, larger/private
runners, paid signing or paid cloud infrastructure. CI evidence uses logs/job
summaries; cross-job builds rebuild or keep work in one job, without Actions
artifact handoff. If a selected feature may incur non-AI charges, STOP and
notify the execution coordinator before activation.

**VC-037 - Qualified free distribution (MVP and release).** Use free GitHub
Releases within documented asset limits, not Actions storage. D07/R04 must
record current release-asset limits before publication; D01 invents no numbers.
Produce reproducible Windows packages with versions, native/model acquisition,
checksums, licenses and diagnostics. Label unsigned packages honestly; no paid
signing or fabricated provenance. Validate clean installation/startup without
checkout/dev SDK, upgrades and the exact release commit/artifact using authorized
live integration. Publication must succeed before mandatory epics close.

**VC-038 - Completion boundaries (all gates).** Internal MVP is not complete
v1. V08 requires attachments, models, dictionary, settings/input parity and
their combined journeys. M17/V08/R05 require requirement/test/artifact evidence,
no unfinished production stubs, hidden success defaults, critical defects or
unverified mandatory integrations. R05 closes the mandatory product only after
qualified publication; optional work follows, never substitutes.

### Optional follow-ons, not initial-release requirements

**VC-039 - Advanced voice editing (E01).** Precisely edit/spell selected
fragments with undo; no implicit rewrite or submit.

**VC-040 - Rich history navigation (E02).** Add scalable search/filter/navigation
without changing stable identities or causing unintended execution.

**VC-041 - Rich dictionary exchange (E03).** Add export/sharing/richer import
with versioning/conflict handling; entries remain data.

**VC-042 - Optional cloud ASR (E04).** Only a verified free option is permissible;
no paid provider, new charge or silent switch. Existing AI allowance is not
permission to buy another service.

**VC-043 - Optional ASR profiles (E05).** Add acceleration/resource tuning only
with measured benefit, CPU fallback and bounded consumption.

**VC-044 - Optional secure updates (E06).** Free provenance/integrity and release
channels, safe interrupted updates and retained data; no paid signing/services.

## Non-goals and exclusions

No embedded TUI, project/entity management requirement, non-English ASR
requirement, required GPU, wake phrase, TTS/audio notifications, local-only
dictionary exclusions, app document processing, arbitrary prompt-mode command
handling, app task-clarification layer, silent submission or automatic restart
execution. No speculative orchestration framework, paid infrastructure,
self-approved PRs, merge queue assumption, estimates, sprint schedules or
routine manual operating/validation steps. Optional cloud ASR and acceleration
are not substitutes for the initial CPU-capable local English path.

## Planned test catalog

These are stable acceptance-suite IDs, NOT claims that tests exist or passed.
D06 specifies fixtures/assertions and live environments; implementation tickets
add executable tests and the gates collect exact evidence.

| Test ID | Planned verification |
| --- | --- |
| T01 | Session IDs, positional tabs, lifecycle/history, working folders and inheritance. |
| T02 | WPF view-model/STA and unattended UI capability matrix; editing, keyboard/mouse, focus and accessibility. |
| T03 | Actual local ASR onset, pauses, long speech, silence/noise, devices, CPU resources and asset integrity. |
| T04 | Startup command/listening, foreground setting, no mute restore/audio output and cloud-independent local controls. |
| T05 | Annotated natural/compound utterances, literal prompt mode, quoted/reported/negated endings and explicit execution. |
| T06 | Stable speech/action targets, stale tabs, delayed/superseded output and duplicate prevention. |
| T07 | Append, clear/replace/undo, tracked speech edits and manual edit/cleanup revision races. |
| T08 | Cleanup semantic preservation, uncertain terms, original text and visible failure. |
| T09 | Actual SDK protocol plus deterministic dispatch/acknowledgement, finalize/send, uncertainty, queues, concurrency and abort. |
| T10 | Default/inherited/effective/pending coding models, busy changes, queued sends and failures. |
| T11 | Model discovery, Haiku/fallback visibility, bounded validated separate tool-free contexts and absent ending catalog. |
| T12 | Actual authorization/permissions/refusal, task ownership, separated usage and no purchased capacity/overage. |
| T13 | SDK file/image references, picker/drop/clipboard/voice paths, ownership, missing refs and restart. |
| T14 | Dictionary editor/import/overrides, Unicode, data isolation and actual domain-recording evaluation. |
| T15 | Atomic writes/migrations/interruption; passive history/draft/reference restore, safe asset ownership and no replay. |
| T16 | Fault injection, bounded queues/buffers, cloud/local latency, soak, responsiveness and resource ownership. |
| T17 | Voice/manual scrolling, anchoring, return-to-latest and prompt-mode/focus isolation. |
| T18 | Dependency/analyzer checks, architecture walkthroughs, branch coverage and focused transition/property/mutation tests. |
| T19 | Reproducible D02 probes and actual packaged ASR/SDK/inference/unattended UI integration, labeled separately from fakes. |
| T20 | Delivery simulations, independent review/current base, event triggers, free-only config, package/install/release integrity. |
| T21 | MVP/v1/release combined journeys and complete requirement-to-exact-artifact evidence. |
| T22 | E01 selected-fragment editing, undo and no implicit rewrite/submit. |
| T23 | E02 history identity, navigation scale and no execution. |
| T24 | E03 dictionary exchange/version/conflicts and data isolation. |
| T25 | E04 verified free service, explicit switch and ASR adapter behavior. |
| T26 | E05 measured resource benefit, bounds and CPU fallback. |
| T27 | E06 free integrity/provenance, interrupted update and retained data. |

## Requirement traceability

Ticket lists include the primary design/implementation/evidence owners; shared
D06 test planning, D08 design review and per-ticket quality rules apply to all
mandatory rows. Tests remain planned until their linked evidence is produced.

| Requirement | Tickets | Tests |
| --- | --- | --- |
| VC-001 | D02, D03, M01, M05, M07, M16 | T02, T09, T18, T19 |
| VC-002 | D04, D05, M03, M07, M10 | T01, T06 |
| VC-003 | D04, D05, M03, M05, M07, M14 | T01, T02, T09, T15 |
| VC-004 | D03, M03, M04, R03 | T01, T15 |
| VC-005 | D04, M06, M14, R01 | T06, T09, T16 |
| VC-006 | D05, M07, M13, V02, V04, V05, V07 | T02, T21 |
| VC-007 | D05, M04, M07, M12, V07 | T02, T07, T17 |
| VC-008 | D02, M08, M16, R02 | T03, T16, T19 |
| VC-009 | D04, M13, M14 | T04, T15 |
| VC-010 | D04, M13, M14, R02 | T04, T16 |
| VC-011 | D04, M09, M10, V03, V04, V07 | T05, T06, T11 |
| VC-012 | D04, M09, M10, M16 | T05, T11, T19 |
| VC-013 | D04, M10, M13, M16 | T05, T09, T21 |
| VC-014 | D04, M03, M06, M10, M12, R01 | T06, T09, T17 |
| VC-015 | D04, M04, M07, M11 | T02, T07 |
| VC-016 | D04, M11, V06 | T08, T14 |
| VC-017 | D04, M04, M11, M14 | T07, T08, T15 |
| VC-018 | D04, M06, M11, M13, R01 | T07, T09, T16 |
| VC-019 | D04, M03, V04, V07 | T01, T10 |
| VC-020 | D02, D03, M09, M10, M11, R01 | T11, T12, T19 |
| VC-021 | D02, D07, M05, M09, M16, R05 | T12, T19, T20 |
| VC-022 | D02, D03, M05, M09, R01 | T09, T11, T12 |
| VC-023 | D04, M05, M07, M10, V03 | T02, T05, T12 |
| VC-024 | D02, D04, V01, V02, V08 | T13, T19, T21 |
| VC-025 | D03, V02, R03 | T13, T15 |
| VC-026 | D04, V03, V07 | T05, T06, T13 |
| VC-027 | D03, D05, V05, V07 | T02, T14, T15 |
| VC-028 | D02, V06, V08 | T08, T14, T19 |
| VC-029 | D03, D04, M04, M14, V02, V04, V05, R03 | T04, T10, T13, T14, T15 |
| VC-030 | D04, M06, M08, M09, M14, R01, R02 | T06, T09, T11, T15, T16 |
| VC-031 | D05, M10, M12, M16 | T02, T05, T17 |
| VC-032 | D03, D08, M01, M03, M17, R01 | T18 |
| VC-033 | D06, M01, M16, M17, V08, R05 | T18, T19, T21 |
| VC-034 | D02, D06, D08, M05, M16, R05 | T03, T09, T11, T19 |
| VC-035 | D07, D08, M02, M17, R05 | T20, T21 |
| VC-036 | D07, M01, M02, M15, R04 | T12, T20 |
| VC-037 | D07, M15, M16, R03, R04, R05 | T15, T19, T20, T21 |
| VC-038 | D01, D08, M17, V08, R05 | T18, T20, T21 |
| VC-039 | E01 | T22 |
| VC-040 | E02 | T23 |
| VC-041 | E03 | T24 |
| VC-042 | E04 | T25 |
| VC-043 | E05 | T26 |
| VC-044 | E06 | T27 |

## Design questions and current evidence

D02 has not run: versions/model availability, supported SDK operations,
attachment formats, ASR/hints/resources and unattended UI feasibility are
unverified. D03-D05 must resolve concrete folder defaults, boundary spans,
revision/queue/model ordering, binding/accessibility and failure UX without
changing the fixed behavior above. D06 supplies measurable assertions and
corpora; D07 verifies free release limits, credential/event-trigger feasibility
and check bootstrap; D08 resolves contradictions or blocks implementation.
No D01 text is evidence that those gates have already passed.
