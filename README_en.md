# Crystal Studio Model Council

[中文](README.md)

A local OpenAI-compatible Model Council. One inbound chat request is
answered by several models in parallel: isolated proposals, anonymous
peer review, then a chair that selects an original proposal instead of
inventing a fused answer.

Crystal Studio talks to models through the Crystal `IChatClient`
contracts. Provider catalogs and API keys are shared with
[CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness) under
`~/.crystal`. Council seats live in `~/.crystal/studio/councils`.

This product is not a coding CLI and not a replacement for
[CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness).

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
- Returns every `tool_calls` entry on the selected proposal as-is
  (several independent reads in one turn). Crystal Studio does not
  execute tools; the caller (any compatible harness) applies its own
  approval policy before running them.

The council has no session of its own. Each HTTP request is a one-shot
derivation from the full inbound transcript. It does not execute tools
and does not stitch proposals into a new answer.

## Requirements

- .NET 10 SDK
- A sibling checkout of Crystal at `../Crystal`
- A sibling checkout of [CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness)
  at `../CrystalHarness`
- API keys for the providers used by council seats (same resolution as
  CrystalHarness)

## Tutorial

### 1. Check out the siblings

From a parent directory, clone Crystal, CrystalHarness, and this repo
next to each other:

```bash
git clone https://github.com/YELANDAOKONG/CrystalHarness.git
git clone <this-repository-url> CrystalStudio
```

Crystal must sit at `../Crystal` relative to this repository root.
CrystalHarness must sit at `../CrystalHarness`.

### 2. Put an API key where Harness can see it

Do not put secrets in this repository. Use the same credential path as
[CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness):

```bash
export DEEPSEEK_API_KEY=your-api-key-here
```

Or write `~/.crystal/credentials.json` / `providers.<name>.apiKey` in
`~/.crystal/config.json`. Environment variables win.

Default council seats all use `deepseek` / `deepseek-v4-flash`. If you
already run Harness, the same key works here.

### 3. Build and start the council

```bash
cd CrystalStudio
dotnet build CrystalStudio.sln
dotnet test CrystalStudio.sln
dotnet run --project CrystalStudio
```

The first run writes `~/.crystal/studio/councils/coding.json`
(model id `crystal-council`) and `writing.json` (model id
`crystal-writing`) if they are missing. Harness settings are read from
`~/.crystal/config.json` the same way CrystalHarness does.

You should see a listen line similar to:

```text
Crystal Studio council listening on http://127.0.0.1:18790/
```

Leave that process running. Ctrl+C stops it.

### 4. Smoke-test the compatible API

```bash
curl http://127.0.0.1:18790/v1/models
curl http://127.0.0.1:18790/health
curl http://127.0.0.1:18790/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d "{\"model\":\"crystal-council\",\"messages\":[{\"role\":\"user\",\"content\":\"Explain Borda count.\"}]}"
curl http://127.0.0.1:18790/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d "{\"model\":\"crystal-writing\",\"messages\":[{\"role\":\"user\",\"content\":\"Write a cold opening for this book.\"}]}"
```

Add `"stream": true` to watch council progress in `reasoning_content`.

### 5. Connect CrystalHarness

In [CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness)
`~/.crystal/config.json`, add an OpenAI-compatible provider that points
at the council. `replayReasoningContent` must be `true`:

```json
{
  "provider": "council",
  "model": "crystal-council",
  "providers": {
    "council": {
      "protocol": "openai",
      "baseUri": "http://127.0.0.1:18790/v1/",
      "replayReasoningContent": true,
      "tokenLimit": "max_tokens",
      "apiKey": "local",
      "models": {
        "crystal-council": {
          "contextWindow": 200000
        },
        "crystal-writing": {
          "contextWindow": 200000
        }
      }
    }
  }
}
```

Then start Harness with that provider and model:

```bash
cd ../CrystalHarness
dotnet run --project CrystalHarness -- --provider council --model crystal-council
```

