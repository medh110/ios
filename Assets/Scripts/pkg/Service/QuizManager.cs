using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class QuizManager : DisableOnAnimationTrigger
{
    [Header("UI Elements")]
    [SerializeField]
    private TMP_Text _questionText;
    [SerializeField]
    private QuizAnswerButton _answerAButton;
    [SerializeField]
    private QuizAnswerButton _answerBButton;
    [SerializeField]
    private QuizAnswerButton _answerCButton;
    [SerializeField]
    private QuizAnswerButton _answerDButton;
    [SerializeField]
    private TMP_Text _explanation;
    [SerializeField]
    private TMP_Text _answerIndicatorText;
    [SerializeField]
    private RawImage _explanationImage;

    [SerializeField]
    private Image _answerIndicator;
    [SerializeField]
    private Color _correctColor;
    [SerializeField]
    private Color _incorrectColor;

    private string correctAnswer;
    private QuizAnswerButton CorrectButton;
    private const string SHOW_ANSWER_TRIGGER = "ShowAnswer";



    public override void Start()
    {
        base.Start();
        
        _answerAButton.InitializeQuizManager(this);
        _answerBButton.InitializeQuizManager(this);
        _answerCButton.InitializeQuizManager(this);
        _answerDButton.InitializeQuizManager(this);
    }
    
    public void SetQuizData(APIClient.QuizResponse quizData)
    {
        // Set the question and answers
        _questionText.text = quizData.questions;
        
        // Store the correct answer
        correctAnswer = quizData.correct_answer;
        _explanation.text = quizData.explanation;

        // Set Button data
        SetButton(_answerAButton, quizData.answer_a);
        SetButton(_answerBButton, quizData.answer_b);
        SetButton(_answerCButton, quizData.answer_c);
        SetButton(_answerDButton, quizData.answer_d);

        gameObject.SetActive(true);
    }

    private void SetButton(QuizAnswerButton button, string answerText)
    {
        if (!string.IsNullOrEmpty(answerText))
        {
            if (correctAnswer == answerText)
            {
                // Cached the button that shows the correct answer
                CorrectButton = button;
            }

            button.gameObject.SetActive(true);
            button.SetButtonData(answerText);
        }
        else
        {
            button.gameObject.SetActive(false);
        }
    }

    public void ValidateAnswer(QuizAnswerButton quizAnswerButton)
    {
        var result = true;  
        if(quizAnswerButton != CorrectButton)
        {
            result = false;
        }
     
        ShowAnswerIndicator(result);

        // Trigger to show the answer in animation
        PageAnimator.SetTrigger(SHOW_ANSWER_TRIGGER);
        DisableAllButtons();
    }

    public void ShowAnswerIndicator(bool isCorrect)
    {
        // Toggle indicator if the answer is correct or incorrect
        _answerIndicator.gameObject.SetActive(true);
        if (isCorrect)
        {
            _answerIndicatorText.text = "CORRECT";
            _answerIndicator.color = _correctColor;
        }
        else
        {
            _answerIndicatorText.text = "INCORRECT";
            _answerIndicator.color = _incorrectColor;
        }
    }

    private void DisableAllButtons()
    {
        // Set button interactable to false
        _answerAButton.ToggleInteractable(false);
        _answerBButton.ToggleInteractable(false);
        _answerCButton.ToggleInteractable(false);
        _answerDButton.ToggleInteractable(false);
    }

    public void SetIcon(Texture2D tex)
    {
        // Set the texture for image of the answer
        _explanationImage.texture = tex;
    }

    public void SetEmpty()
    {

    }
}
