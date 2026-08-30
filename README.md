# CrystalStudio

A local OpenAI-compatible Model Council. One inbound chat request is
answered by several models in parallel: isolated proposals, anonymous
peer review, then a chair that selects an original proposal instead of
inventing a fused answer.

CrystalStudio talks to models through the Crystal `IChatClient`
contracts. Provider catalogs and API keys are shared with CrystalHarness
under `~/.crystal`. Council seats live in `~/.crystal/studio`.

## What it does

- Hosts `POST /v1/chat/completions`, `GET /v1/models`, and `GET /health`
  on a loopback port (default `http://127.0.0.1:18790/`).
- Runs a three-phase council: isolated proposals, anonymous ranking
  (Borda), then chair confirmation.
- Streams council progress as `reasoning_content` (the compatible
  thinking field). The final `content` or `tool_calls` is one original
  member proposal.
- Returns aggregated token usage from every member call:
  `prompt_tokens`, `completion_tokens`, `total_tokens`, and
  `completion_tokens_details.reasoning_tokens` when a provider reports
  reasoning tokens.
- Downgrades a disputed high-risk tool call to a text request for human
  confirmation instead of returning that tool call.

The council has no session of its own. Each HTTP request is a one-shot
derivation from the full inbound transcript.

## What it does not do

It is not a coding CLI, not a replacement for CrystalHarness, and not a
multi-turn meeting. It does not execute tools. It does not invent a new
answer by stitching proposals together.

## Requirements

- .NET 10 SDK
- A sibling checkout of Crystal at `../Crystal`
- A sibling checkout of CrystalHarness at `../CrystalHarness`
- API keys for the providers used by council seats (same resolution as
  CrystalHarness)

## Build

From this repository root:

```bash
dotnet build CrystalStudio.sln
dotnet test CrystalStudio.sln
```

The executable project is `CrystalStudio`.

## Run

```bash
dotnet run --project CrystalStudio
```

The first run writes `~/.crystal/studio/council.json` if it is missing,
and reads `~/.crystal/config.json` plus credentials the same way
CrystalHarness does.

| Option | Meaning |
| :--- | :--- |
| `--port <n>` | Listen port (overrides `council.json`) |
| `--studio-home <path>` | Council data directory (default: `CRYSTAL_STUDIO_HOME`, then `~/.crystal/studio`) |
| `--harness-home <path>` | Shared Harness home (default: `CRYSTAL_HOME`, then `~/.crystal`) |
| `--home <path>` | Alias for `--harness-home` |
| `--help` | Print the option list |

Ctrl+C stops the listener.

## Compatible API

Point any OpenAI Chat Completions client at the listen prefix.

```bash
curl http://127.0.0.1:18790/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d "{\"model\":\"crystal-council\",\"messages\":[{\"role\":\"user\",\"content\":\"Explain Borda count.\"}]}"
```

| Endpoint | Role |
| :--- | :--- |
| `POST /v1/chat/completions` | Run the council (`/chat/completions` is accepted) |
| `GET /v1/models` | List the advertised council model |
| `GET /health` | Liveness |

The inbound `model` field is not used to pick a seat. Every request
uses the configured council. `stream: true` emits SSE chunks: thinking
as `reasoning_content`, then the final answer, then `usage` on the
finish chunk.

Non-stream responses include `usage` on the completion object.

## Configuration

Council seats and listen settings:

```text
~/.crystal/studio/
  council.json
  logs/
```

Shared providers and secrets (Harness):

```text
~/.crystal/
  config.json
  credentials.json
```

Override the studio directory with `CRYSTAL_STUDIO_HOME` or
`--studio-home`. Override the Harness directory with `CRYSTAL_HOME` or
`--harness-home`.

Default `council.json` seats are `analyst`, `skeptic`, `engineer`, and
`chair`. Every default seat uses provider `deepseek` and model
`deepseek-v4-flash`. Edit a seat's `provider` and `model` to use any
entry already listed in the Harness catalog.

| Field | Meaning |
| :--- | :--- |
| `listen` | Absolute HttpListener prefix |
| `maxRounds` | Review rounds before forced adjudication (default `2`) |
| `convergenceThreshold` | Stop debate when average lexical similarity reaches this value (default `0.85`) |
| `memberTimeoutSeconds` | Per-member call timeout (default `180`) |
| `model` | Advertised model id (default `crystal-council`) |
| `members` | Seats: `id`, `persona`, `provider`, `model`, optional `chair` |

Exactly one seat should set `"chair": true`. If none do, the last seat
is the chair.

## Credentials

API keys are not stored under `~/.crystal/studio`. They are resolved
from the Harness home, in the same order as CrystalHarness:

1. Process environment (`DEEPSEEK_API_KEY`, `OPENAI_API_KEY`,
   `<PROVIDER>_API_KEY`, or `CRYSTAL_API_KEY`)
2. `providers.<name>.apiKey` in `~/.crystal/config.json`
3. `~/.crystal/credentials.json`

Do not put secrets in this repository or in `council.json`.

## Council protocol

1. **Propose.** Every member answers in isolation.
2. **Review.** Proposals are shuffled and relabeled. Members return a
   JSON ranking. Scores use Borda count.
3. **Decide.** Debate stops when proposals converge or `maxRounds` is
   hit. The chair confirms the leading original proposal. It does not
   rewrite it.

A member that times out or faults abstains for that round and may
still take part in the next one. Cancelling the HTTP request cancels
every in-flight Crystal call.

## Safety

Runtime text is plain English. Secrets are not written to logs or to
the thinking stream. High-risk tool calls (shell, write, delete, and
similar) that still have ranking disagreement are returned as text
asking for confirmation, not as an executable `tool_calls` payload.
