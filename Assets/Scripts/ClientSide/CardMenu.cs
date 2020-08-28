using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    Animator animator;

    public void OnPointerClick(PointerEventData eventData)
    {
        animator.SetBool("isUp", false);
        enabled = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        animator.SetBool("isUp", true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        animator.SetBool("isUp", false);
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
       // Used here so script can be enabled/disabled
    }


}
