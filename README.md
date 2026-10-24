# Better Leader and Moral Guide 更好的领袖和传道者

RimWorld **1.7** 模组（需 Ideology + Harmony）。让 Ideology 的领袖与传道者回归"关键时刻带头"的定位。

## 功能

| 功能 | 说明 |
| --- | --- |
| 期望偏移 -2 | 领袖/传道者不再要求更高特权，期望反而降低两级（受最低档限制） |
| 领袖坚韧 | 疼痛偏移 -10%（减法、封底 0），受到伤害 x80% |
| 传道者文化影响 | 教化能力 x300%（原 x200% 再乘 150%），文化传播系数 x150% |
| 兼任专家 | 领袖/传道者可通过命令按钮兼任本文化形态的一个职位（含 VIE / AlphaMemes 等模组职位），获得其数值补正与能力；不继承工作限制与着装要求，不占用职位名额；任何变更（切换/无兼任）有 7 天冷却 |

兼任与真指派行为对齐：通过原版 `RequirementsMet` 检查职位要求（如射击 6 级），未达标灰显并标注原因；能力授予与原版职位路径一致。

## 构建

```
cd Source
dotnet build   # .NET SDK 10，输出到 1.7/Assemblies/
```

csproj 内含本机游戏与 Harmony 路径（`GameDir`/`HarmonyDir`），换机器时改这两个属性。

## 部署

```
powershell -NoProfile -File tools/deploy.ps1
```

把整个工程镜像到游戏的本地 Mods 目录（需先关闭游戏，脚本会检测）。

## 结构

```
About/          Mod 元数据
1.7/            Defs / Patches / Languages / Assemblies
Source/         C# 源码（Harmony 补丁 + 兼任系统）
tools/          部署脚本
tools_inspect/  针对 1.7.4982 程序集的只读取证脚本（dnfile/dncil，开发用）
docs/development/  开发记录：概览、检查点、事故记录、发行记录
```

## 兼容性说明

- 兼任效果覆盖 `RoleEffect_PawnStatOffset/Factor`、`ProductionQualityOffset` 与 `grantedAbilities`；第三方 mod 的自定义 RoleEffect 子类的专属效果不在兼任范围内。
- 属性加成会显示在属性分解提示（与原版角色加成同格式）与兼任 hediff 悬停明细中。
