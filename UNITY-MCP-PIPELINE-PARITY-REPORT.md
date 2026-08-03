# Unity-MCP 与 Unity Pipeline 技能能力对比报告

更新日期：2026-08-03  
目标工程：`E:\source\unity-cli-test`  
基线工程：`E:\source\unity-cli-test.worktrees\unity-mcp-IvanMurzak`

## 1. 目标与判定标准

本工作的目标是将 IvanMurzak/Unity-MCP 配套技能迁移为 `com.unity.pipeline` 可执行的技能，并验证 Pipeline 方案能完成相同类型的 Unity 编辑任务。

本报告中的“通过”采用**任务能力等价**标准：在两个工程中，分别使用原始 Unity-MCP 工具与 Unity Pipeline 命令，完成同一类创建、读取、修改、删除、烘焙或捕获任务，并回读结果确认。它不要求两个接口的命令名、输入对象引用格式或返回 JSON 字段完全相同。

本报告不把“能力域主路径已验证”误记为“每一个技能的每一个可选参数、失败分支和输出字段均已穷举”。第 8 节列出了仍未做逐技能独立覆盖的范围。

## 2. 测试环境与安全边界

| 项目 | Pipeline 候选工程 | Unity-MCP 基线工程 |
| --- | --- | --- |
| 工作目录 | `E:\source\unity-cli-test` | `E:\source\unity-cli-test.worktrees\unity-mcp-IvanMurzak` |
| Unity | 2022.3.62f3 | 2022.3.62f3 |
| 通信 | Pipeline Editor server，`localhost:7800` | Unity-MCP HTTP endpoint，`localhost:25147` |
| 使用权限 | Unity Pipeline | 仅用于对比测试的 Unity MCP 授权 |

遵循的约束：

- 不修改官方 `Packages/com.unity.pipeline`；所有新增兼容实现放入 `Packages/com.unity.pipeline.extensions`。
- 场景、Prefab、材质及其他 Unity 序列化资源仅经 Editor 的 Pipeline/MCP 命令修改。
- 每一轮临时测试都删除测试对象或新建默认空场景丢弃改动；收尾时两个工程均为未保存的默认空场景（`rootCount = 2`、`isDirty = false`）。
- 测试前检查打开场景是否 dirty，避免触发 Unity 的保存确认模态框。

## 3. 对比方法

每个能力域采用如下步骤：

1. 在基线工程调用与 IvanMurzak 技能对应的 MCP 工具，记录能否完成任务及回读结果。
2. 在候选工程调用翻译后的 Pipeline 技能所列命令；需要兼容实现时调用 extension 命令。
3. 对创建、修改、烘焙、捕获等操作回读 Unity 状态或资产数据。
4. 清除临时对象、临时资产和控制台测试噪声，并确认场景状态。

例如，GameObject 用例覆盖了“创建 Cube → 添加 Rigidbody → 将质量设为 2 → 使用 Unity 标准复制 → 删除副本和原对象”。两侧均成功；基线使用公开属性 `mass`，Pipeline 的批量序列化命令使用 `m_Mass`。这是接口/字段面不同，不是功能差异。

## 4. 已执行的能力域与结果

