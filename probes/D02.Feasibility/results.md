# D02 reproducible probe results

Local observation date: 2026-11-14. Reviewed base:
`98cac3ff2ccb400a14030ca6d5f3561ad5c922b1`. Source/probe distinctions and decisions:
[technical-feasibility.md](../../docs/technical-feasibility.md).
These are diagnostic measurements, not production acceptance or independent review.

## Commands

Run from the diagnostic directory using PowerShell, not the repository root:

```powershell
Set-Location C:\Users\vojte\Projects\voice-copilot-worktrees\D02-feasibility\probes\D02.Feasibility
$dotnet = 'C:\Users\vojte\.copilot\session-state\1e48f66a-939b-4518-8168-5eb9171e08c4\files\tools\dotnet10\dotnet.exe'
.\Get-Models.ps1 -IncludeSmall
.\New-OwnedAudio.ps1
.\Invoke-Probes.ps1 -Case sdk-intercept -Dotnet $dotnet
.\Invoke-Probes.ps1 -Case asr,ui-local -Dotnet $dotnet
```

On a different supported Windows host with pinned SDK 10.0.404, use `-Dotnet dotnet`.
`Invoke-Probes.ps1` scopes generated assets/CLI home/packages/TEMP and invokes
`dotnet build --verbosity quiet -p:RestoreLockedMode=true`, then the real diagnostic
DLL. Sanitized local logs remain ignored in `.assets`; the table is the retained
evidence. BLOCKED is not PASSED and does not fail the diagnostic process; assertion
failures do. Initial/alternative rejected candidates are reported below too.

The SDK ZIP was downloaded only after NETSDK1045. Official URL:
`https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.404/dotnet-sdk-10.0.404-win-x64.zip`.
Verified SHA512:
`330ddd2665b43cd2e428216a0d8ea1ac7bb19b5434fdc530e62010dc3b52823a620bf95489d628e1ed6f5122401793ee4887cca4a019122361a1231497f632ee`.
No global SDK 10 install/PATH edit. Subsequent runs used SDK 10.0.404/runtime 10.0.15.

## SDK actual results

Final `sdk-intercept` process: exit **0**, with the following explicit blockers.
No production model request was forwarded. Native metadata requests are distinct.

