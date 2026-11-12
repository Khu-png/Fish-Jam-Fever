using UnityEngine;

namespace FishSort
{
    // Locked tanks (the grey ones with a plus) can be bought: touching one opens the shop; paying opens it with the next tank
    // of the sequence, and the grey tank fades into it.
    public sealed partial class GameplayController
    {
        private const string NO_MORE_TANKS_MESSAGE = "There are no more tanks to open";

        public void OfferTankPurchase(int slot)
        {
            if (!IsInputEnabled)
            {
                return;
            }

            FishJam.UI.GamePlay hud = UIManager.Ins.GetUI<FishJam.UI.GamePlay>();
            if (!shelf.CanUnlock)
            {
                hud.ShowToast(NO_MORE_TANKS_MESSAGE);
                return;
            }

            UIManager.Ins.OpenUI<FishJam.UI.BoosterShop>().ShowTank(() => shelf.UnlockSlot(slot, -1, true));
        }
    }
}
