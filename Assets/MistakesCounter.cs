using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MistakesCounter : MonoBehaviour
{
    private TMP_Text mistaketext;
    // Start is called before the first frame update
    void Start()
    {
        mistaketext = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        //int mistakes = PlayerPrefs.GetInt(mistake);
        //mistaketext.text = ("B³êdy" + mistakes).ToString();
    }
}