| Case | Outcome / exact scope |
| --- | --- |
| SDK.RUNTIME, SDK.AUTH | PASSED: actual 1.0.112/protocol 3/start/ping; existing user auth and expected-account boolean true. |
| SDK.MODELS_FIRST_ATTEMPT / SDK.MODELS | Intercepted metadata catalog attempt failed twice with locally generated 403; BLOCKED route compatibility. |
| SDK.UNHOOKED_METADATA / SDK.CATALOG | PASSED catalog observation: ordinary no-send metadata client returned 19 models, 14 policy, 18 token-price, zero multiplier entries. |
| SDK.MODEL_CHOICE | OBSERVED enabled input-price candidate claude-haiku-5.5, vision true, Default context; no assumption of request-based cost. |
| SDK.ACCOUNT_QUOTA / SDK.SESSION_QUOTA | OBSERVED premium/chat/completion snapshots and provider account metadata; tokenBilling true, premiumBillable false. No numeric personal allowance retained. |
| SDK.QUOTA_SEMANTICS | OBSERVED allowed/available, account_snapshot, authoritative_budget, succeeded, ai_credits. |
| SDK.ALLOWANCE_GATE / SDK.LIVE | BLOCKED live: existing positive authoritative capacity observed, serverForbidsOverage false, hard output/price-unit/coverage gate unverified. Zero actual model calls, no extra capacity/overage purchased/enabled. |
| SDK.CREATE | PASSED: actual local named session. |
| SDK.ACK / SDK.CORRELATION | PASSED: real acceptance MessageId and user.message echo of it plus ClientTurnId; no resend/idempotency claim. |
| SDK.INTERCEPT / SDK.INFERENCE_ISOLATION | PASSED: serialized HTTP request parseable, tools/config marker/environment context absent; body/headers not exported. |
| SDK.INFERENCE_CAPABILITIES | PASSED after explicit native-MCP disable: loaded skills=0, active MCP servers=0, MCP tool count=0. First candidate with only empty McpServers did not prevent startup. |
| SDK.QUEUE_PENDING | PASSED: drain-paused pending count=1, accepted MessageId discoverable. |
| SDK.QUEUE_WITHDRAW_GUARD / SDK.QUEUE_WITHDRAW | PASSED: wrong expectedPrompt did not remove; exact prompt removed without interrupting running turn. |
| SDK.QUEUE_REMOVE / SDK.QUEUE_CLEAR | PASSED: native item ID removed; clear pending count=0. |
| SDK.WAIT_TIMEOUT | PASSED: 200 ms SendAndWait timeout did not abort the already active intercepted runtime request. |
| SDK.MODEL_DEFERRED | PASSED: busy target claude-sonnet-5.5; Deferred=true, QueueId present, Status=applied, confirmation absent; GetCurrent independently confirmed initial effective model. Change explicitly cleared. |
| SDK.ALLOW_ALL / SDK.AUTOPILOT | PASSED: effective AllowAll at create/RPC readback; ModeApplied + Get(Autopilot) before send; continuation disabled, tools denied. |
| SDK.CONCURRENT / SDK.ABORT_ISOLATION | PASSED: two distinct simultaneous held runtime requests; first request token cancelled after abort while second remained active. |
| SDK.ROOT_CORRELATION | PASSED: root user.message on second session echoed its acceptance ID/ClientTurnId. |
| SDK.ABORT / SDK.STOP | PASSED: actual request cancellation token confirmed cancelled after AbortAsync, own client stopped; no side effects/rollback guarantee tested. |
| SDK.RESUME_NO_SEND / SDK.COLD_RESUME_PENDING | BLOCKED: attempted detach/resume with ContinuePendingWork=false failed (IOException); cold pending fixture/locks not proved. No fallback Send. |
| SDK.DELETE / SDK.SECOND_DELETE | BLOCKED: native delete refused only owned diagnostic session IDs after detach. No unrelated session modified. |
| SDK.COMPLETION_STREAMING_USAGE | BLOCKED: no live response/deltas, originating-message answer/error, per-role usage or autopilot terminal evidence. |
| SDK.MODEL_APPLY_AFTER_COMPLETION | BLOCKED: deferred item deliberately cleared; eventual model_change/before-next-send ordering not observed. |
| SDK.REAL_SUBAGENT_FILTER / SDK.NATIVE_IDEMPOTENCY_ROLLBACK | BLOCKED: no real subagent, duplicate delivery or side-effect/unsend/rollback qualification. |
| Native text/code/PNG forwarding | BLOCKED: actual upstream inference gate did not pass; paths/types are source evidence only. |

Observed root event kinds included user.message, assistant.turn_start/turn_end,
model.call_start/finished, session.idle, abort and agent.interrupted. A native
model.call event is **not** evidence of an upstream model call; the request handler
held/refused inference. No completion/usage success is inferred from these events.

### Rejected/failed preparation cases

- SDK 9.0.300 smallest net10 build: **FAILED NETSDK1045**, remedied by the isolated
  official SDK acquisition.
- Initial multiplier-only selection: no candidates, despite a real 19-model
  catalog; corrected to token-price-aware discovery, not model-ID guessing.
- Busy model switch with additional RequireAvailable: **FAILED expected deferral**,
  native Status=cancelled, Deferred=false/no QueueId under metadata interception.
  Default source-recommended switch request subsequently produced observed deferral.
  Neither outcome proves the live RequireAvailable path.
- Resume/delete remained blocked after distinct attempts; no fake fallback success.

## ASR actual results

`asr` process exit **0**. All four model hashes verified before native loading.
CPU four-thread decoding; ONNX one-thread provider; no microphone/audio output.
SAPI David English-US, rate -1, fixture hashes:

| Fixture | Seconds | SHA256 |
| --- | ---: | --- |
| ordinary | 5.225 | `14b98f0c9f44295548e6137850d907491e6c0af5aa1d42ef01888fb61857f84a` |
| domain | 6.575 | `50083942890abd95af26135f72d54532fe2b877b0e7d2574cffbdac4e364c45f` |

Both mono 16000 Hz PCM16. One active capture endpoint's metadata reported
16000 Hz/1 channel/32-bit mix; AudioClient was never initialized/started.

