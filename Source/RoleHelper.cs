using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BLAP
{
    /// <summary>角色查询与兼任状态的工具方法。兼任状态存在 Hediff_SpecialistEmulationMarker 里随 pawn 存档。</summary>
    public static class RoleHelper
    {
        private static readonly HashSet<string> EmulationSourceRoles = new HashSet<string>
        {
            "IdeoRole_Leader",
            "IdeoRole_Moralist"
        };

        public static List<Precept_Role> AssignedRoles(Pawn pawn)
        {
            var result = new List<Precept_Role>();
            if (pawn?.Ideo == null) return result;
            foreach (var role in pawn.Ideo.RolesListForReading)
            {
                if (role.IsAssigned(pawn)) result.Add(role);
            }
            return result;
        }

        public static bool HoldsLeaderOrMoralist(Pawn pawn)
        {
            if (pawn?.Ideo == null) return false;
            foreach (var role in pawn.Ideo.RolesListForReading)
            {
                if (role.IsAssigned(pawn) && EmulationSourceRoles.Contains(role.def.defName)) return true;
            }
            return false;
        }

        public static bool HoldsLeader(Pawn pawn)
        {
            if (pawn?.Ideo == null) return false;
            foreach (var role in pawn.Ideo.RolesListForReading)
            {
                if (role.IsAssigned(pawn) && role.def.defName == "IdeoRole_Leader") return true;
            }
            return false;
        }

        public static Hediff_SpecialistEmulationMarker EmulationMarker(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(BLAP_DefOf.BLAP_SpecialistEmulationMarker)
                as Hediff_SpecialistEmulationMarker;
        }

        /// <summary>当前兼任的专家 precept；已失效（文化变革后不存在）时返回 null。</summary>
        /// <summary>当前兼任的职位；已失效（文化变革后不存在）时返回 null。
        /// 兼任对象是除领袖/传道者外的任意职位——模组专家（如 VIE 护士、AlphaMemes 角色）
        /// 很多是 Precept_RoleSingle 单持有者职位，不能按 RoleMulti 过滤。</summary>
        public static Precept_Role EmulatedRole(Pawn pawn)
        {
            var marker = EmulationMarker(pawn);
            if (marker == null || marker.EmulatedRoleDef.NullOrEmpty()) return null;
            if (pawn.Ideo == null) return null;
            foreach (var role in pawn.Ideo.RolesListForReading)
            {
                if (role.def.defName == marker.EmulatedRoleDef && !IsExcludedRole(role)) return role;
            }
            return null;
        }

        /// <summary>领袖与传道者本身不可被兼任。</summary>
        public static bool IsExcludedRole(Precept_Role role)
        {
            return EmulationSourceRoles.Contains(role.def.defName);
        }

        public static List<Precept_Role> EmulatableRoles(Pawn pawn)
        {
            var result = new List<Precept_Role>();
            if (pawn?.Ideo == null) return result;
            foreach (var role in pawn.Ideo.RolesListForReading)
            {
                if (!IsExcludedRole(role) && role.Active) result.Add(role);
            }
            return result;
        }

        /// <summary>文化里全部可兼任职位（含未激活），供菜单展示与诊断。</summary>
        public static List<Precept_Role> AllSpecialistRoles(Pawn pawn)
        {
            var result = new List<Precept_Role>();
            if (pawn?.Ideo == null) return result;
            foreach (var role in pawn.Ideo.RolesListForReading)
            {
                if (!IsExcludedRole(role)) result.Add(role);
            }
            return result;
        }

        public static void SetEmulation(Pawn pawn, Precept_Role role)
        {
            var marker = EmulationMarker(pawn);
            if (role == null)
            {
                // 取消：只清空兼任对象，保留 hediff；冷却时间戳同样刷新（切换与取消共用计时）
                if (marker == null) return;
                marker.EmulatedRoleDef = null;
                marker.lastSetTick = Find.TickManager.TicksGame;
                // 能力缓存失效，旧的被兼任能力立即从列表消失（否则要等征召切换才刷新）
                pawn.abilities.Notify_TemporaryAbilitiesChanged();
                return;
            }
            if (marker == null)
            {
                marker = HediffMaker.MakeHediff(BLAP_DefOf.BLAP_SpecialistEmulationMarker, pawn) as Hediff_SpecialistEmulationMarker;
                pawn.health.AddHediff(marker);
            }
            marker.EmulatedRoleDef = role.def.defName;
            marker.lastSetTick = Find.TickManager.TicksGame;
            pawn.abilities.Notify_TemporaryAbilitiesChanged();
        }
    }
}
