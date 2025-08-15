using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class secondPlayerGoal : MonoBehaviour
{
    public gamemanager gm;

    // Start is called before the first frame update
    // Trigger に入った瞬間
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ball"))
        {
            //０はP1、１はP2の得点を指す
            gm.AddPoint(1);
            Debug.Log($"[Enter] {other.name} が {gameObject.name} のトリガーに入った。");
        }
    }
}
