using Spine.Unity;
using UnityEngine;

namespace FishJam.UI
{
    /// <summary>Chay chuoi animation Spine moi lan bat (OnEnable): anims[0..n-2] 1 lan, anims[n-1] lap. Chay ca khi Time.timeScale = 0.</summary>
    [RequireComponent(typeof(SkeletonGraphic))]
    public class SpineSequence : MonoBehaviour
    {
        public string[] anims = { "idle" };
        SkeletonGraphic sg;

        void OnEnable()
        {
            if (sg == null) sg = GetComponent<SkeletonGraphic>();
            sg.unscaledTime = true;
            if (!sg.IsValid) sg.Initialize(false);
            if (anims == null || anims.Length == 0) return;
            var st = sg.AnimationState;
            st.SetAnimation(0, anims[0], anims.Length == 1);
            for (int i = 1; i < anims.Length; i++) st.AddAnimation(0, anims[i], i == anims.Length - 1, 0);
            sg.Update(0); sg.LateUpdate();
        }
    }
}
