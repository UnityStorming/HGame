# 目标

把项目从 DGame 全面改名为 HGame：源码命名空间、程序集（asmdef）、目录与文件名、Unity 资产中的字符串引用、构建脚本、代码生成模板、文档和 AI skill 配置中的 "DGame" 标识统一替换为 "HGame"，改名后 Unity 工程能正常编译。

# 范围

- C#：`namespace DGame*` → `namespace HGame*`，`using DGame*` → `using HGame*`，`DGame.` 限定引用 → `HGame.`；`DGame*` 命名的类型/文件（`DGameLog`、`DGameLogHelper`、`DGameLog2File`、`DGameLogLevel`、`DGameException`、`DGameDictionary`、`DGameLinkedList`、`DGameLinkedListRange`、`DGameMultiDictionary`、`DGameStringUtilHelper`、`DGameInspector`、`DGameUIGeneratorSettingsProvider`、`DGameHotfixSettingsProvider`、`DGameEditorTools`、`DGameLocalizationHelper`）→ `HGame*`。
- 程序集：`DGame.Runtime` / `DGame.Editor` / `DGame.AOT` / `DGame.Obfuz` 四个 asmdef 的文件名与 `name`、`rootNamespace` → `HGame.*`；GUID 不变。
- 目录：`GameUnity/Assets/DGame` → `HGame`、`GameUnity/Assets/DGame.AOT` → `HGame.AOT`、`GameUnity/Assets/Editor/DGameSettingsProvider` → `HGameSettingsProvider`、`Runtime/Core/DGameLog` → `HGameLog`、`Runtime/Core/DGameStruct` → `HGameStruct`；对应 `.meta` 随目录/文件一起移动，内容不变。
- Unity 资产中的字符串：`ProjectSettings/HybridCLRSettings.asset`、`ProjectSettings/Obfuz.asset`、`ProjectSettings/AtlasRefWindowSettings.asset`、`Runtime/Setting/UpdateSettings.asset`、`Runtime/Setting/Prefabs/GameEntry.prefab`（反射类型名）、`Obfuz/SymbolObfus/symbol-mapping.xml`、`Sprites Shader.shader`（Shader 名 `HGame/UI/Sprites Shader`）。
- 反射与字符串常量：`RootModule.cs` 默认 helper 类型名、`UpdateSettings.cs` 的 `HGame.Runtime.dll`、`TypeUtil.cs` 程序集名列表、`DGameEditorTools.cs` 的 `Assets/HGame/` 路径判断、`DGameLogHelper.cs` 堆栈过滤文件名、EditorPrefs key、`MenuItem("HGame Tools/...")`、`CreateAssetMenu("HGame/...")`、`Project/HGame/...` SettingsProvider 路径、编译宏 `ENABLE_DGAME_*` → `ENABLE_HGAME_*`。
- 构建与工具：`Tools/BuildTools/*.bat|sh` 的 `-executeMethod HGame.ReleaseTools.*` 及示例路径；`Tools/Generata Tools/SourceGenerator/*/Definition.cs` 的 `DefaultFrameworkNameSpace`/`DefaultDebugName`/`UsingNameSpace`；`GameConfig/CustomTemplate/Client/Bin/ConfigSystem.cs` 与生成的 `GameProto/ConfigSystem.cs` 的 `using HGame;`；`GameUnity/.gitignore` 中的 `Assets/HGame/...` 路径。
- 文档与 AI 配置：`README.md`（含 GitHub 链接 → `ifunxgame/HGame`）、`CLAUDE.md`、`AGENTS.md`、`.claude/skills/dgame-dev` → `.claude/skills/hgame-dev`（SKILL.md `name: hgame-dev`、触发词、references）、`.codex/skills/dgame-dev` → `hgame-dev`、`luban-dev` 的 `dgame-integration.md` → `hgame-integration.md`、`dgame-table-rules.md` → `hgame-table-rules.md` 及其引用、`improve-codebase-architecture`、`.claude/agents/*`、`.claude/scripts/kg_query.py`、`.codex/agents/*`、`GameConfig/Tools/Luban工具缺失自动修复指南.md`、`.understand-anything/*`。
- `.understand-anything/` 下已跟踪的生成索引做同样的文本替换（保持与源码一致）。

