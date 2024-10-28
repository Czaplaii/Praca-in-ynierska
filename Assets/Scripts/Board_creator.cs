using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Board_creator : MonoBehaviour
{
    [SerializeField] int[,] Board = new int[9, 9];
    [SerializeField] Button[] BoardButtons;
    [SerializeField] int lastrow, lastcol;
    [SerializeField] List<int> numbers = new List<int>{1, 2, 3, 4, 5, 6, 7, 8, 9};

    void Start()
    {
        BoardInit(Board);
        StartCoroutine(CreateBoard(1, 0));

        for(int i = 0; i<numbers.Count; i++) 
        {
            Debug.Log(numbers[i]);
        }
    }

    /*private IEnumerator CreateBoard()
    {
        for(int answer = 1; answer<=9; answer++)
        {
            int placedcounter = 0;
            while (placedcounter <9) // ile zmieœci siê konkretnych liczb na planszy
            {
                while (placedcounter < 9)
                {
                    int RandomRow = Random.Range(0, 9);
                    int RandomColumn = Random.Range(0, 9);
                    if (Board[RandomRow, RandomColumn] == 0)
                    {
                        if (!IsInRow(RandomRow, answer) && !IsInColumn(RandomColumn, answer) && !IsInSector(RandomRow, RandomColumn, answer))
                        {
                            Board[RandomRow, RandomColumn] = answer;
                            Przypisz(RandomRow, RandomColumn, answer);
                            lastrow = RandomRow;
                            lastcol = RandomColumn;
                            placedcounter++;
                        }
                    }
                    else
                        yield return null;
                }
            }
        }
    }*/

    private IEnumerator CreateBoard(int number, int placedCounter)
    {
        if (placedCounter == 9)
        {
            if (number < 9)
            {
                yield return StartCoroutine(CreateBoard(number + 1, 0));
            }
            else
            {
                yield break;
            }
        }
        else
        {
            for (int i = 0; i < 81; i++)
            {
                int randomRow = Random.Range(0, 9);
                int randomColumn = Random.Range(0, 9);

                if (Board[randomRow, randomColumn] == 0)
                {
                    if (!IsInRow(randomRow, number) && !IsInColumn(randomColumn, number) && !IsInSector(randomRow, randomColumn, number))
                    {
                        Board[randomRow, randomColumn] = number;
                        Przypisz(randomRow, randomColumn, number);
                        placedCounter++;

                        yield return StartCoroutine(CreateBoard(number, placedCounter));

                        Board[randomRow, randomColumn] = 0;
                        Przypisz(randomRow, randomColumn, 0);
                        placedCounter--;
                    }
                }
                yield return null;
            }
        }
    }

    void BoardInit(int[,] tab) //wype³niamy tablice zerami
    {
        for (int i = 0; i < Board.GetLength(0); i++)
        {
            for (int j = 0; j < Board.GetLength(1); j++)
            {
                Board[i, j] = 0;
                int buttonIndex = (9 * i) + j;
                TMP_Text buttonText = BoardButtons[buttonIndex].GetComponentInChildren<TMP_Text>();
                buttonText.text = " ";
            }
        }
    }

    bool IsInRow(int row, int number) // czy wyst¹pi³a w wierszu?
    {
        for (int i = 0; i < Board.GetLength(1); i++)
        {
            if (Board[row, i] == number)
            {
                return true;
            }
        }
        return false;
    }

    bool IsInColumn(int column, int number)// czy wyst¹pi³a w kolumnie?
    {
        for (int i = 0; i < Board.GetLength(0); i++)
        {
            if (Board[i, column] == number)
            {
                return true;
            }
        }
        return false;
    }

    bool IsInSector(int row, int column, int number) //czy wyst¹pi³a w kwadracie 9x9
    {
        int sectorRowStart = (row / 3) * 3;
        int sectorColumnStart = (column / 3) * 3;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (Board[sectorRowStart + i, sectorColumnStart + j] == number)
                {
                    return true;
                }
            }
        }
        return false;
    }

    void Przypisz(int row,int column, int answer)
    {
        int buttonIndex = (9 * row) + column;
        TMP_Text buttonText = BoardButtons[buttonIndex].GetComponentInChildren<TMP_Text>();
        if (answer != 0)
            buttonText.text = answer.ToString();
        else buttonText.text = " ";
    }

    void ShuffleList()//Fisher-Yates shuffle
    {
        for(int i = numbers.Count-1; i < 1; i--) 
        { 
            int k=Random.Range(0, i); 
        }
    }

}
