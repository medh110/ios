using UnityEngine;
using UnityEngine.InputSystem;

public class MainHud : MonoBehaviour
{
    [SerializeField]
    private ARImageBehaviorManager _imageBehaviorManager;
    [SerializeField] 
    private GameObject _helpPage;
    [SerializeField] 
    private GameObject _previewPage;
    [SerializeField]
    private GameObject _hudPage;
    
    private TouchControls touchControls;
    private InputAction pinchGap;
    private InputAction touchPress;
    private InputAction secondPress;
    
    private Camera mainCamera;
    private Ray cameraRay;

    private bool onTouchHold;
    private bool onPreviewMode;
    private bool onPointerDown;
    private bool initialTouchPoint;
    private bool isTouchScreenActive = true;
    private bool secondTouchInitiated = false;
    private bool hasInitialDistance = false;

    private float prevDist;
    
    private GameObject currentMovableObject;
    private GameObject lastSelectedObject;

    private Vector3 touchPosition;
    private Vector3 originPoint;
    private Vector3 mousePosition;
    private Vector3 touchDelta;
    private Vector3 inputScale;

    private const float SCALE_DAMPENER = 0.1f;
    private const float ROTATION_DAMPENER_X = 0.5f;
    private const float ROTATION_DAMPENER_Y = 0.3f;

    private void Start()
    {
        mainCamera = Camera.main;
        
        // Initialize the input action of controls
        touchControls = new TouchControls();
        touchControls.Enable();
        
        pinchGap = touchControls.Touch.PinchGapDelta;
        touchPress = touchControls.Touch.PrimaryTouchPressed;
        secondPress = touchControls.Touch.SecondaryTouchContact;

        // Listen for input action triggers when action is performed and cancelled
        secondPress.performed += OnSecondPressPerformed;
        secondPress.canceled += OnSecondPressCancelled;
        touchPress.performed += OnTouchPerformed;
        touchPress.canceled += OnTouchCancelled;
        pinchGap.performed += OnPinchPerformed;
        
#if UNITY_EDITOR
        // Check if view is in simulator or not
        isTouchScreenActive = Touchscreen.current != null;
#elif UNITY_ANDROID || UNITY_IOS
        // No need to change value
#else
        // Default to false not editor or mobile
        IsTouchScreenActive = false;
#endif
    }

    private void OnSecondPressCancelled(InputAction.CallbackContext obj)
    {
        // Triggered when system detects the second touch is removed
        // Disable flag for pinch scale
        hasInitialDistance = false;
        secondTouchInitiated = false;
    }

    private void OnSecondPressPerformed(InputAction.CallbackContext obj)
    {
        // Triggered when system detects a second touch
        // Enabled the flag to detect pinch scale
        secondTouchInitiated = true;
    }

    private void OnTouchCancelled(InputAction.CallbackContext obj)
    {
        // Triggered when system detects that the primary touch is removed
        // Disable all flags related to drag and rotate
        initialTouchPoint = false;
        onPointerDown = false;
        onTouchHold = false;
    }

    private void OnTouchPerformed(InputAction.CallbackContext obj)
    {
        // Triggered when system detects that the primary touch 
        onPointerDown = true;
    }

    private void OnEnable()
    {
        touchControls?.Enable();
    }

    private void OnDisable()
    {
        touchControls.Disable();
    }

    private void OnDestroy()
    {
        //Dispose all input action on destroy
        secondPress.performed -= OnSecondPressPerformed;
        secondPress.canceled -= OnSecondPressCancelled;
        touchPress.performed -= OnTouchPerformed;
        touchPress.canceled -= OnTouchCancelled;
        pinchGap.performed -= OnPinchPerformed;
        
        touchControls.Dispose();
    }

    public void ShowHelpPage()
    {
        // Enable help page
        _helpPage.SetActive(true);
    }

    public void ClosePreview()
    {
        // Close HUD Preview mode and set values of movable objects
        // to null for proper handling
        currentMovableObject = null;
        lastSelectedObject = null;
        TogglePreview(false);
        
        // We must inform behavior manager that preview is closed
        // to properly reset any values need on the manager side
        _imageBehaviorManager.OnPreviewClosed();
    }

    public void ToggleFlashLight()
    {
        
    }

    public void TogglePreview(bool onPreview)
    {
        // Toggle the flag if preview is enabled
        onPreviewMode = onPreview;

        if (onPreviewMode)
        {
            // If preview mode is enabled. we cache the spawned object in the manager
            currentMovableObject = _imageBehaviorManager.CurrentMovableObject;
        }
        
        _previewPage.SetActive(onPreview);
        _hudPage.SetActive(!onPreview);
    }

