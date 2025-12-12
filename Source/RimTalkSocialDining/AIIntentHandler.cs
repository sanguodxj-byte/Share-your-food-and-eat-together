using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimTalkSocialDining
{
    /// <summary>
    /// Module 4.1: AI Intent Handler
    /// 处理来自 AI 系统的"分享食物"意图
    /// 确保在主线程执行，并提供视觉反馈
    /// </summary>
    public static class AIIntentHandler
    {
        // 支持的意图名称
        private const string IntentShareFood = "share_food";

        /// <summary>
        /// 处理 AI 意图的主入口
        /// 在主线程执行，确保线程安全
        /// </summary>
        /// <param name="intentName">意图名称（例如："share_food"）</param>
        /// <param name="initiator">发起者 Pawn</param>
        /// <param name="recipient">接收者 Pawn</param>
        /// <param name="food">食物 Thing（可选）</param>
        /// <returns>是否成功处理意图</returns>
        public static bool HandleAIIntent(string intentName, Pawn initiator, Pawn recipient, Thing food = null)
        {
            // 检查 RimTalk AI 模式是否启用
            if (!SocialDiningSettings.useRimTalkAI)
            {
                Log.Message("[RimTalkSocialDining] RimTalk AI 模式已禁用，跳过意图处理");
                return false;
            }

            // 参数验证
            if (string.IsNullOrEmpty(intentName))
            {
                Log.Warning("[RimTalkSocialDining] HandleAIIntent: 意图名称为空");
                return false;
            }

            if (initiator == null || recipient == null)
            {
                Log.Warning($"[RimTalkSocialDining] HandleAIIntent: Pawn 参数无效 (initiator={initiator?.LabelShort}, recipient={recipient?.LabelShort})");
                return false;
            }

            // 确保在主线程执行
            bool result = false;
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                try
                {
                    result = HandleAIIntentInternal(intentName, initiator, recipient, food);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimTalkSocialDining] HandleAIIntent 异常: {ex}");
                    result = false;
                }
            });

            return result;
        }

        /// <summary>
        /// 内部意图处理逻辑
        /// </summary>
        private static bool HandleAIIntentInternal(string intentName, Pawn initiator, Pawn recipient, Thing food)
        {
            // 匹配意图名称
            if (intentName.ToLower() == IntentShareFood)
            {
                return HandleShareFoodIntent(initiator, recipient, food);
            }

            // 未知意图
            Log.Warning($"[RimTalkSocialDining] 未知的 AI 意图: {intentName}");
            return false;
        }

        /// <summary>
        /// 处理"分享食物"意图
        /// </summary>
        private static bool HandleShareFoodIntent(Pawn initiator, Pawn recipient, Thing food)
        {
            // 如果没有提供食物，尝试自动查找
            if (food == null)
            {
                food = FindFoodForSharing(initiator);
                
                if (food == null)
                {
                    Log.Message($"[RimTalkSocialDining] {initiator.LabelShort} 没有可分享的食物");
                    return false;
                }
            }

            // 生成上下文描述（用于日志）
            string context = ContextBaitGenerator.GetFoodContextDescription(initiator, recipient);
            if (!string.IsNullOrEmpty(context))
            {
                Log.Message($"[RimTalkSocialDining] AI 意图触发: {context}");
            }

            // 调用核心工具类触发共餐
            bool success = FoodSharingUtility.TryTriggerShareFood(initiator, recipient, food);

            // 视觉反馈处理
            if (!success)
            {
                // 失败时的视觉反馈已由 FoodSharingUtility 内部处理（拒绝气泡）
                // 这里只记录日志，不抛出错误
                Log.Message($"[RimTalkSocialDining] AI 意图执行失败: {initiator.LabelShort} → {recipient.LabelShort}");
            }
            else
            {
                // 成功时的日志
                Log.Message($"[RimTalkSocialDining] AI 意图执行成功: {initiator.LabelShort} 和 {recipient.LabelShort} 开始共餐");
            }

            return success;
        }

        /// <summary>
        /// 为发起者查找可分享的食物
        /// </summary>
        private static Thing FindFoodForSharing(Pawn pawn)
        {
            // 优先检查手持物品
            if (pawn.carryTracker?.CarriedThing != null)
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried.def.IsIngestible && carried.def.ingestible.preferability != FoodPreferability.Undefined)
                {
                    return carried;
                }
            }

            // 检查背包
            if (pawn.inventory?.innerContainer != null)
            {
                foreach (Thing thing in pawn.inventory.innerContainer)
                {
                    if (thing.def.IsIngestible && thing.def.ingestible.preferability != FoodPreferability.Undefined)
                    {
                        return thing;
                    }
                }
            }

            // 搜索附近的食物
            if (pawn.Map != null)
            {
                Thing nearbyFood = GenClosest.ClosestThingReachable(
                    pawn.Position,
                    pawn.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree),
                    PathEndMode.ClosestTouch,
                    TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                    15f, // 15 格搜索半径
                    (Thing t) => t.def.IsIngestible && 
                                 !t.IsForbidden(pawn) && 
                                 ReservationUtility.CanReserve(pawn, t)
                );

                return nearbyFood;
            }

            return null;
        }

        /// <summary>
        /// 批量处理多个 AI 意图（用于特殊事件）
        /// </summary>
        public static int HandleBatchIntents(string intentName, System.Collections.Generic.List<(Pawn, Pawn, Thing)> intentData)
        {
            if (intentData == null || intentData.Count == 0)
                return 0;

            int successCount = 0;

            foreach (var (initiator, recipient, food) in intentData)
            {
                if (HandleAIIntent(intentName, initiator, recipient, food))
                {
                    successCount++;
                }
            }

            Log.Message($"[RimTalkSocialDining] 批量处理 AI 意图: {successCount}/{intentData.Count} 成功");
            return successCount;
        }
    }
}
