using System;
using UnityEngine;

public class DisableOnAnimationTrigger : MonoBehaviour
{
    protected Animator PageAnimator;
    
    private const string EXIT_TRIGGER = "Exit";
    
    public virtual void Start()
    {
        PageAnimator = GetComponent<Animator>();
    }

    public void DisableGameobjectOnTrigger()
    {
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
