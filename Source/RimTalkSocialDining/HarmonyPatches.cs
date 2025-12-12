using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimTalkSocialDining
{
    /// <summary>
    /// Harmony 补丁类，用于将社交共餐系统注入到殖民者的 AI 思考树中
    /// </summary>
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("rimtalk.socialdining");
            harmony.PatchAll();
            
            Log.Message("[RimTalkSocialDining] Harmony 补丁已应用");
        }
    }

    /// <summary>
    /// 补丁：在殖民者获取任务时插入社交共餐检查
    /// 这是 XML Patch 的备用方案
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
    public static class Patch_JobGiver_GetFood_TryGiveJob
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            // 在普通进食任务之前，尝试社交共餐
            if (pawn == null || pawn.Dead || !pawn.Spawned)
                return true;

            // 检查是否已经在执行社交共餐任务
            if (pawn.CurJob != null && pawn.CurJob.def == SocialDiningDefOf.SocialDine)
            {
                // 已经在社交共餐，不要覆盖
                __result = null;
                return false;
            }

            // 检查是否可以社交共餐
            ThinkNode_ConditionalCanSocialDine conditionalNode = new ThinkNode_ConditionalCanSocialDine();
            if (conditionalNode.IsSatisfied(pawn))
            {
                // 尝试获取社交共餐任务
                JobGiver_SocialDine socialDineGiver = new JobGiver_SocialDine();
                Job socialJob = socialDineGiver.TryGiveJobInternal(pawn);
                
                if (socialJob != null)
                {
                    __result = socialJob;
                    return false; // 跳过原始方法，使用社交共餐任务
                }
            }

            return true; // 继续执行原始方法（普通进食）
        }
    }

    /// <summary>
    /// 补丁：在殖民者思考树中插入社交共餐节点（旧的日志补丁）
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class Patch_PawnJobTracker_StartJob
    {
        [HarmonyPrefix]
        public static void Prefix(Pawn ___pawn, Job newJob)
        {
            // 在任务开始时记录社交共餐任务
            if (newJob != null && newJob.def == SocialDiningDefOf.SocialDine)
            {
                Log.Message($"[RimTalkSocialDining] {___pawn.LabelShort} 开始社交共餐任务");
            }
        }
    }

    /// <summary>
    /// 补丁：防止食物在被多人共享时被过早销毁
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
                    Log.Warning($"[RimTalkSocialDining] 阻止销毁共享食物 {__instance.Label}，还有 {tracker.ActiveEatersCount} 人在用餐");
                    return false;
                }
            }

            return true; // 允许正常销毁
        }
    }

    /// <summary>
    /// 补丁：防止食物在共享时被其他系统干扰
    /// </summary>
    [HarmonyPatch(typeof(FoodUtility), "BestFoodSourceOnMap")]
    public static class Patch_FoodUtility_BestFoodSourceOnMap
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn getter, ref Thing __result)
        {
            // 如果选中的食物正在被共享且已满员，则排除它
            if (__result != null && __result.def.IsIngestible)
            {
                SharedFoodTracker tracker = __result.TryGetComp<SharedFoodTracker>();
                if (tracker != null && tracker.ActiveEatersCount >= 2)
                {
                    // 这个食物已经被两个人使用，不再提供给其他人
                    __result = null;
                }
            }
        }
    }
}
