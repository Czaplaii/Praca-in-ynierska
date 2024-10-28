using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Board_creator : MonoBehaviour
{
    [SerializeField] int[,] Board = new int[9, 9];
    [SerializeField] Button[] BoardButtons;
    [SerializeField] List<int> numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    private static readonly System.Random rand = new System.Random();


    void Start()
    {
        BoardInit(Board);
        //ShuffleList(numbers);
        CreateBoard();
        //StartCoroutine(CreateBoard(numbers[0], 0, 0));
        PuzzleMaker();
        StartButtonDeactivate();
    }

    /* metoda dzia³a, ale wykonuje siê bardzo d³ugo (wiêcej ni¿ 10 minut na kompilacjê)

    private IEnumerator CreateBoard(int number, int placedCounter, int index)            
    {
        if (index >= numbers.Count) yield break;

        if (placedCounter == 9)
        {
            yield return StartCoroutine(CreateBoard(numbers[index + 1], 0, index + 1));
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

                        yield return StartCoroutine(CreateBoard(number, placedCounter, index));

                        if (placedCounter < 9)
                        {
                            Board[randomRow, randomColumn] = 0;
                            Przypisz(randomRow, randomColumn, 0);
                            placedCounter--;
                        }
                    }
                }
                yield return null;
            }
        }
    }
    */

    void CreateBoard()
    {
        ShuffleList(numbers); // mieszamy numery
        BoardFiller(0, 0);
    }

    bool BoardFiller(int row, int column)
    {
        if (row == 9) // czy skoñczyliœmy ostatni wiersz
        {
            return true; // Plansza zosta³a wype³niona
        }

        if (column == 9)
        {
            return BoardFiller(row + 1, 0); // skoñczyliœmy ostatni¹ kolumnê, przechodzimy do nastêpnego wiersza
        }

        // jeœli komórka jest pe³na, przechodzimy do kolejnej
        if (Board[row, column] != 0)
        {
            return BoardFiller(row, column + 1);
        }

        if (Board[row, column] == 0) //jeœli komórka jest pusta
        {
            foreach (var number in numbers) //iteracja po liœcie numerów
            {
                if (!IsInRow(row, number) && !IsInColumn(column, number) && !IsInSector(row, column, number)) //czy zgodna z zasadami
                {
                    Board[row, column] = number; // Umieszczamy liczbê
                    //Przypisz(row, column, number); // Aktualizujemy UI
                    if (BoardFiller(row, column + 1)) // Jeœli nie mo¿na dalej wype³niæ
                    {
                        return true;
                    }
                    Board[row, column] = 0;
                    //Przypisz(row, column, 0);
                }
            }
        }
        return false;
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

    void Przypisz(int row, int column, int answer) //wstêpny kod do zapisywania 
    {

        int buttonIndex = (9 * row) + column;
        TMP_Text buttonText = BoardButtons[buttonIndex].GetComponentInChildren<TMP_Text>();
        if (answer != 0)
            buttonText.text = answer.ToString();
        else buttonText.text = " ";
    }

    //¿eby tablice sudoku jak najmniej siê powtarza³y, mieszamy numery
    void ShuffleList(List<int> list) //metoda Fisher-Yates
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rand.Next(0, i + 1);
            int temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    bool IsBoardFull()
    {
        for (int i = 0; i < Board.GetLength(0); i++)
        {
            for (int j = 0; j < Board.GetLength(1); j++)
            {
                if (Board[i, j] == 0)
                {
                    return false;
                }
            }
        }
        return true;
    }

    void PuzzleMaker() //wersja wstêpna z pokazywaniem komórek na bazie iloœci
    {
        //int showeasy = Random.Range(41, 51); //³atwa plansza
        int showmedium = Random.Range(31, 41); //œrednia plansza
        //int showhard = Random.Range(20, 30); //trudna plansza
        for (int i = 0;i < showmedium; i++)
        {
            int k = Random.Range(0,81);
            Debug.Log(showmedium);
            TMP_Text buttonText = BoardButtons[k].GetComponentInChildren<TMP_Text>();
            if (buttonText.text == " ")
            {
                buttonText.text = (Board[k % 9, k / 9]).ToString();
            }
            else
            {
                i--;
            }
        }
    }

    void StartButtonDeactivate()
    {
        for (int i = 0; i < BoardButtons.GetLength(0); i++)
        {
                TMP_Text buttonText = BoardButtons[i].GetComponentInChildren<TMP_Text>();
                if (buttonText.text != " ")
                    BoardButtons[i].interactable = false;
        }
    }

}
