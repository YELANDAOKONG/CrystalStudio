# Crystal Studio 模型理事会

[English](README_en.md)

本地运行的 OpenAI 兼容多模型协商服务。一次入站聊天请求会由多个模型并行回答：先隔离提案，再匿名互评，最后由主席从已有提案里选出一份，而不是把几份答案缝成一份从未被验证过的新内容。

模型调用走 Crystal 的 `IChatClient` 契约。提供商目录和 API Key 与
[CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness)
共享，放在 `~/.crystal`。理事会自己的席位配置在 `~/.crystal/studio/councils`。

本项目不是编程 CLI，也不是
[CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness)
的替代品。

## 它做什么

- 在本机端口开放 `POST /v1/chat/completions`、`GET /v1/models`、
  `GET /health`（默认 `http://127.0.0.1:18790/`）。
- 三阶段议事：隔离提案、匿名排序（Borda 计分）、主席确认。
- 把理事会正在做的事以 `reasoning_content`（兼容接口的思考字段）流式返回。最终的 `content` 或 `tool_calls` 一定是某位成员提过的原始方案。
- 汇总每一次成员调用的 token：`prompt_tokens`、`completion_tokens`、
  `total_tokens`；若提供商报了思考 token，还会带
  `completion_tokens_details.reasoning_tokens`。
- 理事会不执行工具。选中的一份方案里的全部 `tool_calls` 原样返回（可一次多条，便于并行读取），由调用方（任意兼容的 Harness）按自己的批准策略执行。

理事会没有自己的会话。每一次 HTTP 请求都是基于完整入站记录的一次性推导。它不执行工具，也不把多份提案融合成新答案。

## 运行要求

- .NET 10 SDK
- 旁路检出 Crystal，路径为 `../Crystal`
- 旁路检出 [CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness)，路径为 `../CrystalHarness`
- 各席位所用提供商的 API Key（解析顺序与 CrystalHarness 相同）

## 教程

### 1. 把三个仓库放在一起

在同一父目录下并列克隆 Crystal、CrystalHarness 和本仓库：

```bash
git clone https://github.com/YELANDAOKONG/CrystalHarness.git
git clone <本仓库地址> CrystalStudio
```

相对本仓库根目录，Crystal 必须在 `../Crystal`，
[CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness)
必须在 `../CrystalHarness`。

### 2. 把 API Key 放到 Harness 能读到的地方

不要把密钥写进本仓库。凭据路径与
[CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness)
相同：

```bash
export DEEPSEEK_API_KEY=your-api-key-here
```

也可以写 `~/.crystal/credentials.json`，或在
`~/.crystal/config.json` 的 `providers.<name>.apiKey` 里配置。进程环境变量优先。

默认两个理事会的席位都是 `deepseek` / `deepseek-v4-flash`。如果你已经在跑
Harness，同一把 Key 这里也能用。

### 3. 构建并启动理事会

```bash
cd CrystalStudio
dotnet build CrystalStudio.sln
dotnet test CrystalStudio.sln
dotnet run --project CrystalStudio
```

首次启动会在 `~/.crystal/studio/councils/` 写出默认的 `coding.json`
（模型 id `crystal-council`）和 `writing.json`（模型 id `crystal-writing`）。
`~/.crystal/config.json` 的读法与 CrystalHarness 相同。

控制台应出现类似：

```text
Crystal Studio council listening on http://127.0.0.1:18790/
```

保持该进程运行。Ctrl+C 停止。

### 4. 用兼容接口冒烟测试

```bash
curl http://127.0.0.1:18790/v1/models
curl http://127.0.0.1:18790/health
curl http://127.0.0.1:18790/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d "{\"model\":\"crystal-council\",\"messages\":[{\"role\":\"user\",\"content\":\"解释一下 Borda 计分。\"}]}"
curl http://127.0.0.1:18790/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d "{\"model\":\"crystal-writing\",\"messages\":[{\"role\":\"user\",\"content\":\"给这本书起一个冷峻的开篇。\"}]}"
```

加上 `"stream": true` 可以在 `reasoning_content` 里看到议事进度。

### 5. 接到 CrystalHarness

在 [CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness)
的 `~/.crystal/config.json` 里增加一个指向理事会的 OpenAI 兼容提供商。
**必须**打开 `replayReasoningContent`：

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

然后用该提供商和模型启动 Harness：

```bash
cd ../CrystalHarness
dotnet run --project CrystalHarness -- --provider council --model crystal-council
```

不设 `replayReasoningContent` 时，第一轮能通，第二轮用户消息会在
Harness 本地报 `cannot replay reasoning blocks on Chat Completions`，
请求到不了理事会。官方 OpenAI Chat Completions 拒绝回放该字段；理事会用它传思考，所以必须打开这个旗标。

