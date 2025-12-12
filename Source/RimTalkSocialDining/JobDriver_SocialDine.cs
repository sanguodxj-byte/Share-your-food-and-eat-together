using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace RimTalkSocialDining
{
    /// <summary>
    /// JobDriver for synchronized social dining.
    /// Supports multi-reservation (maxClaimants=2), save-safe state management,
    /// and survivor logic for food destruction.
    /// </summary>
    public class JobDriver_SocialDine : JobDriver
    {
        // Job target indices
        private const TargetIndex FoodInd = TargetIndex.A;
        private const TargetIndex TableInd = TargetIndex.B;
        private const TargetIndex PartnerInd = TargetIndex.C;

        // State persistence for save compatibility
        private Thing foodCache;
        private Pawn partnerCache;
        private Building tableCache;
        private int ticksToEat = -1;
        private float nutritionPerTick = 0f;
        private bool isRegisteredWithTracker = false;

        // Properties for easy access
        private Thing Food => foodCache ?? (foodCache = job.GetTarget(FoodInd).Thing);
        private Pawn Partner => partnerCache ?? (partnerCache = job.GetTarget(PartnerInd).Pawn);
        private Building Table => tableCache ?? (tableCache = job.GetTarget(TableInd).Thing as Building);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 检查食物是否已经被其他社交共餐任务预留
            ThingWithComps foodWithComps = Food as ThingWithComps;
            if (foodWithComps != null)
            {
                SharedFoodTracker tracker = foodWithComps.TryGetComp<SharedFoodTracker>();
                if (tracker != null && tracker.ActiveEatersCount >= 2)
                {
                    // 食物已经被 2 人使用，不能再添加
                    if (errorOnFailed)
                    {
                        Log.Warning($"[RimTalkSocialDining] 食物 {Food.Label} 已被 {tracker.ActiveEatersCount} 人使用，无法预留");
                    }
                    return false;
                }
            }

            // 尝试预留食物
            if (!pawn.Reserve(Food, job, 1, -1, null, false))
            {
                // 如果预留失败，检查是否是因为伙伴已经预留了
                if (Partner != null && pawn.Map != null)
                {
                    // 使用 Map 的预留管理器检查
                    Pawn reserver = pawn.Map.reservationManager.FirstRespectedReserver(Food, pawn);
                    if (reserver == Partner)
                    {
                        // 伙伴已经预留，这是预期的情况，允许继续
                        Log.Message($"[RimTalkSocialDining] {pawn.LabelShort} 的伙伴 {Partner.LabelShort} 已预留食物，允许共享");
                    }
                    else if (errorOnFailed)
                    {
                        Log.Warning($"[RimTalkSocialDining] {pawn.LabelShort} 无法预留食物 {Food.Label} (已被 {reserver?.LabelShort ?? "未知"} 预留)");
                        return false;
                    }
                }
                else if (errorOnFailed)
                {
                    Log.Warning($"[RimTalkSocialDining] {pawn.LabelShort} 无法预留食物 {Food.Label}");
                    return false;
                }
            }

            // Reserve table if available (not required - picnic mode support)
            if (Table != null)
            {
                if (!pawn.Reserve(Table, job, 1, -1, null, false))
                {
                    // 餐桌预留失败不是致命错误，可以使用野餐模式
                    Log.Message($"[RimTalkSocialDining] {pawn.LabelShort} 无法预留餐桌，使用野餐模式");
                }
            }

            return true;
        }

        /// <summary>
        /// Save/load support - CRITICAL for preventing job loss on load.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            
            Scribe_References.Look(ref foodCache, "foodCache");
            Scribe_References.Look(ref partnerCache, "partnerCache");
            Scribe_References.Look(ref tableCache, "tableCache");
            Scribe_Values.Look(ref ticksToEat, "ticksToEat", -1);
            Scribe_Values.Look(ref nutritionPerTick, "nutritionPerTick", 0f);
            Scribe_Values.Look(ref isRegisteredWithTracker, "isRegisteredWithTracker", false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Validation: fail if food or partner becomes invalid
            this.FailOnDespawnedNullOrForbidden(FoodInd);
            this.FailOnDestroyedOrNull(PartnerInd);
            
            // Food must be ingestible
            this.FailOn(() => Food?.def.IsIngestible != true);

            // Partner must be capable and not downed
            this.FailOn(() => Partner == null || Partner.Downed || Partner.Dead || Partner.InMentalState);

            // --- Toil 1: Go to food ---
            Toil gotoFood = Toils_Goto.GotoThing(FoodInd, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(FoodInd);
            yield return gotoFood;

            // --- Toil 2: Pickup food ---
            Toil pickupFood = new Toil
            {
                initAction = delegate
                {
                    Job curJob = pawn.jobs.curJob;
                    Thing food = Food;
                    
                    if (food.stackCount > 1)
                    {
                        // Split off one item for eating
                        Thing splitFood = food.SplitOff(1);
                        curJob.SetTarget(FoodInd, splitFood);
                        foodCache = splitFood;
                    }
                    
                    // Pick up the food
                    if (pawn.carryTracker.TryStartCarry(Food))
                    {
                        curJob.count = 1;
                    }
                    else
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return pickupFood;

            // --- Toil 3: Go to dining location (table or fallback position) ---
            Toil gotoDining = new Toil
            {
                initAction = delegate
                {
                    IntVec3 diningSpot;
                    
                    if (Table != null && !Table.Destroyed && Table.Spawned)
                    {
                        // Use table - find interaction cell
                        diningSpot = Table.InteractionCell;
                        if (!diningSpot.IsValid || !diningSpot.Standable(pawn.Map))
                        {
                            // Fallback to position adjacent to table
                            diningSpot = Table.Position;
                        }
                    }
                    else
                    {
                        // Picnic mode - find suitable spot near partner
                        if (Partner != null && Partner.Spawned)
                        {
                            diningSpot = RCellFinder.SpotToChewStandingNear(pawn, Partner);
                        }
                        else
                        {
                            diningSpot = pawn.Position;
                        }
                    }
                    
                    pawn.pather.StartPath(diningSpot, PathEndMode.OnCell);
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return gotoDining;

            // --- Toil 4: Place food on table or ground ---
            Toil placeFood = new Toil
            {
                initAction = delegate
                {
                    if (pawn.carryTracker.CarriedThing != null)
                    {
                        Thing carriedFood = pawn.carryTracker.CarriedThing;
                        
                        if (!pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out Thing droppedFood))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                        
                        job.SetTarget(FoodInd, droppedFood);
                        foodCache = droppedFood;
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return placeFood;

            // --- Toil 5: Register with SharedFoodTracker ---
            Toil registerEater = new Toil
            {
                initAction = delegate
                {
                    ThingWithComps foodWithComps = Food as ThingWithComps;
                    if (foodWithComps != null)
                    {
                        SharedFoodTracker tracker = foodWithComps.TryGetComp<SharedFoodTracker>();
                        if (tracker == null)
                        {
                            // Cannot add component dynamically in RimWorld, skip tracking
                            Log.Warning("[RimTalkSocialDining] Food item missing SharedFoodTracker component");
                        }
                        else
                        {
                            tracker.RegisterEater(pawn);
                            isRegisteredWithTracker = true;
                        }
                    }
                    
                    // Calculate eating duration
                    float nutrition = FoodUtility.GetNutrition(pawn, Food, Food.def);
                    ticksToEat = Mathf.CeilToInt(nutrition / pawn.needs.food.NutritionBetweenHungryAndFed * 1000f);
                    if (ticksToEat < 1) ticksToEat = 1;
                    nutritionPerTick = nutrition / ticksToEat;
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return registerEater;

            // --- Toil 6: Synchronized eating ---
            Toil eatFood = new Toil
            {
                initAction = delegate
                {
                    if (Partner != null && Partner.Spawned)
                    {
                        pawn.rotationTracker.FaceTarget(Partner);
                    }
                    pawn.jobs.curDriver.ticksLeftThisToil = ticksToEat;
                },
                tickAction = delegate
                {
                    // Face partner during eating
                    if (Partner != null && Partner.Spawned)
                    {
                        pawn.rotationTracker.FaceTarget(Partner);
                    }
                    
                    // Consume nutrition gradually
                    if (pawn.needs?.food != null)
                    {
                        pawn.needs.food.CurLevel += nutritionPerTick;
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = ticksToEat,
                handlingFacing = true
            };
            
            // Add eating effects
            if (Food?.def?.ingestible?.ingestEffect != null)
            {
                eatFood.WithEffect(() => Food.def.ingestible.ingestEffect, FoodInd);
            }
            eatFood.WithProgressBar(FoodInd, () => 1f - (float)pawn.jobs.curDriver.ticksLeftThisToil / ticksToEat, interpolateBetweenActorAndTarget: false);
            if (Food?.def?.ingestible?.ingestSound != null)
            {
                eatFood.PlaySustainerOrSound(() => Food.def.ingestible.ingestSound);
            }
            
            yield return eatFood;

            // --- Toil 7: Finish eating and cleanup ---
            Toil finishEating = new Toil
            {
                initAction = delegate
                {
                    // Apply ingestion effects
                    if (Food?.def.ingestible != null && !Food.Destroyed)
                    {
                        // Use proper Ingested signature
                        if (Food.IngestibleNow && pawn.needs?.food != null)
                        {
                            float nutritionGained = FoodUtility.GetNutrition(pawn, Food, Food.def);
                            pawn.needs.food.CurLevel += nutritionGained;
                        }
                    }
                    
                    // Unregister from tracker
                    CleanupTracker();
                    
                    // Social interaction - gain positive mood
                    if (Partner != null && !Partner.Dead && pawn.needs?.mood?.thoughts?.memories != null)
                    {
                        // Use AteInImpressiveDiningRoom as fallback since AteWithColonist may not exist
                        ThoughtDef socialThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("AteWithColonist");
                        if (socialThought != null)
                        {
                            pawn.needs.mood.thoughts.memories.TryGainMemory(socialThought, Partner);
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return finishEating;
        }

        /// <summary>
        /// Cleanup on job end - ensure tracker is updated.
        /// </summary>
        public override void Notify_PatherFailed()
        {
            base.Notify_PatherFailed();
            CleanupTracker();
        }

        private void CleanupTracker()
        {
            if (isRegisteredWithTracker && Food != null && !Food.Destroyed)
            {
                ThingWithComps foodWithComps = Food as ThingWithComps;
                if (foodWithComps != null)
                {
                    SharedFoodTracker tracker = foodWithComps.TryGetComp<SharedFoodTracker>();
                    if (tracker != null)
                    {
                        bool isLastEater = tracker.UnregisterEater(pawn);
                        isRegisteredWithTracker = false;
                        
                        // Survivor logic: only destroy if last eater
                        if (isLastEater && !Food.Destroyed)
                        {
                            Food.Destroy(DestroyMode.Vanish);
                        }
                    }
                }
            }
        }
    }
}
