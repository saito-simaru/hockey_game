using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class wall : MonoBehaviour
{
    // Start is called before the first frame update
    // Trigger に入った瞬間
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ball"))
        {
            Debug.Log($"[Enter] {other.name} が {gameObject.name} のトリガーに入った。");
        }
    }
}
