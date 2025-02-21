using TMPro;
using UnityEngine;
using UnityEngine.UI;

    public class QuizAnswerButton : MonoBehaviour
    {
        [SerializeField] 
        private Button _answerButton;
        [SerializeField] 
        private TMP_Text _caption;
        [SerializeField] 
        private Image _answerIndicator;
        [SerializeField] 
        private Color _correctColor;
        [SerializeField] 
        private Color _incorrectColor;
        
        private QuizManager quizManager;

        public void InitializeQuizManager(QuizManager quizManager)
        {
            // Cache a reference to quiz manager
            quizManager = quizManager;
        }

        public void SetButtonData(string answerText)
        {
            // Show the answer assigned to this button
            _caption.text = answerText;
            _answerIndicator.gameObject.SetActive(false);     
            
            // Set button interactable to true
            ToggleInteractable(true);
        }

        public void OnAnswerButtonClicked()
        {
            ShowAnswerIndicator(quizManager.ValidateAnswer(this));
        }

        public void ShowAnswerIndicator(bool isCorrect)
        {
            // Toggle indicator if the answer is correct or incorrect
            _answerIndicator.gameObject.SetActive(true);
            if (isCorrect)
            {
                _answerIndicator.color = _correctColor;
            }
            else
            {
                _answerIndicator.color = _incorrectColor;
            }
        }

        public void ToggleInteractable(bool isInteractable)
        {
            _answerButton.interactable = isInteractable;
        }
    }
