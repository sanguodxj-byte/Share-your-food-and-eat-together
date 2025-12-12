using RimWorld;
using Verse;
using Verse.AI;

namespace RimTalkSocialDining
{
    /// <summary>
    /// Module 3: Context Bait Generator
    /// 生成上下文描述，用于触发 AI 识别共餐机会
    /// </summary>
    public static class ContextBaitGenerator
    {
        // 饥饿阈值 - 低于此值视为饥饿
        private const float HungerThreshold = 0.3f;

        /// <summary>
        /// 获取食物共享的上下文描述
        /// 用于 AI 系统识别共餐机会
        /// </summary>
        /// <param name="initiator">发起者 Pawn</param>
        /// <param name="recipient">接收者 Pawn</param>
        /// <returns>上下文描述字符串，如果不满足条件则返回空字符串</returns>
        public static string GetFoodContextDescription(Pawn initiator, Pawn recipient)
        {
            // 参数验证
            if (initiator == null || recipient == null)
                return string.Empty;

            // 检查接收者是否饥饿（< 30%）
            if (recipient.needs?.food == null)
                return string.Empty;

            float hungerLevel = recipient.needs.food.CurLevelPercentage;
            bool isHungry = hungerLevel < HungerThreshold;

            if (!isHungry)
                return string.Empty;

            // 检查发起者是否拥有食物
            bool hasFood = InitiatorHasFood(initiator);

            if (!hasFood)
                return string.Empty;

            // 满足所有条件 - 生成上下文描述
            return GenerateContextString(recipient.LabelShort);
        }

        /// <summary>
        /// 检查发起者是否拥有可以分享的食物
        /// </summary>
        private static bool InitiatorHasFood(Pawn initiator)
        {
            // 检查 1: 手持物品
            if (initiator.carryTracker?.CarriedThing != null)
            {
                Thing carried = initiator.carryTracker.CarriedThing;
                if (carried.def.IsIngestible && carried.def.ingestible.preferability != FoodPreferability.Undefined)
                {
                    return true;
                }
            }

            // 检查 2: 背包中的食物
            if (initiator.inventory?.innerContainer != null)
            {
                foreach (Thing thing in initiator.inventory.innerContainer)
                {
                    if (thing.def.IsIngestible && thing.def.ingestible.preferability != FoodPreferability.Undefined)
                    {
                        return true;
                    }
                }
            }

            // 检查 3: 附近可达的食物（小范围）
            if (initiator.Map != null)
            {
                Thing nearbyFood = GenClosest.ClosestThingReachable(
                    initiator.Position,
                    initiator.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree),
                    PathEndMode.ClosestTouch,
                    TraverseParms.For(initiator, Danger.Deadly, TraverseMode.ByPawn, false),
                    10f, // 较小的搜索半径
                    (Thing t) => t.def.IsIngestible && 
                                 !t.IsForbidden(initiator) && 
                                 ReservationUtility.CanReserve(initiator, t)
                );

                if (nearbyFood != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 生成标准化的上下文描述字符串
        /// 包含关键词：饥饿、食物、分享
        /// </summary>
        private static string GenerateContextString(string recipientName)
        {
            return $"[环境状态] 目标 {recipientName} 处于**饥饿**状态。发起者拥有**食物**，并且可以进行**分享**。";
        }

        /// <summary>
        /// 批量检查多个潜在接收者，返回所有符合条件的上下文描述
        /// </summary>
        public static string GetBatchContextDescription(Pawn initiator, System.Collections.Generic.IEnumerable<Pawn> potentialRecipients)
        {
            if (initiator == null || potentialRecipients == null)
                return string.Empty;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (Pawn recipient in potentialRecipients)
            {
                string context = GetFoodContextDescription(initiator, recipient);
                if (!string.IsNullOrEmpty(context))
                {
                    sb.AppendLine(context);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取详细的饥饿状态描述（用于调试或 UI 显示）
        /// </summary>
        public static string GetDetailedHungerStatus(Pawn pawn)
        {
            if (pawn?.needs?.food == null)
                return "未知";

            float percentage = pawn.needs.food.CurLevelPercentage;

            if (percentage < 0.1f)
                return "极度饥饿";
            else if (percentage < 0.3f)
                return "饥饿";
            else if (percentage < 0.5f)
                return "有点饿";
            else if (percentage < 0.8f)
                return "正常";
            else
                return "饱食";
        }
    }
}
