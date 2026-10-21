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
        public static Precept_RoleMulti EmulatedRole(Pawn pawn)
        {
            var marker = EmulationMarker(pawn);
            if (marker == null || marker.EmulatedRoleDef.NullOrEmpty()) return null;
            if (pawn.Ideo == null) return null;
            foreach (var role in pawn.Ideo.RolesListForReading)
            {
                if (role is Precept_RoleMulti multi && role.def.defName == marker.EmulatedRoleDef) return multi;
            }
            return null;
        }

        public static List<Precept_RoleMulti> EmulatableRoles(Pawn pawn)
        {
            var result = new List<Precept_RoleMulti>();
            if (pawn?.Ideo == null) return result;
            foreach (var role in pawn.Ideo.RolesListForReading)
            {
                if (role is Precept_RoleMulti multi && role.Active) result.Add(multi);
            }
            return result;
        }

        /// <summary>文化里全部专家职位（含未激活），供菜单展示与诊断。</summary>
        public static List<Precept_RoleMulti> AllSpecialistRoles(Pawn pawn)
        {
            var result = new List<Precept_RoleMulti>();
            if (pawn?.Ideo == null) return result;
            foreach (var role in pawn.Ideo.RolesListForReading)
            {
                if (role is Precept_RoleMulti multi) result.Add(multi);
            }
            return result;
        }

        public static void SetEmulation(Pawn pawn, Precept_RoleMulti role)
        {
            var marker = EmulationMarker(pawn);
            if (role == null)
            {
                // 取消：只清空兼任对象，保留 hediff；冷却时间戳同样刷新（切换与取消共用计时）
                if (marker == null) return;
                marker.EmulatedRoleDef = null;
                marker.lastSetTick = Find.TickManager.TicksGame;
                return;
            }
            if (marker == null)
            {
                marker = HediffMaker.MakeHediff(BLAP_DefOf.BLAP_SpecialistEmulationMarker, pawn) as Hediff_SpecialistEmulationMarker;
                pawn.health.AddHediff(marker);
            }
            marker.EmulatedRoleDef = role.def.defName;
            marker.lastSetTick = Find.TickManager.TicksGame;
        }
    }
}
