using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentFaceUpCard : MonoBehaviour
{
    private Animator animController;

    private void Awake()
    {
        animController = GetComponent<Animator>();
    }

    public void PlayAnimation(int animIndex)
    {
        animController.SetBool("canPlay", true);
        animController.SetInteger("animIndex", animIndex);
    }
}
