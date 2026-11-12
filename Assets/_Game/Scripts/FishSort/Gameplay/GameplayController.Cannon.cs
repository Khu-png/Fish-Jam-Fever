using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    // Cannon booster: the fish-count board makes room for the cannon, which fires one bullet along the tray. Every
    // fish it passes follows the bullet and goes into the FishJar at the right end of the tray (the jar's counter
    // ticks up fish by fish). Then the cannon leaves and the fish-count board comes back. The jar has priority over
    // the tray when a tank needs fish.
    public sealed partial class GameplayController
    {
        private const float MUZZLE_FLASH_SCALE = 2f;
        private const float JAR_POP_SCALE = 0.8f;
        // Followers trail the bullet in a tight line and chase the jar once the bullet has gone into it.
        private const float FOLLOW_GAP = 0.2f;
        private const float FOLLOW_SHARPNESS = 20f;
        private const float JAR_CHASE_SPEED = 14f;
        private const float JAR_ARRIVE_DISTANCE = 0.06f;

        private readonly List<Fish> cannonFish = new List<Fish>();
        private readonly List<Fish> jarCatch = new List<Fish>();
        private CannonBooster cannon;
        private FishJar fishJar;
        private Vector3 shakeRest;
        private bool isShaking;

        // Returns false when the tray is empty or the jar from an earlier shot is still there.
        public bool TryUseCannon()
        {
            bool canUse = !isOver && !isResolvingTray && !isBoosterBusy && tray.Count > 0 && !fishJar.IsOpen
                && GameManager.IsState(GameState.GamePlay);
            if (canUse)
            {
                StartCoroutine(CannonRoutine());
            }

            return canUse;
        }

        // The fish leaves wherever it was: a bubble of the field, or the jar.
        private void TakeFishOut(Fish fish)
        {
            if (fishJar.Owns(fish))
            {
                fishJar.Release(fish);
            }
            else
            {
                field.RemoveFish(fish);
            }
        }

        private IEnumerator CannonRoutine()
        {
            isBoosterBusy = true;
            hasPickedOnce = true;
            cannonFish.Clear();
            tray.TakeAll(cannonFish);
            SetTrayWarning(false);
            UIManager.Ins.GetUI<FishJam.UI.GamePlay>().SetFishCounterVisible(false);
            Vector3 first = tray.GetSlotPosition(0);
            yield return cannon.Enter(first + Vector3.left * config.cannonRestOffset + Vector3.down * config.cannonRestDrop);

            // One bullet along the tray into the jar's spot; the fish it passes follow it.
            SoundManager.Ins.PlayFx(FxID.SummonItem);
            ParticlePool.Play(ParticleType.FishPick, cannon.MuzzlePosition, Quaternion.identity, MUZZLE_FLASH_SCALE);
            StartCoroutine(ShakeCamera());
            Vector3 jarPosition = fishJar.HomePosition;
            int[] rank = new int[cannonFish.Count];
            for (int i = 0; i < rank.Length; i++)
            {
                rank[i] = -1;
            }

            int followers = 0;
            yield return cannon.Fire(jarPosition, orbPoint => followers = Follow(orbPoint, rank, followers));

            // The bullet is in: the jar appears, the cannon leaves, and the fish dive into the jar one by one.
            fishJar.Open(cannonFish.Count);
            ParticlePool.Play(ParticleType.TankBubblePop, jarPosition, Quaternion.identity, JAR_POP_SCALE);
            StartCoroutine(CannonLeaves());
            yield return FillJar(jarPosition);
            // Any open tank that wants a fish from the jar gets it straight away.
            for (int slot = 0; slot < shelf.SlotCount; slot++)
            {
                TankGoal goal = shelf.GoalAt(slot);
                if (goal != null && goal.IsShown && goal.Reserved < config.tankCapacity)
                {
                    PullJarFish(goal);
                }
            }

            isBoosterBusy = false;
        }

        // The jar has priority over the tray: when a tank needs a fish type that is waiting in the jar,
        // those fish dive into that tank at once (the jar's counter goes down, and it pops when empty).
        private void PullJarFish(TankGoal goal)
        {
            jarCatch.Clear();
            fishJar.CollectFish(goal.TypeId, config.tankCapacity - goal.Reserved, jarCatch);
            for (int i = 0; i < jarCatch.Count; i++)
            {
                fishJar.Release(jarCatch[i]);
                jarCatch[i].Detach(flightRoot);
                SendToTank(jarCatch[i], goal);
            }

            jarCatch.Clear();
        }

        // Every frame while the bullet flies: fish it has passed start following it, in a tight line behind it.
        private int Follow(Vector3 orbPoint, int[] rank, int followers)
        {
            float blend = 1f - Mathf.Exp(-FOLLOW_SHARPNESS * Time.deltaTime);
            for (int i = 0; i < cannonFish.Count; i++)
            {
                if (rank[i] < 0 && cannonFish[i].TF.position.x <= orbPoint.x)
                {
                    rank[i] = followers;
                    followers++;
                    cannonFish[i].BeginFollow();
                }

                if (rank[i] >= 0)
                {
                    Vector3 behind = orbPoint + Vector3.left * (FOLLOW_GAP * (rank[i] + 1));
                    cannonFish[i].TF.position = Vector3.Lerp(cannonFish[i].TF.position, behind, blend);
                }
            }

            return followers;
        }

        // The followers rush into the jar; each one that arrives joins it and the counter goes up.
        private IEnumerator FillJar(Vector3 jarPosition)
        {
            int arrived = 0;
            bool[] isInside = new bool[cannonFish.Count];
            while (arrived < cannonFish.Count)
            {
                for (int i = 0; i < cannonFish.Count; i++)
                {
                    if (!isInside[i])
                    {
                        Fish fish = cannonFish[i];
                        fish.BeginFollow();
                        fish.TF.position = Vector3.MoveTowards(fish.TF.position, jarPosition, JAR_CHASE_SPEED * Time.deltaTime);
                        if (Vector3.Distance(fish.TF.position, jarPosition) < JAR_ARRIVE_DISTANCE)
                        {
                            isInside[i] = true;
                            arrived++;
                            fishJar.Add(fish);
                            SoundManager.Ins.PlayFx(FxID.PickCorrect, config.PickPitch(arrived));
                        }
                    }
                }

                yield return null;
            }

            cannonFish.Clear();
        }

        // The cannon pops away and the fish-count board is shown again.
        private IEnumerator CannonLeaves()
        {
            yield return cannon.Retreat();
            UIManager.Ins.GetUI<FishJam.UI.GamePlay>().SetFishCounterVisible(true);
        }

        private IEnumerator ShakeCamera()
        {
            Transform cameraTransform = gameCamera.transform;
            shakeRest = cameraTransform.position;
            isShaking = true;
            float elapsed = 0f;
            while (elapsed < config.cannonShakeDuration)
            {
                elapsed += Time.deltaTime;
                float strength = config.cannonShakeAmount * (1f - elapsed / config.cannonShakeDuration);
                cameraTransform.position = shakeRest + (Vector3)(Random.insideUnitCircle * strength);
                yield return null;
            }

            cameraTransform.position = shakeRest;
            isShaking = false;
        }

        private void StopCannon()
        {
            if (isShaking)
            {
                gameCamera.transform.position = shakeRest;
                isShaking = false;
            }

            cannonFish.Clear();
            if (cannon != null)
            {
                cannon.Hide();
            }

            if (fishJar != null)
            {
                fishJar.Hide();
            }

            if (UIManager.Ins.IsOpened<FishJam.UI.GamePlay>())
            {
                UIManager.Ins.GetUI<FishJam.UI.GamePlay>().SetFishCounterVisible(true);
            }
        }
    }
}
