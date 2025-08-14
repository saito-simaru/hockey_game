using UnityEngine;
using TMPro;

public class gamemanager : MonoBehaviour
{
    public int maxScore = 5;
    public int[] scores = new int[2];

    [Header("UI")]
    public TextMeshProUGUI scoreText;

    [Header("Respawn")]
    public Transform spawn0;
    public Transform spawn1;

    public void AddPoint(int playerId)
    {
        if (playerId < 0 || playerId > 1) return;
        scores[playerId]++;
        UpdateUI();

        if (scores[playerId] >= maxScore)
        {
            // 勝利演出（簡易）
        Time.timeScale = 0f;
            if (scoreText) scoreText.text = $"Player {playerId+1} Wins!";
        }
        else
        {
            // 相手をリスポーン（すぐ）
            RespawnPlayers();
        }
    }

    void Start()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText)
            scoreText.text = $"P1: {scores[0]}  -  P2: {scores[1]}";
    }

    void RespawnPlayers()
    {
        var players = FindObjectsOfType<player>();
        foreach (var p in players)
        {
            if (p.playerId == 0 && spawn0)
            {
                p.transform.position = spawn0.position;
                p.transform.localRotation = spawn0.rotation;
            }
            else if (p.playerId == 1 && spawn1)
            {
                p.transform.position = spawn1.position;
                p.transform.localRotation = spawn1.rotation;
            }

        }
    }
}
