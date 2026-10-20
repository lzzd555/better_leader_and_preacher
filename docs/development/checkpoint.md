# 检查点 — 2026-10-21（v0.2.2：冷却 7 天 + 交互改进）

## 最新变更（v0.2.2）

- 用户反馈：使用一次后"无法再次使用/无法修改兼任"（实为按钮 1 天冷却置灰、点击无反应造成误解）；冷却要求改为 7 天。
- 变更：`EmulationCooldownTicks` 60000 → **420000**（7 游戏日）；按钮**永不置灰**，点击始终打开菜单；冷却期内"切换"选项灰显并显示剩余时间，"取消兼任"始终可用（纯亏操作，无刷点）。
- 堵漏洞：取消兼任不再删除 marker hediff，只清空 `EmulatedRoleDef`（hediff 转为 `Visible=false` 隐藏），`lastSetTick` 保留——防止"取消后立刻换一个"绕过冷却。
- v0.2.1 补充：AllAbilitiesForReading 补丁全面空安全 + 清洗存档中 def 失效的坏能力条目（v0.1.0 坏技能实例残留）+ 兜底 try/catch。

---

## 历史版本记录（保留备查）

### v0.2 变更记录（23:45-23:55）

- 用户实测 v0.1.1：补丁 7/0 全部挂载，非暴力角色被禁用 + 目标选择异常 + 施放后无实际效果。
- 根因：1.7 `AbilityDef` 构造默认值激进（`casterMustBeCapableOfViolence=true`、`targetRequired=true`、`hostile=true`，原版 13 个能力全部显式覆盖）；且"无目标即发"路径 `QueueCastingJob(invalid target) → job → verb → Activate(target,dest)` 语义复杂，comp 的 `Apply` 是否被调用难以保证。
- 决策：**放弃能力施法管道**。删除 AbilityDef 与 grantedAbilities 补丁，改为 `Pawn.GetGizmos` postfix（ref IEnumerable<Gizmo> 追加 Command_Action）。点击即弹 FloatMenu，无目标选择/无施法 job/无暴力检查。冷却改为 marker hediff 上的 `lastSetTick` 字段（随档保存），按钮禁用并显示剩余时间。
- 副作用：兼任不再出现在角色能力卡（它本来就不该算"能力"）；按钮对所有玩家可控的领袖/传道者殖民者可见（含未征召）。

## 验证状态

- 已验证（静态）：v0.2 编译零错误；游戏 Mods 副本与工程逐字节一致；无 STALE 文件。
- 未验证（游戏内）：待用户重启实测。

## 游戏内验证步骤（v0.2）

1. 启动后日志：`补丁 8 成功 / 0 失败`（新增 Patch_EmulationGizmo），无 BLAP 红字。
2. 选中领袖/传道者（不必征召）：命令栏出现"兼任专家"按钮（WorkDrive 图标）。
3. 点击 → 立即弹出专家菜单（无目标选择、无施法动作）。选择后：健康页出现"兼任：XX"、对应属性变化、获得专家能力；按钮进入 1 天冷却并显示剩余时间。
4. 再次点击（冷却结束后）可切换或取消；卸任职位标记自动清除；存档重读兼任保留。
5. 非暴力的领袖/传道者同样可用（Command_Action 无暴力门槛）。
6. 回归：P1 期望 -2、P2 教化 300%/传播 150%、P3 领袖承伤 80%/疼痛 90%。

## 遗留边界（已知并接受）

- 兼任只叠加 `RoleEffect_PawnStatOffset/Factor`、`ProductionQualityOffset`、`grantedAbilities`（含 requiredMemes 门槛、按 def 去重）；不继承工作禁用/着装要求/武器限制；第三方自定义 RoleEffect 子类的专属效果不会被复制。
- 兼任不占用专家职位名额；一 pawn 同时只能兼任一个；切换/取消后 1 天冷却。

---

## 历史事故记录（v0.1.x，保留备查）

- v0.1.0 事故 A：AbilityDef verbProperties 缺 `verbClass`（1.7 默认抽象类 `Verse.Verb`）→ "Could not instantiate Verb" ×48 → 任职仪式 `FillOrUpdateAbilityList` NRE → 小人每 tick 报错。修复：显式 `Verb_CastAbility`。
- v0.1.0 事故 B：`Precept_Role.Unassign` 在 1.7 为抽象方法，Harmony 报 "Abstract methods cannot be prepared"，PatchAll 整体中止。修复：拆补丁到 `Precept_RoleSingle/RoleMulti.Unassign`；入口改逐类应用+单独捕获。
- 用户曾误用旧副本（游戏 Mods 目录）测试，已建立"工程 → Mods 目录"同步校验流程。
