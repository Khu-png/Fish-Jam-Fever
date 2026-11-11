using UnityEngine;

namespace FishSort
{
    [System.Serializable]
    public struct ComboStep
    {
        public int streak;
        public string text;
        public Color color;
        public float scale;
    }

    // Every gameplay tunable lives here so designers can tweak without touching code.
    [CreateAssetMenu(fileName = "FishSortConfig", menuName = "FishSort/Config")]
    public sealed class FishSortConfig : ScriptableObject
    {
        [Header("Rules")]
        public int trayCapacity = 5;
        public int tankCapacity = 3;

        [Header("Bubble (world units, sprite diameter = 1)")]
        public float[] bubbleDiameterByFishCount = { 0f, 1f, 1.24f, 1.44f, 1.6f, 1.76f };
        public float[] fishWidthByFishCount = { 0f, 0.46f, 0.46f, 0.42f, 0.38f, 0.35f };
        public int smallLevelFishCount = 12;
        public float smallLevelBubbleScale = 1.12f;
        public int mediumLevelFishCount = 60;
        public float mediumLevelBubbleScale = 1f;
        public float bubbleMinScale = 0.85f;
        public float bubbleShrinkSpeed = 10f;
        public float bubblePopScale = 1.3f;
        public float bubblePopDuration = 0.34f;
        public float fieldHalfWidth = 2.7f;
        public float spawnJitter = 0.12f;
        public float fishRotationRange = 9f;
        [Header("Opening bubble fall (gentle glide down, soft landing)")]
        public float bubbleEntryDropHeight = 4f;
        public float bubbleEntryGravityScale = 0.5f;
        [Tooltip("Higher = slower, steadier fall. Max fall speed ~ 9.81 * gravityScale / damping.")]
        public float bubbleEntryDamping = 2f;
        [Tooltip("Earliest time the bubble may switch to normal physics.")]
        public float bubbleEntryDuration = 1.5f;
        [Tooltip("Normal physics starts once the bubble moves slower than this (it has landed).")]
        public float bubbleEntrySettleSpeed = 0.2f;
        [Tooltip("Safety limit so a bubble never stays in the gentle fall forever.")]
        public float bubbleEntryMaxDuration = 6f;
        [Header("Bubble physics after the opening fall (normal physics)")]
        public float bubbleGravityScale = 1f;
        public float bubbleWaterDamping = 0.5f;
        [Header("Settling (bubbles slide into gaps after landing)")]
        [Tooltip("Maximum sideways acceleration when steering into a reachable lower gap.")]
        public float bubbleSettlePull = 1.8f;
        [Tooltip("Maximum assisted sideways speed; gravity still controls falling.")]
        public float bubbleSettleMaxSpeed = 0.65f;
        public float bubbleSettleSearchDistance = 1.4f;
        public float bubbleSettleMinDrop = 0.08f;
        public float bubbleSettleRefreshInterval = 0.16f;
        public Color frozenFishTint = new Color(0.8f, 0.9f, 1f, 1f);

        [Header("Underwater idle motion")]
        public bool animateUnderwater = true;
        public float fishSwimRadius = 0.018f;
        public float fishBobHeight = 0.014f;
        public float fishSwimSpeed = 1.6f;
        public float fishIdleTilt = 3f;
        public float fishBreathing = 0.018f;
        public float bubbleIdleStretch = 0.012f;
        public float bubbleReactionStretch = 0.045f;

        [Header("Pick")]
        // Extra reach around the finger; total reach = pickRadius + fish collider radius.
        public float pickRadius = 0.12f;
        public float probeOffset = 0.5f;
        public float highlightScale = 1.18f;
        public float highlightTweenDuration = 0.08f;
        public Color highlightColor = Color.white;
        public Color goodHintColor = new Color(0.24f, 0.81f, 0.35f, 1f);
        public Color badHintColor = new Color(1f, 0.54f, 0.16f, 1f);

        [Header("Flight")]
        public float pickAnticipationDuration = 0.1f;
        public float pickPopScale = 1.16f;
        public float flyToTankDuration = 0.68f;
        public float flyToTrayDuration = 0.56f;
        public float flySideOffset = 0.5f;
        public float flyArcRatio = 0.55f;
        public float flyArcLift = 0.3f;
        public float flyTiltDegrees = 12f;
        public float tankIconWidth = 0.54f;
        public float tankFishHeight = 0.4f;
        public float trayFishWidth = 0.32f;
        public float trayFishHeight = 0.25f;
        public float traySlideDuration = 0.2f;

