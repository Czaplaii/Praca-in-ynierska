using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class banner_bar : MonoBehaviour
{
    [SerializeField] Button[] Buttons;

    void Start()
    {
        PlayerPrefs.SetInt("number", -1);
        for (int i = 0; i < Buttons.Length; i++)
        {
            int index = i;
            Buttons[i].onClick.AddListener(() => OnButtonClick(index));
        }

        int last = PlayerPrefs.GetInt("number");
    }

    void OnButtonClick(int index)
    {
        int last = PlayerPrefs.GetInt("number", index);
        if (last != -1)
        {
            Buttons[last].interactable = true;
        }
        Buttons[index].interactable = false;
        PlayerPrefs.SetInt("number", index);
    }
}
