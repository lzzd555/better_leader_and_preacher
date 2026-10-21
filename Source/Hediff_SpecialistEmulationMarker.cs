using System.Linq;
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

        /// <summary>健康页提示里列出兼任获得的全部数值补正与能力。</summary>
        public override string TipStringExtra
        {
            get
            {
                var precept = EmulatedPrecept;
                if (precept == null) return null;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("BLAP_EmulationBonusHeader".Translate());
                if (precept.def.roleEffects != null)
                {
                    foreach (var effect in precept.def.roleEffects)
                    {
                        string line = effect?.Label(pawn, precept);
                        if (!line.NullOrEmpty()) sb.AppendLine("  - " + line);
                    }
                }
                if (precept.def.grantedAbilities != null && precept.def.grantedAbilities.Count > 0)
                {
                    sb.AppendLine("  - " + "BLAP_EmulationAbilitiesLine".Translate(
                        precept.def.grantedAbilities.Where(a => a != null).Select(a => a.LabelCap.Resolve()).ToCommaList()));
                }
                return sb.ToString().TrimEndNewlines();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref EmulatedRoleDef, "BLAP_emulatedRoleDef");
            Scribe_Values.Look(ref lastSetTick, "BLAP_lastSetTick", -999999);
        }
    }
}
