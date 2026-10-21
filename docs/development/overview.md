# 工程概览 — better_leader_and_preacher

- 位置：`D:\rimworld_mods_station\better_leader_and_preacher`
- packageId：`station.BetterLeaderAndPreacher`（命名空间 BLAP）
- 目标版本：仅 RimWorld **1.7**（本机实测构建 1.7.4982 rev1280，与技能画像 1.7.4977 有差异，所有接口以本机 DLL 取证为准）
- 依赖：Ideology（必须）、brrainz.harmony（必须，本机 workshop 2009463077 的 1.7 包，Harmony 3.0）
- 目标：强化 Ideology 领袖（Leader）与传道者（Moralist）
- 构建：`Source/dotnet build`（.NET SDK 10.0.402，net10.0，输出到 `1.7/Assemblies/`）
- **部署（固定规则）**：每次修改并编译通过后，运行 `powershell -NoProfile -File tools/deploy.ps1`，把整个工程镜像到 `D:\Steam\steamapps\common\RimWorld\Mods\better_leader_and_preacher`（排除 .git/bin/obj/.vs）
- 开发阶段：首个版本已产出，**游戏内验证未做**

## 功能块

| 块 | 内容 | 实现层 |
| --- | --- | --- |
| P1 期望偏移 | Leader/Moralist `expectationsOffset` 2 → -2（期望降两档，`Math.Max(0)` 封底已核实） | XML 补丁 |
| P2 传道者强化 | `convertPowerFactor` 2 → 3（教化能力 x300%）；新增 `RoleEffect_PawnStatFactor(SocialIdeoSpreadFrequencyFactor, 1.5)` | XML 补丁 |
| P3 领袖坚韧 | 疼痛 x0.9（`HediffSet.get_PainTotal` postfix）、承伤 x0.8（`HediffSet.FactorForDamage` postfix） | C# Harmony |
| P4 兼任专家 | 主动技能 `BLAP_SpecialistEmulation` 授予两角色；释放后浮动菜单选本文化专家 precept；标记 hediff 持久化 defName；属性/品质/能力三个 postfix 应用被兼任 precept 的 roleEffects | XML + C# |

## 关键证据（全部来自本机 1.7.4982）

- 期望机制：`ExpectationsUtility.CurrentExpectationFor` 末尾 `ExpectationForOrder(Math.Max(order + role.def.expectationsOffset, 0), 1)`，负值合法。
- 原版 RoleEffect 仅 9 类，无疼痛/承伤类 → P3 必须 C#。
- 1.7 接口变化（相对 1.6）：`StatWorker.GetValueUnfinalized(StatRequest, ref float)` → `(StatRequest req, bool applyPostProcess)` 返回 float；`CompProperties_Ability` → `CompProperties_AbilityEffect`；`GlobalTargetInfo` 在 `RimWorld.Planet`；`StatWorker.stat` 无公开访问器（用 `AccessTools.FieldRefAccess`）。
- 兼任方案：参考工坊 3823004424（csh.LeaderCanBeSpecialist，1.6）的双职位架构已完整还原，但用户实测其在 1.7 不生效，且真双职位要补 45 处 `GetRole` 消费点；改为"技能兼任"方案（用户拍板），只读被兼任 precept 的 roleEffects，不碰指派/仪式系统。
- 工具：`tools_inspect/`（dnfile+dncil 只读检查脚本，属开发工具不随 mod 发布）。

## 未验证项

- 游戏内加载、四个功能块的实际触发（见 checkpoint 的验证步骤）。
- 与用户 100+ mod 列表（含 RaiseRolesLimit 等角色类 mod）的组合兼容未测。
