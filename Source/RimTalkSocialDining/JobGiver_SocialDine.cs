using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimTalkSocialDining
{
    /// <summary>
    /// JobGiver 用于社交共餐系统。
    /// 现在使用 FoodSharingUtility 作为核心决策引擎
    /// </summary>
    public class JobGiver_SocialDine : ThinkNode_JobGiver
    {
        // 搜索半径常量
        private const float MaxFoodSearchDistance = 50f;
        private const float MaxPartnerSearchDistance = 30f;

        // 冷却时间：同一对 Pawn 在完成一次共餐后，多久才能再次共餐（游戏刻数）
        private const int CooldownTicks = 5000; // 约 2 小时游戏时间

        // 静态字典存储冷却状态 (PawnPairKey -> LastAttemptTick)
        private static Dictionary<string, int> cooldownTracker = new Dictionary<string, int>();

        protected override Job TryGiveJob(Pawn pawn)
        {
            return TryGiveJobInternal(pawn);
        }

        /// <summary>
        /// Public wrapper for TryGiveJob (used by Harmony patches)
        /// </summary>
        public Job TryGiveJobInternal(Pawn pawn)
        {
            // 检查 AI 自动触发是否启用
            if (!SocialDiningSettings.enableAutoSocialDining)
            {
                return null;
            }

            // 验证基本条件
            if (pawn.needs?.food == null || !pawn.RaceProps.EatsFood)
                return null;

            // 使用工具类进行安全检查
            if (!FoodSharingUtility.IsSafeToDisturb(pawn))
                return null;

            // 检查饥饿度 - 使用设置中的阈值
            float hungerThreshold = 1f - SocialDiningSettings.hungerThreshold;
            if (pawn.needs.food.CurLevelPercentage > hungerThreshold)
                return null;

            // 第一步：寻找合适的用餐伙伴
            Pawn partner = FindBestDiningPartner(pawn);
            if (partner == null)
                return null;

            // 检查冷却时间 - 使用设置中的冷却时间
            string pairKey = GetPawnPairKey(pawn, partner);
            int currentTick = Find.TickManager.TicksGame;
            
            if (cooldownTracker.TryGetValue(pairKey, out int lastAttempt))
            {
                if (currentTick - lastAttempt < SocialDiningSettings.CooldownTicks)
                {
                    // 还在冷却期，不触发
                    return null;
                }
            }

            // 记录尝试时间
            cooldownTracker[pairKey] = currentTick;

            // 第二步：寻找合适的食物
            Thing food = FindBestFood(pawn);
            if (food == null)
            {
                return null;
            }

            // 第三步：使用 FoodSharingUtility 触发完整的共餐流程
            // 这会处理所有逻辑：接受度判定、餐桌查找、任务创建等
            if (FoodSharingUtility.TryTriggerShareFood(pawn, partner, food))
            {
                // 成功触发，任务已由工具类创建并启动
                return null;
            }

            // 失败，返回 null 让 AI 尝试其他任务
            return null;
        }

        /// <summary>
        /// 生成 Pawn 对的唯一键（用于冷却追踪）
        /// </summary>
        private string GetPawnPairKey(Pawn pawn1, Pawn pawn2)
        {
            // 确保键的顺序一致（无论谁是发起者）
            int id1 = pawn1.thingIDNumber;
            int id2 = pawn2.thingIDNumber;
            
            if (id1 < id2)
                return $"{id1}_{id2}";
            else
                return $"{id2}_{id1}";
        }

        /// <summary>
        /// 清理过期的冷却记录（定期调用以防止内存泄漏）
        /// </summary>
        public static void CleanupOldCooldowns()
        {
            int currentTick = Find.TickManager.TicksGame;
            List<string> toRemove = new List<string>();

            foreach (var kvp in cooldownTracker)
            {
                if (currentTick - kvp.Value > CooldownTicks * 2)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (string key in toRemove)
            {
                cooldownTracker.Remove(key);
            }
        }

        /// <summary>
        /// 寻找最佳食物 - 考虑营养价值、距离和可达性
        /// </summary>
        private Thing FindBestFood(Pawn pawn)
        {
            if (pawn.Map == null)
                return null;

            Predicate<Thing> foodValidator = (Thing t) =>
            {
                // 必须是可食用的
                if (!t.def.IsIngestible)
                    return false;

                // 必须是食物（不是药物等）
                if (t.def.ingestible.preferability == FoodPreferability.Undefined)
                    return false;

                // 不能被禁止
                if (t.IsForbidden(pawn))
                    return false;

                // 必须有足够的营养
                if (FoodUtility.GetNutrition(pawn, t, t.def) < 0.05f)
                    return false;

                // 不能在燃烧
                if (t.IsBurning())
                    return false;

                // 必须能够预留
                if (!pawn.CanReserve(t))
                    return false;

                // 检查是否已经被过多的人共享
                ThingWithComps twc = t as ThingWithComps;
                if (twc != null)
                {
                    SharedFoodTracker tracker = twc.TryGetComp<SharedFoodTracker>();
                    if (tracker != null && tracker.ActiveEatersCount >= 2)
                        return false;
                }

                return true;
            };

            // 使用 RimWorld 的食物搜索系统
            Thing foundFood = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree),
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                MaxFoodSearchDistance,
                foodValidator
            );

            return foundFood;
        }

        /// <summary>
        /// 寻找最佳用餐伙伴 - 考虑距离、饥饿程度和安全状态
        /// </summary>
        private Pawn FindBestDiningPartner(Pawn pawn)
        {
            if (pawn.Map == null)
                return null;

            Pawn bestPartner = null;
            float bestScore = float.MaxValue;

            // 使用设置中的饥饿阈值
            float hungerThreshold = 1f - SocialDiningSettings.hungerThreshold;

            // 遍历所有自由殖民者
            foreach (Pawn colonist in pawn.Map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist == pawn)
                    continue;

                // 使用工具类检查是否安全可打扰
                if (!FoodSharingUtility.IsSafeToDisturb(colonist))
                    continue;

                // 必须饥饿（使用设置中的阈值）
                if (colonist.needs?.food == null || colonist.needs.food.CurLevelPercentage > hungerThreshold)
                    continue;

                // 必须能够社交
                if (colonist.WorkTagIsDisabled(WorkTags.Social))
                    continue;

                // 必须可达
                if (!pawn.CanReach(colonist, PathEndMode.Touch, Danger.Deadly))
                    continue;

                // 距离限制
                float distance = colonist.Position.DistanceTo(pawn.Position);
                if (distance > MaxPartnerSearchDistance)
                    continue;

                // 计算评分：距离越近越好，饥饿度越高越好
                float hungerScore = 1f - colonist.needs.food.CurLevelPercentage;
                float score = distance - (hungerScore * 10f); // 饥饿度权重更高

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPartner = colonist;
                }
            }

            return bestPartner;
        }
    }
}