# 非目标

- 不改名仓库根目录 `F:\Test\DGame`（需要在文件系统层面由用户在 Unity 与本会话关闭后自行完成）；因此不修改 `.codex/hooks.json`、`.claude/settings.local.json` 中指向 `F:\Test\DGame\...` 的本机绝对路径。
- 不修改 `DLogger`、`LoadGameObject*`、`m_RemovedGameObjects`、`m_AndroidGameId` 等仅因子串或大小写不敏感匹配到 "dgame" 的标识。
- 不修改 Unity `productName`（当前 `GameUnity`）、`companyName`、包名、YooAsset `packageName`（当前均不含 DGame）。
- 不重命名本地 Git 分支 `DGame_Unity6+`、不改动 Git 历史。
- 不处理 Unity 自动生成且未跟踪的 `GameUnity/*.csproj`、`GameUnity.sln`、`.vs/`、`UserSettings/`、`Library/`。
- 不借改名之机重构或调整任何逻辑。

# 验收示例

- A1: 在仓库文本文件中执行边界感知搜索 `rg -i -P '(?<![A-Za-z])dgame'`（排除 `.git/`、`.codegraph/`、`GameUnity/Library|Temp|Logs|obj/`、`GameUnity/*.csproj`、`GameUnity/*.sln`、`GameUnity/.vs/`、`GameUnity/UserSettings/`、`.comet/`、`docs/comet/`、`.claude/hooks/*.log`、`Tools/Generata Tools/SourceGenerator/*/obj|bin/`），剩余命中只允许是 `.codex/hooks.json` 与 `.claude/settings.local.json` 中的本机绝对路径 `F:\Test\DGame`。
- A2: 目录 `GameUnity/Assets/HGame`、`GameUnity/Assets/HGame.AOT`、`GameUnity/Assets/Editor/HGameSettingsProvider`、`GameUnity/Assets/HGame/Runtime/Core/HGameLog`、`GameUnity/Assets/HGame/Runtime/Core/HGameStruct`、`.claude/skills/hgame-dev`、`.codex/skills/hgame-dev` 存在，对应旧 DGame 路径不存在；`git status` 中所有 `.meta` 均表现为重命名（内容 diff 为空），即 GUID 全部保留。
- A3: 四个 asmdef 文件名与 `name` 字段分别为 `HGame.Runtime`、`HGame.Editor`、`HGame.AOT`、`HGame.Obfuz`（`rootNamespace` 为 `HGame` 或保持原空值），`guid`（.meta）不变；`HybridCLRSettings.asset` 的 patchAOTAssemblies、`Obfuz.asset` 的程序集列表、`UpdateSettings.asset` 与 `UpdateSettings.cs` 的 `AOTMetaAssemblies`、`TypeUtil.cs` 的程序集名列表均使用 `HGame.*`。
- A4: `RootModule.cs` 与 `GameEntry.prefab` 中的反射类型名为 `HGame.HGameStringUtilHelper`、`HGame.HGameLogHelper`、`HGame.NewtonsoftJsonHelper`/`HGame.DefaultJsonHelper`，且这些类型在 `HGame` 命名空间下真实存在；`AtlasRefWindowSettings.asset` 的 `m_EditorClassIdentifier` 使用 `HGame` 命名空间。
- A5: 以 Unity 6000.3.10f1 用 `-batchmode -quit -logFile` 方式打开 `GameUnity` 工程，日志中没有 `error CS` 编译错误，也没有 "Assembly with name 'DGame" / 找不到 `DGame` 类型的报错。
- A6: `Tools/BuildTools/*.bat|sh` 中 `-executeMethod` 目标全部为 `HGame.ReleaseTools.*`；SourceGenerator 三个 `Definition.cs` 中 `DefaultFrameworkNameSpace = "HGame"`、`DefaultDebugName = "HGame.DLogger"`、`UsingNameSpace` 含 `"HGame"`；`GameConfig/CustomTemplate/Client/Bin/ConfigSystem.cs` 与 `GameUnity/Assets/Scripts/HotFix/GameProto/ConfigSystem.cs` 均为 `using HGame;`。
- A7: 改名前后 `rg -c 'LoadGameObject'` 与 `rg -c 'DLogger'` 在源码目录的命中总数不变；`GameUnity/Assets/YooAsset/**/LoadGameObjectOperation.cs` 路径不变。
- A8: `README.md`、`CLAUDE.md`、`AGENTS.md` 中不再出现 DGame / `dgame-dev`，改为 HGame / `hgame-dev`；`README.md` 的 GitHub 链接指向 `https://github.com/ifunxgame/HGame`；`.claude/skills/hgame-dev/SKILL.md` frontmatter `name: hgame-dev`。

