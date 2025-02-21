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
        touchControls = new TouchControls();
        touchControls.Enable();
        
        pinchGap = touchControls.Touch.PinchGapDelta;
        touchPress = touchControls.Touch.PrimaryTouchPressed;
        secondPress = touchControls.Touch.SecondaryTouchContact;

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
        hasInitialDistance = false;
        secondTouchInitiated = false;
    }

    private void OnSecondPressPerformed(InputAction.CallbackContext obj)
    {
        secondTouchInitiated = true;
    }

    private void OnTouchCancelled(InputAction.CallbackContext obj)
    {
        initialTouchPoint = false;
        onPointerDown = false;
        onTouchHold = false;
    }

    private void OnTouchPerformed(InputAction.CallbackContext obj)
    {
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
        secondPress.performed -= OnSecondPressPerformed;
        secondPress.canceled -= OnSecondPressCancelled;
        touchPress.performed -= OnTouchPerformed;
        touchPress.canceled -= OnTouchCancelled;
        pinchGap.performed -= OnPinchPerformed;
        
        touchControls.Dispose();
    }

    public void ShowHelpPage()
    {
        _helpPage.SetActive(true);
    }

    public void ClosePreview()
    {
        currentMovableObject = null;
        lastSelectedObject = null;
        TogglePreview(false);
        _imageBehaviorManager.OnPreviewClosed();
    }

    public void ToggleFlashLight()
    {
        
    }

    public void TogglePreview(bool onPreview)
    {
        onPreviewMode = onPreview;

        if (onPreviewMode)
        {
            currentMovableObject = _imageBehaviorManager.CurrentMovableObject;
        }
        _previewPage.SetActive(onPreview);
        _hudPage.SetActive(!onPreview);
    }

    private bool CheckTargetScale(Vector3 targetScale, ARType currentType)
    {
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
            return;
        }

        var input = context.ReadValue<float>() * SCALE_DAMPENER;
        if (_imageBehaviorManager.CurrentType == ARType.Model)
        {
            inputScale = Vector3.one * input * Time.deltaTime;
        }
        else
        {
            inputScale = Vector2.one * input * Time.deltaTime;
        }

        var targetScale = _imageBehaviorManager.CurrentMovableObject.transform.localScale + inputScale;

        if (!CheckTargetScale(targetScale, _imageBehaviorManager.CurrentType))
        {
            return;
        }
        
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
                prevDist = dist;
                hasInitialDistance = true;
            }

            var targetDist = dist - prevDist;
            prevDist = dist;
            if (_imageBehaviorManager.CurrentType == ARType.Model)
            {
                inputScale = Vector3.one * targetDist * Time.deltaTime;
            }
            else
            {
                inputScale = Vector2.one * targetDist * Time.deltaTime;
            }

            var targetScale = currentMovableObject.transform.localScale + inputScale;
            if (!CheckTargetScale(targetScale, _imageBehaviorManager.CurrentType))
            {
                return;
            }
            currentMovableObject.transform.localScale = targetScale;
        
            return;
        }

        if (onPointerDown)
        {
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
                initialTouchPoint = true;
                cameraRay = mainCamera.ScreenPointToRay(touchPosition);
                RaycastHit hitObj;
                if (Physics.Raycast(cameraRay, out hitObj))
                {
                    if (hitObj.transform.gameObject == currentMovableObject)
                    {
                        if (!onTouchHold)
                        {
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
                lastSelectedObject.transform.position = mainCamera.ScreenToWorldPoint(touchPosition - mousePosition);
            }
            else
            {
                var relativeUp = mainCamera.transform.TransformDirection(Vector3.up);
                var relativeRight = mainCamera.transform.TransformDirection(Vector3.right);
                currentMovableObject.transform.Rotate(relativeUp, -touchDelta.x * ROTATION_DAMPENER_X, Space.World);
                currentMovableObject.transform.Rotate(relativeRight, touchDelta.y * ROTATION_DAMPENER_Y, Space.World);
            }            
        }
    }
}
