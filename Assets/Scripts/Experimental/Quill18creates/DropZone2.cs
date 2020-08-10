using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone2 : MonoBehaviour, IDropHandler
{
    private int rotationIndex;
    private float[] rotationAngles = new float[] { -45, 30, -15, 15, -30, 45 };

    int childCount = 0;

    public void OnDrop(PointerEventData eventData)
    {
        // In the actual implementation I would check over the network if placing the card here is a valid move
        if(eventData.pointerDrag != null)
        {
            var isc = eventData.pointerDrag.GetComponent<InteractableStateController>();
            if(isc != null)
            {
                isc.isPlacedOnTable = true;
                Debug.Log("RotationAngles: " + rotationAngles[rotationIndex]);
                isc.MoveCardToTargetPosition(transform, rotationAngles[rotationIndex]);
                rotationIndex = (rotationIndex + 1) % rotationAngles.Length;
            }
        }
    }

    private void Update()
    {
        if (transform.childCount == childCount) return;

        if (childCount < transform.childCount)
        {
            childCount = transform.childCount;
        }
    }
}
