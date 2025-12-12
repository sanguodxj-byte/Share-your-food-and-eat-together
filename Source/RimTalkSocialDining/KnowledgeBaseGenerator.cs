using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTalkSocialDining
{
    /// <summary>
    /// 常识库生成器 - 为 RimTalk 常识库生成社交共餐相关的提示词
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

            // 1. 核心行为指令
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "社交,共餐,行为指令",
                content = "当小人处于饥饿状态（饥饿度 < 50%）且附近有其他饥饿的殖民者时，可以主动邀请对方一起共享同一份食物进餐。这样可以增进关系并获得社交愉悦感。",
                importance = 0.9f
            });

            // 2. 触发条件说明
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "社交,共餐,触发条件",
                content = "触发社交共餐需要满足：(1) 双方都处于饥饿状态，(2) 发起者携带或能找到食物，(3) 双方关系不是敌对（好感度 > -20），(4) 接收者未在执行重要任务。",
                importance = 0.85f
            });

            // 3. 社交效果说明
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "社交,共餐,效果",
                content = "成功的社交共餐会带来正面效果：(1) 双方获得'共同进餐'心情加成（+3，持续0.5天），(2) 增进相互之间的关系，(3) 提升社交需求。",
                importance = 0.8f
            });

            // 4. 拒绝原因说明
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "社交,共餐,拒绝",
                content = "邀请可能被拒绝的原因包括：(1) 接收者不够饥饿（饱食度 > 80%），(2) 双方关系太差（好感度 < -20），(3) 接收者正在执行重要任务，(4) 接收者不喜欢该食物类型。",
                importance = 0.75f
            });

            // 5. 优先级建议
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "社交,共餐,优先级",
                content = "社交共餐优先级建议：优先邀请好感度高的殖民者（好感度 ≥ 20），优先在有餐桌的地方进餐（心情加成更高），避免在危险或紧急情况下触发。",
                importance = 0.7f
            });

            // 6. 接受概率因素
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "社交,共餐,接受概率",
                content = "影响接受邀请的因素：(1) 饥饿程度越高，越容易接受（饥饿 > 50% 时 +40%），(2) 好感度越高越容易接受（好感 ≥ 20 时 +30%），(3) 发起者社交技能越高越容易成功（技能 ≥ 8 时 +15%），(4) Kind 特性增加接受率，Abrasive 特性降低接受率。",
                importance = 0.85f
            });

            // 7. 餐桌使用说明
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "社交,共餐,餐桌",
                content = "系统会自动寻找合适的餐桌供双方共餐。如果找不到餐桌，会在野外选择相邻位置站立进餐（野餐模式）。在餐桌用餐比野餐获得更好的心情加成。",
                importance = 0.65f
            });

            // 8. 食物类型适配
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "社交,共餐,食物类型",
                content = "几乎所有可食用的食物都可以用于社交共餐，包括：简单餐食、精致餐食、营养膏、生食等。系统会自动检查接收者的食物偏好，避免提供对方厌恶的食物。",
                importance = 0.7f
            });

            // 9. 时机选择建议
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "社交,共餐,时机",
                content = "最佳社交共餐时机：(1) 用餐时间（早中晚餐时段），(2) 双方都在休息或空闲时，(3) 在公共区域（餐厅、休息室）遇到饥饿的同伴时，(4) 完成共同任务后的休息时间。",
                importance = 0.75f
            });

            // 10. 主动邀请策略
            entries.Add(new CommonKnowledgeEntryData
            {
                tag = "社交,共餐,邀请策略",
                content = "主动邀请建议：(1) 优先邀请关系亲密的殖民者（配偶、恋人、家人、好友），(2) 帮助新加入的殖民者快速融入团队，(3) 改善与关系较差的殖民者的关系，(4) 在殖民者心情低落时提供社交支持。",
                importance = 0.8f
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
                    // 检查是否已存在相同标签的条目
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
