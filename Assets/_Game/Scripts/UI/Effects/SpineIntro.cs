using Spine.Unity;
using UnityEngine;

namespace FishJam.UI
{
    /// <summary>Chay 1 animation mo dau roi lap animation idle (logo: appear -> idle).</summary>
    [RequireComponent(typeof(SkeletonGraphic))]
    public class SpineIntro : MonoBehaviour
    {
        public string intro = "appear";
        public string loop = "idle";
        void Start()
        {
            var sg = GetComponent<SkeletonGraphic>();
            if (!string.IsNullOrEmpty(intro)) 
            { 
                sg.AnimationState.SetAnimation(0, intro, false); 
                sg.AnimationState.AddAnimation(0, loop, true, 0); 
            }
            else sg.AnimationState.SetAnimation(0, loop, true);
        }
    }
}
