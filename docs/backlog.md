# Approved dependency-ordered backlog

Status: D01 review candidate for
[issue #2](https://github.com/vojtadtenis-personal/voice-copilot/issues/2), under
[epic D / issue #1](https://github.com/vojtadtenis-personal/voice-copilot/issues/1).
The parent-supplied approved ticket extract defines these 44 IDs and dependency
edges; final product/free-only decisions refine their obligations, not their DAG.
[product-spec.md](product-spec.md) owns requirement definitions, release boundaries
and the requirement-to-ticket/test table; they are not repeated here.

## Reading and completion rules

- `Dependencies` are actual prerequisites, not suggestions or schedules.
  A dependency gate must complete before its dependent ticket. Independent
  branches in the DAG may proceed concurrently; no estimates/sprint schedule.
- D01-D08 finalize design. D02 probes remain distinct from production and must
  not claim unrun checks passed. D08 must complete before ANY production ticket.
- M01-M17 produce an internal packaged MVP with voice AND manual scrolling.
  V01-V08 are mandatory complete-v1 features, R01-R05 qualify/publish it.
  E01-E06 are optional, all after R05, not permission to defer v1 requirements.
- Every implementation ticket adds appropriate targeted automated tests/failure
  cases, updates changed docs/contracts, respects inward architecture boundaries,
  and has linked current-base CI, independent review and merge evidence.
  D06 defines executable suites for the planned T01-T27 catalog. Fakes are
  labeled; they do not replace actual ASR/SDK/inference/packaged UI qualification.
- At least 90% Core/Application branch coverage of meaningful handwritten
  logic is the target, with transition/property tests for critical invariants
  and focused mutation testing. No trivial test padding, hidden logic exclusions
  or weakened cases. Fix related findings in the ticket that causes them.
- Prefer cohesive encapsulated objects, immutable values, composition and one
  shared use-case backend. Core/Application have no WPF/SDK dependencies. Avoid
  speculative interfaces, duplication, broad catches/success-shaped failures,
  and comments compensating for poor names/control flow.
- Routine implementation/review/testing/merging is autonomous using isolated
  feature branches/worktrees and PRs, never direct production edits on main.
  Authors cannot approve themselves; use independent agent review evidence and
  a required check, not fake approving reviews. Missing external authorization
  blocks work. Personal-account merges are serialized and validated on latest
  base. README is the only bootstrap; D07 defines check bootstrap before checks
  exist. D01 itself activates no workflow/service and does not run paid/live
  probes or claim CI that has not been configured.
- Free-only applies to EVERY ticket: public repository, standard free public
  runners, no paid service/new charge/purchase/overage, no Actions artifacts
  upload/download, cache/implicit caching, Packages/GHCR, LFS, Codespaces,
  larger/private runners, paid signing or cloud infrastructure. Use logs/job
  summaries for CI evidence and documented free Release assets for distribution.
  Keep builds in one job or independently rebuild without artifact handoff.
  Stop and notify the coordinator if a choice may incur non-AI charges.
- Existing Copilot account allowance and conversation-agent credits are
  separately authorized, not permission to buy capacity/enable overages. Track
  separate inference usage; no guessed allowance amounts or initial hard app
  budget. Real tests use authorized isolated contexts; missing credentials,
  account limits or runtime policy are blocked states, never success.

## Epic D - Product definition and finalized design

### D01 - Product specification and release boundaries
Dependencies: none.

Deliver: docs/product-spec.md and docs/backlog.md, stable requirement IDs,
glossary, non-goals and traceability separating internal MVP, mandatory v1 and
optional follow-ons. Include required MVP voice/manual scrolling.

Acceptance: every accepted behavior has a requirement and planned ticket/test;
file references/model switching remain mandatory; input parity and final
free-only behavior are consistent; the 44-ticket DAG is valid and design
precedes production. Independent review validates the documents before merge.

### D02 - Technical feasibility and compatibility investigation
Dependencies: D01.

Deliver: evidence for selected .NET/WPF baseline, released SDK/runtime pairing,
authentication, create/resume, streaming/completion, abort, autopilot,
permissions, model listing, tool-free inference, local English ASR and
unattended Windows UI testing. Investigate actual attachment forwarding and
ASR hints; keep probes distinct from production.

Acceptance: reproducible versions/results, supported platforms, measured
resources, limitations and alternatives; restoration cannot resume pending
execution. No assumed GPU/RAM/format support or unrun live success. Stay inside
authorized existing allowance and report missing prerequisites accurately.

### D03 - Clean architecture and decision records
Dependencies: D01, D02.

Deliver: docs/architecture.md, dependency diagrams, component ownership,
concurrency boundaries and concise ADRs for SDK, ASR, persistence, inference
isolation and UI tests. Specify neutral working folders and asset ownership.

Acceptance: walkthrough create, dictate/submit, stop, restart and cloud failure;
Core/Application do not depend on WPF/SDK; practical external test seams and
separate tool-free inference roles without speculative abstraction.

### D04 - Behavioral state machines and action contracts
Dependencies: D03.

Deliver: docs/behavior.md, state transitions, action schemas, utterance spans,
stable session targeting, draft revisions, queues/model ordering and failures.
Include normal, invalid, delayed, cancelled and duplicate events, compound
speech, quoted commands, manual edits during cleanup and explicit finish+submit.

Acceptance: every transition has an outcome; no general commands in prompt
mode, implicit submission, stale-target execution or duplicate delivery.
Define manual send during capture, deferred models and close/abort races.

### D05 - UI and input interaction design
Dependencies: D04.

Deliver: docs/ui-and-input.md, tabs/composer/transcript/history, focus, keyboard
bindings, accessibility IDs, mic indicators, scroll anchoring/return-to-latest.
Define append and explicit submission during capture and complete-v1 controls.

Acceptance: MVP journeys work by voice AND independently mouse/keyboard,
including manual transcript scrolling; background processing never steals
focus. Typing is prompt text, with ordinary editor/selection/undo/redo behavior.

### D06 - Test strategy and evaluation corpus
Dependencies: D02, D04, D05.

Deliver: docs/testing.md, test layers, synthetic/owned audio fixtures,
annotated utterances, fault cases, coverage policy and live-integration
requirements. Specify T01-T27 as applicable, and separate deterministic replay
from actual ASR/model evaluations.

Acceptance: critical requirements have automated verification and real
execution environments; no mandatory human listening/clickthrough; fakes and
blocked live tests are labeled; semantic, concurrency and input parity cases
cannot be replaced by trivial adapter assertions.

### D07 - Autonomous delivery and repository governance design
Dependencies: D02, D06.

Deliver: docs/delivery.md for issue lifecycle, worktrees, PR gates, independent
review, credentials, serialized merging, retries, evidence and release gates.
Define bootstrap avoiding nonexistent required checks. Prefer existing
GitHub/agent tooling, not a new orchestration framework.

Acceptance: simulate success, failed CI, conflicts, stale reviews, duplicate
dispatch and missing credentials; no routine human approval or silent bypass.
Record default-token event-trigger limitations (bot/default-token changes may
not trigger expected follow-on workflows), and verify an authorized free
credential/dispatch solution before M02. No new paid bot service. Specify
independent evidence/check instead of author approval, latest-base validation,
explicitly uncached standard-runner jobs/log summaries, and current documented
free Release asset limits/unsigned distribution.

### D08 - Final design consistency gate
Dependencies: D01, D02, D03, D04, D05, D06, D07.

Deliver: independently reviewed coherent design baseline, resolved
contradictions and finalized implementation tickets.

Acceptance: requirement coverage, architecture, testability, trust boundaries,
MVP scope and autonomous free delivery feasibility are evidenced. Resolve
blockers or explicitly block implementation. No production starts beforehand.

## Epic M - Internal MVP

### M01 - Solution, test infrastructure, and initial CI
Dependencies: D08.

Deliver: production/test structure, pinned SDK/dependencies, central build
settings, formatting/analyzers and standard Windows build/test CI.

Acceptance: clean checkout builds/tests; formatting/architecture violations
fail; test/coverage evidence is logs/job summaries, not Actions storage/cache.
No feature behavior is introduced here.

### M02 - Repository automation and protected delivery
Dependencies: M01.

Deliver: issue/PR templates, dependency/status conventions, worktree dispatch,
independent-review checks, branch rules and automatic serialized merges.
Narrow normal credentials; administration limited to configuration.

Acceptance: disposable trial PR proves issue linking, validation, independent
review, rejection on failure and gated merging; repeated dispatch creates no
duplicate PR. Bot-authored changes actually trigger CI using the D07 verified
free solution; authors do not self-approve and stale reviews/base changes
invalidate evidence as designed.

### M03 - Session/tab domain and shared application contracts
Dependencies: M02.

Deliver: stable session IDs/numbers, positional open tabs, selection,
create/open/resume/rename/close, basic saved history access, creation/model
defaults, close semantics, neutral working-folder policy and shared use cases.

Acceptance: reorder/close/reopen, stale references, inherited models and stable
numbers; no mandatory project/entity and no WPF/SDK dependency in
domain/application contracts.

### M04 - Versioned draft model and minimal persistence
Dependencies: M03.

Deliver: append/edit/clear/replace/undo/redo, revisions, speech-owned edits,
atomic mappings/draft/history/folder persistence and schema versioning.

Acceptance: append existing text, manual edits during delayed cleanup, undo,
interrupted writes and passive restore without submission; never persist
mute state or raw audio.

### M05 - Copilot SDK session adapter
Dependencies: M03.

Deliver: actual create/resume, events/streaming, send acknowledgements,
completion/error mapping, abort, effective autopilot/allow-all and separated
coding/inference roles. Forward prompts/responses without task forms.

Acceptance: protocol ordering, disconnects, cancelled wait versus actual abort,
completion and managed refusal. Isolated authenticated actual-runtime smoke
uses authorized existing allowance; missing prerequisites/policy limits block,
not pass. No runtime/credential/consent restriction bypass.

### M06 - Per-session execution and dispatch queues
Dependencies: M04, M05.

Deliver: execution states, ordered explicit sends, message correlation,
stop/close and duplicate protection; busy sessions do not block each other.

Acceptance: concurrent sessions, repeated acknowledgements, uncertain sends,
late events, abort races and busy-tab close with retained history/draft.
No blind retry of possibly accepted prompts, stale-target sends or replay.

### M07 - Functional WPF shell and normal input
Dependencies: M03, M04.

Deliver: tabs, basic saved-session access, composer, streaming transcript,
status and shared mouse/keyboard use cases. Fakes only in test composition.

Acceptance: view-model/STA tests create/open/resume/select/rename/close,
type/edit/submit, selection/copy/paste/undo/redo, focus and streaming without
focus theft. Typed text is not interpreted as app commands.

### M08 - Local English speech pipeline
Dependencies: M02.

Deliver: capture/device handling, VAD, pre-roll, resident CPU-capable English
ASR, final utterances and explicit initialization/failure; pinned,
integrity-checked assets and bounded processing.

Acceptance: onset, pauses, long speech, repeated windows, silence/noise, device
loss, bounded buffers and throughput on declared reference environment.
No universal hardware/GPU/RAM guarantee or raw-audio persistence.

### M09 - Tool-free cloud inference and visible fallback
Dependencies: M05.

Deliver: authenticated isolated command/ending/cleanup inference contexts,
model catalog, available Haiku preference, compatible visible fallback, bounded
context, validated outputs and separately tracked usage, no initial hard budget.

Acceptance: unavailable models, timeout, malformed/superseded outputs and
exhausted alternatives; no repository/shell tools in inference contexts and
no action catalog in ending context. No guessed allowance/purchased capacity.

### M10 - Command interpreter and strict prompt boundaries
Dependencies: M06, M09.

Deliver: initial action registry, ordered planning, natural start-prompt,
restricted ending detection and multiple mode boundaries in one utterance.
MVP covers sessions, draft entry/exit/basic editing, explicit submit, stop
and scrolling integrated by M12.

Acceptance: state/corpus tests prevent prompt text invoking handlers; quoted,
reported and negated endings, compound utterances, invalid actions/stale tabs.
No fixed-phrase whitelist, app task interview or invented unresolved targets.

### M11 - Conservative cleanup and draft reconciliation
Dependencies: M04, M09.

Deliver: clean newly dictated text only, preservation rules/original text,
revision-aware application; cleanup finalization precedes explicitly
authorized sending.

Acceptance: protect negations/numbers/quantities/paths/identifiers/requirements
and manual text; no restructuring or overwrite of newer edits; failures
visible and never authorize submission. Preserve uncertain words and undo.

### M12 - Voice and manual transcript scrolling
Dependencies: M07, M10.

Deliver: up/down, mouse/keyboard scrolling, return-to-latest and preservation
of scroll position while output streams.

Acceptance: correct stable target without focus theft; voice scroll only in
command mode, same speech is text in prompt mode; no forced bottom scrolling.
This ticket is required internal MVP, not an optional enhancement.

### M13 - Complete voice-to-Copilot vertical slice
Dependencies: M06, M07, M08, M10, M11.

Deliver: integrate capture, interpretation, modes, drafts, cleanup, execution
and UI; every launch listening in command mode, global/foreground settings,
local mute/emergency stop and no wake phrase/spoken/audio output.

Acceptance: true create/select/dictate/finish/review/execute/stop and
same-utterance finish+execute; ending alone never sends. Enter/Send during
capture finalizes safely; normal input never depends on voice inference.
Tab switches do not retarget dictation.

### M14 - MVP lifecycle and failure recovery
Dependencies: M13.

Deliver: shutdown, passive restore, mic reconnect, visible outage/backlog and
stale-work retirement, without restoring data by accidentally resuming work.

Acceptance: interrupted capture/cleanup/SDK/persistence/shutdown; no restart
autosend/audio replay/task execution/mute restore; local mute/stop responsive
during cloud failure and normal input remains usable.

### M15 - Packaged MVP artifact
Dependencies: M13.

Deliver: reproducible portable Windows package, native dependencies, model
acquisition, versions and diagnostics. Publish through documented free
Release assets, never Actions artifacts/caches.

Acceptance: clean supported environment launches without checkout/dev SDK;
missing dependencies/downloads explicit, not an empty success-shaped app.
Use D07 release limits and honest unsigned labeling, no paid signing.

### M16 - Automated MVP system qualification
Dependencies: M12, M14, M15.

Deliver: packaged system tests, actual ASR evaluation, authorized isolated
SDK/inference smoke and unattended Windows UI environment.

Acceptance: actual MVP journeys, modes, scrolling, concurrency and input
parity. Deterministic fakes cannot replace proving real integrations; report
actual versions/resource limits and missing prerequisites.

### M17 - MVP maintainability and completion gate
Dependencies: M16.

Deliver: requirement/implementation/test/package evidence and focused
architecture/duplication/error-handling review with fixes. M02 automation
remains required.

Acceptance: no unfinished production stubs, hidden success defaults,
unresolved critical defects or unverified required integrations; CI and
independent review pass on actual current-base merge candidate.
Label this internal MVP, not complete v1.

## Epic V - Mandatory complete-v1 feature set

### V01 - File-reference transport
Dependencies: M17.

Deliver: draft-bound paths and native SDK forwarding, no parser or app format
whitelist; references do not submit a draft or copy/modify originals.

Acceptance: contract/live tests ordinary code/text references and image
attachments; disclose accurate tested runtime limitations, not invented
format support or app content analysis.

### V02 - Picker, drag/drop, and clipboard adaptation
Dependencies: V01.

Deliver: normal attachment inputs/removal, paths from drop/clipboard, app-owned
files for raw clipboard bitmaps and persisted unsent-draft references.

Acceptance: spaces/Unicode, multiple files, images without paths, restart and
missing references; retain referenced clipboard assets; delete only
unreferenced app-owned assets, never user originals.

### V03 - Voice file-reference actions
Dependencies: V02.

Deliver: natural add/remove of explicit paths or known-folder relative
references; no whole-disk guessing or app task-clarification interview.

Acceptance: ordering/explicit submit, stable targets and attachment-like prompt
text stays text; unresolved app targets fail nonblockingly, not by invention.

### V04 - Coding-model selection and deferred changes
Dependencies: M17.

Deliver: mouse/keyboard/voice selection, effective/pending state, inheritance,
persisted choices and pending application before the next dispatched turn.

Acceptance: idle/busy switch, queued sends, failed application and session
isolation; separate control-model fallback; requested model is not shown as
effective early. First session default and prior-active inheritance remain.

### V05 - Domain dictionary storage and editor
Dependencies: M17.

Deliver: global/session overrides, versioned import, validation and normal
editor for canonical names, aliases, acronyms and short meanings.

Acceptance: invalid import without partial corruption, collisions, precedence,
persistence/Unicode; entries are data, not executable instructions. No required
local-only exclusions.

### V06 - Dictionary integration with speech and inference
Dependencies: V05.

Deliver: supported ASR hints and bounded dictionary context in intent/cleanup;
preserve uncertain words instead of fabricating substitutions.

Acceptance: actual domain-term recordings and semantic preservation, not just
tests that values reached an adapter. Hint support comes from D02 evidence.

### V07 - Complete input/settings parity audit
Dependencies: V03, V04, V06.

Deliver: missing bindings, configurable shortcuts, settings/accessibility/focus
corrections and one shared handler implementation per use case.

Acceptance: automated capability matrix covers every operation by normal
inputs and every defined voice action, including models, attachments,
dictionary/settings/mic; mute/inference failure never disables normal UI.

### V08 - Feature-complete v1 acceptance gate
Dependencies: V07.

Deliver: packaged v1 candidate and complete requirement evidence.

Acceptance: attach/dictate/manual edit/finish/model change/explicit submit,
restart and parallel variants; mandatory features cannot hide in optional
backlog. Pass current-base CI/independent review, then qualify through R.

## Epic R - Hardening, cleanup, release

### R01 - Reliability and trust-boundary audit
Dependencies: V08.

Deliver: fix state races, model-output validation, role isolation, native
resource ownership, credential/logging and failure-handling review findings.

Acceptance: malformed output, command-like prompt text, late callbacks,
duplicates, partial writes/policy refusal; coding allow-all never enables
control/ending/cleanup tools. No leaking credentials or hidden success defaults.

### R02 - Performance and soak qualification
Dependencies: V08.

Deliver: timing/resources, long-dictation/parallel soak and fixes for leaks,
stalls and unbounded queues.

Acceptance: sustained throughput, bounded buffers, responsiveness, warm/cold
start and local mute/stop latency; separate local/cloud latency and declared
reference resources, no all-PC guarantee.

### R03 - Upgrade, recovery, and data-lifecycle qualification
Dependencies: V08.

Deliver: migrations, interrupted-upgrade recovery, safe cleanup and asset/path
ownership.

Acceptance: older-data fixtures survive; preserve referenced/user assets,
IDs/order/drafts/history/folders/dictionary/model choices; restore never sends
prompts, replays audio, restores mute or restarts tasks.

### R04 - Release pipeline and installation validation
Dependencies: R01, R02, R03.

Deliver: versioned free Release files, checksums, licenses, clean-install/startup
tests and automated publication in one job or independently rebuilt jobs
without Actions artifact/cache handoff. No paid signing; label unsigned honestly.

Acceptance: clean install, provenance/checksums, upgrade path and documented
current free asset limits; no fabricated signing/authorization or enabled
paid service/storage. Logs/summaries are CI evidence, Releases distribution.

### R05 - Final product release gate
Dependencies: R04.

Deliver: qualified release, requirement/test/compatibility/performance
evidence, limitations and release notes in free logs/summaries/repository or
Release metadata.

Acceptance: exact release commit/artifact and authorized isolated live
integration, independent review and no skipped required checks. Publication
succeeds before mandatory epics close; no mandatory feature moved to E.

## Epic E - Optional subsequent improvements

Each follows R05, requires its own scoped specification/tests, and remains
subject to the free-only policy. None expands/delays the initial product by
default.

### E01 - Advanced voice editing and spelling
Dependencies: R05.

Deliver: optional editing/spelling of selected fragments.
Acceptance: precise targeting, undo and no implicit rewrite/submit (T22).

### E02 - Richer history navigation
Dependencies: R05.

Deliver: optional history search/filter/navigation.
Acceptance: stable IDs, scale and no unintended execution (T23).

### E03 - Richer dictionary exchange
Dependencies: R05.

Deliver: optional export/sharing/richer import.
Acceptance: versioning/conflicts and entries remain data (T24).

### E04 - Optional cloud ASR adapter
Dependencies: R05.

Deliver: only an independently specified, verified FREE cloud ASR alternative.
Acceptance: no paid provider/new charge or silent switch; stop before any
possibly chargeable activation (T25).

### E05 - Optional accelerated/local ASR profiles
Dependencies: R05.

Deliver: optional acceleration and resource tuning.
Acceptance: measurable benefit, CPU fallback and bounded consumption (T26).

### E06 - Secure updates and release channels
Dependencies: R05.

Deliver: optional secure updates/channels without paid signing/services.
Acceptance: free provenance/integrity, interrupted updates and retained data
(T27).

## DAG and gate audit

Counts: D = 8, M = 17, V = 8, R = 5, E = 6; total = 44.
The headings form a valid topological order, NOT a requirement to serialize
independent work. Exact edges are the `Dependencies` lines above.

- D01 -> D02 -> D03 -> D04 -> D05; D06 joins D02/D04/D05,
  D07 joins D02/D06, D08 joins all D tickets.
- M01 depends on D08. Every other M ticket transitively depends on M01;
  M16 joins scrolling, recovery and packaging, M17 closes internal MVP.
- V01/V04/V05 depend on M17; V07 joins attachments/models/dictionary, V08
  closes feature completeness.
- R01/R02/R03 depend on V08; R04 joins them and R05 qualifies publication.
- All six optional E tickets depend on R05, with no edge back into mandatory
  completion.

D01 review checks the ID counts, source-edge equality, absence of cycles,
transitive D08 production barrier and full requirement/test references with a
small read-only validation. This is documentary validation only: D02 probes,
CI implementation and D08 design approval are outstanding. An independent
reviewer must validate this candidate before merge; D01 author publication is
not that approval.
