using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Board_creator : MonoBehaviour
{
    [SerializeField] int[,] Board = new int[9, 9];
    [SerializeField] Button[] BoardButtons;

    void Start()
    {
        StartCoroutine(CreateBoard());
    }

    private IEnumerator CreateBoard()
    {
        BoardInit(Board);
        for(int answer = 1; answer<=9; answer++)
        {
            int placedcounter = 0;
            while (placedcounter <9) // ile zmieœci siê konkretnych liczb na planszy
            {
                bool taken = false;
                while (!taken && placedcounter < 9)
                {
                    int RandomRow = Random.Range(0, 9);
                    int RandomColumn = Random.Range(0, 9);

                    if (Board[RandomRow, RandomColumn] == 0)
                    {
                        if (!IsInRow(RandomRow, answer) && !IsInColumn(RandomColumn, answer) && !IsInSector(RandomRow, RandomColumn, answer))
                        {
                            Board[RandomRow, RandomColumn] = answer;

                            int buttonIndex = (9 * RandomRow) + RandomColumn;
                            TMP_Text buttonText = BoardButtons[buttonIndex].GetComponentInChildren<TMP_Text>();
                            buttonText.text = answer.ToString();

                            taken = true;
                            placedcounter++;
                        }
                    }
                    else
                        continue;  
                    yield return null;
                }
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
}
