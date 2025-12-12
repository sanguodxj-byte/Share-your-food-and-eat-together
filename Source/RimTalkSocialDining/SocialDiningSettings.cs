using UnityEngine;
using Verse;

namespace RimTalkSocialDining
{
    /// <summary>
    /// Mod 设置类 - 存储玩家偏好设置
    /// </summary>
    public class SocialDiningSettings : ModSettings
    {
        // 设置选项
        public static bool useVanillaInteraction = true;  // 是否使用原版互动方式
        public static bool useRimTalkAI = false;          // 是否使用 RimTalk AI 方式
        public static bool enableAutoSocialDining = true; // 是否启用 AI 自动触发
        public static float hungerThreshold = 0.5f;       // 饥饿阈值 (0.5 = 50%)
        public static int cooldownHours = 2;              // 冷却时间（游戏小时）

        /// <summary>
        /// 保存和加载设置
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            
            Scribe_Values.Look(ref useVanillaInteraction, "useVanillaInteraction", true);
            Scribe_Values.Look(ref useRimTalkAI, "useRimTalkAI", false);
            Scribe_Values.Look(ref enableAutoSocialDining, "enableAutoSocialDining", true);
            Scribe_Values.Look(ref hungerThreshold, "hungerThreshold", 0.5f);
            Scribe_Values.Look(ref cooldownHours, "cooldownHours", 2);
        }

        /// <summary>
        /// 获取冷却时间（游戏刻数）
        /// </summary>
        public static int CooldownTicks
        {
            get { return cooldownHours * 2500; } // 1 小时 = 2500 刻
        }
    }

    /// <summary>
    /// Mod 主类 - 处理设置界面
    /// </summary>
    public class SocialDiningMod : Mod
    {
        private SocialDiningSettings settings;

        public SocialDiningMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<SocialDiningSettings>();
        }

        /// <summary>
        /// 设置类别名称
        /// </summary>
        public override string SettingsCategory()
        {
            return "Share your food and eat together";
        }

        /// <summary>
        /// 绘制设置界面
        /// </summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // ========== 标题 ==========
            Text.Font = GameFont.Medium;
            listingStandard.Label("SocialDining_SettingsTitle".Translate());
            Text.Font = GameFont.Small;
            listingStandard.Gap();

            // ========== 模式选择 ==========
            listingStandard.Label("SocialDining_ModeSelection".Translate());
            listingStandard.Gap(6f);

            // 原版互动模式
            bool vanillaMode = SocialDiningSettings.useVanillaInteraction;
            listingStandard.CheckboxLabeled(
                "SocialDining_UseVanillaMode".Translate(),
                ref vanillaMode,
                "SocialDining_UseVanillaModeTooltip".Translate()
            );
            SocialDiningSettings.useVanillaInteraction = vanillaMode;

            // RimTalk AI 模式
            bool rimtalkMode = SocialDiningSettings.useRimTalkAI;
            listingStandard.CheckboxLabeled(
                "SocialDining_UseRimTalkMode".Translate(),
                ref rimtalkMode,
                "SocialDining_UseRimTalkModeTooltip".Translate()
            );
            SocialDiningSettings.useRimTalkAI = rimtalkMode;

            listingStandard.Gap();
            
            // 提示：两种模式可以同时启用
            if (vanillaMode && rimtalkMode)
            {
                listingStandard.Label("SocialDining_BothModesEnabled".Translate());
            }
            else if (!vanillaMode && !rimtalkMode)
            {
                listingStandard.Label("SocialDining_NoModeEnabled".Translate());
            }

            listingStandard.Gap();
            listingStandard.GapLine();

            // ========== AI 自动触发设置 ==========
            listingStandard.Label("SocialDining_AISettings".Translate());
            listingStandard.Gap(6f);

            // 启用 AI 自动触发
            bool autoTrigger = SocialDiningSettings.enableAutoSocialDining;
            listingStandard.CheckboxLabeled(
                "SocialDining_EnableAutoTrigger".Translate(),
                ref autoTrigger,
                "SocialDining_EnableAutoTriggerTooltip".Translate()
            );
            SocialDiningSettings.enableAutoSocialDining = autoTrigger;

            listingStandard.Gap();

            // 饥饿阈值滑块
            listingStandard.Label(
                "SocialDining_HungerThreshold".Translate() + 
                ": " + 
                (SocialDiningSettings.hungerThreshold * 100f).ToString("F0") + "%"
            );
            SocialDiningSettings.hungerThreshold = listingStandard.Slider(
                SocialDiningSettings.hungerThreshold, 
                0.3f, 
                0.8f
            );
            
            // 提示文本
            Text.Font = GameFont.Tiny;
            listingStandard.Label("SocialDining_HungerThresholdDesc".Translate());
            Text.Font = GameFont.Small;

            listingStandard.Gap();

            // 冷却时间滑块
            listingStandard.Label(
                "SocialDining_CooldownHours".Translate() + 
                ": " + 
                SocialDiningSettings.cooldownHours.ToString() + " " +
                "SocialDining_Hours".Translate()
            );
            SocialDiningSettings.cooldownHours = (int)listingStandard.Slider(
                SocialDiningSettings.cooldownHours, 
                1f, 
                8f
            );
            
            // 提示文本
            Text.Font = GameFont.Tiny;
            listingStandard.Label("SocialDining_CooldownDesc".Translate());
            Text.Font = GameFont.Small;

            listingStandard.Gap();
            listingStandard.GapLine();

            // ========== 功能说明 ==========
            listingStandard.Label("SocialDining_FeatureDescription".Translate());
            listingStandard.Gap(6f);

            Text.Font = GameFont.Tiny;
            
            // 原版模式说明
            listingStandard.Label("? " + "SocialDining_VanillaModeDesc".Translate());
            listingStandard.Gap(2f);
            
            // RimTalk 模式说明
            listingStandard.Label("? " + "SocialDining_RimTalkModeDesc".Translate());
            listingStandard.Gap(2f);
            
            // AI 自动触发说明
            listingStandard.Label("? " + "SocialDining_AutoTriggerDesc".Translate());
            
            Text.Font = GameFont.Small;

            listingStandard.Gap();
            listingStandard.GapLine();

            // ========== 重置按钮 ==========
            if (listingStandard.ButtonText("SocialDining_ResetToDefaults".Translate()))
            {
                SocialDiningSettings.useVanillaInteraction = true;
                SocialDiningSettings.useRimTalkAI = false;
                SocialDiningSettings.enableAutoSocialDining = true;
                SocialDiningSettings.hungerThreshold = 0.5f;
                SocialDiningSettings.cooldownHours = 2;
            }

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
