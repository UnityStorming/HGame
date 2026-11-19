---
generated_from_state_version: 8
---

# 验证

## 当前结果

- 结果: **已归档**
- 验证情况: **已完成检查，验证结果已确认**
- 目标周期: 1
- 迭代: 1
- 验证器尝试次数: 1
- 完成时间: 2026-09-14T17:26:46.336Z
- 摘要: 在 worktree 上以只读方式独立核对 A1–A9 全部通过：残留扫描命中集合精确等于允许集合；两份 mcp-visual.md/mcp-unity-cli.md/evals.json 各自一致且内容满足工具名 ⊆149、URP 重置说明、HGame 落位与 uGUI 流程、已核实来源等要求；SKILL.md/CLAUDE.md/AGENTS.md 路由与描述已切换到 mcp-unity-cli.md/mcp-visual.md 且不再引用 mcp-tools.md；evals.json 仅四用例变化；replace 规格仅第 260 行变化；全局 unity-mcp-skill 已删除；git 改动范围与编码均合规。

## 验收

| 编号 | 结果 | 来源 | 验收项 | 原因 |
| --- | --- | --- | --- | --- |
| A1 | passed | brief.md | A1: 在 worktree 中执行 `rg -n -i --hidden "coplay\|unity-mcp\|McpForUnity\|manage_(gameobject\|script\|asset\|scene\|components\|prefabs\|editor\|ui\|material\|shader\|texture\|animation\|camera\|graphics\|vfx\|scriptable_object)\|batch_execute\|execute_menu_item\|unity_reflect\|get_test_job\|refresh_unity\|execute_code"`（排除 `.git/`、`.comet/`、`.codegraph/`、`.understand-anything/`、`.worktrees/`、`GameUnity/Library\|Temp/`、`docs/comet/archive/`、`docs/comet/changes/`、`docs/unity-cli-mcp-test-report.md`），命中只允许是：`CLAUDE.md`、`AGENTS.md` 各 1 行「MCP 工具链」移除说明；两份 `mcp-unity-cli.md` 各 1 行「已核实来源」移除说明；本 change 的 replace 规格 `docs/comet/specs/unity-cli-mcp-test-report/spec.md`（归档后）或其它文件 0 命中；两份 `evals.json` 中只在 `forbidden_patterns` 出现 `manage_`。 | git grep 残留扫描命中恰好 5 行且每文件 1 行：两份 mcp-unity-cli.md「已核实来源」(行10)、CLAUDE.md:41 与 AGENTS.md:40「MCP 工具链」、docs/comet/specs/…/spec.md:260（A1 允许的归档前规格）；.mcp.json 与 evals.json 对该正则 0 命中，evals.json 的 4 处 manage_ 全在 id 11/12/13/18 的 forbidden_patterns |
| A2 | passed | brief.md | A2: `.claude/skills/hgame-dev/references/mcp-tools.md` 与 `.codex/skills/hgame-dev/references/mcp-tools.md` 不存在；两份 `mcp-visual.md` 内容一致，其「工具速查」表首列的每个反引号工具名都属于 `unity list --project-path GameUnity --json` 返回的 149 个工具，且文中含 URP 材质 `renderQueue` / 关键字被重置的说明与 "VFX Graph / Timeline 未安装" 的标注。 | 两份 mcp-tools.md 不存在；两份 mcp-visual.md cmp 一致；工具速查表首列 28 个反引号名全部属于 149 工具集；第 58/168 行含 URP renderQueue/_EMISSION 关键字被 MaterialPostprocessor 重置说明；第 26/116 行标注 Timeline 与 VFX Graph 未安装（语义满足，非逐字短语） |
| A3 | passed | brief.md | A3: 两份 `mcp-unity-cli.md` 内容一致，含二级标题「HGame 落位与 uGUI Prefab 流程」「常用菜单项」「常见错误」；「HGame 落位与 uGUI Prefab 流程」含路径表（至少 `Assets/Scripts/HotFix/GameLogic/UI/`、`Assets/Scripts/HotFix/GameProto/`、`Assets/BundleAssets/UI/`、`Assets/BundleAssets/UIRaw/`）、CanvasScaler 推荐值（`ScaleWithScreenSize`、`1920`、`1080`、`MatchWidthOrHeight`、`0.5`）、`create_prefab` 到 `Assets/BundleAssets/UI/` 的步骤与对 `naming-rules.md` 的引用；「已核实来源」含 `7600c17e` 与 "`mcp-tools.md` 已删除"。 | 两份 mcp-unity-cli.md cmp 一致；三个二级标题存在(206/245/257)；路径表含 4 条必需路径；CanvasScaler 含 ScaleWithScreenSize/[1920,1080]/MatchWidthOrHeight/0.5；create_prefab 保存到 Assets/BundleAssets/UI/<XxxUI>.prefab；引用 naming-rules.md；已核实来源含 7600c17e 与「`mcp-tools.md` 已删除」 |
| A4 | passed | brief.md | A4: 两份 `SKILL.md` 的文档路由表都不含 `mcp-tools.md`，都含指向 `mcp-unity-cli.md` 与 `mcp-visual.md` 的行；两份 frontmatter `description` 都不含 `CoplayDev`，都含 `unity-cli` 与 `com.unity.pipeline`。 | 两份 SKILL.md 均无 mcp-tools，路由表均有 mcp-unity-cli.md 与 mcp-visual.md 行；frontmatter description 均无 CoplayDev、含 unity-cli 与 com.unity.pipeline（两份第 31–32 行的差异为基线已存在，非本 change 引入） |
| A5 | passed | brief.md | A5: `CLAUDE.md` 与 `AGENTS.md`：「MCP 工具链」行与「Reference 路由」表不含 `mcp-tools.md`，含 `mcp-unity-cli.md` 与 `mcp-visual.md`；「MCP 工具链」行含 `7600c17e` 与 `manage_*`（移除说明）。 | CLAUDE.md/AGENTS.md 0 处 mcp-tools；「MCP 工具链」行含 mcp-unity-cli.md、mcp-visual.md、7600c17e、manage_*；Reference 路由表含两份 mcp 文档行 |
| A6 | passed | brief.md | A6: 两份 `evals.json` 内容一致、可被 `json.load` 解析、`evals` 仍为 19 项且 `skill_name` 不变；id 11/12/13/18 的 `prompt` 与 `description` 不含 `CoplayDev`，`expected_patterns` 中出现的工具名均属于 149 个 unity-cli 工具（id 11 含 `write_text_file` 或 `create_script` 与 `Assets/Scripts/HotFix/GameLogic/UI`；id 12 含 `create_asset`、`set_material_properties`、`Assets/BundleAssets/Materials`、`Universal Render Pipeline/Lit`；id 13 含 `create_animator_controller`、`add_animator_parameter`、`add_animator_state`、`add_animator_transition`；id 18 含 `Assets/BundleAssets/UI` 与 `UIWindow`），四个用例的 `forbidden_patterns` 都含 `manage_`；其余 15 个用例与 `7600c17e` 版本逐字相同。 | 两份 evals.json cmp 一致、json 可解析、19 项、skill_name=hgame-dev；与 7600c17e 比较仅 id 11/12/13/18 变化；四用例无 CoplayDev、forbidden 含 manage_、expected_patterns 工具名全部 ∈149、各自必需字面项齐全 |
| A7 | passed | brief.md | A7: 本 change 目录 `specs/unity-cli-mcp-test-report/spec.md` 存在（replace），其「hgame-dev 参考文档」第 1 条不含 `com.coplaydev.unity-mcp` 对比描述而含"已移除"；其余内容与 `docs/comet/specs/unity-cli-mcp-test-report/spec.md` 逐字相同。 | change 目录 replace 规格存在，与 docs/comet/specs 主规格 diff 仅 260c260，新行为「已移除（提交 7600c17e）」记录而非对比描述；主规格/archive/测试报告/.mcp.json/GameUnity 相对基线无改动 |
| A8 | passed | brief.md | A8: `C:\Users\BingLau\.claude\skills\unity-mcp-skill` 目录不存在。 | test -d /c/Users/BingLau/.claude/skills/unity-mcp-skill 不存在 |
| A9 | passed | brief.md | A9: worktree 中 `git status --porcelain --untracked-files=all` 只包含：`CLAUDE.md`、`AGENTS.md`、两份 `SKILL.md`、两份 `mcp-unity-cli.md`、两份 `mcp-visual.md`、两份被删除的 `mcp-tools.md`、两份 `evals.json`、`docs/comet/changes/remove-coplay-mcp-guidance/**`；`.mcp.json`、`docs/unity-cli-mcp-test-report.md`、`docs/comet/archive/**`、`docs/comet/specs/**`、`GameUnity/**` 无改动；所有改动文本文件 UTF-8 且不含 U+FFFD。 | git status 恰为 12 个允许的修改/删除文件 + 4 个 docs/comet/changes/remove-coplay-mcp-guidance/** 未跟踪文件，无其他路径；14 个改动文本文件均 UTF-8、0 个 U+FFFD |

## 检查

| 检查 | 命令 | 工作目录 | 状态 | 退出码 | 耗时 |
| --- | --- | --- | --- | ---: | ---: |
| A1 残留扫描：CoplayDev / unity-mcp / manage_* 等只出现在允许的入口说明行 | F:/Test/DGame/.comet/runtime/checks/rcmg-residual.sh | . | passed | 0 | 603 ms |
| A2–A9 内容检查：mcp-tools.md 不存在、镜像一致、工具名属于 149 集合、章节与路由、evals 四用例、replace 规格单行差异、全局 skill 不存在、git 范围与 U+FFFD | F:/Test/DGame/.comet/runtime/checks/rcmg-content.sh | . | passed | 0 | 439 ms |

## 阻塞项

_无。_

## 风险与跳过的工作

- 两份 SKILL.md 存在基线即有的第 31–32 行差异（模块使用/模块系统架构行），本 change 未引入也未修复（brief 非目标），建议后续单独同步
- A2 的 'VFX Graph / Timeline 未安装' 以语义标注满足（mcp-visual.md 第 26/116 行），非逐字短语
- 归档后 replace 规格第 260 行仍含 com.coplaydev.unity-mcp（作为已移除记录），未来残留扫描需保留该允许项

## 之前的迭代

| 目标周期 | 迭代 | 尝试 | 结果 | 未解决项 | 摘要 | 完成时间 |
| ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 1 | 1 | pass | — | 在 worktree 上以只读方式独立核对 A1–A9 全部通过：残留扫描命中集合精确等于允许集合；两份 mcp-visual.md/mcp-unity-cli.md/evals.json 各自一致且内容满足工具名 ⊆149、URP 重置说明、HGame 落位与 uGUI 流程、已核实来源等要求；SKILL.md/CLAUDE.md/AGENTS.md 路由与描述已切换到 mcp-unity-cli.md/mcp-visual.md 且不再引用 mcp-tools.md；evals.json 仅四用例变化；replace 规格仅第 260 行变化；全局 unity-mcp-skill 已删除；git 改动范围与编码均合规。 | 2026-09-14T17:26:46.336Z |



## 结论

在 worktree 上以只读方式独立核对 A1–A9 全部通过：残留扫描命中集合精确等于允许集合；两份 mcp-visual.md/mcp-unity-cli.md/evals.json 各自一致且内容满足工具名 ⊆149、URP 重置说明、HGame 落位与 uGUI 流程、已核实来源等要求；SKILL.md/CLAUDE.md/AGENTS.md 路由与描述已切换到 mcp-unity-cli.md/mcp-visual.md 且不再引用 mcp-tools.md；evals.json 仅四用例变化；replace 规格仅第 260 行变化；全局 unity-mcp-skill 已删除；git 改动范围与编码均合规。
