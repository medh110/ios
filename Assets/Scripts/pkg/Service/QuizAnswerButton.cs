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
        
        private QuizManager _quizManager;

        public void InitializeQuizManager(QuizManager quizManager)
        {
            _quizManager = quizManager;
        }

        public void SetButtonData(string answerText)
        {
            _caption.text = answerText;
            
            _answerIndicator.gameObject.SetActive(false);     
            ToggleInteractable(true);
        }

        public void OnAnswerButtonClicked()
        {
            ShowAnswerIndicator(_quizManager.ValidateAnswer(this));
        }

        public void ShowAnswerIndicator(bool isCorrect)
        {
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
