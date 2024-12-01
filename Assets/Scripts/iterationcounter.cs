using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class iterationcounter : MonoBehaviour
{
    private TMP_Text iterationtext;
    // Start is called before the first frame update
    void Start()
    {
        iterationtext = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        int iteration = PlayerPrefs.GetInt("iter");
        iterationtext.text = ("Iteracja " + iteration).ToString();
    }
}
