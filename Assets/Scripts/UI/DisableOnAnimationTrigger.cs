using System;
using UnityEngine;

public class DisableOnAnimationTrigger : MonoBehaviour
{
    protected Animator PageAnimator;
    protected Action OnCloseAction { get; set; }
    private const string EXIT_TRIGGER = "Exit";
    
    public virtual void Start()
    {
        PageAnimator = GetComponent<Animator>();
    }

    public virtual void SetOnCloseAction(Action action)
    {
        OnCloseAction = action;
    }
    
    protected virtual void DisableGameobjectOnTrigger()
    {
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
        
        PageAnimator.SetTrigger(EXIT_TRIGGER);
    }
}
