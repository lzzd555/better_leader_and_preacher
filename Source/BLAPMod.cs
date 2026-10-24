using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace BLAP
{
    public partial class BetterLeaderAndPreacherMod : Mod
    {
        public static BLAPModSettings Settings;

        public BetterLeaderAndPreacherMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<BLAPModSettings>();
            var harmony = new Harmony("station.BetterLeaderAndPreacher");
            // 逐类打补丁并单独捕获：一个补丁类失败不拖垮其余补丁（重 mod 环境下更稳）
            int ok = 0, failed = 0;
            foreach (var type in typeof(BetterLeaderAndPreacherMod).Assembly.GetTypes()
                         .Where(t => t.IsClass && t.IsDefined(typeof(HarmonyPatch), false)))
            {
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    failed++;
                    Log.Error($"[BetterLeaderAndPreacher] 补丁类 {type.Name} 应用失败：{e.Message}");
                }
            }
            Log.Message($"[BetterLeaderAndPreacher] 已加载。补丁 {ok} 成功 / {failed} 失败。");
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            // 工作禁用开关变化后刷新 pawn 的工作缓存相关通知
            foreach (var pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                pawn.Notify_DisabledWorkTypesChanged();
            }
        }
    }

    [DefOf]
    public static class BLAP_DefOf
    {
        public static HediffDef BLAP_SpecialistEmulationMarker;

        static BLAP_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BLAP_DefOf));
        }
    }

    public static class BLAPSettings
    {
        public const float LeaderPainOffset = 0.1f;   // 疼痛偏移 -10%（减法，封底 0）
        public const float LeaderDamageFactor = 0.8f;
        public const int EmulationCooldownTicks = 420000; // 7 游戏日
    }
}