# 约束与不变量

- 替换为大小写敏感的三种映射：`DGame` → `HGame`、`dgame` → `hgame`（kebab/文件名）、`DGAME` → `HGAME`（宏）；只替换单词边界前不是字母的 "dgame"，避免误伤 `LoadGameObject` 等。
- 所有目录/文件重命名必须连带 `.meta`，使用 `git mv` 保留历史，GUID 不变；asmdef 之间的引用按 GUID 维持，无需改引用。
- Build 开始前 Unity 编辑器（当前正打开 `F:\Test\DGame\GameUnity`，PID 14284）必须关闭，避免目录改名期间 Unity 重导入产生新的 `.meta` 或锁文件冲突；改名完成后再用 batchmode 编译验证。
- 只改与改名直接相关的文本，不顺手重构。

# 决策

- D1（用户 2026-09-08 确认）: 改名范围包含 `DGame*` 前缀的 15 个类型名及其文件名（如 `DGameLog` → `HGameLog`），调用方同步替换，而不只是命名空间/程序集/目录。
- D2（用户 2026-09-08 确认）: AI skill 目录与名称同步改为 `hgame-dev`（`.claude`、`.codex` 两份），`luban-dev` 的 `dgame-integration.md`、`dgame-table-rules.md` 改为 `hgame-*`；CLAUDE.md / AGENTS.md / README 的引用同步。
- D3（用户 2026-09-08 确认）: Build 开始前由用户手动关闭 Unity 编辑器；Agent 在 Unity 关闭后执行目录改名，并用 batchmode 编译做验证，之后由用户重新打开工程。
- D4（Agent 决定）: 仓库根目录名与本机绝对路径不在本 change 内处理，见非目标。
- D5（Agent 实现选择）: `.understand-anything/` 已跟踪的生成索引做文本替换而不是重新生成；`symbol-mapping.xml` 同样做文本替换。
- D6（Agent 实现选择，2026-09-09）: Roslyn 源生成器 `SourceGenerator.dll`、`UIControllerGenerator.dll` 内嵌 `using DGame;` 字符串会导致生成代码引用不存在的命名空间而编译失败，因此用 dotnet 8 SDK 以 Release 从 `Tools/Generata Tools/SourceGenerator` 重新编译并替换 `GameUnity/Assets/HGame/Runtime/Core/GameEvent/SourceGenerator.dll` 与 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule/UIControllerGenerator.dll`（源码自上次 DLL 提交后除改名外无变化）。

# 待解决问题

- （无）目标、范围、非目标、A1–A8 验收项与 D1–D5 决策已由用户于 2026-09-09 确认，进入 Build。

# 验证预期

- 边界感知 `rg` 扫描确认无残留（A1、A7、A8）。
- 文件系统与 `git status --short` 检查目录/文件重命名及 `.meta` 保留（A2、A3）。
- 针对反射类型名、程序集名、构建脚本、模板的定点 `rg` 检查（A3、A4、A6）。
- Unity 6000.3.10f1 batchmode 编译日志检查（A5）。
