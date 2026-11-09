using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FishSort
{
    // Tap = pick immediately. Hold + drag = the fish nearest the finger glows yellow; release picks it.
    // Data only changes on release; while holding only visuals change (design doc 4.3).
    public sealed class FishPicker : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera;
        [SerializeField] private BubbleField field;
        [SerializeField] private TankShelf shelf;
        [SerializeField] private FishSortConfig config;

        private Fish highlighted;
        private bool isHolding;

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            if (!GameplayController.Ins.IsInputEnabled)
            {
                CancelHold();
                return;
            }

            if (pointer.press.wasPressedThisFrame)
            {
                isHolding = !IsOverUI();
            }

            if (!isHolding)
            {
                return;
            }

            Vector2 worldPoint = ToWorld(pointer.position.ReadValue());
            if (pointer.press.isPressed)
            {
                SetHighlight(field.FindFishNear(worldPoint));
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                // A very fast tap can press and release in one frame: look up the fish at release.
                Fish chosen = highlighted != null ? highlighted : field.FindFishNear(worldPoint);
                CancelHold();
                if (chosen != null)
                {
                    GameplayController.Ins.PickFish(chosen);
                }
            }
        }

        private void CancelHold()
        {
            isHolding = false;
            SetHighlight(null);
        }

        private void SetHighlight(Fish fish)
        {
            if (fish == highlighted)
            {
                return;
            }

            if (highlighted != null && highlighted.IsSelectable)
            {
                highlighted.SetHighlight(FishHighlight.None);
            }

            highlighted = fish;
            if (fish != null)
            {
                fish.SetHighlight(HighlightFor(fish));
                //TODO: light haptic tick when the highlighted fish changes (needs a haptics plugin).
            }
        }

        private FishHighlight HighlightFor(Fish fish)
        {
            FishHighlight result = FishHighlight.Select;
            if (PlayerData.HintColors)
            {
                result = shelf.FindGoal(fish.TypeId) != null ? FishHighlight.Good : FishHighlight.Bad;
            }

            return result;
        }

        private Vector2 ToWorld(Vector2 screenPoint)
        {
            Vector2 world = gameCamera.ScreenToWorldPoint(screenPoint);
            if (PlayerData.ProbeOffset)
            {
                world.y += config.probeOffset;
            }

            return world;
        }

        private static bool IsOverUI()
        {
            EventSystem eventSystem = EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject();
        }
    }
}
