---
generated_from_state_version: 11
---

# 验证

## 当前结果

- 结果: **已归档**
- 验证情况: **已完成检查，验证结果已确认**
- 目标周期: 2
- 迭代: 2
- 验证器尝试次数: 1
- 完成时间: 2026-09-11T22:38:51.115Z
- 摘要: 八项验收全部通过。报告恰好覆盖 unity list 的 149 个工具且执行方式与 Spec 策略表逐一一致；唯一失败项 clear_baked_lighting 与 8 个预期不可用均有明确原因并落入允许集合；bake_lighting 仅记录 dry_run 预览经源码核实是因为该命令没有 confirm 拒绝门且真实烘焙被 brief 禁止，不构成 A3 未通过；仓库改动集合、临时目录、ProjectSettings/Packages、GameStart 哈希、编辑器终态、两份 skill 文档与四处路由登记、六条 CLI 冒烟均经 Verifier 独立实测符合要求。

## 验收

| 编号 | 结果 | 来源 | 验收项 | 原因 |
| --- | --- | --- | --- | --- |
| A1 | passed | brief.md | A1: 报告文件 `docs/unity-cli-mcp-test-report.md` 存在，开头有环境段（Unity `6000.3.10f1`、`unity` CLI `1.0.0-beta.9`、`com.unity.pipeline` `0.6.0-exp.1`、测试日期、执行档位），工具结果表恰好覆盖 `unity list --project-path GameUnity --json` 返回的 149 个工具名，每个工具恰好一行，结果列取值仅限 `通过`、`失败`、`预期不可用`、`跳过` 四种。 | 报告存在且环境段含 Unity 6000.3.10f1 / CLI 1.0.0-beta.9 / com.unity.pipeline 0.6.0-exp.1 / 日期 2026-09-12 / 深度档；Verifier 自行运行 unity list 得 149 个名字，报告 §3 解析出 149 行、无重复、缺失与多余均为空，15 类计数全部相符；结果列仅 通过140/失败1/预期不可用8 |
| A2 | passed | brief.md | A2: 结果为 `失败` 或 `跳过` 的每一行都写明原因；`预期不可用` 只出现在依赖缺失包或未连接 Player 的工具上（`create_timeline`、`add_timeline_track`、`add_timeline_clip`、`get_timeline`、`bake_navmesh_surfaces`、`reload_file_player_interpreter`，以及 `audit` / `audit_status` 在返回 `unavailable` 时）；报告末尾有「发现的问题」段，逐条记录实测异常、与文档不符或可疑行为（无则写"无"）。 | 唯一失败行 clear_baked_lighting（第 181 行）写明 dry_run 被 confirm 门拦截（问题 1）；无跳过行；8 个预期不可用恰为允许集合且 audit/audit_status 返回 unavailable；§5 发现的问题 16 条 |
| A3 | passed | brief.md | A3: 报告中每个工具的执行方式与 Spec「执行策略表」一致；策略为「真实执行」的工具结果只能是 `通过` 或 `失败`，不能是 `跳过`；策略为「dry_run + 拒绝门」的工具需同时记录 `dry_run=true` 的预览返回与 `confirm=false` 时的拒绝返回。 | 程序化比对 149/149 工具执行方式与 Spec 策略表一致，无真实执行+跳过；7 个 dry_run+拒绝门行中 6 个按工具能力记录了可获得的返回。bake_lighting 只记录 dry_run 预览不构成未通过：源码 LightingBakeCommands.cs:58 声明 confirm 但 61–98 行从未读取，dryRun=false 即无条件 BakeAsync，拒绝返回���物理上不存在，获取 confirm=false 返回必须真实烘焙而 brief 非目标禁止；Spec 该行输入要点仅 dry_run=true；与 switch_build_target 无 dry_run 参数对称；缺门事实已记为问题 2 并写入 skill 安全门 |
| A4 | passed | brief.md | A4: 报告记录的端到端链路结果满足：`create_script` 后 `recompile_status` 到达 `completed`/`up_to_date` 且 `attach_script` 成功；临时测试程序集写入后 `list_tests` 数量 ≥ 1，`run_tests` 后 `test_status` 报告全部通过；`batch` 的 `dry_run` 不产生对象、事务成功批次创建的对象可被 `find_gameobjects` 找到、含故意失败操作的事务批次回滚后其对象不存在。 | 报告 §3.6/§3.12/§3.15/§4：recompile_status up_to_date failed:false，attach_script type= 与 script= 各成功；list_tests 0→2，run_tests/test_status completed 2/2 passed；batch dry_run applied:0，事务成功 McpTxDone 可被 find_gameobjects 找到，失败批次 applied:1 reverted:true 且 McpTxFail 不存在；GameUnity/Temp/mcp-test/worklog.md 第 54/60/67 行独立佐证 |
| A5 | passed | brief.md | A5: 测试结束后，相对测试前的 `git status --porcelain` 只包含 `docs/unity-cli-mcp-test-report.md`、`docs/comet/**`、`GameUnity/Assets/Scenes/GameStart/GameStart.unity`（仅来自测试前的 `save_all` 写盘）以及 D8 列出的 skill/路由文档；`GameUnity/Assets/PipelineMcpTest/`（及 `.meta`）与 `GameUnity/AgentScripts/` 不存在；`GameUnity/ProjectSettings/*.asset`、`GameUnity/Packages/manifest.json`、`GameUnity/Packages/packages-lock.json` 无改动；`GameStart.unity` 在测试前 `save_all` 之后的 SHA-256 与测试结束时一致（报告记录该哈希与 `git diff --stat` 结果）。 | git status --porcelain --untracked-files=all 恰为两份 SKILL.md、CLAUDE.md、AGENTS.md、GameStart.unity、两份 mcp-unity-cli.md、docs/comet/** 三文件、报告；PipelineMcpTest(.meta)/AgentScripts/Assets/Temp/ProjectSettings/Packages/com.unity.pipeline 均不存在；ProjectSettings 与 Packages 无 diff；GameStart SHA-256 61dfc63a…23ad23cfff 与报告一致，diff --stat 4 ++++ |
| A6 | passed | brief.md | A6: 测试结束后编辑器终态：`list_open_scenes` 只含 `Assets/Scenes/GameStart/GameStart.unity` 且 `isActive=true`、`isDirty=false`；`editor_status` 的 `playMode=stopped`、`status=ready`；`get_authoring_root` 返回 `Assets`；`get_tags_layers` 不含 `McpTestTag`；`get_build_settings` 的场景列表与测试前一致。 | list_open_scenes 仅 GameStart isActive=true isDirty=false；editor_status status=ready playMode=stopped；get_authoring_root=Assets；get_tags_layers 无 McpTestTag；get_build_settings.scenes 仅 GameStart enabled，与 HEAD 的 EditorBuildSettings.asset 一致 |
| A7 | passed | brief.md | A7: `.claude/skills/hgame-dev/references/mcp-unity-cli.md` 与 `.codex/skills/hgame-dev/references/mcp-unity-cli.md` 内容一致，包含「已核实来源」（包名与版本、`.mcp.json` 配置、`unity list` 数量 149）、「安全门」（`confirm` / `dry_run` 约定与永不真实执行的工具清单）、「路径与 authoring root」、「按分类的工具速查」（覆盖 15 类、149 个工具名）、「端到端链路模板」（脚本编译、测试、批处理、Play 往返）与「已知问题」（与报告「发现的问题」一致）六节；两份 `SKILL.md` 的文档路由表、根 `CLAUDE.md` 与 `AGENTS.md` 的「第一步」主题表及「Reference 路由」表都新增指向 `mcp-unity-cli.md` 的条目，且现有条目不被删除。 | diff 两份 mcp-unity-cli.md 无差异；六个 H2 齐全；已核实来源含包版本、.mcp.json 配置、149；安全门含约定与永不执行清单；速查表 15 类 149 名全部命中；链路模板含编译/测试/batch/Play；已知问题 16 条对应报告；两份 SKILL.md、CLAUDE.md、AGENTS.md 的第一步主题表与 Reference 路由表均新增 mcp-unity-cli.md 且 mcp-tools.md/mcp-visual.md 条目保留；U+FFFD 为 0 |
| A8 | passed | brief.md | A8: 报告附录记录以下六条命令的退出码与关键返回字段：`unity status --json`（含本工程条目与端口 7800）、`unity list --project-path GameUnity --json`（`count=149`）、`unity command editor_status --project-path GameUnity --json`（`status=ready`）、`unity command --help`、`unity mcp configure --list`、`unity job --help`；任一命令失败时如实记录输出。 | 实测六条命令 rc 均 0：status --json instances[0] port 7800/project GameUnity/6000.3.10f1/ready；list count=149 target.port=7800；command editor_status status=ready playMode=stopped；command --help 用法行含 --project-path/--detach；mcp configure --list 17 个客户端；job --help 用法行；与报告附录一致 |

## 检查

| 检查 | 命令 | 工作目录 | 状态 | 退出码 | 耗时 |
| --- | --- | --- | --- | ---: | ---: |
| 报告结果表恰好覆盖 unity list 的 149 个工具且结果列取值合法 | .comet/runtime/checks/report-covers-149.sh | . | passed | 0 | 312 ms |
| 工作树无残留：git 范围、临时目录不存在、GameStart SHA-256 不变、ProjectSettings/Packages 无改动 | .comet/runtime/checks/git-residue.sh | . | passed | 0 | 205 ms |
| 两份 mcp-unity-cli.md 一致、六节齐全、速查覆盖 149 工具、四处路由登记、无替换字符 | .comet/runtime/checks/skill-docs.sh | . | passed | 0 | 361 ms |
| 编辑器终态（场景 / 状态 / authoring root / tags / 构建场景）与六条 CLI 冒烟命令退出码 | .comet/runtime/checks/editor-and-cli.sh | . | passed | 0 | 11266 ms |

## 阻塞项

_无。_

## 风险与跳过的工作

- 报告问题 2 对 bake_lighting「confirm 不强制」为按描述记录；Verifier 已从源码 LightingBakeCommands.cs:58/61–98 证实 confirm 从未被读取，建议后续修订时引用源码行号
- GameStart.unity 的 4 行改动在测试开始前已在磁盘（mtime 2026-09-11 23:50:32），并非测试的 save_all 产生；内容为 UICamera m_ClearFlags 覆盖，哈希全程不变，是否提交由用户决定
- 非 git 残留：GameUnity/Temp/mcp-test/ 与 .comet/runtime/checks/*.sh 均被 gitignore，无害

## 之前的迭代

| 目标周期 | 迭代 | 尝试 | 结果 | 未解决项 | 摘要 | 完成时间 |
| ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 1 | 0 | recovery | — | Formal requirement write requested for brief.md | 2026-09-11T21:09:42.621Z |
| 2 | 1 | 0 | recovery | — | Observed implementation write before .comet/runtime/dispatch-verifier-unity-cli-mcp-full-test.json | 2026-09-11T21:52:41.160Z |
| 2 | 2 | 1 | pass | — | 八项验收全部通过。报告恰好覆盖 unity list 的 149 个工具且执行方式与 Spec 策略表逐一一致；唯一失败项 clear_baked_lighting 与 8 个预期不可用均有明确原因并落入允许集合；bake_lighting 仅记录 dry_run 预览经源码核实是因为该命令没有 confirm 拒绝门且真实烘焙被 brief 禁止，不构成 A3 未通过；仓库改动集合、临时目录、ProjectSettings/Packages、GameStart 哈希、编辑器终态、两份 skill 文档与四处路由登记、六条 CLI 冒烟均经 Verifier 独立实测符合要求。 | 2026-09-11T22:38:51.115Z |



## 结论

八项验收全部通过。报告恰好覆盖 unity list 的 149 个工具且执行方式与 Spec 策略表逐一一致；唯一失败项 clear_baked_lighting 与 8 个预期不可用均有明确原因并落入允许集合；bake_lighting 仅记录 dry_run 预览经源码核实是因为该命令没有 confirm 拒绝门且真实烘焙被 brief 禁止，不构成 A3 未通过；仓库改动集合、临时目录、ProjectSettings/Packages、GameStart 哈希、编辑器终态、两份 skill 文档与四处路由登记、六条 CLI 冒烟均经 Verifier 独立实测符合要求。