    private bool CheckTargetScale(Vector3 targetScale, ARType currentType)
    {
        // Check if the scale of the target doesn't exceed the limit to
        // prevent object from getting scaled to a large or negative value
        if (currentType == ARType.Model)
        {
            if (targetScale.z < 0.1 || targetScale.z > 5f)
            {
                return false;
            }            
        }

        if (targetScale.x < 0.1f || targetScale.y < 0.1f)
        {
            return false;
        }

        if (targetScale.x > 5f || targetScale.y > 5f)
        {
            return false;
        }

        return true;
    }

    private void OnPinchPerformed(InputAction.CallbackContext context)
    {
        if (!onPreviewMode)
        {
            // Return if preview mode is disabled
            return;
        }

        // Get the value from input action 
        var input = context.ReadValue<float>() * SCALE_DAMPENER;
        if (_imageBehaviorManager.CurrentType == ARType.Model)
        {
            // We use vector3 for model to include z-axis in scale
            inputScale = Vector3.one * input * Time.deltaTime;
        }
        else
        {
            // We use vector2 for video to exclude z-axis in scale
            inputScale = Vector2.one * input * Time.deltaTime;
        }

        // Compute the end result of scale and check if it is within the limit
        var targetScale = _imageBehaviorManager.CurrentMovableObject.transform.localScale + inputScale;

        if (!CheckTargetScale(targetScale, _imageBehaviorManager.CurrentType))
        {
            // Don't apply if it is outside the scale limit
            return;
        }
        
        // Apply the target scale if within the limit 
        currentMovableObject.transform.localScale = targetScale;
    }

    private void Update()
    {
        if (!onPreviewMode)
        {
            return;
        }

        if (secondTouchInitiated)
        {
            var posA = Touchscreen.current.touches[0].position.value;
            var posB = Touchscreen.current.touches[0].position.value;
            var dist = Vector2.Distance(posA, posB);
            if (!hasInitialDistance)
            {
                // If no dist is recorded yet use the computed dist as starting distance
                prevDist = dist;
                hasInitialDistance = true;
            }

            var targetDist = dist - prevDist;
            prevDist = dist;
            if (_imageBehaviorManager.CurrentType == ARType.Model)
            {
                // We use vector3 for model to include z-axis in scale
                inputScale = Vector3.one * targetDist * Time.deltaTime;
            }
            else
            {
                // We use vector2 for video to exclude z-axis in scale
                inputScale = Vector2.one * targetDist * Time.deltaTime;
            }
            
            // Compute the end result of scale and check if it is within the limit
            var targetScale = currentMovableObject.transform.localScale + inputScale;
            if (!CheckTargetScale(targetScale, _imageBehaviorManager.CurrentType))
            {
                // Don't apply if it is outside the scale limit
                return;
            }
            
            // Apply the target scale if within the limit 
            currentMovableObject.transform.localScale = targetScale;
        
            return;
        }

        if (onPointerDown)
        {
            // This allow us to switch from Game/Simulator view
            // Mouse is only available for Game View and TouchScreen
            // is only available for simulator view
            if (!isTouchScreenActive)
            {
                touchPosition = Mouse.current.position.value;
                touchDelta = Mouse.current.delta.value;
            }
            else
            {
                touchPosition = Touchscreen.current.primaryTouch.position.value;
                touchDelta = Touchscreen.current.delta.value;
            }

            if (!initialTouchPoint)
            {
                // If initial touch is detected check if it touches the object collider
                initialTouchPoint = true;
                cameraRay = mainCamera.ScreenPointToRay(touchPosition);
                RaycastHit hitObj;
                if (Physics.Raycast(cameraRay, out hitObj))
                {
                    if (hitObj.transform.gameObject == currentMovableObject)
                    {
                        if (!onTouchHold)
                        {
                            // If the current preview object is touched we initiate drag functionality
                            originPoint = mainCamera.WorldToScreenPoint(hitObj.transform.position);
                            mousePosition = touchPosition - originPoint;
                            lastSelectedObject = hitObj.transform.gameObject;
                            onTouchHold = true;                        
                        }
                    }
                }            
            }
        
            if (onTouchHold)
            {
                // Move the object to mouse position until the primary touch is released
                lastSelectedObject.transform.position = mainCamera.ScreenToWorldPoint(touchPosition - mousePosition);
            }
            else
            {
                // Rotate the object relative to camera view using the touch movement delta
                var relativeUp = mainCamera.transform.TransformDirection(Vector3.up);
                var relativeRight = mainCamera.transform.TransformDirection(Vector3.right);
                currentMovableObject.transform.Rotate(relativeUp, -touchDelta.x * ROTATION_DAMPENER_X, Space.World);
                currentMovableObject.transform.Rotate(relativeRight, touchDelta.y * ROTATION_DAMPENER_Y, Space.World);
            }            
        }
    }
}
