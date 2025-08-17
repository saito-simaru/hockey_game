using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Titlescene : MonoBehaviour
{

    public void OnRestart()
    {
        SceneManager.LoadScene("SampleScene");
    }

}
