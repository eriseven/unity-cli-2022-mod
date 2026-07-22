# Unity CLI 2022 技术修改适配

> [认识 Unity CLI：在终端里操作 Unity](https://unity.com/cn/blog/meet-the-unity-cli)
>
> 开发越来越多地发生在终端——脚本里、CI 里，乃至 AI 代理手中。Unity CLI 顺势而生：一个快速、统一的 `unity` 命令，把 Unity 由外及内地带进终端——安装与管理编辑器、用脚本驱动运行中的编辑器，甚至无需重新编译即可在工程内执行 C#。

---

本仓库提供**经过最小修改的 Unity CLI 安装脚本**，用于解决官方安装地址被重定向到国区 CDN 并安装了老版本 `0.1.x`（缺少 `pipeline` / `command` 等命令）的问题。

- **唯一功能改动**：当显式指定安装版本号时，跳过 SHA-256 校验。原因是官方“浮动清单”（`latest-beta.json`）里的校验值属于清单自身的（旧）版本，与你指定的新版本二进制并不匹配。二进制本身仍从 Unity **官方 CDN** 按版本号路径下载。
- **本仓库不包含**修改版的 `com.unity.pipeline` 包，也不分发任何 Unity 二进制文件。`com.unity.pipeline` 在 Unity 2022 下的适配方式见下文。

---

## 🛠️ 安装 Unity CLI

### ✅ 官方安装方法

```
# macOS / Linux
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
```

```
# Windows (PowerShell)
$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex
```

安装完成后，运行 `unity --version` 确认版本。

### ⚠️ 修改版安装方法（网络被重定向时）

**如果装到的版本号低于 `1.0.0`（例如 `0.1.0`）**，说明官方下载地址在你的网络里被重定向了。此时请改用「**指定版本号 + 本仓库的公开安装脚本**」重新安装——脚本会跳过校验，重新按「当前已知最新版本」`1.0.0-beta.2` 进行安装：

```
# macOS / Linux
curl -fsSL https://raw.githubusercontent.com/rocwood/unity-cli-2022-mod/main/cli/install.sh | UNITY_CLI_CHANNEL=beta UNITY_CLI_VERSION=1.0.0-beta.2 bash
```

```
# Windows (PowerShell)
$env:UNITY_CLI_CHANNEL='beta'; & ([scriptblock]::Create((irm https://raw.githubusercontent.com/rocwood/unity-cli-2022-mod/main/cli/install.ps1))) -Target 1.0.0-beta.2
```

---

## 📦 安装 Pipeline 包

### ✅ 官方安装方法（适用于 Unity 6.0+）

```
# 默认作用于当前目录
unity pipeline install

# 或指定工程路径：
unity pipeline install --project-path /path/to/your/unity/project
```

### ⚠️ Unity 2022 安装方法

`com.unity.pipeline` 官方包面向较新的 Unity 6.0+ 版本，Unity 2022 工程无法通过 CLI 安装或在 Package Manager 中添加。

- 打开任意一款 AI 编程工具（如 Codex/Claude Code）
- 让 AI 从 Unity Registry 抓取 `com.unity.pipeline` 源码放入工程
- 让 AI 修复 `PhysicsMaterial` 与 `Material.rawRenderQueue` 等编译错误
- 修复完成后即可在 Unity 2022 工程内正常使用。

---

## ⚖️ 免责声明

> 本仓库以 MIT 许可发布，但该许可仅适用于本仓库作者所做的修改及原创内容（安装脚本的改动、README 等文档）。本仓库中的 Unity CLI 安装脚本改编自 Unity Technologies 的官方安装器；Unity CLI 及其下载的二进制文件均为 Unity Technologies 的财产，仍受 Unity 适用的许可条款、服务条款及第三方许可证约束。
>
> 本仓库包含对 Unity 官方 CLI 安装脚本的最小修改版本，作为在 Unity 2022 环境下的兼容性与技术方案评估参考。本仓库不包含 Unity Pipeline 包本身，也不分发任何 Unity 二进制文件——安装脚本仅从 Unity 官方 CDN 下载官方发布的二进制。
>
> 本仓库无意替代 Unity Technologies 提供的任何官方软件或软件包。原始软件、脚本及其中的源代码、二进制文件和其他受保护材料，仍受其适用的许可条款、服务条款及第三方许可证约束。
>
> 本项目与 Unity Technologies 无关联，亦未获其赞助、认可或授权。“Unity”及相关名称、标识和商标均归 Unity Technologies 或其关联方所有；本仓库中对其的提及仅用于识别兼容目标，不构成任何商标使用许可。
>
> 本仓库按“现状”提供，不提供任何明示或默示保证，包括但不限于适销性、特定用途适用性、非侵权性、稳定性、安全性或与任何 Unity 版本的兼容性保证。使用者应自行测试，并自行承担使用本项目所产生的一切风险与责任。
```