using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class GridPanController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridSpawner gridSpawner;

    [Header("Panning")]
    [SerializeField] private bool allowPanning = true;
    [SerializeField, Min(0f)] private float panSpeed = 1f;
    [SerializeField, Min(0f)] private float dragThresholdPixels = 8f;
    [Tooltip("Extra world-space room to the left and right of the grid.")]
    [FormerlySerializedAs("panPadding")]
    [SerializeField, Min(0f)] private float horizontalPanPadding = 0.35f;
    [Tooltip("Extra room above and below the grid so it can move past UI.")]
    [SerializeField, Min(0f)] private float verticalPanPadding = 2f;

    private Camera controlledCamera;
    private bool pointerHeld;
    private bool isDragging;
    private int activeFingerId = -1;
    private Vector2 pointerStartPosition;
    private Vector3 previousPointerWorldPosition;
    private Bounds gridBounds;
    private bool hasGridBounds;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (!allowPanning || gridSpawner == null ||
            gridSpawner.transform.childCount == 0)
        {
            CancelPan();
            return;
        }

        if (!controlledCamera.orthographic)
        {
            Debug.LogWarning(
                "GridPanController requires an orthographic Camera.",
                this);
            enabled = false;
            return;
        }

        Touchscreen touchscreen = Touchscreen.current;
        bool touchIsActive = touchscreen != null &&
            (touchscreen.primaryTouch.press.isPressed ||
             touchscreen.primaryTouch.press.wasReleasedThisFrame);

        if (touchIsActive)
        {
            HandleTouchPan(touchscreen);
        }
        else
        {
            HandleMousePan();
        }
    }

    private void HandleMousePan()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 pointerPosition = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame &&
            !IsPointerOverBlockingUi(pointerPosition, -1))
        {
            BeginPan(pointerPosition, -1);
        }

        if (pointerHeld && mouse.leftButton.isPressed)
        {
            ContinuePan(pointerPosition);
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            CancelPan();
        }
    }

    private void HandleTouchPan(Touchscreen touchscreen)
    {
        var touch = touchscreen.primaryTouch;
        Vector2 pointerPosition = touch.position.ReadValue();
        int fingerId = touch.touchId.ReadValue();

        if (touch.press.wasPressedThisFrame &&
            !IsPointerOverBlockingUi(pointerPosition, fingerId))
        {
            BeginPan(pointerPosition, fingerId);
        }

        if (!pointerHeld || fingerId != activeFingerId)
        {
            return;
        }

        if (touch.press.isPressed)
        {
            ContinuePan(pointerPosition);
        }

        if (touch.press.wasReleasedThisFrame)
        {
            CancelPan();
        }
    }

    private void BeginPan(Vector2 screenPosition, int fingerId)
    {
        if (!TryRefreshGridBounds())
        {
            return;
        }

        pointerHeld = true;
        isDragging = false;
        activeFingerId = fingerId;
        pointerStartPosition = screenPosition;
        previousPointerWorldPosition = ScreenToGridPlane(screenPosition);
    }

    private void ContinuePan(Vector2 screenPosition)
    {
        if (!isDragging)
        {
            isDragging = Vector2.Distance(
                pointerStartPosition,
                screenPosition) >= dragThresholdPixels;

            if (!isDragging)
            {
                return;
            }

            previousPointerWorldPosition = ScreenToGridPlane(screenPosition);
            return;
        }

        Vector3 currentPointerWorldPosition =
            ScreenToGridPlane(screenPosition);
        Vector3 dragDelta =
            previousPointerWorldPosition - currentPointerWorldPosition;
        Vector3 nextPosition = transform.position +
            new Vector3(dragDelta.x, dragDelta.y, 0f) * panSpeed;

        transform.position = ClampToGrid(nextPosition);

        // Recalculate after moving the camera to keep the world point beneath
        // the pointer stable throughout the drag.
        previousPointerWorldPosition = ScreenToGridPlane(screenPosition);
    }

    private Vector3 ScreenToGridPlane(Vector2 screenPosition)
    {
        float distanceToGrid = Mathf.Abs(
            gridBounds.center.z - transform.position.z);

        return controlledCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distanceToGrid));
    }

    private Vector3 ClampToGrid(Vector3 position)
    {
        float cameraHalfHeight = controlledCamera.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * controlledCamera.aspect;

        position.x = ClampAxis(
            position.x,
            gridBounds.center.x,
            gridBounds.extents.x + horizontalPanPadding,
            cameraHalfWidth);
        position.y = ClampAxis(
            position.y,
            gridBounds.center.y,
            gridBounds.extents.y + verticalPanPadding,
            cameraHalfHeight);

        return position;
    }

    private bool TryRefreshGridBounds()
    {
        Renderer[] renderers =
            gridSpawner.GetComponentsInChildren<Renderer>();

        hasGridBounds = false;

        foreach (Renderer gridRenderer in renderers)
        {
            if (!gridRenderer.enabled)
            {
                continue;
            }

            if (!hasGridBounds)
            {
                gridBounds = gridRenderer.bounds;
                hasGridBounds = true;
            }
            else
            {
                gridBounds.Encapsulate(gridRenderer.bounds);
            }
        }

        return hasGridBounds;
    }

    private static float ClampAxis(
        float value,
        float gridCenter,
        float gridHalfSize,
        float cameraHalfSize)
    {
        float movementRange = Mathf.Max(
            0f,
            gridHalfSize - cameraHalfSize);

        return Mathf.Clamp(
            value,
            gridCenter - movementRange,
            gridCenter + movementRange);
    }

    private static bool IsPointerOverBlockingUi(
        Vector2 screenPosition,
        int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData =
            new PointerEventData(EventSystem.current)
            {
                position = screenPosition,
                pointerId = pointerId
            };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            // Physics raycasters can hit grid tiles. Only Canvas UI should
            // prevent a camera drag from beginning.
            if (result.module is GraphicRaycaster)
            {
                return true;
            }
        }

        return false;
    }

    private void CancelPan()
    {
        pointerHeld = false;
        isDragging = false;
        activeFingerId = -1;
    }
}