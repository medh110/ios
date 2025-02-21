using System;
using UnityEngine;

public class DisableOnAnimationTrigger : MonoBehaviour
{
    protected Animator PageAnimator;
    protected Action OnCloseAction { get; set; }
    private const string EXIT_TRIGGER = "Exit";
    
    public virtual void Start()
    {
        // Get the animator of the Page
        PageAnimator = GetComponent<Animator>();
    }

    public virtual void SetOnCloseAction(Action action)
    {
        // Cache any on close execution assigned in UI Page
        OnCloseAction = action;
    }
    
    protected virtual void DisableGameobjectOnTrigger()
    {
        // Trigger On close action after exit animation ends
        OnCloseAction?.Invoke();
        OnCloseAction = null;
        gameObject.SetActive(false);
    }
    
    public void OnCloseButtonClicked()
    {
        if (PageAnimator == null)
        {
            throw new NullReferenceException("Page Animator should not be null");
        }
        
        // Trigger the exit animator of the UI
        PageAnimator.SetTrigger(EXIT_TRIGGER);
    }
}
