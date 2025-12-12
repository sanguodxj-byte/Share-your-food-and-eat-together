using RimWorld;
using Verse;
using Verse.AI;

namespace RimTalkSocialDining
{
    /// <summary>
    /// ThinkNode condition that checks if a pawn can engage in social dining.
    /// Validates hunger level, social capability, and available partners.
    /// </summary>
    public class ThinkNode_ConditionalCanSocialDine : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return IsSatisfied(pawn);
        }

        /// <summary>
        /// Public wrapper for Satisfied check (used by Harmony patches)
        /// </summary>
        public bool IsSatisfied(Pawn pawn)
        {
            // 检查 AI 自动触发是否启用
            if (!SocialDiningSettings.enableAutoSocialDining)
                return false;

            // Must have hunger need
            if (pawn.needs?.food == null)
                return false;

            // Must be hungry enough (使用设置中的阈值)
            float hungerThreshold = 1f - SocialDiningSettings.hungerThreshold;
            if (pawn.needs.food.CurLevelPercentage > hungerThreshold)
                return false;

            // Must be capable of eating
            if (!pawn.RaceProps.EatsFood)
                return false;

            // Must not be in mental state
            if (pawn.InMentalState)
                return false;

            // Must be capable of social interaction
            if (pawn.WorkTagIsDisabled(WorkTags.Social))
                return false;

            // Must be colonist or friendly
            if (pawn.Faction == null || (!pawn.Faction.IsPlayer && !pawn.IsPrisonerOfColony))
                return false;

            // Check if there's at least one potential partner available
            if (!HasAvailablePartner(pawn))
                return false;

            return true;
        }

        /// <summary>
        /// Check if there's at least one valid partner for social dining.
        /// </summary>
        private bool HasAvailablePartner(Pawn pawn)
        {
            if (pawn.Map == null)
                return false;

            // 使用设置中的饥饿阈值
            float hungerThreshold = 1f - SocialDiningSettings.hungerThreshold;

            foreach (Pawn otherPawn in pawn.Map.mapPawns.FreeColonistsSpawned)
            {
                if (otherPawn == pawn)
                    continue;

                // Partner must be hungry (使用设置中的阈值)
                if (otherPawn.needs?.food == null || otherPawn.needs.food.CurLevelPercentage > hungerThreshold)
                    continue;

                // Partner must be capable
                if (otherPawn.InMentalState || otherPawn.Downed || otherPawn.Dead)
                    continue;

                // Partner must be able to do social interaction
                if (otherPawn.WorkTagIsDisabled(WorkTags.Social))
                    continue;

                // Partner must not be in an incompatible job
                if (otherPawn.CurJob != null && 
                    (otherPawn.CurJob.def == JobDefOf.Ingest || 
                     otherPawn.CurJob.def.playerInterruptible == false))
                    continue;

                // Must be reachable
                if (!pawn.CanReach(otherPawn, PathEndMode.Touch, Danger.Deadly))
                    continue;

                // Found at least one valid partner
                return true;
            }

            return false;
        }
    }
}