| Case | Actual result |
| --- | --- |
| ASR.VAD_SILENCE / SPEECH / RESET | PASSED: zero segments on 2 s zeros; two ordinary one-shot segments; reset exactly reproduces start/end times. |
| ASR.VAD_WINDOWS | REJECTED native frame candidate: 512-sample calls yielded **zero** segments vs two one-shot. Not a streaming pass. |
| ASR.ONNX_INTEGRITY / GRAPH | PASSED official graph input/state/sr -> output/stateN and pinned bytes/hash. |
| ASR.ONNX_FRAME_SPEECH / SILENCE / RESET | PASSED: 164 frames /101 speech frames at 0.5; zero silence speech frames; reset gives identical probabilities. 52 ms processing. |
| ASR.CPU / TRANSCRIPTION | PASSED native CPU library and all six nonempty synthetic transcriptions. |
| Benchmark | OBSERVED base ordinary/domain/hinted RTF 0.273/0.218/0.221 and WER 0/0.364/0.182; small 0.915/0.730/0.733 and WER 0/0.182/0. |
| ASR.RESOURCES | OBSERVED peak working set 605,192,192 bytes; base/small cold factory load 77/199 ms. |
| Long/noisy speech, pre-roll/utterance stitching, device reconnect, NoAvx | UNRUN; not qualified by the two synthetic fixtures. |

Hashes / HTTPS acquisition:

| Asset | Bytes | SHA256 |
| --- | ---: | --- |
| base.en-q5_1 @ whisper.cpp commit 5359861c739e955e79d9a303bcbc70fb988958b1 | 59,721,011 | `4baf70dd0d7c4247ba2b81fafd9c01005ac77c2f9ef064e00dcf195d0e2fdd2f` |
| small.en-q5_1 @ same commit | 190,098,681 | `bfdff4894dcb76bbf647d56263ea2a96645423f1669176f4844a1bf8e478ad30` |
| ggml-silero-v6.2.0 @ whisper-vad commit 9ffd54a1e1ee413ddf265af9913beaf518d1639b | 885,098 | `2aa269b785eeb53a82983a20501ddf7c1d9c48e33ab63a41391ac6c9f7fb6987` |
| official silero_vad.onnx v6.2.3 | 2,327,524 | `1a153a22f4509e292a94e67d6f9b85e8deb25b4988682b7e174c65279d8788e3` |

Installed nupkg SHA256 (lock file also retains NuGet content hashes):

- GitHub.Copilot.SDK 1.0.34: `640af39c65e5b47e67a7740ff2436d4c71068ff250a41668eb5a1bdd391e733b`
- Whisper.net 1.9.1: `b501cb8743e6c67b504cf194ec0bdbcaf21a1c0951a0f28cc82516d743ba1ecb`
- Whisper.net.Runtime 1.9.1: `b5224f0dad44d5eb8233e5d83f4333a8a3fccadc77095f50f361f06d65e0736b`
- NAudio 3.3.0: `fa2088e054d4227319f1ff6d93565d4018990e952a7665916335ff38b72a932a`
- Microsoft.ML.OnnxRuntime 1.32.0: `a9150bc2992cbbb6722070da9910e1ffa989a8bfc67bdd5bb8e745ca755641db`

## UI actual results / canary

Local `ui-local` process exit **0**: UI.STA, UI.SELECTION, UI.PNG_ENCODING passed.
No shown window, global clipboard/keyboard/focus mutation on the shared local host.

The real `ui-canary` is restricted **only** to this repository's disposable public
Windows runner. **Publication/activation BLOCKED**: both MCP workflow writes
(`push_files`, `create_or_update_file`) returned **Re-authentication required**.
Ordinary contents/read access still worked; a browser check was unauthenticated.
No interactive login, credential extraction/new app token or authorization bypass.
The inert [workflow proposal](d02-feasibility.workflow.yml) is committed for review;
the intended `.github/workflows/d02-feasibility.yml` is local-only and untracked.
**Actual workflow runs: zero; run/job URLs: none; UIA/real clipboard/keyboard/focus
canary coverage: BLOCKED.** A manual QA checklist is not a substitute.
