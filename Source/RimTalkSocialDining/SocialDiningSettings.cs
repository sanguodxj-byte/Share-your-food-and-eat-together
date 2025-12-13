using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkSocialDining
{
    /// <summary>
    /// Mod Settings - Store player preferences
    /// </summary>
    public class SocialDiningSettings : ModSettings
    {
        // Setting options
        public static bool useVanillaInteraction = true;
        public static bool useRimTalkAI = false;
        public static bool enableAutoSocialDining = true;
        public static float hungerThreshold = 0.5f;
        public static int cooldownHours = 2;
        public static bool enableDebugLogging = false;

        /// <summary>
        /// Save and load settings
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            
            Scribe_Values.Look(ref useVanillaInteraction, "useVanillaInteraction", true);
            Scribe_Values.Look(ref useRimTalkAI, "useRimTalkAI", false);
            Scribe_Values.Look(ref enableAutoSocialDining, "enableAutoSocialDining", true);
            Scribe_Values.Look(ref hungerThreshold, "hungerThreshold", 0.5f);
            Scribe_Values.Look(ref cooldownHours, "cooldownHours", 2);
            Scribe_Values.Look(ref enableDebugLogging, "enableDebugLogging", false);
        }

        /// <summary>
        /// Get cooldown time in game ticks
        /// </summary>
        public static int CooldownTicks
        {
            get { return cooldownHours * 2500; }
        }
    }

    /// <summary>
    /// Mod Main Class - Handle settings UI
    /// </summary>
    public class SocialDiningMod : Mod
    {
        private SocialDiningSettings settings;

        public SocialDiningMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<SocialDiningSettings>();
        }

        /// <summary>
        /// Settings category name
        /// </summary>
        public override string SettingsCategory()
        {
            return "Share your food and eat together";
        }

        /// <summary>
        /// Draw settings window
        /// </summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Title
            Text.Font = GameFont.Medium;
            listingStandard.Label("SocialDining_SettingsTitle".Translate());
            Text.Font = GameFont.Small;
            listingStandard.Gap();

            // Mode Selection
            listingStandard.Label("SocialDining_ModeSelection".Translate());
            listingStandard.Gap(6f);

            // Vanilla Interaction Mode
            bool vanillaMode = SocialDiningSettings.useVanillaInteraction;
            listingStandard.CheckboxLabeled(
                "SocialDining_UseVanillaMode".Translate(),
                ref vanillaMode,
                "SocialDining_UseVanillaModeTooltip".Translate()
            );
            SocialDiningSettings.useVanillaInteraction = vanillaMode;

            // RimTalk AI Mode
            bool rimtalkMode = SocialDiningSettings.useRimTalkAI;
            listingStandard.CheckboxLabeled(
                "SocialDining_UseRimTalkMode".Translate(),
                ref rimtalkMode,
                "SocialDining_UseRimTalkModeTooltip".Translate()
            );
            SocialDiningSettings.useRimTalkAI = rimtalkMode;

            listingStandard.Gap();
            
            // Mode status hints
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

            // AI Auto-Trigger Settings
            listingStandard.Label("SocialDining_AISettings".Translate());
            listingStandard.Gap(6f);

            // Enable AI Auto-Trigger
            bool autoTrigger = SocialDiningSettings.enableAutoSocialDining;
            listingStandard.CheckboxLabeled(
                "SocialDining_EnableAutoTrigger".Translate(),
                ref autoTrigger,
                "SocialDining_EnableAutoTriggerTooltip".Translate()
            );
            SocialDiningSettings.enableAutoSocialDining = autoTrigger;

            listingStandard.Gap();

            // Hunger Threshold Slider
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
            
            // Hint text
            Text.Font = GameFont.Tiny;
            listingStandard.Label("SocialDining_HungerThresholdDesc".Translate());
            Text.Font = GameFont.Small;

            listingStandard.Gap();

            // Cooldown Hours Slider
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
            
            // Hint text
            Text.Font = GameFont.Tiny;
            listingStandard.Label("SocialDining_CooldownDesc".Translate());
            Text.Font = GameFont.Small;

            listingStandard.Gap();
            listingStandard.GapLine();

            // Debug Settings
            listingStandard.Label("SocialDining_DebugSettings".Translate());
            listingStandard.Gap(6f);

            // Enable Debug Logging
            bool debugLogging = SocialDiningSettings.enableDebugLogging;
            listingStandard.CheckboxLabeled(
                "SocialDining_EnableDebugLogging".Translate(),
                ref debugLogging,
                "SocialDining_EnableDebugLoggingTooltip".Translate()
            );
            SocialDiningSettings.enableDebugLogging = debugLogging;

            listingStandard.Gap();
            listingStandard.GapLine();

            // RimTalk Knowledge Base Integration
            listingStandard.Label("SocialDining_KnowledgeBaseIntegration".Translate());
            listingStandard.Gap(6f);

            Text.Font = GameFont.Tiny;
            listingStandard.Label("SocialDining_KnowledgeBaseDesc".Translate());
            Text.Font = GameFont.Small;
            listingStandard.Gap();

            // Generate and add knowledge base button
            if (listingStandard.ButtonText("SocialDining_GenerateKnowledgeBase".Translate()))
            {
                if (KnowledgeBaseGenerator.TryAddToRimTalkKnowledgeBase(out string resultMessage))
                {
                    Messages.Message(resultMessage, MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message(resultMessage, MessageTypeDefOf.RejectInput);
                }
            }

            listingStandard.Gap();
            listingStandard.GapLine();

            // Reset Button
            if (listingStandard.ButtonText("SocialDining_ResetToDefaults".Translate()))
            {
                SocialDiningSettings.useVanillaInteraction = true;
                SocialDiningSettings.useRimTalkAI = false;
                SocialDiningSettings.enableAutoSocialDining = true;
                SocialDiningSettings.hungerThreshold = 0.5f;
                SocialDiningSettings.cooldownHours = 2;
                SocialDiningSettings.enableDebugLogging = false;
            }

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
