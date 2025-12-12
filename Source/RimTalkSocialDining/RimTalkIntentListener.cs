using System;
using System.Text.RegularExpressions;
using Verse;

namespace RimTalkSocialDining
{
    /// <summary>
    /// RimTalk 意图监听器 - 解析 AI 输出中的共餐命令并执行
    /// 作为 RimTalk 和本 Mod 之间的桥梁
    /// </summary>
    public static class RimTalkIntentListener
    {
        // 命令匹配模式
        private static readonly Regex ShareFoodPattern = new Regex(
            @"share_food\s*\(\s*([^,]+)\s*,\s*([^,]+)\s*(?:,\s*([^)]+))?\s*\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        /// <summary>
        /// 解析 AI 输出文本，提取并执行共餐意图
        /// 应该被 RimTalk 的响应处理流程调用
        /// </summary>
        /// <param name="aiResponse">AI 的响应文本</param>
        /// <param name="speaker">说话的 Pawn</param>
        /// <param name="listener">听话的 Pawn</param>
        /// <returns>是否成功执行了意图</returns>
        public static bool TryParseAndExecute(string aiResponse, Pawn speaker, Pawn listener)
        {
            if (string.IsNullOrEmpty(aiResponse))
                return false;

            // 检查是否包含 share_food 命令
            var match = ShareFoodPattern.Match(aiResponse);
            if (!match.Success)
                return false;

            try
            {
                // 提取参数
                string initiatorName = match.Groups[1].Value.Trim();
                string recipientName = match.Groups[2].Value.Trim();
                string foodName = match.Groups.Count > 3 ? match.Groups[3].Value.Trim() : null;

                // 解析 Pawn（简化版，实际可能需要更复杂的映射）
                Pawn initiator = ResolvePawnByName(initiatorName, speaker, listener);
                Pawn recipient = ResolvePawnByName(recipientName, speaker, listener);

                if (initiator == null || recipient == null)
                {
                    Log.Warning($"[RimTalkIntentListener] 无法解析 Pawn：initiator={initiatorName}, recipient={recipientName}");
                    return false;
                }

                // 查找食物（如果指定）
                Thing food = null;
                if (!string.IsNullOrEmpty(foodName))
                {
                    food = FindFoodByName(initiator, foodName);
                }

                // 执行意图
                bool success = AIIntentHandler.HandleAIIntent("share_food", initiator, recipient, food);

                if (success)
                {
                    Log.Message($"[RimTalkIntentListener] 成功执行共餐意图：{initiator.LabelShort} -> {recipient.LabelShort}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalkIntentListener] 解析意图时出错：{ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 根据名字解析 Pawn
        /// </summary>
        private static Pawn ResolvePawnByName(string name, Pawn candidate1, Pawn candidate2)
        {
            // 优先匹配候选者
            if (candidate1 != null && NameMatches(candidate1, name))
                return candidate1;

            if (candidate2 != null && NameMatches(candidate2, name))
                return candidate2;

            // 在当前地图中搜索
            if (Find.CurrentMap != null)
            {
                foreach (var pawn in Find.CurrentMap.mapPawns.FreeColonists)
                {
                    if (NameMatches(pawn, name))
                        return pawn;
                }
            }

            return null;
        }

        /// <summary>
        /// 检查名字是否匹配
        /// </summary>
        private static bool NameMatches(Pawn pawn, string name)
        {
            if (pawn == null || string.IsNullOrEmpty(name))
                return false;

            // 移除常见代词
            name = name.ToLower().Replace("you", "").Replace("me", "").Replace("i", "").Trim();

            string pawnName = pawn.LabelShort.ToLower();
            string pawnNick = pawn.Name?.ToStringShort?.ToLower() ?? "";

            return pawnName.Contains(name) || name.Contains(pawnName) ||
                   pawnNick.Contains(name) || name.Contains(pawnNick);
        }

        /// <summary>
        /// 根据名字查找食物
        /// </summary>
        private static Thing FindFoodByName(Pawn pawn, string foodName)
        {
            // 简化实现：返回 pawn 携带或附近的第一个食物
            // 实际可以根据 foodName 做更精确的匹配
            
            // 检查携带
            if (pawn.carryTracker?.CarriedThing != null)
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried.def.IsIngestible)
                    return carried;
            }

            // 检查背包
            if (pawn.inventory?.innerContainer != null)
            {
                foreach (Thing thing in pawn.inventory.innerContainer)
                {
                    if (thing.def.IsIngestible)
                        return thing;
                }
            }

            return null;
        }

        /// <summary>
        /// 注册到 RimTalk 的响应处理流程（需要通过 Harmony 补丁实现）
        /// </summary>
        public static void RegisterWithRimTalk()
        {
            // 这里需要通过反射或 Harmony 补丁来挂载到 RimTalk 的响应处理流程
            // 例如：patch RimTalk.Service.AIService.ProcessResponse() 方法
            
            Log.Message("[RimTalkIntentListener] RimTalk 意图监听器已注册");
        }
    }
}
