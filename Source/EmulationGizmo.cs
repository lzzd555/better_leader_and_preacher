using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace BLAP
{
    /// <summary>兼任专家入口：给持有领袖/传道者职位的殖民者注入一个命令按钮。
    /// 冷却在按钮上整体生效（切换与无兼任共用一个计时）；菜单内提供各专家与"无兼任"选项。</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_EmulationGizmo
    {
        private static Texture2D iconCache;

        private static Texture2D Icon =>
            iconCache ??= ContentFinder<Texture2D>.Get("UI/Abilities/WorkDrive", true);

        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!__instance.IsColonistPlayerControlled) return;
            if (!RoleHelper.HoldsLeaderOrMoralist(__instance)) return;
            __result = __result.Append(MakeGizmo(__instance));
            if (DebugSettings.ShowDevGizmos)
                __result = __result.Append(MakeDevGizmo(__instance));
        }

        private static Gizmo MakeDevGizmo(Pawn pawn)
        {
            return new Command_Action
            {
                icon = Icon,
                defaultLabel = "BLAP_DevResetCooldown".Translate(),
                defaultDesc = "BLAP_DevResetCooldownDesc".Translate(),
                action = () =>
                {
                    var marker = RoleHelper.EmulationMarker(pawn);
                    if (marker != null)
                        marker.lastSetTick = Find.TickManager.TicksGame - BLAPSettings.EmulationCooldownTicks;
                    Messages.Message("[DEV] 兼任冷却已重置", pawn, MessageTypeDefOf.SilentInput);
                },
            };
        }

        private static Gizmo MakeGizmo(Pawn pawn)
        {
            var marker = RoleHelper.EmulationMarker(pawn);
            int cooldownLeft = marker == null
                ? 0
                : BLAPSettings.EmulationCooldownTicks - (Find.TickManager.TicksGame - marker.lastSetTick);
            var command = new Command_Action
            {
                icon = Icon,
                defaultLabel = "BLAP_EmulateGizmoLabel".Translate(),
                defaultDesc = "BLAP_EmulateGizmoDesc".Translate(),
                alsoClickIfOtherInGroupClicked = false,
                action = () => EmulationMenu.Open(pawn),
            };
            if (cooldownLeft > 0)
            {
                string left = cooldownLeft.ToStringTicksToPeriod();
                command.defaultLabel = "BLAP_EmulateGizmoLabel".Translate() + " (" + left + ")";
                command.Disable("BLAP_EmulateCooldown".Translate(left));
            }
            return command;
        }
    }

    internal static class EmulationMenu
    {
        public static void Open(Pawn pawn)
        {
            var options = new List<FloatMenuOption>();
            var current = RoleHelper.EmulatedRole(pawn);
            int totalRoles = pawn.Ideo?.RolesListForReading?.Count ?? 0;
            var specialists = RoleHelper.AllSpecialistRoles(pawn);
            Log.Message($"[BetterLeaderAndPreacher] 兼任菜单：文化职位共 {totalRoles} 个，专家职位 {specialists.Count} 个" +
                        $"（激活 {specialists.Count(r => r.Active)}，当前兼任：{current?.def.defName ?? "无"}）");
            foreach (var role in specialists)
            {
                var captured = role;
                bool isCurrent = role == current;
                string label = isCurrent
                    ? "BLAP_EmulateCurrent".Translate(role.LabelForPawn(pawn))
                    : "BLAP_EmulateOption".Translate(role.LabelForPawn(pawn));
                if (!role.Active)
                {
                    // 未激活的专家也展示出来（灰显+标注），便于玩家看清为什么不能选
                    options.Add(new FloatMenuOption(label + " " + "BLAP_EmulateInactive".Translate(), null));
                }
                else if (!role.RequirementsMet(pawn))
                {
                    // 与真指派一致的要求检查（技能/性别/儿童等），未达标灰显并标注原因
                    var unmet = role.GetFirstUnmetRequirement(pawn);
                    string req = unmet != null ? unmet.GetLabelCap(role) : "";
                    options.Add(new FloatMenuOption(label + " " + "BLAP_EmulateRequirement".Translate(req), null));
                }
                else if (isCurrent)
                {
                    options.Add(new FloatMenuOption(label, null));
                }
                else
                {
                    options.Add(new FloatMenuOption(label, () => ApplyChoice(pawn, captured)));
                }
            }
            // “无兼任”与其他选项并列；当前即无兼任时灰显
            bool noEmulation = current == null;
            options.Add(noEmulation
                ? new FloatMenuOption("BLAP_EmulateNone".Translate(), null)
                : new FloatMenuOption("BLAP_EmulateNone".Translate(), () => ApplyChoice(pawn, null)));
            Find.WindowStack.Add(new FloatMenu(options, "BLAP_EmulateMenuTitle".Translate()));
        }

        private static void ApplyChoice(Pawn pawn, Precept_Role role)
        {
            if (role != null)
            {
                RoleHelper.SetEmulation(pawn, role);
                Messages.Message(
                    "BLAP_EmulateStarted".Translate(pawn.LabelShortCap, role.LabelForPawn(pawn)),
                    pawn, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                string label = RoleHelper.EmulatedRole(pawn)?.LabelForPawn(pawn) ?? "";
                RoleHelper.SetEmulation(pawn, null);
                Messages.Message(
                    "BLAP_EmulateEnded".Translate(pawn.LabelShortCap, label),
                    pawn, MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}
