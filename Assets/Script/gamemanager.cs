using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class gamemanager : MonoBehaviour
{
    public goal goalscript;
    public int maxScore = 5;
    public int[] scores = new int[2];

    private bool isplaying = false;
    private PlayerInputManager pim;
    [Header("PlayerPrefab")]
    public GameObject PlayerPrefab;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI winningText;

    [Header("Respawn")]
    public Transform spawn0;
    public Transform spawn1;

    void Awake()
    {
        pim = GetComponent<PlayerInputManager>();
    }

    void Start()
    {
        UpdateUI();
        goalscript.RespawnBall();
        winningText.gameObject.SetActive(false);
    }


    public void AddPoint(int playerId)
    {
        if (playerId < 0 || playerId > 1) return;
        scores[playerId]++;
        UpdateUI();

        if (scores[playerId] >= maxScore)
        {
            // 勝利演出（簡易）
            Time.timeScale = 0f;
            winningText.gameObject.SetActive(true);
            isplaying = false;

            if (playerId == 0)
            {
                //青色
                winningText.color = new Color(70, 190, 255);
                winningText.text = $"    Player {playerId + 1} Wins!                           Restert to R";
            }
            else
            {
                //オレンジ色
                winningText.color = new Color(255, 150, 20);
                winningText.text = $"      Restert to R                           Player {playerId + 1} Wins!";
            }

        }
        else
        {
            // 相手をリスポーン（すぐ）
            RespawnPlayers();
        }
    }
    void UpdateUI()
    {
        if (scoreText)
            scoreText.text = $"{scores[0]} - {scores[1]}";
    }

    public void OnRestert()
    {
        //Debug.Log("restert");
        if (isplaying) return;
            Time.timeScale = 1f; // ポーズ解除しておくと安全
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
    }

    
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        Debug.Log($"Player Joined: {playerInput.playerIndex + 1}");

        // 生成位置を調整（例：プレイヤー番号で左右に配置）
        if (playerInput.playerIndex == 0)
        {
            playerInput.transform.position = spawn0.position;
            playerInput.transform.rotation = spawn0.rotation;
            playerInput.name = "P1";
        }
        else if (playerInput.playerIndex == 1)
        {
            playerInput.transform.position = spawn1.position;
            playerInput.transform.rotation = spawn1.rotation;
            playerInput.name = "P2";
        }
    }

    // public void SpawnPlayer()
    // {
    //     Debug.Log("スポーン関数を起動");
    //     if (spawncount == 0)
    //     {

    //         // プレハブを生成
    //         GameObject obj = Instantiate(PlayerPrefab, spawn0.position, spawn0.rotation);

    //         // 例：生成したオブジェクトの名前を変える
    //         obj.name = "P1";
    //     }
    //     else if (spawncount == 1)
    //     {
    //         // プレハブを生成
    //         GameObject obj = Instantiate(PlayerPrefab, spawn1.position, spawn1.rotation);

    //         // 例：生成したオブジェクトの名前を変える
    //         obj.name = "P2";
    //     }


    //     spawncount++;
    // }

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
