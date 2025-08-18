using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ball : MonoBehaviour
{
    public Rigidbody2D rb;
    void Start()
    {
        StartCoroutine(pushcomment());
        Debug.Log("yonnda");
    }

    IEnumerator pushcomment()
    {
        while(true)
        {
            //Debug.Log(rb.velocity);
            yield return new WaitForSeconds(0.1f); // 0.5秒待つ
        }

    }


    // public void Deletemyself()
    // {
    //     // "ball" または "ball1" "ball2" など、部分一致で検索したい場合
    //     GameObject[] allObjects = FindObjectsOfType<GameObject>();

    //     foreach (var obj in allObjects)
    //     {  
    //         if (obj.name.StartsWith("ball")) // 名前が"ball"で始まるもの   
    //         {  
    //             Destroy(obj); // シーン上の実体を破棄  
    //             Debug.Log($"{obj.name} を削除しました");   
    //         }  
    //     }  
    // }

}
