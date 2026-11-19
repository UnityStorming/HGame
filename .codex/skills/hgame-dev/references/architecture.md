# HGame 架构与项目结构

> **适用场景**：理解项目分层架构、程序集划分、目录结构、启动流程 | **关联文档**：[modules.md](modules.md)、[hotfix-workflow.md](hotfix-workflow.md)、[resource-api.md](resource-api.md)

## 核心架构

### 分层架构

```
┌─────────────────────────────────────────────┐
│            热更业务层 (HotFix)              │
│  GameLogic / GameBattle / GameProto         │
│  ↑ UI、业务模块、战斗逻辑、Luban 生成代码    │
└─────────────────────────────────────────────┘
                     ↓ 依赖
┌─────────────────────────────────────────────┐
│            AOT 启动层                        │
│  HGame.AOT / Procedure / LoadAssembly        │
│  ↑ 主包流程、热更 DLL 加载、AOT metadata     │
└─────────────────────────────────────────────┘
                     ↓ 依赖
┌─────────────────────────────────────────────┐
│            HGame 框架层                      │
│  HGame.Runtime / ModuleSystem / Resource     │
│  ↑ 项目级框架封装与模块系统                  │
└─────────────────────────────────────────────┘
                     ↓ 依赖
┌─────────────────────────────────────────────┐
│            基础设施层                        │
│  Unity / HybridCLR / YooAsset / UniTask / Luban │
└─────────────────────────────────────────────┘
```

**依赖规则**：上层依赖下层，`HGame.Runtime` 不引用 `GameLogic`；AOT 层通过反射调用热更入口。

---

### 程序集划分

| 程序集 | 路径 | 热更 | 职责 |
|--------|------|------|------|
| `HGame.Runtime` | `GameUnity/Assets/HGame/Runtime` | 否 | 框架运行时、模块、通用工具 |
| `HGame.Editor` | `GameUnity/Assets/HGame/Editor` | 否 | 编辑器工具、打包工具、Inspector |
| `HGame.AOT` | `GameUnity/Assets/HGame.AOT` | 否 | AOT 流程、资源更新、热更 DLL 加载 |
| `GameProto` | `GameUnity/Assets/Scripts/HotFix/GameProto` | 是 | Luban 生成代码和 `ConfigSystem` |
| `GameLogic` | `GameUnity/Assets/Scripts/HotFix/GameLogic` | 是 | 主热更业务、UI、业务模块 |
| `GameBattle` | `GameUnity/Assets/Scripts/HotFix/GameBattle` | 是 | 战斗域逻辑 |

**约束**：
- `GameLogic` 可依赖 `GameProto` 和 `HGame.Runtime`。
- `HGame.Runtime` 只提供框架能力，不引用任何热更业务程序集。
- `HGame.AOT` 通过反射调用热更入口 `GameStart.Entrance`，不直接依赖热更类型。

---

### 目录结构

```
HGame/
├── GameConfig/                         # Luban 配置工程
│   ├── Datas/                          # Excel 数据源
│   ├── Datas/Defines/                  # __tables__/__beans__/__enums__
│   └── GenerateTool_Binary/            # 导表脚本
│
└── GameUnity/Assets/
    ├── HGame/                          # HGame 框架封装
    │   ├── Runtime/                    # 运行时核心与模块
    │   └── Editor/                     # 编辑器工具
    ├── HGame.AOT/                      # 主包 AOT 流程
    ├── Scripts/HotFix/                 # 热更代码
    │   ├── GameProto/                  # Luban 生成代码
    │   ├── GameLogic/                  # 主业务热更
    │   └── GameBattle/                 # 战斗热更
    ├── BundleAssets/                   # YooAsset 热更资源
    └── Scenes/GameStart/               # 主启动场景
```

---

### 资源目录与 PRELOAD

| 目录/标签 | 用途 | 注意 |
|-----------|------|------|
| `GameUnity/Assets/BundleAssets` | HGame YooAsset 热更资源根目录 | 所有热更资源统一放这里 |
| `GameUnity/Assets/BundleAssets/DLL` | 热更 DLL 和 AOT metadata 的默认 `.dll.bytes` 输出目录 | 实际目录由 `UpdateSettings.AssemblyTextAssetPath` 配置 |
| `GameUnity/Assets/AssetArt` | 非热更资源根目录（如图集 `Atlas`） | 按资源类型划分子目录，不参与 YooAsset 热更收集 |
| `PRELOAD` 标签 | AOT `PreloadProcedure` 会通过 `GetAssetInfos("PRELOAD")` 预加载 | 适合启动前必须可用的小体量资源 |
| `WEBGL_PRELOAD` 标签 | WebGL 下额外预加载 | 仅 `UNITY_WEBGL` 分支处理 |

`PreloadProcedure` 位于 `GameUnity/Assets/HGame.AOT/Procedure/PreloadProcedure.cs`，通过 `m_resourceModule.GetAssetInfos("PRELOAD")` 枚举资源并 `LoadAssetAsync`。新增 PRELOAD 资源前确认体量和启动耗时，避免把大 UI、大模型、可延迟加载资源放入启动预加载。

location/addressable 约定：

