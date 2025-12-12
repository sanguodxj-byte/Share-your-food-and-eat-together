using System.Collections.Generic;
using Verse;

namespace RimTalkSocialDining
{
    /// <summary>
    /// Component attached to food Thing to track multi-pawn consumption.
    /// Implements survivor logic: food is only destroyed when the last eater finishes.
    /// </summary>
    public class SharedFoodTracker : ThingComp
    {
        private HashSet<Pawn> activePawns = new HashSet<Pawn>();
        private int initialStackCount = -1;
        private bool isBeingShared = false;

        public bool IsBeingShared => isBeingShared;
        public int ActiveEatersCount => activePawns.Count;

        /// <summary>
        /// Register a pawn as starting to eat this food.
        /// Thread-safe registration for multi-pawn access.
        /// </summary>
        public void RegisterEater(Pawn pawn)
        {
            if (pawn == null) return;

            lock (activePawns)
            {
                if (activePawns.Count == 0)
                {
                    // First eater - capture initial stack for restoration
                    initialStackCount = parent.stackCount;
                    isBeingShared = true;
                }

                activePawns.Add(pawn);
            }
        }

        /// <summary>
        /// Unregister a pawn as finishing eating.
        /// Returns true if this was the last eater (food should be destroyed).
        /// </summary>
        public bool UnregisterEater(Pawn pawn)
        {
            if (pawn == null) return false;

            bool isLastEater = false;
            lock (activePawns)
            {
                activePawns.Remove(pawn);
                
                if (activePawns.Count == 0)
                {
                    // Last eater finished
                    isBeingShared = false;
                    isLastEater = true;
                }
            }

            return isLastEater;
        }

        /// <summary>
        /// Check if a specific pawn is currently registered as eating this food.
        /// </summary>
        public bool IsEaterRegistered(Pawn pawn)
        {
            lock (activePawns)
            {
                return activePawns.Contains(pawn);
            }
        }

        /// <summary>
        /// Prevent stack count changes while being shared.
        /// Each eater will consume the stack only when they are the last one.
        /// </summary>
        public bool ShouldPreventConsumption()
        {
            lock (activePawns)
            {
                // If more than one eater is active, prevent consumption
                return activePawns.Count > 1;
            }
        }

        /// <summary>
        /// Save/load support for the component state.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            
            // Convert HashSet to List for serialization
            List<Pawn> pawnList = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                lock (activePawns)
                {
                    pawnList = new List<Pawn>(activePawns);
                }
            }
            
            Scribe_Collections.Look(ref pawnList, "activePawns", LookMode.Reference);
            Scribe_Values.Look(ref initialStackCount, "initialStackCount", -1);
            Scribe_Values.Look(ref isBeingShared, "isBeingShared", false);
            
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                lock (activePawns)
                {
                    activePawns.Clear();
                    if (pawnList != null)
                    {
                        foreach (var pawn in pawnList)
                        {
                            if (pawn != null)
                                activePawns.Add(pawn);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Cleanup on component removal.
        /// </summary>
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            lock (activePawns)
            {
                activePawns.Clear();
            }
        }
    }
}
