using UnityEngine;
using UnityEngine.Android;

public class QuizController : MonoBehaviour
{
    public GameObject TopicUI;
    public GameObject QuizUI;
    public int GameMode = 1; //1. Topic Select , 2 Quiz Mode

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (GameMode == 1)
        {
            TopicUI.SetActive(true);
            QuizUI.SetActive(false);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (GameMode == 1) {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.tag == "Topic")
                    {
                        TopicUI.SetActive(false);
                        QuizUI.SetActive(true);
                        GameMode = 2;
                    }
                }
                TopicUI.SetActive(false);
                QuizUI.SetActive(true);
                GameMode = 2;
            }
        }

    }
}
