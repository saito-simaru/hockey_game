using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements.Experimental;

public class UImanager : MonoBehaviour
{
    public TextMeshProUGUI scoretext;
    public int maxpoint = 5;
    public gamemanager gamemanager;
    public static UImanager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        scoretext.text = "5";
    }
    //入力を受けて最高点を変動させる
    public void OnMovecursor(InputAction.CallbackContext ctx)
    {
        //入力を受け始めた時以外は無効
        if (!ctx.performed) return;

        float value = ctx.ReadValue<float>();

        int changeamount = (value < 0) ? -1 : 1;

        if ((maxpoint <= 1 && changeamount < 0) || (maxpoint >= 99 && changeamount > 0)) return;

        maxpoint += changeamount;
        scoretext.text = $"{maxpoint}";
    }

    //gmで管理する変数に最高点を代入
    public void OnDetect(int playerID)
    {
        gamemanager.Setmaxpoint(maxpoint, playerID);
    }
}