        [Header("Tank")]
        public float tankFlashDuration = 0.18f;
        public float tankLeaveDuration = 0.38f;
        public float tankLeaveRise = 0.6f;
        public float tankLeaveScale = 0.15f;
        public float tankDropHeight = 1.3f;
        public float tankDropDuration = 0.3f;
        public float badgePunchScale = 1.18f;
        public float badgePunchDuration = 0.22f;

        [Header("Fishing rod booster")]
        [Tooltip("The hook comes straight down to the gathering point.")]
        public float rodDropDuration = 0.45f;
        [Tooltip("Time for each fish to fly from its bubble to the hook.")]
        public float rodGatherDuration = 0.5f;
        [Tooltip("Delay between one fish and the next leaving for the hook.")]
        public float rodGatherStagger = 0.08f;
        public float rodHookPause = 0.15f;
        [Tooltip("Lifting the hook with the fish up to the tank.")]
        public float rodPullDuration = 0.4f;
        [Tooltip("The fish dangle above the tank this long before dropping in.")]
        public float rodHoverDuration = 0.5f;
        public float rodRetractDuration = 0.3f;
        [Tooltip("How high above the water the fish hang before they drop into the tank.")]
        public float rodReleaseHeight = 0.9f;
        [Tooltip("The line is tied this far above the top of the screen.")]
        public float rodAnchorMargin = 0.6f;

        [Header("Cannon booster")]
        [Tooltip("The cannon rests this far to the left of the first tray slot.")]
        public float cannonRestOffset = 1f;
        public float cannonEnterDuration = 0.35f;
        [Tooltip("Time the blast takes to sweep along the tray.")]
        public float cannonBlastDuration = 0.3f;
        public float cannonRetreatDuration = 0.3f;
        [Tooltip("Width of the glass dome. Where it stands is the FishJar object in GameplayWorld.prefab: drag it.")]
        public float cannonJarDiameter = 0.8f;
        [Tooltip("The cannon body rests this far below the tray row so its muzzle lines up with the tray.")]
        public float cannonRestDrop = 0.17f;
        [Tooltip("Time between one fish and the next entering the jar (the counter ticks up).")]
        public float cannonJarTick = 0.09f;
        [Tooltip("Camera shake when the cannon fires.")]
        public float cannonShakeAmount = 0.08f;
        public float cannonShakeDuration = 0.18f;

        [Header("Ice")]
        public float icePunchScale = 1.3f;
        public float icePunchDuration = 0.25f;
        public float iceMeltDuration = 0.35f;

        [Header("Combo")]
        public float comboWindow = 1.5f;
        public int perfectRepeat = 3;
        public float comboPopDuration = 0.3f;
        public float comboHoldDuration = 0.4f;
        public float comboFadeDuration = 0.35f;
        public float comboRise = 0.3f;
        public ComboStep[] comboSteps =
        {
            new ComboStep { streak = 2, text = "Nice!", color = new Color(0.49f, 1f, 0.42f), scale = 1f },
            new ComboStep { streak = 3, text = "Good!", color = new Color(0.37f, 0.89f, 1f), scale = 1.1f },
            new ComboStep { streak = 5, text = "Great!", color = new Color(1f, 0.82f, 0.25f), scale = 1.2f },
            new ComboStep { streak = 7, text = "Excellent!", color = new Color(1f, 0.6f, 0.23f), scale = 1.3f },
            new ComboStep { streak = 9, text = "Amazing!", color = new Color(1f, 0.35f, 0.71f), scale = 1.4f },
            new ComboStep { streak = 12, text = "Perfect!", color = new Color(0.77f, 0.55f, 1f), scale = 1.5f }
        };

        [Header("Result")]
        public float winDelay = 0.35f;
        public float loseDelay = 0.77f;

        [Header("Sound")]
        public float pickPitchStep = 0.06f;
        public int maxPitchSteps = 20;

        [Header("Tutorial")]
        public float tipStepDelay = 1.6f;

        public float BubbleDiameter(int fishCount, int levelFishTotal)
        {
            float scale = 1f;
            if (levelFishTotal <= smallLevelFishCount)
            {
                scale = smallLevelBubbleScale;
            }
            else if (levelFishTotal <= mediumLevelFishCount)
            {
                scale = mediumLevelBubbleScale;
            }

            return bubbleDiameterByFishCount[ClampCount(fishCount)] * scale;
        }

        public float FishWidth(int fishCount)
        {
            return fishWidthByFishCount[ClampCount(fishCount)];
        }

        public float PickPitch(int streak)
        {
            return Mathf.Pow(1f + pickPitchStep, Mathf.Min(streak, maxPitchSteps));
        }

        private int ClampCount(int fishCount)
        {
            return Mathf.Clamp(fishCount, 1, bubbleDiameterByFishCount.Length - 1);
        }
    }
}
