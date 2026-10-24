using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace BLAP
{
    /// <summary>领袖坚韧：疼痛偏移 -10%（减法，封底 0），受到伤害 x0.8。</summary>
    [HarmonyPatch(typeof(HediffSet), "get_PainTotal")]
    public static class Patch_LeaderPain
    {
        public static void Postfix(HediffSet __instance, ref float __result)
        {
            if (__result <= 0f) return;
            var pawn = __instance.pawn;
            if (pawn == null || !RoleHelper.HoldsLeader(pawn)) return;
            __result = Mathf.Max(__result - BLAPSettings.LeaderPainOffset, 0f);
        }
    }

    [HarmonyPatch(typeof(HediffSet), nameof(HediffSet.FactorForDamage))]
    public static class Patch_LeaderToughness
    {
        public static void Postfix(HediffSet __instance, ref float __result)
        {
            var pawn = __instance.pawn;
            if (pawn == null || !RoleHelper.HoldsLeader(pawn)) return;
            __result *= BLAPSettings.LeaderDamageFactor;
        }
    }

    /// <summary>StatWorker.stat 私有字段的共享读取器。</summary>
    internal static class BLAPStatAccess
    {
        public static readonly AccessTools.FieldRef<StatWorker, StatDef> StatOf =
            AccessTools.FieldRefAccess<StatWorker, StatDef>("stat");
    }

    /// <summary>兼任：被兼任专家的 RoleEffect_PawnStatOffset / PawnStatFactor 参与属性计算。
    /// 每个属性首次实际生效时打一条日志，便于游戏内确认。</summary>
    [HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
    public static class Patch_EmulationStats
    {
        private static readonly HashSet<string> LoggedApplications = new HashSet<string>();

        public static void Postfix(StatWorker __instance, StatRequest req, ref float __result)
        {
            var pawn = req.Thing as Pawn;
            if (pawn == null) return;
            var statDef = BLAPStatAccess.StatOf(__instance);
            if (statDef == null) return;
            var emulated = RoleHelper.EmulatedRole(pawn);
            if (emulated?.def?.roleEffects == null) return;
            bool changed = false;
            foreach (var effect in emulated.def.roleEffects)
            {
                if (effect is RoleEffect_PawnStatOffset offset && offset.statDef == statDef)
                {
                    __result += offset.modifier;
                    changed = true;
                }
                else if (effect is RoleEffect_PawnStatFactor factor && factor.statDef == statDef)
                {
                    __result *= factor.modifier;
                    changed = true;
                }
            }
            if (changed && LoggedApplications.Add($"{pawn.thingIDNumber}:{statDef.defName}"))
            {
                Log.Message($"[BetterLeaderAndPreacher] 兼任属性生效：{pawn.LabelShortCap} 的 {statDef.defName} 应用后未终值 = {__result}");
            }
        }
    }

    /// <summary>兼任：属性分解提示里显示被兼任职位的加成行（原版只解释 GetRole 的单职位）。</summary>
    [HarmonyPatch(typeof(StatWorker), "GetOffsetsAndFactorsExplanation")]
    public static class Patch_EmulationStatsExplanation
    {
        public static void Postfix(StatWorker __instance, StatRequest req, StringBuilder sb, float baseValue, string whitespace)
        {
            try
            {
                var pawn = req.Thing as Pawn;
                if (pawn == null || sb == null) return;
                var statDef = BLAPStatAccess.StatOf(__instance);
                if (statDef == null) return;
                var emulated = RoleHelper.EmulatedRole(pawn);
                if (emulated?.def?.roleEffects == null) return;
                foreach (var effect in emulated.def.roleEffects)
                {
                    string value = null;
                    // 与原版角色加成行相同的格式化（偏移/系数各用一组 ToStringNumberStyle）
                    if (effect is RoleEffect_PawnStatOffset offset && offset.statDef == statDef)
                        value = __instance.ValueToString(offset.modifier, false, (Verse.ToStringNumberSense)3);
                    else if (effect is RoleEffect_PawnStatFactor factor && factor.statDef == statDef)
                        value = __instance.ValueToString(factor.modifier, false, (Verse.ToStringNumberSense)2);
                    if (value != null)
                    {
                        sb.AppendInNewLine(whitespace + "BLAP_EmulationStatLine"
                            .Translate(emulated.LabelForPawn(pawn), value).Resolve());
                    }
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce($"[BetterLeaderAndPreacher] 属性解释补丁异常：{e.Message}", 73195003);
            }
        }
    }

    /// <summary>兼任：被兼任专家的 RoleEffect_ProductionQualityOffset 参与品质判定。</summary>
    [HarmonyPatch(typeof(QualityUtility), nameof(QualityUtility.GenerateQualityCreatedByPawn),
        new[] { typeof(Pawn), typeof(SkillDef), typeof(bool) })]
    public static class Patch_EmulationQuality
    {
        public static void Postfix(Pawn pawn, ref QualityCategory __result)
        {
            var emulated = RoleHelper.EmulatedRole(pawn);
            if (emulated?.def?.roleEffects == null) return;
            foreach (var effect in emulated.def.roleEffects)
            {
                if (effect is RoleEffect_ProductionQualityOffset qualityOffset)
                {
                    int value = Mathf.Min((int)__result + qualityOffset.offset, (int)QualityCategory.Legendary);
                    __result = (QualityCategory)value;
                }
            }
        }
    }

    /// <summary>兼任：被兼任专家的 grantedAbilities 出现在能力列表中。
    /// 注意不能用 Precept_RoleMulti.AbilitiesFor(pawn)——它只对 chosenPawns 里真正指派的 pawn 返回缓存，
    /// 未指派的兼任者返回 null；这里按 def 自建实例并缓存（保证冷却状态跨查询稳定）。
    /// 同时清洗列表中 def 已失效的坏条目（历史版本或卸掉的 mod 在存档里留下的 null 能力）。</summary>
    [HarmonyPatch(typeof(Pawn_AbilityTracker), "get_AllAbilitiesForReading")]
    public static class Patch_EmulationAbilities
    {
        private static readonly AccessTools.FieldRef<Pawn_AbilityTracker, List<Ability>> AbilitiesField =
            AccessTools.FieldRefAccess<Pawn_AbilityTracker, List<Ability>>("abilities");

        // pawn.thingIDNumber -> (兼任的 defName, 该职位各能力 def 的活动实例)
        private static readonly Dictionary<long, KeyValuePair<string, Dictionary<AbilityDef, Ability>>> AbilityCache =
            new Dictionary<long, KeyValuePair<string, Dictionary<AbilityDef, Ability>>>();

        public static bool IsBroken(Ability a) => a == null || a.def == null;

        public static void Postfix(Pawn_AbilityTracker __instance, ref List<Ability> __result)
        {
            try
            {
                var pawn = __instance.pawn;
                if (pawn == null || __result == null) return;

                // 从 tracker 内部列表清除坏条目，其他消费方（AbilityTick 等）不再踩雷
                var abilities = AbilitiesField(__instance);
                if (abilities != null && abilities.Any(IsBroken))
                    abilities.RemoveAll(IsBroken);
                if (__result.Any(IsBroken))
                    __result.RemoveAll(IsBroken);

                CleanupIfRoleLost(pawn);
                var emulated = RoleHelper.EmulatedRole(pawn);
                if (emulated == null)
                {
                    AbilityCache.Remove(pawn.thingIDNumber);
                    return;
                }

                if (!AbilityCache.TryGetValue(pawn.thingIDNumber, out var cached) || cached.Key != emulated.def.defName)
                {
                    var fresh = new Dictionary<AbilityDef, Ability>();
                    foreach (var def in emulated.def.grantedAbilities)
                    {
                        if (def == null) continue;
                        // 与真指派一致：职位路径（FillOrUpdateAbilityList）授能力不做 meme 检查；
                        // 原版 getter 的 requiredMemes 过滤只针对血红(hediff)授予的能力，不适用于兼任
                        fresh[def] = AbilityUtility.MakeAbility(def, pawn, emulated);
                    }
                    Log.Message($"[BetterLeaderAndPreacher] 兼任能力构建：{emulated.def.defName} 授予 " +
                                $"{fresh.Count} 项（{string.Join(", ", fresh.Keys.Select(d => d.defName))}）");
                    cached = new KeyValuePair<string, Dictionary<AbilityDef, Ability>>(emulated.def.defName, fresh);
                    AbilityCache[pawn.thingIDNumber] = cached;
                }
                foreach (var ability in cached.Value.Values)
                {
                    if (__result.Any(a => !IsBroken(a) && a.def == ability.def)) continue;
                    __result.Add(ability);
                }
            }
            catch (Exception e)
            {
                // gizmo 渲染高频调用，任何异常都会炸掉整个命令栏；兜底并留痕
                Log.ErrorOnce($"[BetterLeaderAndPreacher] EmulationAbilities 补丁异常：{e.Message}", 73195002);
            }
        }

        private static void CleanupIfRoleLost(Pawn pawn)
        {
            var marker = RoleHelper.EmulationMarker(pawn);
            if (marker != null && !RoleHelper.HoldsLeader(pawn))
            {
                pawn.health.RemoveHediff(marker);
                AbilityCache.Remove(pawn.thingIDNumber);
                pawn.abilities.Notify_TemporaryAbilitiesChanged();
            }
        }
    }

    /// <summary>兼任工作禁用的公共判定（可配置，默认开启）。带一次性调试日志。</summary>
    internal static class EmulationWorkBlocker
    {
        private static readonly HashSet<string> LoggedDecisions = new HashSet<string>();

        public static bool Blocks(Pawn pawn, WorkTags tags)
        {
            bool settingOn = BetterLeaderAndPreacherMod.Settings != null &&
                             BetterLeaderAndPreacherMod.Settings.emulationInheritsDisabledWork;
            var emulated = settingOn ? RoleHelper.EmulatedRole(pawn) : null;
            WorkTags roleTags = emulated?.def.roleDisabledWorkTags ?? WorkTags.None;
            bool result = settingOn && emulated != null && (roleTags & tags) != 0;
            // 调试：每个 (pawn, tags) 组合只打一次，避免刷屏
            if (LoggedDecisions.Add($"{pawn?.thingIDNumber}:{tags}"))
            {
                Log.Message($"[BetterLeaderAndPreacher] 工作禁用判定：{pawn?.LabelShortCap ?? "?"} 查询 {tags} -> " +
                            $"{(result ? "禁用" : "放行")}（设置开关={settingOn}，兼任={emulated?.def.defName ?? "无"}，" +
                            $"职位禁用标签={roleTags}）");
            }
            return result;
        }
    }

    /// <summary>兼任继承工作禁用。注意 1.7 有多条独立判定链：
    /// WorkTagIsDisabled 走 CombinedDisabledWorkTags，而 WorkTypeIsDisabled 走
    /// GetDisabledWorkTypes 迭代器（自己直读 roleDisabledWorkTags），必须分别拦截。</summary>
    [HarmonyPatch(typeof(Pawn), "get_CombinedDisabledWorkTags")]
    public static class Patch_EmulationWorkTags_Combined
    {
        public static void Postfix(Pawn __instance, ref WorkTags __result)
        {
            try
            {
                if (BetterLeaderAndPreacherMod.Settings == null ||
                    !BetterLeaderAndPreacherMod.Settings.emulationInheritsDisabledWork) return;
                var emulated = RoleHelper.EmulatedRole(__instance);
                if (emulated == null) return;
                __result |= emulated.def.roleDisabledWorkTags;
            }
            catch (Exception e)
            {
                Log.ErrorOnce($"[BetterLeaderAndPreacher] 工作禁用补丁异常：{e.Message}", 73195004);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.WorkTypeIsDisabled))]
    public static class Patch_EmulationWorkTags_WorkType
    {
        public static void Postfix(Pawn __instance, WorkTypeDef w, ref bool __result)
        {
            if (!__result && w != null && EmulationWorkBlocker.Blocks(__instance, w.workTags))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.OneOfWorkTypesIsDisabled))]
    public static class Patch_EmulationWorkTags_WorkTypeList
    {
        public static void Postfix(Pawn __instance, List<WorkTypeDef> wts, ref bool __result)
        {
            if (__result || wts == null) return;
            foreach (var w in wts)
            {
                if (w != null && EmulationWorkBlocker.Blocks(__instance, w.workTags))
                {
                    __result = true;
                    return;
                }
            }
        }
    }

    /// <summary>工作禁用原因文本（列表/提示用）。原方法带缓存，附加理由时用副本，不改缓存。</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetReasonsForDisabledWorkType))]
    public static class Patch_EmulationWorkTags_Reasons
    {
        public static void Postfix(Pawn __instance, WorkTypeDef workType, ref List<string> __result)
        {
            try
            {
                if (__result == null || workType == null) return;
                if (!EmulationWorkBlocker.Blocks(__instance, workType.workTags)) return;
                var emulated = RoleHelper.EmulatedRole(__instance);
                var copy = new List<string>(__result)
                {
                    "BLAP_EmulationWorkReason".Translate(emulated.LabelForPawn(__instance))
                };
                __result = copy;
            }
            catch (Exception e)
            {
                Log.ErrorOnce($"[BetterLeaderAndPreacher] 工作禁用原因补丁异常：{e.Message}", 73195005);
            }
        }
    }

    /// <summary>职位被卸任时清理兼任标记。1.7 中 Precept_Role.Unassign 是抽象方法，
    /// 只能补丁两个具体子类的实现。</summary>
    [HarmonyPatch(typeof(Precept_RoleSingle), nameof(Precept_RoleSingle.Unassign))]
    public static class Patch_EmulationCleanupOnUnassignSingle
    {
        public static void Postfix(Pawn p) => EmulationCleanup.Cleanup(p);
    }

    [HarmonyPatch(typeof(Precept_RoleMulti), nameof(Precept_RoleMulti.Unassign))]
    public static class Patch_EmulationCleanupOnUnassignMulti
    {
        public static void Postfix(Pawn p) => EmulationCleanup.Cleanup(p);
    }

    internal static class EmulationCleanup
    {
        public static void Cleanup(Pawn p)
        {
            var marker = RoleHelper.EmulationMarker(p);
            if (marker != null && !RoleHelper.HoldsLeader(p))
            {
                p.health.RemoveHediff(marker);
                p.abilities.Notify_TemporaryAbilitiesChanged();
            }
        }
    }
}
