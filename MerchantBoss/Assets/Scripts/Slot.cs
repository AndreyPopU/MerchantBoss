using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IEndDragHandler
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData data)
    {
        // Show item info
    }

    public void OnPointerEnter(PointerEventData data)
    {
        // Highlight
    }

    public void OnPointerExit(PointerEventData data)
    {
        // Opposite of highlight
    }

    public void OnDrag(PointerEventData data)
    {
        // Move item slot
    }

    public void OnEndDrag(PointerEventData data)
    {
        // Check if it's stacked, swapped or just moved
    }
}
