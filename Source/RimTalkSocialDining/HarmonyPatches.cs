using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// 可选的调试补丁：记录殖民者的思维树中包含社交用餐节点（通过日志验证）
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class Patch_PawnJobTracker_StartJob
    {
        [HarmonyPrefix]
        public static void Prefix(Pawn ___pawn, Job newJob)
        {
            // 在开发模式下记录社交用餐任务
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
    /// ? 新增：RimTalk 响应处理补丁 - 拦截 AI 输出并解析意图命令
    /// </summary>
    [HarmonyPatch]
    public static class Patch_RimTalk_ProcessResponse
    {
        // 尝试多个可能的目标方法
        static bool Prepare()
        {
            // 检查 RimTalk 是否加载
            var rimTalkAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name.Contains("RimTalk") && !a.GetName().Name.Contains("ExpandMemory"));
            
            return rimTalkAssembly != null;
        }

        static System.Reflection.MethodBase TargetMethod()
        {
            // 查找 RimTalk.Service.AIService 的响应处理方法
            var rimTalkAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name.Contains("RimTalk") && !a.GetName().Name.Contains("ExpandMemory"));
            
            if (rimTalkAssembly == null)
                return null;

            // 查找 AIService 类型
            var aiServiceType = rimTalkAssembly.GetType("RimTalk.Service.AIService");
            if (aiServiceType == null)
                return null;

            // 查找响应处理方法（可能是 HandleResponse、ProcessResponse 等）
            var methods = aiServiceType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance);
            
            foreach (var method in methods)
            {
                // 寻找包含 response 参数的方法
                var parameters = method.GetParameters();
                if (parameters.Any(p => p.Name.ToLower().Contains("response") || p.ParameterType.Name.ToLower().Contains("response")))
                {
                    Log.Message($"[RimTalkSocialDining] 找到 RimTalk 响应处理方法：{method.Name}");
                    return method;
                }
            }

            return null;
        }

        [HarmonyPostfix]
        public static void Postfix(object __instance, string __result)
        {
            // 尝试解析 AI 响应中的共餐意图
            if (!string.IsNullOrEmpty(__result))
            {
                try
                {
                    // 获取当前对话的 Pawn（需要通过反射获取）
                    // 这里简化处理，实际可能需要更复杂的上下文获取
                    
                    // 尝试解析意图
                    // RimTalkIntentListener.TryParseAndExecute(__result, speaker, listener);
                    
                    if (Prefs.DevMode || SocialDiningSettings.enableDebugLogging)
                    {
                        if (__result.Contains("share_food"))
                        {
                            Log.Message($"[RimTalkSocialDining] 检测到 AI 输出中的共餐意图：{__result}");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimTalkSocialDining] RimTalk 响应处理补丁出错：{ex.Message}");
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
