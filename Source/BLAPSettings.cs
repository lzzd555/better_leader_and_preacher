using RimWorld;
using Verse;
using UnityEngine;

namespace BLAP
{
    /// <summary>模组设置。写在存档级的 ModSettings 里，随档保存。</summary>
    public class BLAPModSettings : ModSettings
    {
        /// <summary>兼任是否继承被兼任职位的工作禁用（roleDisabledWorkTags）。</summary>
        public bool emulationInheritsDisabledWork = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref emulationInheritsDisabledWork, "BLAP_emulationInheritsDisabledWork", true);
            base.ExposeData();
        }
    }
}

namespace BLAP
{
    public partial class BetterLeaderAndPreacherMod
    {
        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);
            list.CheckboxLabeled("BLAP_SettingsInheritDisabledWork".Translate(),
                ref Settings.emulationInheritsDisabledWork,
                "BLAP_SettingsInheritDisabledWorkTip".Translate());
            list.Gap(12f);
            list.Label("BLAP_SettingsNote".Translate());
            list.End();
        }

        public override string SettingsCategory()
        {
            return "Better Leader and Preacher";
        }
    }
}
