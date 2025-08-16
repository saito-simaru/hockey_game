using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class goal : MonoBehaviour
{
    public gamemanager gm;
    public GameObject ball;
    private ball ballscript;
    private int ballcount = 0;
    private Vector3 spawnpoint = new Vector3(-0.5f, -3.5f, 0);

    // Start is called before the first frame update
    // Trigger に入った瞬間

    void Start()
    {
        ballscript = ball.GetComponent<ball>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ball"))
        {
            //０はP1、１はP2の得点を指す
            if (gameObject.transform.position.y > 0)
            {
                gm.AddPoint(0);
            }
            else
            {
                gm.AddPoint(1);
            }


            //Debug.Log($"[Enter] {other.name} が {gameObject.name} のトリガーに入った。");

            Deletemyself();

            RespawnBall();
        }
    }

    public void RespawnBall()
    {
        ballcount++;

        GameObject obj = Instantiate(ball, spawnpoint, ball.transform.rotation);

        obj.name = "ball";
    }
    
    public void Deletemyself()
    {
        // "ball" または "ball1" "ball2" など、部分一致で検索したい場合
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (var obj in allObjects)
        {
            if (obj.name.StartsWith("ball")) // 名前が"ball"で始まるもの
            {
                Destroy(obj); // シーン上の実体を破棄
                //Debug.Log($"{obj.name} を削除しました");
            }
        }
    }
}
