using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Collections.Generic;

public class SudokuAgent : Agent
{
    public int[,] Board = new int[9, 9]; // Pe³ne rozwi¹zanie sudoku (ukryte przed AI)
    private int[,] playerBoard = new int[9, 9]; // Plansza gracza (to widzi i modyfikuje AI)
    private Board_creator board;
    private banner_bar bar;
    private Button[] buttons, intension;
    private int episodeCount = 0,Actions=0, Streak =0; // Licznik epizodów
    int number = -1;



    public override void Initialize()
    {
        board = FindObjectOfType<Board_creator>();
        bar = FindObjectOfType<banner_bar>();
        playerBoard = board.GetBoard();
        Board = board.GetTrueBoard();
        buttons = board.GetButtons();
        intension = bar.GetIntensionButtons();
    }


    public override void OnEpisodeBegin()
    {
        episodeCount++;
        if (episodeCount != 1)
        {
            board.ResetGame();
        }
        playerBoard = board.GetBoard();
        Board = board.GetTrueBoard();
        ShowPlayerBoard();
        ShowTruePlayerBoard();

    }

    /*public override void OnEpisodeEnd()
    {
        // Rejestruj liczbê poprawnych odpowiedzi
        Academy.Instance.StatsRecorder.Add("CorrectActions", correctActions);

        // Rejestruj liczbê b³êdnych odpowiedzi
        Academy.Instance.StatsRecorder.Add("Errors", incorrectActions);

        // Debugowanie w konsoli
        Debug.Log($"Epizod zakoñczony: CorrectMoves = {correctActions}, Errors = {incorrectActions}");

        // Zapisz liczbê epizodów do statystyk w Unity (opcjonalne)
        Academy.Instance.StatsRecorder.Add("episodes", episodeCount);
        // Wyœwietl liczbê zakoñczonych epizodów (dla debugowania)
        Debug.Log($"Epizod {episodeCount} rozpoczêty.");

        // Resetowanie zmiennych na nowy epizod
        correctActions = 0;
        incorrectActions = 0;
    }*/


    public override void CollectObservations(VectorSensor sensor)
    {
        // Przekazuj planszê gracza jako obserwacje (0 = puste pole, 1-9 = liczby)
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                sensor.AddObservation(playerBoard[i, j]);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Akcja AI: wybór pola i liczby
        int fieldIndex = actions.DiscreteActions[0]; // Pole do modyfikacji (0-80)
        number = actions.DiscreteActions[1]; // Liczba do wstawienia (1-9)
        bool didMove = false;

        int row = fieldIndex % 9;
        int col = fieldIndex / 9;
        Actions++;
        Debug.Log("Iloœæ akcji: " + Actions);
        // Jeœli pole jest puste, pozwól na ruch
        if (playerBoard[row, col] == 0)
        {
            Debug.Log("Playerboard response : "+row + col + playerBoard[row, col]);
            if (intension[number].interactable)  // 'intension' to przyciski z banner_bar
            {
                intension[number].onClick.Invoke();
                number++;
                AddReward(0.1f);

                if (IsMoveValid(row, col, number))
                {
                    buttons[fieldIndex].onClick.Invoke();
                    playerBoard[row, col] = number; // Wstaw liczbê
                    AddReward(4.0f+(0.5f*Streak)); // Nagroda za poprawny ruch
                    //Debug.Log($"Agent poprawnie oznaczy³ liczbê {number},field at field {buttons[fieldIndex]} (row: {row}, col: {col}).");
                    if (Streak <= 5)
                    {
                        Streak++;
                    }
                    didMove = true;
                    playerBoard = board.GetBoard();
                    Debug.Log("Agent poprawnie oznaczy³ liczbê");
                    ShowPlayerBoard();
                }
                else
                {
                    buttons[fieldIndex].onClick.Invoke();
                    AddReward(-1.0f); // Kara za b³êdny ruch
                    Streak = 0;
                    //Debug.Log($"Agent b³êdnie oznaczy³ liczbê {number} at field {buttons[fieldIndex]} (row: {row}, col: {col}).");
                    Debug.Log("Agent niepoprawnie oznaczy³ liczbê");
                    didMove = true;
                }
            }
            else
            { 
                AddReward(-1f); // Kara za próbê wybrania nieaktywnego przycisku
                Debug.Log($"Agent próbowa³ wybraæ nieaktywn¹ liczbê.");
                didMove = true;
            }
        }
        else
        {
            Debug.Log($"Agent wybra³ oznaczone pole");
            AddReward(-2f); // Kara za próbê zmiany zajêtego pola
            didMove = true;
        }

        // SprawdŸ, czy gra zosta³a ukoñczona
        if (IsPuzzleSolved())
        {
            AddReward(50.0f); // Nagroda za ukoñczenie planszy
            didMove= true;
            EndEpisode();
        }

        // Jeœli agent nie wykona³ ruchu, na³ó¿ dodatkow¹ karê
        if (!didMove)
        {
            AddReward(-3f); // Kara za brak ruchu
        }
        

    }

    private bool IsMoveValid(int row, int col, int number)
    {
        //Debug.Log("IsInRow: " + board.IsInRow(row, number - 1, Board));
        //Debug.Log("IsInColumn: " + board.IsInColumn(col, number - 1, Board));
        //Debug.Log("IsInSector: " + board.IsInSector(row, col, number - 1, Board));
        if (board.IsInRow(row, number,Board) ==false && board.IsInColumn(col, number, Board)==false && board.IsInSector(row, col, number, Board) ==false)
            return true;
        else
            return false;
    }

    private bool IsPuzzleSolved()
    {
        // SprawdŸ, czy wszystkie pola s¹ wype³nione poprawnie
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (playerBoard[i, j] == 0 || !IsMoveValid(i, j, playerBoard[i, j]))
                    return false;
            }
        }

        return true;
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
       
    }


    ///debug
    void ShowTruePlayerBoard()
    {
        string boardRepresentation2 = ""; // Przechowuje reprezentacjê planszy jako tekst

        for (int i = 0; i< 9; i++)
        {
            for (int j = 0; j< 9; j++)
            {
                boardRepresentation2 += Board[j, i] + " "; // Dodajemy liczbê i spacjê dla przejrzystoœci
            }
            boardRepresentation2 += "\n"; // Nowa linia po ka¿dym wierszu
        }

        Debug.Log(boardRepresentation2); // Wypisujemy ca³¹ planszê jako tekst
    }

    void ShowPlayerBoard()
    {
        string boardRepresentation = ""; // Przechowuje reprezentacjê planszy jako tekst

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                boardRepresentation += playerBoard[j, i] + " "; // Dodajemy liczbê i spacjê dla przejrzystoœci
            }
            boardRepresentation += "\n"; // Nowa linia po ka¿dym wierszu
        }

        Debug.Log(boardRepresentation); // Wypisujemy ca³¹ planszê jako tekst
    }
}

