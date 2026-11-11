using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChangeAnim
{
    public class Fish : MonoBehaviour
    {
        [SerializeField] Animator anim;
        private string animName;

        private void Start()
        {
            ChangeAnim(Constants.ANIM_IDLE);
        }

        public void ChangeAnim(string animName)
        {
            if (this.animName != animName)
            {
                anim.ResetTrigger(this.animName);
                this.animName = animName;
                anim.SetTrigger(this.animName);
            }
        }
    }
}
