using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class clock : MonoBehaviour
{
    public static int TimeElapsed;
    private TMP_Text clocktext;
    int time = 0;

    void Start()
    {
        clocktext = GetComponent<TMP_Text>();
        StartCoroutine(ExampleCoroutine());
    }

    IEnumerator ExampleCoroutine()
    {
        while (true)
        {
            time++;
            TimeElapsed=time;
            int minutes = time / 60;
            int seconds = time % 60;
            clocktext.text = minutes + ":" + seconds.ToString("D2");
            yield return new WaitForSeconds(1);
        }
    }

    public void ResetTime() //publiczna funkcja do resetu zegara
    {
        time = 0;
        clocktext.text = "0:00";
    }
}
