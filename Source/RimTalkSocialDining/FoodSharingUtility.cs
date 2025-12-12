using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace RimTalkSocialDining
{
    /// <summary>
    /// 核心工具类 - 社交共餐系统的"大脑"
    /// 包含安全检查、桌子查找、接受度计算、拒绝文本等所有智能决策逻辑
    /// </summary>
    public static class FoodSharingUtility
    {
        // 接受度基础概率
        private const float BaseAcceptanceChance = 0.4f;
        
        // 修正系数
        private const float HighHungerBonus = 0.4f;
        private const float HighOpinionBonus = 0.3f;
        private const float SocialSkillBonus = 0.15f;
        private const float AbrasiveTraitPenalty = 0.2f;

        #region 安全检查

        /// <summary>
        /// 检查 Pawn 是否处于可以被打扰的状态
        /// 如果正在执行重要任务，返回 false
        /// </summary>
        public static bool IsSafeToDisturb(Pawn p)
        {
            if (p == null || p.Dead || p.Downed)
                return false;

            // 征召状态不能打扰
            if (p.Drafted)
                return false;

            // 精神状态不能打扰
            if (p.InMentalState)
                return false;

            // 检查当前工作
            if (p.CurJob != null)
            {
                JobDef jobDef = p.CurJob.def;

                // 灭火
                if (jobDef == JobDefOf.BeatFire || jobDef == JobDefOf.ExtinguishSelf)
                    return false;

                // 手术
                if (jobDef == JobDefOf.TendPatient || jobDef.driverClass == typeof(JobDriver_DoBill))
                    return false;

                // 护理伤员
                if (jobDef.driverClass.Name.Contains("Tend"))
                    return false;

                // 不可打断的任务
                if (!jobDef.playerInterruptible)
                    return false;
            }

            return true;
        }

        #endregion

        #region 桌子和位置查找

        /// <summary>
        /// 尝试找到适合两人用餐的餐桌
        /// </summary>
        /// <param name="map">地图</param>
        /// <param name="pawn1">第一个 Pawn</param>
        /// <param name="pawn2">第二个 Pawn</param>
        /// <param name="maxDistance">最大搜索距离</param>
        /// <returns>找到的餐桌，如果没有返回 null</returns>
        public static Building TryFindTableForTwo(Map map, Pawn pawn1, Pawn pawn2, float maxDistance = 40f)
        {
            if (map == null || pawn1 == null || pawn2 == null)
                return null;

            // 计算中间位置
            IntVec3 midPoint = new IntVec3(
                (pawn1.Position.x + pawn2.Position.x) / 2,
                0,
                (pawn1.Position.z + pawn2.Position.z) / 2
            );

            // 查找所有餐桌类建筑
            IEnumerable<Building> tables = map.listerBuildings.allBuildingsColonist
                .Where(b => b.def.building != null && b.def.building.isMealSource);

            Building bestTable = null;
            float bestScore = float.MaxValue;

            foreach (Building table in tables)
            {
                // 检查餐桌是否有效
                if (table.Destroyed || !table.Spawned)
                    continue;

                // 检查交互单元格
                if (!table.InteractionCell.IsValid)
                    continue;

                // 检查可达性
                if (!pawn1.CanReach(table.InteractionCell, PathEndMode.OnCell, Danger.Deadly))
                    continue;

                if (!pawn2.CanReach(table.InteractionCell, PathEndMode.OnCell, Danger.Deadly))
                    continue;

                // 检查是否能预留（支持双人）
                if (!pawn1.CanReserve(table) || !pawn2.CanReserve(table))
                    continue;

                // 计算距离分数（两人到餐桌的总距离）
                float dist1 = pawn1.Position.DistanceTo(table.Position);
                float dist2 = pawn2.Position.DistanceTo(table.Position);
                float totalDistance = dist1 + dist2;

                if (totalDistance > maxDistance * 2)
                    continue;

                // 选择距离最短的餐桌
                if (totalDistance < bestScore)
                {
                    bestScore = totalDistance;
                    bestTable = table;
                }
            }

            return bestTable;
        }

        /// <summary>
        /// 野餐模式：在指定位置附近找到两个相邻的站立点
        /// </summary>
        public static bool TryFindStandingSpotNear(IntVec3 center, Map map, out IntVec3 spot1, out IntVec3 spot2)
        {
            spot1 = IntVec3.Invalid;
            spot2 = IntVec3.Invalid;

            if (!center.IsValid || map == null)
                return false;

            // 在中心点附近搜索有效的相邻格子对
            for (int radius = 1; radius <= 5; radius++)
            {
                IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(center, radius, true);
                
                foreach (IntVec3 cell in cells)
                {
                    if (!cell.InBounds(map) || !cell.Standable(map))
                        continue;

                    // 检查相邻格子
                    foreach (IntVec3 adjacent in GenAdj.CardinalDirections)
                    {
                        IntVec3 neighbor = cell + adjacent;
                        
                        if (!neighbor.InBounds(map) || !neighbor.Standable(map))
                            continue;

                        // 找到了两个相邻的有效格子
                        spot1 = cell;
                        spot2 = neighbor;
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region 拒绝文本生成

        /// <summary>
        /// 根据拒绝原因生成随机的口语化中文拒绝文本
        /// </summary>
        public static string GetRandomRefusalText(Pawn initiator, Pawn recipient, string reason)
        {
            List<string> texts = new List<string>();

            switch (reason.ToLower())
            {
                case "hostile":
                    texts.Add("离我远点");
                    texts.Add("不想跟你吃");
                    texts.Add("别烦我");
                    texts.Add("我们不熟");
                    texts.Add("滚开");
                    break;

                case "full":
                    texts.Add("还不饿呢");
                    texts.Add("吃不下了");
                    texts.Add("刚吃过");
                    texts.Add("不饿");
                    texts.Add("待会儿再说");
                    break;

                case "busy":
                    texts.Add("我很忙");
                    texts.Add("没空");
                    texts.Add("正忙着呢");
                    texts.Add("等我忙完");
                    break;

                case "foodhate":
                    texts.Add("我不吃这个");
                    texts.Add("这什么玩意");
                    texts.Add("换个别的吧");
                    texts.Add("不喜欢这个");
                    break;

                default: // generic
                    texts.Add("下次吧");
                    texts.Add("没心情");
                    texts.Add("不想吃");
                    texts.Add("改天吧");
                    texts.Add("算了");
                    texts.Add("不了");
                    break;
            }

            return texts.RandomElement();
        }

        #endregion

        #region 接受度计算

        /// <summary>
        /// 计算接受共餐的概率并进行随机判定
        /// 考虑饥饿度、关系、社交技能、特性等因素
        /// </summary>
        public static bool TryRollForAcceptance(Pawn initiator, Pawn recipient, Thing food, out string refusalReason)
        {
            refusalReason = "generic";

            // 基础检查
            if (initiator == null || recipient == null || food == null)
                return false;

            // 检查食物是否可食用
            if (!food.def.IsIngestible)
            {
                refusalReason = "foodhate";
                return false;
            }

            // 检查接收者是否安全可打扰
            if (!IsSafeToDisturb(recipient))
            {
                refusalReason = "busy";
                return false;
            }

            // 检查接收者是否太饱
            if (recipient.needs?.food != null && recipient.needs.food.CurLevelPercentage > 0.8f)
            {
                refusalReason = "full";
                return false;
            }

            // 检查食物偏好（素食主义者 vs 肉食）
            if (!FoodUtility.WillEat(recipient, food, null, false))
            {
                refusalReason = "foodhate";
                return false;
            }

            // 开始计算接受概率
            float acceptanceChance = BaseAcceptanceChance;

            // 修正 1: 饥饿度（非常饿时更容易接受）
            if (recipient.needs?.food != null)
            {
                float hungerLevel = 1f - recipient.needs.food.CurLevelPercentage;
                if (hungerLevel > 0.5f) // 饥饿度超过 50%
                {
                    acceptanceChance += HighHungerBonus;
                }
            }

            // 修正 2: 社交关系（好感度）
            if (recipient.relations != null && initiator != null)
            {
                int opinion = recipient.relations.OpinionOf(initiator);
                if (opinion >= 20) // 好感度 >= 20
                {
                    acceptanceChance += HighOpinionBonus * (opinion / 100f);
                }
                else if (opinion < -20) // 敌对关系
                {
                    refusalReason = "hostile";
                    return false;
                }
            }

            // 修正 3: 发起者的社交技能
            if (initiator.skills != null)
            {
                int socialSkill = initiator.skills.GetSkill(SkillDefOf.Social).Level;
                if (socialSkill >= 8)
                {
                    acceptanceChance += SocialSkillBonus * (socialSkill / 20f);
                }
            }

            // 修正 4: 接收者的特性
            if (recipient.story?.traits != null)
            {
                // Abrasive（粗鲁）特性 - 降低接受率
                if (recipient.story.traits.HasTrait(TraitDefOf.Abrasive))
                {
                    acceptanceChance -= AbrasiveTraitPenalty;
                }

                // Kind（善良）特性 - 提高接受率
                if (recipient.story.traits.HasTrait(TraitDefOf.Kind))
                {
                    acceptanceChance += 0.15f;
                }

                // Ascetic（禁欲）特性 - 降低接受率
                if (recipient.story.traits.HasTrait(TraitDefOf.Ascetic))
                {
                    acceptanceChance -= 0.1f;
                }
            }

            // 确保概率在 0-1 之间
            acceptanceChance = Mathf.Clamp01(acceptanceChance);

            // 进行随机判定
            bool accepted = Rand.Chance(acceptanceChance);

            if (!accepted)
            {
                refusalReason = "generic";
            }

            return accepted;
        }

        #endregion

        #region 触发共餐

        /// <summary>
        /// 尝试触发共餐流程的主入口
        /// 包含完整的检查、掉落食物、多人预留等步骤
        /// </summary>
        public static bool TryTriggerShareFood(Pawn initiator, Pawn recipient, Thing food)
        {
            // Step 0: 检查是否已经在社交共餐中
            if (initiator.CurJob != null && initiator.CurJob.def == SocialDiningDefOf.SocialDine)
            {
                Log.Message($"[RimTalkSocialDining] {initiator.LabelShort} 已经在社交共餐中，跳过");
                return false;
            }

            if (recipient.CurJob != null && recipient.CurJob.def == SocialDiningDefOf.SocialDine)
            {
                Log.Message($"[RimTalkSocialDining] {recipient.LabelShort} 已经在社交共餐中，跳过");
                return false;
            }

            // Step 1: 安全检查
            if (!IsSafeToDisturb(initiator) || !IsSafeToDisturb(recipient))
            {
                return false;
            }

            // Step 1.5: 严格的饥饿检查（使用设置中的阈值）
            float hungerThreshold = 1f - SocialDiningSettings.hungerThreshold;
            
            if (initiator.needs?.food == null || initiator.needs.food.CurLevelPercentage > hungerThreshold)
            {
                Log.Message($"[RimTalkSocialDining] {initiator.LabelShort} 不够饥饿 ({initiator.needs.food.CurLevelPercentage:P0})");
                return false;
            }

            if (recipient.needs?.food == null || recipient.needs.food.CurLevelPercentage > hungerThreshold)
            {
                Log.Message($"[RimTalkSocialDining] {recipient.LabelShort} 不够饥饿 ({recipient.needs.food.CurLevelPercentage:P0})");
                return false;
            }

            // Step 2: 接受度判定
            if (!TryRollForAcceptance(initiator, recipient, food, out string refusalReason))
            {
                // 显示拒绝文本
                string refusalText = GetRandomRefusalText(initiator, recipient, refusalReason);
                SpawnRefusalMote(recipient, refusalText);
                return false;
            }

            // Step 3: 掉落食物（如果发起者持有）
            Thing foodToDrop = null;
            if (initiator.carryTracker?.CarriedThing == food)
            {
                if (initiator.carryTracker.TryDropCarriedThing(initiator.Position, ThingPlaceMode.Near, out foodToDrop))
                {
                    food = foodToDrop;
                }
                else
                {
                    Log.Warning("[RimTalkSocialDining] 无法放下食物");
                    return false;
                }
            }

            // Step 4: 检查食物是否仍然有效
            if (food == null || food.Destroyed || !food.Spawned)
            {
                Log.Warning("[RimTalkSocialDining] 食物无效或已被销毁");
                return false;
            }

            // Step 5: 多人预留检查
            if (!initiator.CanReserve(food, 1, -1, null, false))
            {
                Pawn reserver = initiator.Map?.reservationManager?.FirstRespectedReserver(food, initiator);
                if (reserver != recipient)
                {
                    Log.Warning($"[RimTalkSocialDining] {initiator.LabelShort} 无法预留食物 (已被 {reserver?.LabelShort ?? "未知"} 预留)");
                    return false;
                }
            }

            if (!recipient.CanReserve(food, 1, -1, null, false))
            {
                Pawn reserver = recipient.Map?.reservationManager?.FirstRespectedReserver(food, recipient);
                if (reserver != initiator)
                {
                    Log.Warning($"[RimTalkSocialDining] {recipient.LabelShort} 无法预留食物 (已被 {reserver?.LabelShort ?? "未知"} 预留)");
                    return false;
                }
            }

            // Step 6: 查找餐桌或野餐地点
            Building table = TryFindTableForTwo(initiator.Map, initiator, recipient, 40f);
            
            // Step 7: 创建任务
            Job initiatorJob = JobMaker.MakeJob(SocialDiningDefOf.SocialDine, food, table, recipient);
            initiatorJob.count = 1;
            initiatorJob.playerForced = false;

            Job recipientJob = JobMaker.MakeJob(SocialDiningDefOf.SocialDine, food, table, initiator);
            recipientJob.count = 1;
            recipientJob.playerForced = false;

            // Step 8: 启动任务
            if (initiator.jobs.TryTakeOrderedJob(initiatorJob, JobTag.Misc, false))
            {
                Log.Message($"[RimTalkSocialDining] {initiator.LabelShort} 接受社交共餐任务");
            }
            else
            {
                Log.Warning($"[RimTalkSocialDining] {initiator.LabelShort} 无法接受社交共餐任务");
                return false;
            }

            if (recipient.jobs.TryTakeOrderedJob(recipientJob, JobTag.Misc, false))
            {
                Log.Message($"[RimTalkSocialDining] {recipient.LabelShort} 接受社交共餐任务");
            }
            else
            {
                Log.Warning($"[RimTalkSocialDining] {recipient.LabelShort} 无法接受社交共餐任务，取消发起者任务");
                initiator.jobs.EndCurrentJob(JobCondition.InterruptForced);
                return false;
            }

            return true;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 在 Pawn 头顶生成拒绝文本气泡
        /// </summary>
        private static void SpawnRefusalMote(Pawn pawn, string text)
        {
            if (pawn == null || !pawn.Spawned)
                return;

            MoteMaker.ThrowText(pawn.DrawPos + new Vector3(0f, 0f, 0.5f), pawn.Map, text, Color.white, 3.5f);
        }

        #endregion
    }
}
