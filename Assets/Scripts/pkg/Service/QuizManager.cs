using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text questionText;
    public Button answerAButton;
    public Button answerBButton;
    public Button answerCButton;
    public Button answerDButton;
    public Text feedbackText;

    private string correctAnswer;

    public void SetQuizData(APIClient.QuizResponse quizData)
    {
        // Set the question and answers
        questionText.text = quizData.questions;

        SetButton(answerAButton, quizData.answer_a);
        SetButton(answerBButton, quizData.answer_b);
        SetButton(answerCButton, quizData.answer_c);
        SetButton(answerDButton, quizData.answer_d);

        // Store the correct answer
        correctAnswer = quizData.correct_answer;

        // Clear feedback
        feedbackText.text = string.Empty;
    }

    private void SetButton(Button button, string answerText)
    {
        if (!string.IsNullOrEmpty(answerText))
        {
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<Text>().text = answerText;

            // Set click listener
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ValidateAnswer(answerText));
        }
        else
        {
            button.gameObject.SetActive(false);
        }
    }

    private void ValidateAnswer(string selectedAnswer)
    {
        if (selectedAnswer == correctAnswer)
        {
            feedbackText.text = "Correct!";
            feedbackText.color = Color.green;
        }
        else
        {
            feedbackText.text = "Incorrect!";
            feedbackText.color = Color.red;
        }

        // Optional: Disable buttons after selection
        DisableAllButtons();
    }

    private void DisableAllButtons()
    {
        answerAButton.interactable = false;
        answerBButton.interactable = false;
        answerCButton.interactable = false;
        answerDButton.interactable = false;
    }
}
