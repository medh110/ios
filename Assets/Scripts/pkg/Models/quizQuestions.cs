using UnityEngine;
using System;

[System.Serializable]
public class quizQuestions
{
    public string Q; // Question text
    public string A; // Answer A
    public string B; // Answer B
    public string C; // Answer C
    public string D; // Answer D
    public string answer; // Correct answer (e.g., "A")
    public string explanation; // Explanation for the correct answer
}