### 6. 改席位

编辑 `~/.crystal/studio/councils/` 下的 JSON。每个文件是一个理事会，
`model` 字段是对外广告的模型 id。每个席位是一套人设，外加
Harness 目录里已经存在的提供商和模型。支持思考的模型可再写
`thinking`：`default`（提供商默认）、`off`（关掉思考），或等级
`minimal` / `low` / `medium` / `high` / `maximum`。模型不支持思考时该字段会被忽略。改完后重启 Crystal Studio。
再加一个理事会：在该目录放一份新的 `*.json` 即可。

## 启动参数

| 选项 | 含义 |
| :--- | :--- |
| `--port <n>` | 监听端口（覆盖理事会文件里的 `listen`） |
| `--studio-home <path>` | 理事会数据目录（默认 `CRYSTAL_STUDIO_HOME`，否则 `~/.crystal/studio`） |
| `--harness-home <path>` | 共享的 Harness 主目录（默认 `CRYSTAL_HOME`，否则 `~/.crystal`） |
| `--home <path>` | `--harness-home` 的别名 |
| `--help` | 打印选项 |

## 兼容接口

| 路径 | 作用 |
| :--- | :--- |
| `POST /v1/chat/completions` | 召开理事会（也接受 `/chat/completions`） |
| `GET /v1/models` | 列出对外广告的全部理事会模型 |
| `GET /health` | 探活 |

入站 `model` 字段用来选择召开哪一个理事会。省略时使用
`crystal-council`（若该 id 存在）。未知模型返回 400。
`stream: true` 时以 SSE 推送：思考走 `reasoning_content`，然后是最终答案，结束 chunk 带 `usage`。非流式响应在 completion 对象上带 `usage`。

## 配置

理事会席位和监听：

```text
~/.crystal/studio/
  councils/
    coding.json
    writing.json
  logs/
```

与 [CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness)
共享的提供商和密钥：

```text
~/.crystal/
  config.json
  credentials.json
```

理事会目录可用 `CRYSTAL_STUDIO_HOME` 或 `--studio-home` 覆盖。
Harness 目录可用 `CRYSTAL_HOME` 或 `--harness-home` 覆盖。

默认 `coding.json` 席位是 `analyst`、`skeptic`、`engineer`、`chair`，
对外模型 id 为 `crystal-council`。
默认 `writing.json` 席位是 `architect`、`stylist`、`critic`、`chair`，
对外模型 id 为 `crystal-writing`。
默认全部使用提供商 `deepseek`、模型 `deepseek-v4-flash`。

| 字段 | 含义 |
| :--- | :--- |
| `listen` | 绝对的 HttpListener 前缀（可选；多份文件若都写了，必须一致） |
| `maxRounds` | 强制裁决前的互评轮数上限（默认 `2`） |
| `convergenceThreshold` | 提案平均字面相似度达到该值即停止辩论（默认 `0.85`） |
| `memberTimeoutSeconds` | 单个成员调用超时（默认 `180`） |
| `model` | 对外广告的模型 id（缺省时用文件名去掉 `.json`） |
| `members` | 席位：`id`、`persona`、`provider`、`model`，可选 `chair`、`thinking` |

每个理事会应恰好有一个席位 `"chair": true`。都没标的话，最后一个席位当主席。

## 凭据

API Key 不写在 `~/.crystal/studio` 下。解析顺序与
[CrystalHarness](https://github.com/YELANDAOKONG/CrystalHarness)
相同：

1. 进程环境变量（`DEEPSEEK_API_KEY`、`OPENAI_API_KEY`、
   `<PROVIDER>_API_KEY` 或 `CRYSTAL_API_KEY`）
2. `~/.crystal/config.json` 里的 `providers.<name>.apiKey`
3. `~/.crystal/credentials.json`

不要把密钥写进本仓库或 `councils/*.json`。

## 议事协议

1. **提案。** 每位成员隔离作答。
2. **互评。** 打乱顺序并抹去身份。成员返回 JSON 排序。票数用 Borda 计分。
3. **裁决。** 观点收敛或达到 `maxRounds` 即停止。主席确认得票最高的那份**原始**提案，不重写。

超时或出错的成员本轮弃权，下一轮仍可参加。取消 HTTP 请求会取消所有进行中的 Crystal 调用。

## 安全

运行时文本为英文。密钥不会写入日志或思考流。理事会不执行工具；选中方案里的全部 `tool_calls` 原样返回，由调用方按自己的批准策略决定是否执行。
