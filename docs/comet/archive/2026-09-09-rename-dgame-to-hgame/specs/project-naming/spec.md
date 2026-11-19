# 项目命名（HGame）

## 概述

项目框架、程序集、目录、资产字符串、构建脚本、代码生成模板、文档与 AI skill 统一使用 "HGame" 作为项目标识；"DGame" 仅允许残留在本机绝对路径（`F:\Test\DGame`）与 Git 分支名中。

## 命名映射

- 大小写敏感替换：`DGame` → `HGame`，`dgame` → `hgame`，`DGAME` → `HGAME`。
- 仅替换前一字符不是字母的 "dgame"；`LoadGameObject*`、`DLogger`、`m_RemovedGameObjects`、`m_AndroidGameId` 等不受影响。

## 源码

- 框架运行时命名空间为 `HGame`，编辑器扩展为 `HGame`，AOT 入口为 `HGame.AOT`，PSD 导出工具为 `HGame.PSD2UGUI`。
- 程序集：`HGame.Runtime`（`GameUnity/Assets/HGame/Runtime`）、`HGame.Editor`（`GameUnity/Assets/HGame/Editor`）、`HGame.AOT`（`GameUnity/Assets/HGame.AOT`）、`HGame.Obfuz`（`GameUnity/Assets/Obfuz`）。asmdef 的 GUID 与改名前一致，引用方按 GUID 引用无需改动。
- 框架自带类型：`HGameLog`、`HGameLogHelper`、`HGameLog2File`、`HGameLogLevel`、`HGameException`、`HGameDictionary`、`HGameLinkedList`、`HGameLinkedListRange`、`HGameMultiDictionary`、`HGameStringUtilHelper`、`HGameInspector`、`HGameUIGeneratorSettingsProvider`、`HGameHotfixSettingsProvider`、`HGameEditorTools`、`HGameLocalizationHelper`；文件名与类型名一致。
- `RootModule` 默认 helper 类型名为 `HGame.HGameStringUtilHelper`、`HGame.HGameLogHelper`、`HGame.DefaultJsonHelper`；`UpdateSettings.AOTMetaAssemblies` 默认包含 `HGame.Runtime.dll`；`TypeUtil` 程序集列表为 `HGame.Runtime`、`HGame.AOT`、`HGame.Editor`。
- 编辑器菜单前缀 `HGame Tools/`、`CreateAssetMenu` 前缀 `HGame/`、SettingsProvider 路径 `Project/HGame/`、EditorPrefs key 前缀 `HGame`。
- 日志编译宏为 `ENABLE_HGAME_*`。
- `HGameEditorTools` 判定框架资源路径为 `Assets/HGame/`。

## Unity 资产

- `ProjectSettings/HybridCLRSettings.asset` patchAOTAssemblies 包含 `HGame.Runtime.dll`。
- `ProjectSettings/Obfuz.asset` 相关程序集列表使用 `HGame.Runtime`、`HGame.AOT`。
- `ProjectSettings/AtlasRefWindowSettings.asset` 的 `m_EditorClassIdentifier` 使用 `HGame` 命名空间。
- `HGame/Runtime/Setting/UpdateSettings.asset` 的 `AOTMetaAssemblies` 为 `HGame.Runtime.dll`。
- `HGame/Runtime/Setting/Prefabs/GameEntry.prefab` 的三个 helper 类型名为 `HGame.HGameStringUtilHelper`、`HGame.HGameLogHelper`、`HGame.NewtonsoftJsonHelper`。
- `Sprites Shader.shader` 名为 `HGame/UI/Sprites Shader`。
- `Obfuz/SymbolObfus/symbol-mapping.xml` 中程序集与命名空间为 `HGame.*`。
- 所有被重命名目录与文件的 `.meta` 随之移动，内容不变。

## 构建与工具

- `Tools/BuildTools/*.bat|sh` 的 `-executeMethod` 目标为 `HGame.ReleaseTools.*`；示例路径使用 `HGame\GameUnity`。
- SourceGenerator 的 `Definition.cs`：`DefaultFrameworkNameSpace = "HGame"`、`DefaultDebugName = "HGame.DLogger"`、`UsingNameSpace` 含 `"HGame"`。
- Luban 模板 `GameConfig/CustomTemplate/Client/Bin/ConfigSystem.cs` 与生成的 `GameProto/ConfigSystem.cs` 使用 `using HGame;`。
- `GameUnity/.gitignore` 忽略 `Assets/HGame/AssetSetting/Resources/BuiltinFileManifest.asset(.meta)`。

## 文档与 AI 配置

- `README.md` 描述项目为 HGame，GitHub 链接为 `https://github.com/ifunxgame/HGame`。
- `CLAUDE.md`、`AGENTS.md` 引用 `hgame-dev` skill 与 `.claude/skills/hgame-dev/references/`。
- skill 目录 `.claude/skills/hgame-dev`、`.codex/skills/hgame-dev`（`name: hgame-dev`，触发词含 `HGame`）；`luban-dev` references 为 `hgame-integration.md`、`hgame-table-rules.md`。
- `.understand-anything/` 索引、`.claude/agents`、`.claude/scripts`、`.codex/agents`、`GameConfig/Tools/Luban工具缺失自动修复指南.md` 中的项目标识为 HGame。

## 编译状态

Unity 6000.3.10f1 以 batchmode 打开 `GameUnity` 工程时不产生 `error CS` 编译错误，也不出现引用 `DGame` 程序集或类型缺失的报错。
