using UnityEngine;

public class MainHud : MonoBehaviour
{
    [SerializeField] 
    private GameObject _helpPage;
    
    public void ShowHelpPage()
    {
        _helpPage.SetActive(true);
    }

    public void ToggleFlashLight()
    {
        
    }
}
