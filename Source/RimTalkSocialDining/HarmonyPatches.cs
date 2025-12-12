using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimTalkSocialDining
{
    /// <summary>
    /// Harmony 补丁类，用于将社交用餐系统注入到殖民者的 AI 思维树中
    /// </summary>
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("rimtalk.socialdining");
            harmony.PatchAll();
            
            if (Prefs.DevMode)
            {
                Log.Message("[RimTalkSocialDining] Harmony 补丁已应用");
            }
        }
    }

    /// <summary>
    /// 注意：原来的 JobGiver_GetFood Prefix 补丁已移除
    /// 现在完全依赖 XML ThinkTree 注入（ThinkTree_SocialDining_Patch.xml）
    /// 这样可以避免与其他 Mod 的冲突，降低侵入性
    /// </summary>

    /// <summary>
    /// 可选的调试补丁：记录殖民者的思维树中包括社交用餐节点（可通过日志验证）
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class Patch_PawnJobTracker_StartJob
    {
        [HarmonyPrefix]
        public static void Prefix(Pawn ___pawn, Job newJob)
        {
            // 仅在开发模式下记录社交用餐任务
            if (Prefs.DevMode || SocialDiningSettings.enableDebugLogging)
            {
                if (newJob != null && newJob.def == SocialDiningDefOf.SocialDine)
                {
                    Log.Message($"[RimTalkSocialDining] {___pawn.LabelShort} 开始社交用餐任务");
                }
            }
        }
    }

    /// <summary>
    /// 关键补丁：防止食物在被多人共享时被意外销毁
    /// </summary>
    [HarmonyPatch(typeof(Thing), "Destroy")]
    public static class Patch_Thing_Destroy
    {
        [HarmonyPrefix]
        public static bool Prefix(Thing __instance, DestroyMode mode)
        {
            // 检查是否是被共享的食物
            if (__instance.def.IsIngestible)
            {
                SharedFoodTracker tracker = __instance.TryGetComp<SharedFoodTracker>();
                if (tracker != null && tracker.IsBeingShared && tracker.ActiveEatersCount > 0)
                {
                    // 防止销毁，直到最后一个用餐者完成
                    if (Prefs.DevMode || SocialDiningSettings.enableDebugLogging)
                    {
                        Log.Warning($"[RimTalkSocialDining] 阻止销毁共享食物 {__instance.Label}，还有 {tracker.ActiveEatersCount} 个用餐者");
                    }
                    return false;
                }
            }

            return true; // 允许正常销毁
        }
    }

    /// <summary>
    /// 优化补丁：防止食物在共享时被其他系统选取
    /// 改为 Postfix 以降低冲突风险
    /// </summary>
    [HarmonyPatch(typeof(FoodUtility), "BestFoodSourceOnMap")]
    public static class Patch_FoodUtility_BestFoodSourceOnMap
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn getter, ref Thing __result)
        {
            // 如果选中的食物正在被共享，排除它（让系统重新寻找）
            if (__result != null && __result.def.IsIngestible)
            {
                SharedFoodTracker tracker = __result.TryGetComp<SharedFoodTracker>();
                if (tracker != null && tracker.ActiveEatersCount >= 2)
                {
                    // 此食物已经被两人使用，不再提供给第三者
                    if (Prefs.DevMode || SocialDiningSettings.enableDebugLogging)
                    {
                        Log.Message($"[RimTalkSocialDining] 排除已共享的食物 {__result.Label}");
                    }
                    __result = null;
                }
            }
        }
    }
}
