using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ARGameUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject setupPanel;
    [SerializeField] private GameObject playPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Setup Texts")]
    [SerializeField] private TextMeshProUGUI targetsPlacedText;

    [Header("Play Texts")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject crosshair;

    [Header("Game Over Texts")]
    [SerializeField] private TextMeshProUGUI finalTitleText; // Наприклад "Перемога!" або "Час вийшов"
    [SerializeField] private TextMeshProUGUI finalStatsText; // Тут буде рахунок і час

    public void SetUIState(ARGameManager.GameState state)
    {
        setupPanel.SetActive(state == ARGameManager.GameState.Setup);
        playPanel.SetActive(state == ARGameManager.GameState.Playing);
        gameOverPanel.SetActive(state == ARGameManager.GameState.GameOver);
        
        if (crosshair != null) crosshair.SetActive(state == ARGameManager.GameState.Playing);
    }

    public void UpdateScore(int score) { scoreText.text = $"Очки: {score}"; }

    public void UpdateTime(float time) { timerText.text = $"Час: {Mathf.CeilToInt(time)} с"; }

    public void UpdateTargetsCount(int count) { targetsPlacedText.text = $"Мішеней поставлено: {count}"; }

    // НОВИЙ МЕТОД: Виводить фінальну статистику
    public void ShowFinalResults(int finalScore, float timeTaken, bool allCleared)
    {
        if (allCleared)
        {
            finalTitleText.text = "МІШЕНІ ЗНИЩЕНО!";
            finalTitleText.color = Color.green;
        }
        else
        {
            finalTitleText.text = "ЧАС ВИЙШОВ!";
            finalTitleText.color = Color.red;
        }

        // Форматуємо час до одного знака після коми (наприклад, 14.5 сек)
        finalStatsText.text = $"Зароблено очок: {finalScore}\nВитрачено часу: {timeTaken:F1} сек";
    }

    public bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}