| 能力域 | 已执行的代表性用例 | 结果 | 备注 |
| --- | --- | --- | --- |
| 场景生命周期 | 创建、默认/空模板、加法打开、设为活动场景、保存、卸载、层级回读 | 通过 | 临时场景与文件夹均清理。 |
| GameObject 与组件 | 创建 primitive、Transform、查找、添加/读取/修改/移除组件、父子关系、原生批量复制、删除 | 通过 | `duplicate_gameobject` 兼容 Unity `(1)` 命名和返回源对象语义。 |
| 项目资源 | 文件夹创建、材质创建、URP Lit 属性读取与设置、复制、移动、查找、删除、AssetDatabase 刷新 | 通过 | 候选端还验证了 `get_shader_properties`。 |
| 内置资源 | 查找 `Resources/unity_builtin_extra` 中名称含 Default 的 Material | 通过 | 两边均返回 Default-Diffuse、Default-Line、Default-Material、Default-Particle、Default-ParticleSystem。 |
| Prefab Stage | 从场景对象生成 Prefab、打开、在 Stage 内创建/编辑、保存、关闭、实例化 | 通过 | 由 extensions 补齐 Prefab Stage 工作流。 |
| Timeline | Timeline 创建、轨道创建/查询/绑定、Clip 创建/移动/定时、Marker、PlayableDirector 绑定 | 通过 | 兼容实现覆盖 Timeline 版本差异。 |
| AnimationClip | 创建、曲线读取/修改、元数据、事件添加/清除、细节读取 | 通过 | 由 `AnimationClipExtensionCommands` 补充。 |
| Animator | AnimatorController、参数、Layer、State、Motion 与 Transition 的创建、读取和编辑 | 通过 | 覆盖图结构的主路径。 |
| 粒子系统 | 创建/获取 ParticleSystem，修改 Main、Emission、Shape、Noise 等模块 | 通过 | 由 `ParticleSystemExtensionCommands` 补充读取和受控修改。 |
| AI Navigation | Surface、Modifier、ModifierVolume、Link、Agent 的创建/查询/编辑；Surface 烘焙/清除 | 通过 | Surface 烘焙是本轮发现并修复的功能缺口，见第 5 节。 |
| 截图 | Camera、Game View、Scene View、隔离预览截图；隔离对象的构图控制 | 通过 | 由 `IsolatedCaptureCommands` 补齐高级隔离捕获。 |
| 反射与类型 | 可调用方法发现/调用、C# 类型 JSON Schema | 通过 | JSON 输出契约与 MCP 不同，但能完成类型发现和调用任务。 |
| Profiler | 启停、状态、模块、帧捕获、脚本/内存/渲染统计、保存/加载/清理 | 通过 | 结构化快照由 extensions 实现。 |
| 脚本生命周期 | 读、创建/更新、执行、删除、重新编译/状态轮询 | 通过 | 遵循 Pipeline recompile → status 流程。 |
| Editor 与选择 | 编辑器 Play/Pause/Stop、autotick、选择读写 | 通过 | 测试结束后停止播放。 |
| Console | 清理、按 Error 读取日志 | 通过 | 最终候选端为 0 条 Error。 |
| Package Manager | 已安装包列表、离线搜索 `com.unity.timeline` | 通过 | 两边均能返回 Timeline 1.8.12；包安装流程也在前序安装 Timeline 与 AI Navigation 时实际使用。 |
| Test Runner | Pipeline 定向执行兼容扩展 EditMode 测试 | 通过 | `CustomCommandRegistrationTests`：7/7 通过。基线工程无可发现测试，基线 `tests-run` 返回 No tests found。 |

### 最后一次回归验证记录

- Pipeline：`run_tests --mode editor --filter Unity.Pipeline.Extensions.Tests.Editor.CustomCommandRegistrationTests`，总数 7、通过 7、失败 0。
- Pipeline：`list_open_scenes` 返回一张无路径场景，`isDirty: false`、`rootCount: 2`；`get_console_logs --severity error` 返回 0 条。
- 基线：`scene-list-opened` 返回一张无路径场景，`IsDirty: false`、`RootCount: 2`。基线控制台中曾记录两条故意的测试失败（无测试以及首次错误字段名），后已执行 `console-clear-logs` 清理。

## 5. 发现的缺口与实现修改

所有兼容代码位于 `Packages/com.unity.pipeline.extensions`。官方 `com.unity.pipeline` 未被修改。

### 5.1 AI Navigation：NavMeshSurface 实际烘焙

**发现：** 官方 Pipeline 的 `bake_navmesh_surfaces` 在此版本是 v1 占位实现，不能实际构建 AI Navigation 的 `NavMeshSurface` 数据；旧的 `bake_navmesh` 只适用于 legacy NavMesh 设置，不能替代。

**修改：** 新增 `Editor/Commands/Navigation/NavMeshSurfaceBakeCommands.cs`，注册命令：

```powershell
unity command bake_navmesh_surfaces_compat
unity command bake_navmesh_surfaces_compat --target '{"hierarchyPath":"/NavigationSurface"}'
unity command bake_navmesh_surfaces_compat --target '{"hierarchyPath":"/NavigationSurface"}' --clear true
```

实现直接调用 `NavMeshSurface.BuildNavMesh()` 或 `RemoveData()`，支持全部已加载场景或单一目标，支持 `dry_run`，将 Surface、NavMeshData 与场景正确标记为 dirty，并返回烘焙前后数据存在状态。

**为何不用同名覆盖：** `com.unity.pipeline` 已注册 `bake_navmesh_surfaces`，而命令发现使用的同名优先级对 extension 没有稳定的覆盖保证。因此使用显式的 `_compat` 命令名，避免随机调用到官方占位实现。

**验证：** 创建 Plane 与 NavMeshSurface 后，dry run 不改变数据；实际烘焙得到 `HasNavMeshData = true`；组件回读显示 `m_NavMeshData` 已赋值；`--clear true` 后数据为 null。

相关依赖修改：