Without `replayReasoningContent`, the first turn works and the second
user message fails in Harness with `cannot replay reasoning blocks on
Chat Completions`. The request never reaches the council. Official
OpenAI Chat Completions rejects that field; the council uses it for
thinking, so the flag must be on.

### 6. Change seats

Edit the JSON files under `~/.crystal/studio/councils/`. Each file is
one council; the `model` field is the advertised model id. Each seat is
a persona plus a provider and model that already exist in the Harness
catalog. Models that can think also accept `thinking`: `default` (the
provider default), `off` (disable thinking), or an effort
(`minimal`, `low`, `medium`, `high`, `maximum`). The field is ignored
when the model cannot think. Restart Crystal Studio after editing. Add
another council by dropping another `*.json` into that directory.

## Run options

| Option | Meaning |
| :--- | :--- |
| `--port <n>` | Listen port (overrides `listen` in council files) |
| `--studio-home <path>` | Council data directory (default: `CRYSTAL_STUDIO_HOME`, then `~/.crystal/studio`) |
| `--harness-home <path>` | Shared Harness home (default: `CRYSTAL_HOME`, then `~/.crystal`) |
| `--home <path>` | Alias for `--harness-home` |
| `--help` | Print the option list |

## Compatible API

| Endpoint | Role |
| :--- | :--- |
| `POST /v1/chat/completions` | Run the council (`/chat/completions` is accepted) |
| `GET /v1/models` | List the advertised council models |
| `GET /health` | Liveness |

The inbound `model` field selects which council to convene. When it is
omitted, `crystal-council` is used if that id exists. An unknown model
returns 400. `stream: true` emits SSE chunks: thinking as
`reasoning_content`, then the final answer, then `usage` on the finish
chunk. Non-stream responses include `usage` on the completion object.

## Configuration

Council seats and listen settings:

```text
~/.crystal/studio/
  councils/
    coding.json
    writing.json
  logs/
```

Shared providers and secrets
([CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness)):

```text
~/.crystal/
  config.json
  credentials.json
```

Override the studio directory with `CRYSTAL_STUDIO_HOME` or
`--studio-home`. Override the Harness directory with `CRYSTAL_HOME` or
`--harness-home`.

Default `coding.json` seats are `analyst`, `skeptic`, `engineer`, and
`chair`; the advertised model id is `crystal-council`.
Default `writing.json` seats are `architect`, `stylist`, `critic`, and
`chair`; the advertised model id is `crystal-writing`.
Every default seat uses provider `deepseek` and model
`deepseek-v4-flash`.

| Field | Meaning |
| :--- | :--- |
| `listen` | Absolute HttpListener prefix (optional; if several files set it, they must match) |
| `maxRounds` | Review rounds before forced adjudication (default `2`) |
| `convergenceThreshold` | Stop debate when average lexical similarity reaches this value (default `0.85`) |
| `memberTimeoutSeconds` | Per-member call timeout (default `180`) |
| `model` | Advertised model id (falls back to the file name without `.json`) |
| `members` | Seats: `id`, `persona`, `provider`, `model`, optional `chair` and `thinking` |

Each council should have exactly one seat with `"chair": true`. If none do, the last seat
is the chair.

## Credentials

API keys are not stored under `~/.crystal/studio`. They are resolved
from the Harness home, in the same order as
[CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness):

1. Process environment (`DEEPSEEK_API_KEY`, `OPENAI_API_KEY`,
   `<PROVIDER>_API_KEY`, or `CRYSTAL_API_KEY`)
2. `providers.<name>.apiKey` in `~/.crystal/config.json`
3. `~/.crystal/credentials.json`

Do not put secrets in this repository or in `councils/*.json`.

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
the thinking stream. Crystal Studio does not execute tools. Selected
`tool_calls` on the selected proposal are returned as-is; the caller
decides whether to run them under its own approval policy.
