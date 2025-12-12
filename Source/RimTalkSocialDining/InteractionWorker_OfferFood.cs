using RimWorld;
using Verse;
using Verse.AI;

namespace RimTalkSocialDining
{
    /// <summary>
    /// Module 4.2: Vanilla Interaction Worker
    /// 实现原版社交互动系统中的"提供食物"互动
    /// 统一 AI 和玩家触发的行为逻辑
    /// </summary>
    public class InteractionWorker_OfferFood : InteractionWorker
    {
        // 饥饿阈值 - 低于此值才会触发互动
        private const float HungerThreshold = 0.5f;

        /// <summary>
        /// 计算随机选择权重
        /// 如果接收者饥饿且发起者有食物，返回较高权重
        /// </summary>
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            // 检查原版互动模式是否启用
            if (!SocialDiningSettings.useVanillaInteraction)
            {
                return 0f; // 禁用时不显示在社交菜单中
            }

            // 基础检查
            if (initiator == null || recipient == null)
                return 0f;

            // 发起者必须持有食物
            Thing carriedFood = initiator.carryTracker?.CarriedThing;
            if (carriedFood == null || !carriedFood.def.IsIngestible)
                return 0f;

            // 接收者必须有食物需求
            if (recipient.needs?.food == null)
                return 0f;

            // 接收者必须有一定的饥饿度
            float hungerLevel = 1f - recipient.needs.food.CurLevelPercentage;
            if (hungerLevel < 0.2f) // 至少 20% 饥饿
                return 0f;

            // 接收者不能正在进食
            if (recipient.CurJob != null && recipient.CurJob.def == JobDefOf.Ingest)
                return 0f;

            // 基础权重：根据饥饿程度调整
            float baseWeight = 0.5f;
            float hungerBonus = hungerLevel * 0.5f; // 越饿权重越高

            // 关系修正
            float opinionFactor = 1f;
            if (recipient.relations != null && initiator != null)
            {
                int opinion = recipient.relations.OpinionOf(initiator);
                if (opinion >= 20)
                    opinionFactor = 1.5f; // 好感度高时更容易提供
                else if (opinion < -20)
                    opinionFactor = 0.3f; // 敌对时不太会提供
            }

            return baseWeight * (1f + hungerBonus) * opinionFactor;
        }

        /// <summary>
        /// 执行互动
        /// 调用 FoodSharingUtility 统一的触发逻辑
        /// </summary>
        public override void Interacted(Pawn initiator, Pawn recipient, System.Collections.Generic.List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            // 初始化输出参数
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;

            // 检查原版互动模式是否启用
            if (!SocialDiningSettings.useVanillaInteraction)
            {
                Log.Warning("[RimTalkSocialDining] 原版互动模式已禁用，跳过互动");
                return;
            }

            // 查找食物
            Thing food = FindFoodForSharing(initiator);

            if (food == null)
            {
                Log.Warning($"[RimTalkSocialDining] InteractionWorker: {initiator.LabelShort} 没有可分享的食物");
                return;
            }

            // 生成上下文描述
            string context = ContextBaitGenerator.GetFoodContextDescription(initiator, recipient);
            if (!string.IsNullOrEmpty(context))
            {
                Log.Message($"[RimTalkSocialDining] 原版互动触发: {context}");
            }

            // 调用统一的触发逻辑（包含概率检查）
            bool success = FoodSharingUtility.TryTriggerShareFood(initiator, recipient, food);

            if (success)
            {
                Log.Message($"[RimTalkSocialDining] 原版互动成功触发共餐：{initiator.LabelShort} -> {recipient.LabelShort}");

                // 添加互动记录到历史
                TaleRecorder.RecordTale(TaleDefOf.TradedWith, new object[] { initiator, recipient });

                // 可选：添加到 extraSentencePacks 用于对话气泡
                // extraSentencePacks?.Add(RulePackDefOf.Sentence_OfferFood); // 需要自定义 RulePackDef
            }
            else
            {
                // 失败处理已由 FoodSharingUtility 内部完成（拒绝气泡）
                Log.Message($"[RimTalkSocialDining] 原版互动被拒绝: {initiator.LabelShort} → {recipient.LabelShort}");
            }
        }

        /// <summary>
        /// 检查发起者是否有可分享的食物
        /// </summary>
        private bool InitiatorHasFood(Pawn initiator)
        {
            // 手持物品
            if (initiator.carryTracker?.CarriedThing != null)
            {
                Thing carried = initiator.carryTracker.CarriedThing;
                if (carried.def.IsIngestible && carried.def.ingestible.preferability != FoodPreferability.Undefined)
                {
                    return true;
                }
            }

            // 背包物品
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

            // 附近食物（小范围）
            if (initiator.Map != null)
            {
                Thing nearbyFood = GenClosest.ClosestThingReachable(
                    initiator.Position,
                    initiator.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree),
                    PathEndMode.ClosestTouch,
                    TraverseParms.For(initiator, Danger.Deadly, TraverseMode.ByPawn, false),
                    10f,
                    (Thing t) => t.def.IsIngestible && 
                                 !t.IsForbidden(initiator) && 
                                 ReservationUtility.CanReserve(initiator, t)
                );

                return nearbyFood != null;
            }

            return false;
        }

        /// <summary>
        /// 为发起者查找可分享的食物
        /// </summary>
        private Thing FindFoodForSharing(Pawn pawn)
        {
            // 优先手持
            if (pawn.carryTracker?.CarriedThing != null)
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried.def.IsIngestible && carried.def.ingestible.preferability != FoodPreferability.Undefined)
                {
                    return carried;
                }
            }

            // 背包
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

            // 附近
            if (pawn.Map != null)
            {
                Thing nearbyFood = GenClosest.ClosestThingReachable(
                    pawn.Position,
                    pawn.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree),
                    PathEndMode.ClosestTouch,
                    TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                    15f,
                    (Thing t) => t.def.IsIngestible && 
                                 !t.IsForbidden(pawn) && 
                                 ReservationUtility.CanReserve(pawn, t)
                );

                return nearbyFood;
            }

            return null;
        }
    }
}
