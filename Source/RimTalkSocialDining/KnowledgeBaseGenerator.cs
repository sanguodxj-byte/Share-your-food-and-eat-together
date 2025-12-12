using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTalkSocialDining
{
    /// <summary>
    /// 常识库生成器 - 为 RimTalk 常识库生成社交共餐相关的提示词
    /// 优化版：只包含驱动小人行动的核心常识
    /// </summary>
    public static class KnowledgeBaseGenerator
    {
        /// <summary>
        /// 生成社交共餐的常识条目
        /// 格式符合 RimTalk-ExpandMemory 的 CommonKnowledgeEntry 结构
        /// </summary>
        public static List<CommonKnowledgeEntryData> GenerateSocialDiningKnowledge()
        {
            var entries = new List<CommonKnowledgeEntryData>();

            // 0. 命令执行指令 - 告诉 AI 如何调用系统
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "吃饭,用餐,命令",
                content = "要邀请某人共餐，使用意图命令：share_food(发起者, 接收者, 食物)。例如：当Alice想和Bob共餐时，执行 share_food(Alice, Bob, meal)。这会触发游戏内的共餐系统。",
                importance = 1.0f
            });

            // 1. 核心行为：什么时候应该邀请别人一起吃饭
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "饿了,吃饭,食物",
                content = "当你饥饿且手里有食物时，如果看到附近也有饥饿的同伴，应该主动邀请对方一起分享食物。共同进餐可以增进感情，让彼此心情更好。",
                importance = 0.95f
            });

            // 2. 优先邀请谁
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "吃饭,邀请,朋友",
                content = "优先邀请你喜欢的人一起吃饭，比如好友、恋人、配偶、家人。避免邀请和你关系很差的人，因为他们很可能会拒绝。",
                importance = 0.9f
            });

            // 3. 什么情况下接受邀请
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "饿了,用餐,接受",
                content = "当有人邀请你一起吃饭时，如果你也饿了，且和对方关系还不错，就应该欣然接受。一起吃饭比一个人吃更有意思，还能增进友谊。",
                importance = 0.9f
            });

            // 4. 什么情况下拒绝邀请
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "吃饭,拒绝,忙碌",
                content = "如果你不饿、正在忙重要的事、或者不喜欢对方提供的食物，可以礼貌拒绝共餐邀请。和关系很差的人一起吃饭会让你不舒服。",
                importance = 0.85f
            });

            // 5. 在哪里吃
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "用餐,餐桌,地点",
                content = "共餐时优先去餐厅的餐桌旁，在餐桌上吃饭心情会更好。如果找不到餐桌，也可以在野外站着一起吃，虽然没那么舒适，但总比一个人吃强。",
                importance = 0.75f
            });

            // 6. 什么时候最适合共餐
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "饿了,吃饭,休息",
                content = "用餐时间（早中晚饭点）是邀请共餐的最佳时机。当你和同伴都在休息或空闲时，在餐厅或公共区域遇到饥饿的人，可以主动邀请一起吃。",
                importance = 0.8f
            });

            // 7. 共餐的好处
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "用餐,感情,心情",
                content = "和别人一起吃饭会让你心情愉悦，感到温暖。这也是增进关系的好机会，尤其是和新加入的同伴或者关系需要改善的人。",
                importance = 0.85f
            });

            return entries;
        }

        /// <summary>
        /// 格式化为日志输出格式（用于调试和预览）
        /// </summary>
        public static string FormatForLog(List<CommonKnowledgeEntryData> entries)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== 社交共餐常识库条目 ===");
            sb.AppendLine($"共 {entries.Count} 条");
            sb.AppendLine();

            foreach (var entry in entries)
            {
                sb.AppendLine($"[{entry.tag}] (重要性: {entry.importance:F2})");
                sb.AppendLine($"  {entry.content}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// 将常识条目添加到 RimTalk 常识库
        /// 通过反射调用 RimTalk-ExpandMemory 的 CommonKnowledgeLibrary
        /// </summary>
        public static bool TryAddToRimTalkKnowledgeBase(out string resultMessage)
        {
            try
            {
                // 检查 RimTalk-ExpandMemory 是否加载
                var rimTalkAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name.Contains("RimTalk") && 
                                       (a.GetName().Name.Contains("ExpandMemory") || a.GetName().Name.Contains("Memory")));

                if (rimTalkAssembly == null)
                {
                    resultMessage = "未检测到 RimTalk-ExpandMemory 模组。请确保已安装并启用该模组。";
                    return false;
                }

                // 查找 CommonKnowledgeLibrary 类型
                var knowledgeLibraryType = rimTalkAssembly.GetType("RimTalk.Memory.CommonKnowledgeLibrary");
                if (knowledgeLibraryType == null)
                {
                    resultMessage = "无法找到 RimTalk 常识库类型。可能是版本不兼容。";
                    return false;
                }

                // 查找 Instance 单例属性
                var instanceProperty = knowledgeLibraryType.GetProperty("Instance", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProperty == null)
                {
                    resultMessage = "无法访问 RimTalk 常识库实例。";
                    return false;
                }

                var knowledgeLibraryInstance = instanceProperty.GetValue(null);
                if (knowledgeLibraryInstance == null)
                {
                    resultMessage = "RimTalk 常识库实例未初始化。";
                    return false;
                }

                // 查找 CommonKnowledgeEntry 类型
                var entryType = rimTalkAssembly.GetType("RimTalk.Memory.CommonKnowledgeEntry");
                if (entryType == null)
                {
                    resultMessage = "无法找到常识条目类型。";
                    return false;
                }

                // 查找 entries 字段
                var entriesField = knowledgeLibraryType.GetField("entries", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (entriesField == null)
                {
                    resultMessage = "无法访问常识库条目列表。";
                    return false;
                }

                var entriesList = entriesField.GetValue(knowledgeLibraryInstance) as System.Collections.IList;
                if (entriesList == null)
                {
                    resultMessage = "常识库条目列表为空或类型不匹配。";
                    return false;
                }

                // 生成常识条目
                var knowledgeEntries = GenerateSocialDiningKnowledge();
                int addedCount = 0;
                int skippedCount = 0;

                foreach (var entryData in knowledgeEntries)
                {
                    // 检查是否已存在相同标签和内容的条目
                    bool exists = false;
                    foreach (var existingEntry in entriesList)
                    {
                        var existingTag = entryType.GetField("tag")?.GetValue(existingEntry) as string;
                        var existingContent = entryType.GetField("content")?.GetValue(existingEntry) as string;
                        
                        if (existingTag == entryData.tag && existingContent == entryData.content)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (exists)
                    {
                        skippedCount++;
                        continue;
                    }

                    // 创建新的 CommonKnowledgeEntry 实例
                    var newEntry = Activator.CreateInstance(entryType);
                    
                    // 设置字段值
                    entryType.GetField("id")?.SetValue(newEntry, "ck-sd-" + Guid.NewGuid().ToString("N").Substring(0, 12));
                    entryType.GetField("tag")?.SetValue(newEntry, entryData.tag);
                    entryType.GetField("content")?.SetValue(newEntry, entryData.content);
                    entryType.GetField("importance")?.SetValue(newEntry, entryData.importance);
                    entryType.GetField("isEnabled")?.SetValue(newEntry, true);
                    entryType.GetField("isUserEdited")?.SetValue(newEntry, false);
                    entryType.GetField("targetPawnId")?.SetValue(newEntry, -1); // 全局
                    entryType.GetField("creationTick")?.SetValue(newEntry, -1); // 永久
                    entryType.GetField("originalEventText")?.SetValue(newEntry, "");

                    // 添加到列表
                    entriesList.Add(newEntry);
                    addedCount++;
                }

                resultMessage = $"成功添加 {addedCount} 条常识到 RimTalk 常识库！";
                if (skippedCount > 0)
                {
                    resultMessage += $"\n跳过 {skippedCount} 条已存在的常识。";
                }

                // 记录日志
                Log.Message($"[Share your food and eat together] {resultMessage}");
                Log.Message(FormatForLog(knowledgeEntries));

                return true;
            }
            catch (Exception ex)
            {
                resultMessage = $"添加常识时发生错误：{ex.Message}";
                Log.Error($"[Share your food and eat together] {resultMessage}\n{ex.StackTrace}");
                return false;
            }
        }
    }

    /// <summary>
    /// 常识条目数据结构（用于生成）
    /// </summary>
    public class CommonKnowledgeEntryData
    {
        public string tag;
        public string content;
        public float importance;
    }
}
