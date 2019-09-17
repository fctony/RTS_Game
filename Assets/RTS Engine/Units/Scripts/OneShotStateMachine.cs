using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
	public class OneShotStateMachine : StateMachineBehaviour {

		//in order to avoid getting stuck in the "TakeDamage" state.
		override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			animator.SetBool ("TookDamage", false);
            animator.SetBool("IsAttacking", false);
        }
	}
}
