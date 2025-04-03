using TMPro;
using UnityEngine;
using UnityEngine.UI;

    public class QuizAnswerButton : MonoBehaviour
    {
        [SerializeField] 
        private Button _answerButton;
        [SerializeField] 
        private TMP_Text _caption;

        
        private QuizManager quizManager;

        public void InitializeQuizManager(QuizManager manager)
        {
            // Cache a reference to quiz manager
            quizManager = manager;
        }

        public void SetButtonData(string answerText)
        {
            // Show the answer assigned to this button
            _caption.text = answerText;
            
            // Set button interactable to true
            ToggleInteractable(true);
        }

        public void OnAnswerButtonClicked()
        {
            quizManager.ValidateAnswer(this);
        }

        public void ToggleInteractable(bool isInteractable)
        {
            _answerButton.interactable = isInteractable;
        }
    }
