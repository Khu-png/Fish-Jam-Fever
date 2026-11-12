using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    // Fishing-rod booster: the player picks a tank (RodChoice), then the hook comes down, all the fish that tank needs fly to it from
    // their bubbles (lowest bubbles first, visible or not), then the hook lifts them over the tank and they drop in.
    public sealed partial class GameplayController
    {
        // Where each caught fish hangs, relative to the hook tip.
        private static readonly Vector3[] HookSlots =
        {
            new Vector3(-0.22f, -0.05f, 0f), new Vector3(0.22f, -0.05f, 0f), new Vector3(0f, -0.28f, 0f)
        };

        private const float HOOK_MIN_DEPTH = 0.8f;
        private const float HOOK_MAX_DEPTH = 3.2f;
        private const float HOOK_SIDE_MARGIN = 0.6f;
        private const int RELEASE_ICON = 1;

        private readonly List<Fish> rodCatch = new List<Fish>();
        private FishingRod rod;
        private bool isBoosterBusy;

        private IEnumerator RodRoutine(TankGoal goal)
        {
            isBoosterBusy = true;
            hasPickedOnce = true;
            ChooseRodFish(goal);
            float topY = gameCamera.transform.position.y + gameCamera.orthographicSize + config.rodAnchorMargin;
            Vector3 gatherPoint = GatherPoint();
            SoundManager.Ins.PlayFx(FxID.SummonItem);
            yield return rod.Enter(topY, gatherPoint);

            // The fish leave their bubbles one after another and fly to the hook.
            int[] icons = new int[rodCatch.Count];
            for (int i = 0; i < rodCatch.Count; i++)
            {
                icons[i] = shelf.Reserve(goal);
                SendToHook(rodCatch[i], i);
                yield return new WaitForSeconds(config.rodGatherStagger);
            }

            yield return new WaitForSeconds(config.rodGatherDuration);
            yield return rod.Catch();

            // Lift the cluster over the tank and let it dangle for a moment.
            Vector3 hangPoint = shelf.GetWaterPosition(goal, RELEASE_ICON) + Vector3.up * config.rodReleaseHeight;
            yield return rod.MoveTo(hangPoint, config.rodPullDuration);
            yield return new WaitForSeconds(config.rodHoverDuration);

            // All the fish dive into the tank together and the hook is reeled back up.
            for (int i = 0; i < rodCatch.Count; i++)
            {
                rodCatch[i].TF.SetParent(flightRoot, true);
                FlyToTank(rodCatch[i], goal, icons[i]);
            }

            rodCatch.Clear();
            StartCoroutine(rod.Retract());
            isBoosterBusy = false;
        }

        // Up to as many fish as the tank still needs, lowest bubbles first.
        private void ChooseRodFish(TankGoal goal)
        {
            rodCatch.Clear();
            int needed = config.tankCapacity - goal.Reserved;
            Fish fish = field.FindLowestFish(goal.TypeId, rodCatch);
            while (fish != null && rodCatch.Count < needed)
            {
                rodCatch.Add(fish);
                fish = field.FindLowestFish(goal.TypeId, rodCatch);
            }
        }

        // The middle of the chosen fish, kept inside the visible part of the field.
        private Vector3 GatherPoint()
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < rodCatch.Count; i++)
            {
                sum += rodCatch[i].TF.position;
            }

            Vector3 centre = sum / rodCatch.Count;
            float top = field.FieldTopY - HOOK_MIN_DEPTH;
            float side = config.fieldHalfWidth - HOOK_SIDE_MARGIN;
            return new Vector3(Mathf.Clamp(centre.x, -side, side), Mathf.Clamp(centre.y, field.FieldTopY - HOOK_MAX_DEPTH, top), 0f);
        }

        // The fish leaves its bubble (which shrinks or pops as usual) and flies to the hook.
        private void SendToHook(Fish fish, int slot)
        {
            ParticlePool.Play(ParticleType.FishPick, fish.TF.position, Quaternion.identity);
            SoundManager.Ins.PlayFx(FxID.PickCorrect);
            field.RemoveFish(fish);
            fish.Detach(flightRoot);
            fish.FlyToHook(rod.Tip, HookSlots[slot % HookSlots.Length], config.rodGatherDuration);
        }

        private void StopRod()
        {
            CancelRodSelection();
            isBoosterBusy = false;
            rodCatch.Clear();
            if (rod != null)
            {
                rod.Hide();
            }
        }
    }
}
