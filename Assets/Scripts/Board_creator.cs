using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Board_creator : MonoBehaviour
{
    [SerializeField] int[,] Board = new int[9, 9];
    [SerializeField] Button[] BoardButtons;

    void Start()
    {
        BoardInit(Board);
        BoardCreate(Board);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void BoardInit(int[,] tab)
    {
        for (int i = 0; i < Board.GetLength(0); i++)
        {
            for (int j = 0; j < Board.GetLength(1); j++)
            {
                Board[i, j] = 0;
                //wype³nianie tablicy
                int buttonIndex = (9 * i) + j;
                TMP_Text buttonText = BoardButtons[buttonIndex].GetComponentInChildren<TMP_Text>();
                //buttonText.text = Board[i, j].ToString();
                buttonText.text = " ";
                // Wyœwietlenie wartoœci
                Debug.Log("Wartoœæ w Board[" + i + "," + j + "] = " + Board[i, j]);
                Debug.Log("Tablica wype³niona zerami");
            }
        }
    }

    void BoardCreate(int[,] tab)
    {
        int RandomRow = Random.Range(0, 9);
        int RandomColumn = Random.Range(0, 9);
        int RandomAnswer = Random.Range(1, 10);
        if (!IsInRow(RandomRow, RandomAnswer) && !IsInColumn(RandomColumn,RandomAnswer) && !IsInSector(RandomRow,RandomColumn,RandomAnswer))
        {
            Board[RandomRow, RandomColumn] = RandomAnswer;
            Debug.Log("posz³o");
        }
        Debug.Log("Wiersz " + (RandomRow+1));
        Debug.Log("Kolumna " + (RandomColumn+1));
        Debug.Log("Numer " + RandomAnswer);
        int buttonIndex = (9 * RandomRow ) + RandomColumn;
        TMP_Text buttonText = BoardButtons[buttonIndex].GetComponentInChildren<TMP_Text>();
        buttonText.text = RandomAnswer.ToString();
    }

    bool IsInRow(int row, int number)
    {
        for (int i = 0; i < Board.GetLength(1); i++)
        {
            if(number == Board[i, row])
            {
                return true;
            }
        }
        return false;
    }

    bool IsInColumn(int column, int number)
    {
        for (int j = 0; j < Board.GetLength(1); j++)
        {
            if (number == Board[column,j])
            {
                return true;
            }
        }
        return false;
    }

    bool IsInSector(int column, int row, int number)
    {
        for (int i = 0; i < Board.GetLength(0); i++)
        {
            for (int j = 0; j < Board.GetLength(1); j++)
            {
                if((row/3)*3+(column/3) == (i / 3) * 3 + (j / 3))
                {
                    if(number == Board[i,j])
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

}