- 新增运行时资源优先放 `GameUnity/Assets/BundleAssets/...`，并通过 YooAsset 收集配置生成 location。
- 业务代码通过 `GameModule.ResourceModule` 加载资源，不使用 `Resources.Load()`。
- `UpdateSettings.EnableAddressable` 为 true 时，DLL location 可直接使用 `GameLogic.dll`、`HGame.Runtime.dll` 这类地址。
- `UpdateSettings.EnableAddressable` 为 false 时，`LoadAssemblyProcedure` 会按 `Assets/<AssemblyTextAssetPath>/<dll><AssemblyTextAssetExtension>` 拼接 location；当前默认配置是 `Assets/BundleAssets/DLL/<dll>.bytes`。
- `LoadAssemblyProcedure.cs:316` 的 `Resources.Load<TextAsset>("Obfuz/defaultStaticSecretKey")` 是 AOT 框架层加载 Obfuz 密钥的例外，不作为热更业务资源加载模板。

---

### 渲染管线（URP）

- 工程使用 URP 17.3（Unity 6000.3 内置），不再使用 Built-in 管线；颜色空间保持 Gamma。
- URP 资产统一放在 `GameUnity/Assets/Settings/URP/`（`HGame_URPAsset.asset`、`HGame_URPRenderer.asset`、全局设置与默认 Volume Profile），不在 `BundleAssets` 内，随 Player 内置；六个质量等级共用同一个 URP Asset。
- 相机采用 URP 堆叠：场景主相机为 Base，`UIRoot.prefab` 的 `UICamera` 为 Overlay 并挂 `HGame.UIOverlayCameraBinder`（`Assets/HGame/Runtime/Module/RenderPipeline/`），场景加载后自动把 UI 相机加入当前 `Camera.main` 的堆叠；`UICanvas` 为 Screen Space - Camera 并绑定 `UICamera`。新场景的主相机只需是 Base 相机即可。
- 校验：菜单 `HGame Tools/Render Pipeline/Validate URP Setup`，或批处理 `-executeMethod HGame.URPSetupValidator.RunBatch -reportPath <json>`（不要加 `-nographics`，包含 UI 渲染烟雾测试）；`HGame Tools/Render Pipeline/Migrate To URP` 可重复执行迁移配置。
- 自定义 UI Shader 为无 `LightMode` 的 CG Shader，URP 以 `SRPDefaultUnlit` 渲染；新增 Shader 请使用 URP 兼容写法。

---

### 启动流程

主启动场景：`GameUnity/Assets/Scenes/GameStart/GameStart.unity`。

```
LaunchProcedure
  → SplashProcedure
  → InitPackageProcedure
  → InitResourceProcedure
  → CreateDownloaderProcedure / PreloadProcedure
  → DownloadFileProcedure / PreloadProcedure
  → DownloadOverProcedure / ClearCacheProcedure / PreloadProcedure
  → PreloadProcedure
  → LoadAssemblyProcedure
  → StartGameProcedure
  → GameStart.Entrance(object[] objects)
```

源码分支：

- `SplashProcedure` 进入 `InitPackageProcedure`。
- `InitPackageProcedure` 初始化 YooAsset Package 后进入 `InitResourceProcedure`。
- `InitResourceProcedure` 在编辑器、单机、WebGL 或边玩边下模式可直接进入 `PreloadProcedure`。
- HostPlay 且需要完整下载时进入 `CreateDownloaderProcedure`，再走 `DownloadFileProcedure`、`DownloadOverProcedure` 或 `ClearCacheProcedure`，最后进入 `PreloadProcedure`。

`LoadAssemblyProcedure` 通过反射查找 `GameStart.Entrance`，`objects[0]` 是 `List<Assembly>`。

---

## 使用模式

### 常见落位

| 需求 | 推荐路径 |
|------|----------|
| 新增框架运行时模块 | `GameUnity/Assets/HGame/Runtime/Module/<Name>Module` |
| 新增编辑器工具 | `GameUnity/Assets/HGame/Editor` |
| 新增业务 UI | `GameUnity/Assets/Scripts/HotFix/GameLogic/UI/<Feature>` |
| 新增业务模块 | `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/<Feature>Module` |
| 新增配置消费封装 | `GameUnity/Assets/Scripts/HotFix/GameLogic/ConfigMgr` 或对应模块 |
| 新增热更资源 | `GameUnity/Assets/BundleAssets/<Category>` |
| 新增热更 DLL bytes | `GameUnity/Assets/BundleAssets/DLL` |

---

## 常见错误

| 错误 | 原因 | 修复 |
|------|------|------|
| 把业务 UI 放进 `HGame.Runtime` | Runtime 不热更，改动需重新出包 | 放到 `GameLogic/UI`，保持热更 |
| 在 `HGame.Runtime` 引用 `GameLogic` | 会形成下层依赖上层的反向依赖 | Runtime 只提供框架能力，不依赖业务 |
| 误认为配置生成代码可手改 | 生成代码会在下次导表被覆盖 | 修改 `GameConfig` 源数据或模板后重新导表 |
| 热更入口写成 `Entrance(List<Assembly>)` | AOT 层按 `object[]` 反射调用入口 | 实际签名是 `Entrance(object[] objects)` |
| 新资源放到 `BundleAssets` 之外的目录 | 热更资源必须被 YooAsset 收集 | 统一放 `GameUnity/Assets/BundleAssets` |
| 热更业务使用 `Resources.Load()` | 绕过 YooAsset 无法热更和统一管理 | 使用 `GameModule.ResourceModule`；Obfuz 密钥读取是 AOT 框架层例外 |
| 大资源滥用 `PRELOAD` | 启动预加载会拉长首屏耗时 | 只预加载启动必须资源 |

---

## 交叉引用

- 模块 API 见 [modules.md](modules.md)
- 热更开发见 [hotfix-workflow.md](hotfix-workflow.md)
- UI 开发见 [ui-lifecycle.md](ui-lifecycle.md)
- 资源加载见 [resource-api.md](resource-api.md)
