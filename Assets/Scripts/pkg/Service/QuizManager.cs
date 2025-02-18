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
    private Image _explanationImage;

    private string correctAnswer;
    private QuizAnswerButton CorrectButton;
    private const string SHOW_ANSWER_TRIGGER = "ShowAnswer";

    public void OnEnable()
    {
        APIClient.QuizResponse sampleQuiz = new APIClient.QuizResponse();
        sampleQuiz.questions = "what is the national flower of singapore";
        sampleQuiz.answer_a = "Sunflower";
        sampleQuiz.answer_b = "Epidendrum Orchid";
        sampleQuiz.answer_c = "Vanda Miss Joaquim Orchid";
        sampleQuiz.answer_d = "Brassavola Orchid";
        sampleQuiz.explanation = "Papilionanthe Miss Joaquim, Also known as the Singapore orchid, this hybrid orchid is the national flower of Singapore. It was chosen for its resilience and vibrant colors.";
        sampleQuiz.correct_answer = sampleQuiz.answer_c;
        
        SetQuizData(sampleQuiz);
    }

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

        SetButton(_answerAButton, quizData.answer_a);
        SetButton(_answerBButton, quizData.answer_b);
        SetButton(_answerCButton, quizData.answer_c);
        SetButton(_answerDButton, quizData.answer_d);

    }

    private void SetButton(QuizAnswerButton button, string answerText)
    {
        if (!string.IsNullOrEmpty(answerText))
        {
            if (correctAnswer == answerText)
            {
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

    public bool ValidateAnswer(QuizAnswerButton quizAnswerButton)
    {
        var result = true;  
        if(quizAnswerButton != CorrectButton)
        {
            result = false;
            CorrectButton.ShowAnswerIndicator(true);
        }
        PageAnimator.SetTrigger(SHOW_ANSWER_TRIGGER);
        DisableAllButtons();
        return result;
    }

    private void DisableAllButtons()
    {
        _answerAButton.ToggleInteractable(false);
        _answerBButton.ToggleInteractable(false);
        _answerCButton.ToggleInteractable(false);
        _answerDButton.ToggleInteractable(false);
    }
}
