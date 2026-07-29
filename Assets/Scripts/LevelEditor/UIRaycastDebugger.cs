using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIRaycastDebugger : MonoBehaviour
{
    private void Update()
    {
        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (EventSystem.current == null)
        {
            Debug.LogError("No active EventSystem!");
            return;
        }

        PointerEventData pointerData =
            new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        Debug.Log($"UI raycast found {results.Count} objects:");

        foreach (RaycastResult result in results)
        {
            Debug.Log(
                $"HIT: {result.gameObject.name}",
                result.gameObject
            );
        }
    }
}