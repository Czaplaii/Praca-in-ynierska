using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HorizontalArrayExample : MonoBehaviour
{
    [SerializeField] private int[] sudoku = new int[81]; // Jednowymiarowa tablica dla gotowego rozwi¹zania.
    [SerializeField] private int[] puzzle = new int[81]; // Jednowymiarowa tablica dla zagadki.

    public int GetSudokuValue(int x, int y)
    {
        return sudoku[y * 9 + x];
    }

    public void SetSudokuValue(int x, int y, int value)
    {
        sudoku[y * 9 + x] = value;
    }

    public int GetPuzzleValue(int x, int y)
    {
        return puzzle[y * 9 + x];
    }

    public void SetPuzzleValue(int x, int y, int value)
    {
        puzzle[y * 9 + x] = value;
    }
}
