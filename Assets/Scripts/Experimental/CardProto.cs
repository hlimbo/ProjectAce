using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardProto : MonoBehaviour
{
    [SerializeField]
    private int animIndex;
    private Animator animController;

    private void Awake()
    {


        animController = GetComponent<Animator>();
        animController.SetInteger("animIndex", animIndex);
    }
}
