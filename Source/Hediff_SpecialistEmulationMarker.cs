using RimWorld;
using Verse;

namespace BLAP
{
    /// <summary>兼任状态标记：记录兼任的专家 PreceptDef defName。用 defName 而非 precept 引用，
    /// 这样文化改革重建 precept 后仍能解析回新 precept，不会产生悬空引用。</summary>
    public class Hediff_SpecialistEmulationMarker : Hediff
    {
        public string EmulatedRoleDef;
        public int lastSetTick = -999999;

        public Precept_Role EmulatedPrecept
        {
            get
            {
                if (EmulatedRoleDef.NullOrEmpty() || pawn?.Ideo == null) return null;
                foreach (var role in pawn.Ideo.RolesListForReading)
                {
                    if (role.def.defName == EmulatedRoleDef) return role;
                }
                return null;
            }
        }

        public override string LabelBase => "BLAP_EmulationHediffLabel".Translate(
            EmulatedPrecept?.LabelForPawn(pawn) ?? EmulatedRoleDef ?? "");

        /// <summary>未兼任（已取消但保留冷却时间戳）时在健康页隐藏。</summary>
        public override bool Visible => !EmulatedRoleDef.NullOrEmpty() && base.Visible;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref EmulatedRoleDef, "BLAP_emulatedRoleDef");
            Scribe_Values.Look(ref lastSetTick, "BLAP_lastSetTick", -999999);
        }
    }
}
