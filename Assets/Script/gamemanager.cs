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
    private Vector3 spawnpoint0 = new Vector3(-0.5f, -3.5f, 0);
    private bool isplaying = true;
    private PlayerInputManager pim;
    [Header("PlayerPrefab")]
    public GameObject PlayerPrefab;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public  TextMeshProUGUI winningText;
    public TextMeshProUGUI matchpointText;
    public Canvas startcanvas;

    [Header("Respawn")]
    public Transform spawn0;
    public Transform spawn1;

    public static gamemanager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        pim = GetComponent<PlayerInputManager>();
        Debug.Log($"[GM Awake] name={name}, id={GetInstanceID()}, isplaying={isplaying}");
    }

    void Start()
    {
        Time.timeScale = 0;
        Application.targetFrameRate = 60;
        UpdateUI();
        
        winningText.gameObject.SetActive(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"シーンロード完了: {scene.name}");


        // GameObject spawn0obj = GameObject.Find("spawnpos1");
        // spawn0 = spawn0obj.transform;
        // GameObject spawn1obj = GameObject.Find("spawnpos2");
        // spawn1 = spawn1obj.transform;

        // GameObject scoretext = GameObject.Find("score");
        // scoreText = scoretext.GetComponent<TextMeshProUGUI>();
        // GameObject winningtext = GameObject.Find("winningText");
        // winningText = winningtext.GetComponent<TextMeshProUGUI>();

        

    }

    public void Setmaxpoint(int maxpoint)
    {

        if (isplaying == false)
        {
            // Debug.Log(isplaying);
            // Time.timeScale = 1f; // ポーズ解除しておくと安全
            // var scene = SceneManager.GetActiveScene();
            // SceneManager.LoadScene(scene.buildIndex);
            Debug.Log("rsetert");
            startcanvas.gameObject.SetActive(true);
            scores[0] = 0;
            scores[1] = 0;
            matchpointText.text = null;
            winningText.gameObject.SetActive(false);
            UpdateUI();
            isplaying = true;
        }
        else if (isplaying == true)
        {
            Debug.Log("setpoint(true)");
            maxScore = maxpoint;
            startcanvas.gameObject.SetActive(false);
            Time.timeScale = 1f;
        }

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
                winningText.color = new Color32(70, 190, 255, 255);
                winningText.text = $"   Plyre {playerId + 1} のかち!                       リプレイ：X";
            }
            else
            {
                //オレンジ色
                winningText.color = new Color32(255, 150, 20, 255);
                winningText.text = $"     リプレイ：X                      Player {playerId + 1} のかち!";
            }

        }
        else if (scores[0] == maxScore - 1 || scores[1] == maxScore - 1)
        {
            // 相手をリスポーン（すぐ）
            string p1 = (scores[0] == maxScore - 1) ? "P1" : null;
            string p2 = (scores[1] == maxScore - 1) ? "P2" : null;
            matchpointText.text = $"マッチポイント\n{p1}\n{p2}";
            Debug.Log("マッチポイント");
        }
        RespawnPlayers();
    }
    void UpdateUI()
    {
        if (scoreText)
            scoreText.text = $"{scores[0]} - {scores[1]}";
    }

    public void OnRestart()
    {
        // Debug.Log($"[GM OnRestert] name={name}, id={GetInstanceID()}, isplaying={isplaying}");
        // Debug.Log(isplaying);
        //Debug.Log("restert");
        // if (isplaying == false)
        // {
        //     // Debug.Log(isplaying);
        //     // Time.timeScale = 1f; // ポーズ解除しておくと安全
        //     // var scene = SceneManager.GetActiveScene();
        //     // SceneManager.LoadScene(scene.buildIndex);
        //     Debug.Log("rsetert");
        //     startcanvas.gameObject.SetActive(true);
        //     scores[0] = 0;
        //     scores[1] = 0;
        //     winningText.gameObject.SetActive(false);
        //     UpdateUI();
            
        // }


    }

    
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        // タグが "Player" でなければ無視（gamemanagerなど）
        if (!playerInput.CompareTag("player"))
        {
            Debug.Log($"非プレイヤーのPlayerInputを無視: {playerInput.name}");
            return;
        }

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
            goalscript.RespawnBall(spawnpoint0);
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
