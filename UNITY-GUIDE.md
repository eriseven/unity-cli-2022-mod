# Unity 操作规范

本文规定 Agent 进行 Unity 开发时的工具选择、内容操作边界与验证要求。

## 1. 核心工具

- 统一通过 Unity CLI/Pipeline 与 Unity 交互，不使用 Unity MCP。
- 与 Unity 交互时必须使用 `unity-pipeline` 技能，并遵循其中的命令、参数、状态轮询和操作流程说明。
- 存在多个可连接的 Unity Editor 或开发版 Player 时，必须显式指定目标，避免误操作。
- 使用 `unity command` 查看当前实例支持的命令，不假定所有实例或 Pipeline 版本能力相同。

## 2. 操作边界

### 可直接编辑

- 普通 `.cs` 源码由 Agent 直接创建或修改。
- Shader、CSV、JSON、Markdown 等纯文本也可直接编辑。
- 涉及多个脚本或文本文件时，先批量完成修改，再统一让 Unity 刷新和编译，避免逐文件导入及重复的编译与重载。
- Pipeline 不可达时仅可进行上述纯文本编辑，不得修改序列化资源。若未能完成 Unity 编译、测试或运行验证，须明确说明。

### 必须使用 Pipeline

- 创建或修改场景、Prefab、Material、ScriptableObject、AnimationClip、AnimatorController、Timeline 等由 Unity 管理的序列化内容。
- 导入图片、模型、音频、插件、DLL 等外部资产。
- 移动、重命名、复制或删除 `Assets/` 下的既有文件和文件夹。
- 场景对象操作，包括 GameObject 的创建、删除、层级、Transform、组件及其序列化字段。
- Unity 工程设置，包括 Build Settings、Tags/Layers、Input、Quality、Graphics、Player Settings 等。
- 场景、Prefab、材质和视觉效果等项目内容，应在编辑器中制作并持久化，提供必要的可调参数。不得以运行时代码临时创建来替代资产制作。

### 禁止直接操作


- 禁止手动创建、复制、修改或删除 `.meta` 文件，尤其不得改写其中的 GUID。
- 禁止使用文件系统工具复制、移动、重命名或删除 `Assets/` 下的既有资源。

- 禁止手动修改 Unity 序列化资源文件，如 `.unity`、`.prefab`、`.mat`、`.asset`、`.controller`、`.anim` 等。
- 禁止手动修改 Unity 生成内容或本地状态文件，如 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`*.csproj`、`*.sln` 等。
- 禁止以字节方式改写二进制资产。

## 3. 操作原则

- 操作 Unity 对象前先查询并确认目标，仅执行任务所需的最小操作，完成后回读确认。
- 更改序列化字段、组件类或程序集结构时，必须同步维护相关资源的兼容与脚本绑定。
- 大范围重新导入、切换构建目标、构建 Player 或批量改写资源必须获得明确授权。
- 优先使用专用操作及其提供的类型校验、Undo、预览和确认机制。
- 仅在缺少合适专用操作时，才使用 `eval` 类命令在编辑器中执行 C# 代码。代码应限于任务所需的最小范围，并按需处理 Undo、SerializedObject、AssetDatabase、脏标记和保存流程。

## 4. 验证与收尾

- 修改代码或资源后，必须通过相关测试或最小场景验证；涉及视觉改动时，还须检查实际画面。
- 验证时区分历史 Console 日志与本轮新增错误，避免将历史错误归因于当前改动。
- Scene、Prefab 编辑和 Play Mode 等临时状态结束后，必须按意图保存或放弃修改，并恢复 Editor 状态。
