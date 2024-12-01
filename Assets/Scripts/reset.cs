using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class reset : MonoBehaviour
{
    public void ResetCurrentScene()
    {
        string name= SceneManager.GetActiveScene().name;
        // Za³aduj scenê od nowa
        SceneManager.LoadScene(name);
    }
}
