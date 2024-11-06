using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class clock : MonoBehaviour
{
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
            int minutes = time / 60;
            int seconds = time % 60;
            clocktext.text = minutes + ":" + seconds.ToString("D2");
            yield return new WaitForSeconds(1);
        }
    }
}
