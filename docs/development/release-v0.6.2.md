# 发行记录 — v0.6.2 → 创意工坊（不公开）

- 日期：2026-10-22
- 干净包：`D:\rimworld_mods_station\releases\blap_v0.6.2\`（8 个文件，清单 `blap_v0.6.2_MANIFEST.txt`）
- 上传副本：`D:\Steam\steamapps\common\RimWorld\Mods\BLAP_v0.6.2_upload\`（与干净包逐字节一致）
- 排除项：Source/、docs/、tools/、tools_inspect/、.git、构建中间产物；未夹带游戏/框架程序集
- 上传通道：游戏内置上传器（开发模式 → Mods），Steam 客户端运行中；**可见性选择"私密"**（用户要求不公开）
- 支持版本：仅 1.7；依赖：Harmony (brrainz.harmony)、Ideology；均已声明于 About

## 验证状态

| 项 | 状态 |
| --- | --- |
| 编译 | 通过（v0.6.2，2026-10-22） |
| XML 良构 + xpath 命中 | 通过（历史轮已验 + 本次全量良构复检） |
| 游戏内实测 | 用户多轮实测通过（期望/坚韧/教化/兼任/能力/冷却/门槛/缓存刷新）；疼痛偏移语义 v0.6.2 改动**未再实测** |
| 组合兼容 | 用户实际 mod 列表（100+）环境实测；系统性回归未做 |

## 已知缺项

- 无工坊预览图（Preview.png）——首次上传后可在工坊页面补充
- 长时运行/存档跨版本升级（卸载 mod 后兼任 hediff 残留）未系统测试
