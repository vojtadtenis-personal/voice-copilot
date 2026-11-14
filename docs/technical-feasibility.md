# D02 technical feasibility

Status: **diagnostic review candidate, not an implementation/design approval**.
Linked [issue #8](https://github.com/vojtadtenis-personal/voice-copilot/issues/8).
Base: independently reviewed D01, `98cac3ff2ccb400a14030ca6d5f3561ad5c922b1`.
All executable work is in `probes/D02.Feasibility`; nothing in `src`.

**[S]** means primary-source/package evidence. **[O]** means an actual observation
from the probes. **[P]** means unrun or blocked, never a pass. Exact local outcomes
and commands are in [probe results](../probes/D02.Feasibility/results.md).
Blocked mandatory integrations still block D08/production; this ticket does not
resolve them by changing the product requirements.
The reviewed D01 documents retain their historical unverified-at-D01 status;
this report is the separate current evidence, not a rewrite of that baseline.

## Reproducible baseline and integrity

| Component | Pin and evidence |
| --- | --- |
| .NET/WPF | [O] `net10.0-windows`, WPF, SDK **10.0.404**, runtime **10.0.15**, warning-clean locked restore/build and STA execution. [S] .NET 10 is LTS; SDK 9 cannot target it. |
| Copilot SDK | [O] `GitHub.Copilot.SDK` **1.0.34** restored. `v1.0.34` resolved by native Git to commit **adf1da2c6db766f96ae6be36f3224e61ede010c4**, verified with `git cat-file -t` = `commit`. This is independent verification, not reuse of the research tree-SHA claim. |
| Runtime | [O] Build-acquired **1.0.112**, protocol **3**, actual start/ping/status. Independently downloaded win32-x64 tarball: **100,918,446 bytes**, SHA256 **a60f1c1a2d831093f654cf22b56e1bf3ce4f2c4f00afeca699b4824c90abf40b**. Runtime wrapper `copilot.exe`: SHA256 **e82eed44c9d1c0e2ad312c7c2cb71a30e81735791a68c72b7779d3a20620ec04**. |
| ASR/audio | [O] `Whisper.net` + `Whisper.net.Runtime` **1.9.1**, CPU; `NAudio` meta-package **3.3.0**. Use span `Read` and `MMDevice.CreateAudioClient()`; the older AudioClient property is obsolete. |
| Frame VAD fallback | [O] Official Silero **v6.2.3** ONNX, `Microsoft.ML.OnnxRuntime` **1.32.0**, CPU-only package. Added after native 32 ms segment emission failed. |

`global.json` and `packages.lock.json` belong only to the diagnostic project.
GHCP001 opt-in is limited to its SDK probe files. There is no project-wide warning
suppression, paid assertion library, marketplace test service or production seam.
NuGet package hashes are in the results and lock file.

The first smallest build with the only globally installed SDK, 9.0.300, failed
with **NETSDK1045**. Only then was Microsoft's free SDK ZIP acquired, its official
SHA512 verified, and expanded into the session's `files\tools\dotnet10`.
No Program Files install or persistent global PATH/configuration change was used
for SDK 10. Every subsequent call scopes CLI home, packages, TEMP/TMP and telemetry
inside ignored diagnostic assets and uses the absolute session `dotnet.exe`.
The initial SDK 9 first-run banner reported creating its default development
certificate; subsequent calls explicitly disable certificate generation.

## Copilot observations and decisions

### Authentication, discovery and financial gate

- [O] Default `CopilotCli` mode / `UseLoggedInUser=true` authenticated the expected
  account. Only `authenticated`, `expectedAccount`, and `authType=user` are exported.
  No login, raw credential, account selection ID or numeric allowance is published.
- [O] A normal **metadata-only, no-send** client discovered 19 conversation models:
  14 carried policy, 18 token prices, **none a request multiplier**. Selecting only
  by multiplier was therefore rejected. The probe selected the lowest enabled
  published input-price candidate, **claude-haiku-5.5**, with Default context tier.
  That is catalog eligibility, not proof of available spending capacity.
- [O] Account premium/chat/completion projections and session provider quota metadata
  were present. The provider verdict was `allowed/available`,
  `account_snapshot/authoritative_budget`, acquisition `succeeded`, unit `ai_credits`.
  An existing positive budget was observable internally. **The server did not
  forbid overage on exhaustion.** No setting was changed.
- **[P] Live inference is BLOCKED.** End-to-end token-price units, an enforced
  output bound and worst-case eligible-capacity coverage were not established.
  An available verdict alone does not authorize paid overage. Missing/advisory
  zero readings would also not prove exhaustion. **Actual model calls: zero**
  across D02; eight is only the proposed ticket test ceiling, not an app/user cap.
  `MaxAiCredits` is a soft cap, not financial protection. No BYOK/external provider,
  paid service, purchased capacity, credit budget or overage enablement was used.

### Interception and tool-free capability isolation

- [O] `CopilotRequestHandler` intercepted a real serialized HTTP inference
  request and refused its upstream model call. A parseable body had no tools,
  the planted repository instruction marker, working-directory environment
  context or username. Raw body and headers are neither logged nor retained.
- [O] `AvailableTools=[]` alone did not prevent built-in MCP startup in the first
  candidate. The final fresh-session probe also supplies
  `DisabledMcpServers=["github-mcp-server","github"]`. Actual events then showed
  **zero loaded skills, active MCP servers and MCP tools**. Do not confuse
  `session.tools_updated` (a notification without tool inventory) with no tools.
- [O] Configuration/skills/hooks/git/store/memory/discovery are disabled; explicit
  agent/plugin/root contributions are empty; instructions are replaced with a
  narrow data-only contract; permission and pre-tool callbacks deny execution.
  Coding AllowAll is set only on the distinct diagnostic coding-mode session.
- [O] The request hook also intercepted metadata GETs. The narrow GET allowlist
  did not cover one catalog route and returned 403. Normal metadata discovery
  worked without the hook; inference interception still worked afterward.
  This is a harness compatibility limitation, **not proof of an upstream outage**.
  Do not broaden it to arbitrary network traffic to make a probe pass.
- [S/P] Empty mode disables keytar; do not work around its login isolation or copy
  credentials. BaseDirectory/keychain behavior and disk isolation still need a
  credential-preserving probe. The current evidence does not prove all future
  boundary/command/cleanup system contracts or adversarial schema handling.

### Acceptance, lifecycle, concurrency and models

- [O] Subscribe via `OnEvent` before create. Real SendAsync acceptance returned a
  MessageId; root `user.message` echoed it and ClientTurnId. Acceptance is **not
  completion**. ClientTurnId is not a proven native idempotency key.
- [O] Drain-paused native queue APIs exposed accepted pending items. Exact
  withdrawal and RemoveAt succeeded; wrong expectedPrompt did not remove an item;
  Clear removed pending items. These narrow conditions are not a general unsend
  guarantee and do not replace the future app-local **unsent** snapshot queue.
- [O] A SendAndWait timeout stopped the .NET wait, not the already running held
  runtime request. Abort stopped that request; two distinct sessions were held
  concurrently and aborting one did not cancel the other.
- [O] Create applied AllowAll before returning, confirmed by PermissionModeResult
  and RPC GetMode. Actual mode Set(Autopilot), ModeApplied and Get were checked
  **before send**. Continuation is disabled/bounded in this probe; options alone
  are not a mode switch. No managed refusal is overridden and no ApproveAll handler
  is used. Tools remain denied: this does not qualify real workspace edits.
- [O] Busy switch to **claude-sonnet-5.5** with Default tier and
  DeferIfModelChangeQueued returned **Deferred=true, QueueId present,
  Status=applied, unchanged live model**. Status alone is not effective-model proof.
  The queued change was explicitly cleared. With additional RequireAvailable,
  the intercepted catalog rejection instead produced cancelled/no queue entry.
- [P] Assistant deltas, originating-message error/answer correlation, real
  subagent filtering, per-role usage, autopilot terminal RequestId/Reason and
  post-completion model-change/drain ordering remain live-blocked. Autopilot
  `session.idle` is not terminal completion. Abort never guarantees rollback or
  cancellation of already-started effects.

### Persistence and attachments: unresolved blockers

- [O] Create worked, but attempted detach/resume with explicitly
  ContinuePendingWork=false and re-supplied isolation failed with IOException.
  DeleteSession after detach also refused the owned diagnostic IDs. No fallback
  send/replay or deletion of another session occurred. Treat this as a compatibility
  blocker; do not infer passive persistence from configuration/defaults.
- [P] Cold pending-work recovery, ownership/locks, durable attachment history and
  zero queue drain across process restart **are not verified**. D08 cannot approve
  passive restoration until this is resolved with a real retained fixture.
- [S/P] Native AttachmentFile uses absolute Path/DisplayName via
  MessageOptions.Attachments. Actual text/code/PNG forwarding is live-blocked;
  no document parser or guessed format whitelist was introduced.
- [O/S] Owned bitmap PNG encoding/decoding works locally; real clipboard coverage
  belongs to the isolated CI canary below. Keep a clipboard file while **any**
  retained draft, queue or SDK session history refers to it—not merely until Send.
  Runtime path re-reading and unknown document formats remain unverified.

## Local English ASR and resources

[O] Reference: Windows 11 x64 build 22631, Ryzen 9 3900X (12 cores/24 logical),
34,241,748,992 bytes RAM, installed VC++ x64 runtime v14.50.35719.00. ASR used
four threads and disabled GPU; ONNX used one intra/inter-op thread. This is a
shared reference host measurement, not an all-PC promise.

Owned fixtures are generated by Microsoft David Desktop / English-US SAPI,
rate -1, **output-to-file only**, mono 16 kHz PCM16. No microphone was recorded,
no audio played, and device metadata was inspected without Initialize/Start.
Native ASR returned both phrases. Initial prompt hints improved this tiny domain
sample, but dictionary generalization and semantic preservation are not qualified.

| Model / fixture | RTF | WER |
| --- | ---: | ---: |
| base.en-q5_1 / ordinary, 5.225 s | 0.273 | 0.000 |
| base / domain, 6.575 s | 0.218 | 0.364 |
| base / domain + initial vocabulary prompt | 0.221 | 0.182 |
| small.en-q5_1 / ordinary | 0.915 | 0.000 |
| small / domain | 0.730 | 0.182 |
| small / domain + initial vocabulary prompt | 0.733 | 0.000 |

WER lowercases and tokenizes Unicode letters/numbers, then uses word edit
distance / reference words. Hyphenated N-audio/Co-Pilot therefore count as extra
words; punctuation is ignored. These are observations, not selected accuracy
thresholds. Peak process working set was **605,192,192 bytes** for the combined
ASR/ONNX run. Cold factory loads: base 77 ms, small 199 ms; decoding excludes load.

Native VAD classified zero silence and two one-shot speech segments; ResetState
reproduced the result. **512-sample (32 ms) DetectSpeechNoReset calls emitted zero
segments**, so this path is not accepted as a frame streaming detector. Only then
was the official ONNX alternative evaluated: 164 frames, 101 above threshold 0.5,
silence below threshold, reset determinism, 52 ms total processing. Its 512 new
samples + 64 context and [2,1,128] hidden state follow the official wrapper.
Utterance assembly, pre-roll, noise/long-duration/device-loss tests and NoAvx
compatibility remain [P]. Base is the measured lighter candidate; small is an
observed alternative, not a mandatory hardware assumption.

All models are acquired over HTTPS at the pinned revisions and verified before
any native load. See Get-Models.ps1/results for exact size/hash:
Whisper models commit `5359861c739e955e79d9a303bcbc70fb988958b1`, native VAD commit
`9ffd54a1e1ee413ddf265af9913beaf518d1639b`, official ONNX v6.2.3 + SHA256 pin.
Weights, WAVs, credentials, installers, native downloads and NuGet output are ignored.

## Automated Windows UI canary

[O] Local STA, composer-selection preservation and an injected owned bitmap
round-trip passed without showing a window or touching shared-host input/clipboard.

[P] The branch-only push workflow `d02-feasibility.yml` uses one **windows-2025
standard public** job, a public-visibility/repository/branch guard, preinstalled
SDK 10, native public Git checkout without credentials, and built-in UIAutomation.
No marketplace action, live Copilot credentials/calls, service, artifact or
Actions/implicit cache is used. Evidence goes only to logs/GITHUB_STEP_SUMMARY.
It constructs an owned nonactivating WPF window, locates and invokes own UIA
controls, directs F6 only to its HWND, round-trips a synthetic clipboard bitmap,
and checks foreground handle before/after/close.

**CI run result pending publication; no unattended UI pass is claimed yet.**
Unsupported desktop/UIA/keyboard hosts must fail explicitly, not become a manual
QA checklist. This canary is not packaged product/input-parity qualification.

## Primary sources and legal prerequisites

Accessed 2026-11-14; observations above are separate from these source claims.

1. [.NET 10 official release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json)
   and [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).
2. [SDK release v1.0.34](https://github.com/github/copilot-sdk/releases/tag/v1.0.34),
   [tagged .NET source](https://github.com/github/copilot-sdk/tree/v1.0.34/dotnet/src),
   [NuGet package](https://www.nuget.org/packages/GitHub.Copilot.SDK/1.0.34).
   Types.cs, CopilotRequestHandler.cs, Client.cs, generated Rpc/SessionEvents and
   the installed build props/targets were inspected, not upstream-main assumptions.
3. [CLI release/checksums](https://github.com/github/copilot-cli/releases/tag/v1.0.112).
   The downloaded bundle's LICENSE.md permits royalty-free install/run, but future
   redistribution requires unmodified software, material additional application
   functionality, independent application licensing and a copy of its license.
   **D02 redistributes no runtime/model binary.** Packaging is not approved here.
4. [Whisper.net 1.9.1 source](https://github.com/sandrohanea/whisper.net/tree/1.9.1),
   verified commit `98278acc38ae23590cdfa9859f78f089abae52a7`;
   [Whisper.net](https://www.nuget.org/packages/Whisper.net/1.9.1),
   [NAudio](https://www.nuget.org/packages/NAudio/3.3.0), MIT.
5. [Whisper models pinned revision](https://huggingface.co/ggerganov/whisper.cpp/tree/5359861c739e955e79d9a303bcbc70fb988958b1),
   [native VAD pinned revision](https://huggingface.co/ggml-org/whisper-vad/tree/9ffd54a1e1ee413ddf265af9913beaf518d1639b),
   MIT model metadata; [official ONNX wrapper](https://github.com/snakers4/silero-vad/blob/v6.2.3/src/silero_vad/utils_vad.py),
   MIT; [ONNX Runtime](https://github.com/microsoft/onnxruntime/blob/main/LICENSE), MIT.
6. [Runner image software](https://github.com/actions/runner-images/blob/main/images/windows/Windows2025-Readme.md)
   (source image 20261111.331.1 includes SDK 10.0.404);
   [Actions billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions)
   explicitly makes standard public runners free, unlike larger runners.

## Review boundary

D02 provides measured candidates and visible failures/blockers. It does not close
issue #8, authorize D08, implement the production application, certify packaging,
or provide independent self-approval. The draft PR must stop for independent review.