- `package.json` 增加 `com.unity.ai.navigation: 1.1.7`。
- `Unity.Pipeline.Extensions.Editor.asmdef` 增加 `Unity.AI.Navigation` 引用。
- `mcp/skills/navigation-surface-bake/SKILL.md` 与 `.agents/skills/navigation-surface-bake/SKILL.md` 改为调用 `bake_navmesh_surfaces_compat`，并明确 legacy 命令不可替代。

### 5.2 其余已加入的兼容命令

下列命令是在前序对比中根据实际缺口加入或完善，并由本轮定向回归测试覆盖命令发现、序列化或核心行为：

| 文件/模块 | 补齐的主要能力 |
| --- | --- |
| `Animation/AnimationClipExtensionCommands.cs` | AnimationClip 元数据、事件添加/清除、详细曲线与事件数据。 |
| `Animation/TimelineExtensionCommands.cs` | Timeline 轨道、Clip、Marker、绑定等兼容编辑流程。 |
| `Capture/IsolatedCaptureCommands.cs` | 隔离截图与更细的预览/构图控制。 |
| `GameObjects/CompatibilityGameObjectCommands.cs` | 原生批量复制与组件类型发现等兼容语义。 |
| `GameObjects/ParticleSystemExtensionCommands.cs` | ParticleSystem 主模块及常用子模块读取/写入。 |
| `Prefabs/PrefabStageCommands.cs` | Prefab Stage 打开、创建、保存、关闭。 |
| `Reflection/ReflectionCommands.cs` | 方法元数据、受控调用与兼容 JSON Schema。 |
| `Observability/*` | Profiler 控制与结构化快照。 |
| `Scenes/CompatibilitySceneCommands.cs` | 与 MCP 技能匹配的场景操作辅助。 |

## 6. 重要接口差异及其影响

| 差异 | 影响评估 |
| --- | --- |
| ObjectRef | Pipeline 使用 `hierarchyPath`、`guid`、`globalId`、`instanceId` 等引用；基线常用 `instanceID`/`path`。技能已按 Pipeline 写法翻译。 |
| 序列化字段与公开属性 | Pipeline 常修改 `m_Mass` 等序列化字段，基线反射接口常修改 `mass` 等公开属性。对同一 Unity 状态的修改均可完成。 |
| JSON Schema / 返回结构 | 非字段级兼容；不影响“发现类型、选择参数、调用方法、读取结果”的任务能力。 |
| 异步模型 | Pipeline recompile/test 使用 status 轮询；MCP 可能在单次调用中等待或使用自身处理。技能文档已写明 Pipeline 流程。 |
| `tool-set-enabled-state` | Ivan MCP 可持久化启用/禁用 MCP 工具。Pipeline 为直接 CLI 命令注册机制，extension 无法安全地禁用官方命令；这属于 MCP 服务治理能力，而非 Unity 内容编辑能力。 |

## 7. 技能安装与文档状态

- 翻译后的技能已安装到 `.agents/skills/`，并保留项目中的 `mcp/skills/` 作为技能源。
- 与新增命令有关的技能文档已同步更新，尤其是 navigation-surface-bake。
- 技能编写以 `unity command ...` 为标准入口，遵守 `UNITY-GUIDE.md`：多 Editor 情况需显式指定实例，变更前查询、变更后回读，避免直接改写 Unity 序列化资源。

## 8. 覆盖边界与后续建议

当前结论是：**所有主要技能功能域均已有可完成同类 Unity 任务的实测证据；未完成逐技能、逐参数、逐异常分支的完整测试矩阵。**

尚未作为独立对比用例穷举的范围包括：

- 每个技能所有可选参数、无效引用、权限/包缺失、取消与超时分支；
- Animator、Timeline、ParticleSystem、Profiler 的全部细粒度字段与模块组合；
- Package add/remove 的重复安装、离线/网络失败与卸载回滚场景；
- Test Runner 的完全同测试集对照（基线工程当前没有可发现的测试）；
- MCP 的工具启停、Schema 字段和日志载荷等服务层契约。

若下一步要求“所有技能逐项完成”，建议以技能目录为测试矩阵：每个 `SKILL.md` 至少一条成功用例、一条关键失败/边界用例，并记录基线命令、Pipeline 命令、回读断言、清理步骤和结果。可以在该矩阵上继续增量测试，而不需要重做本报告中的已通过主流程。

## 9. 当前工作树说明

本报告描述的是当前未提交的扩展工作与技能文档。工作树中还存在用户此前已有的修改、自动生成的技能目录及包锁文件变化；本轮没有创建提交，也没有将官方 `Packages/com.unity.pipeline` 纳入修改范围。
