using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Collections.Generic;

public class SudokuAgent : Agent
{
    public int[,] Board = new int[9, 9]; // Pe�ne rozwi�zanie sudoku (ukryte przed AI)
    private int[,] playerBoard = new int[9, 9]; // Plansza gracza (to widzi i modyfikuje AI)
    private Board_creator board;
    private banner_bar bar;
    private Button[] buttons, intension;
    private int episodeCount = 0,Actions=0, Streak =0; // Licznik epizod�w
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
        // Rejestruj liczb� poprawnych odpowiedzi
        Academy.Instance.StatsRecorder.Add("CorrectActions", correctActions);

        // Rejestruj liczb� b��dnych odpowiedzi
        Academy.Instance.StatsRecorder.Add("Errors", incorrectActions);

        // Debugowanie w konsoli
        Debug.Log($"Epizod zako�czony: CorrectMoves = {correctActions}, Errors = {incorrectActions}");

        // Zapisz liczb� epizod�w do statystyk w Unity (opcjonalne)
        Academy.Instance.StatsRecorder.Add("episodes", episodeCount);
        // Wy�wietl liczb� zako�czonych epizod�w (dla debugowania)
        Debug.Log($"Epizod {episodeCount} rozpocz�ty.");

        // Resetowanie zmiennych na nowy epizod
        correctActions = 0;
        incorrectActions = 0;
    }*/


    public override void CollectObservations(VectorSensor sensor)
    {
        // Przekazuj plansz� gracza jako obserwacje (0 = puste pole, 1-9 = liczby)
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
        // Akcja AI: wyb�r pola i liczby
        int fieldIndex = actions.DiscreteActions[0]; // Pole do modyfikacji (0-80)
        number = actions.DiscreteActions[1]; // Liczba do wstawienia (1-9)
        bool didMove = false;

        int row = fieldIndex % 9;
        int col = fieldIndex / 9;
        Actions++;
        Debug.Log("Ilo�� akcji: " + Actions);
        // Je�li pole jest puste, pozw�l na ruch
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
                    playerBoard[row, col] = number; // Wstaw liczb�
                    AddReward(4.0f+(0.5f*Streak)); // Nagroda za poprawny ruch
                    //Debug.Log($"Agent poprawnie oznaczy� liczb� {number},field at field {buttons[fieldIndex]} (row: {row}, col: {col}).");
                    if (Streak <= 5)
                    {
                        Streak++;
                    }
                    didMove = true;
                    playerBoard = board.GetBoard();
                    Debug.Log("Agent poprawnie oznaczy� liczb�");
                    ShowPlayerBoard();
                }
                else
                {
                    buttons[fieldIndex].onClick.Invoke();
                    AddReward(-1.0f); // Kara za b��dny ruch
                    Streak = 0;
                    //Debug.Log($"Agent b��dnie oznaczy� liczb� {number} at field {buttons[fieldIndex]} (row: {row}, col: {col}).");
                    Debug.Log("Agent niepoprawnie oznaczy� liczb�");
                    didMove = true;
                }
            }
            else
            { 
                AddReward(-1f); // Kara za pr�b� wybrania nieaktywnego przycisku
                Debug.Log($"Agent pr�bowa� wybra� nieaktywn� liczb�.");
                didMove = true;
            }
        }
        else
        {
            Debug.Log($"Agent wybra� oznaczone pole");
            AddReward(-2f); // Kara za pr�b� zmiany zaj�tego pola
            didMove = true;
        }

        // Sprawd�, czy gra zosta�a uko�czona
        if (IsPuzzleSolved())
        {
            AddReward(50.0f); // Nagroda za uko�czenie planszy
            didMove= true;
            EndEpisode();
        }

        // Je�li agent nie wykona� ruchu, na�� dodatkow� kar�
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
        // Sprawd�, czy wszystkie pola s� wype�nione poprawnie
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
        string boardRepresentation2 = ""; // Przechowuje reprezentacj� planszy jako tekst

        for (int i = 0; i< 9; i++)
        {
            for (int j = 0; j< 9; j++)
            {
                boardRepresentation2 += Board[j, i] + " "; // Dodajemy liczb� i spacj� dla przejrzysto�ci
            }
            boardRepresentation2 += "\n"; // Nowa linia po ka�dym wierszu
        }

        Debug.Log(boardRepresentation2); // Wypisujemy ca�� plansz� jako tekst
    }

    void ShowPlayerBoard()
    {
        string boardRepresentation = ""; // Przechowuje reprezentacj� planszy jako tekst

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                boardRepresentation += playerBoard[j, i] + " "; // Dodajemy liczb� i spacj� dla przejrzysto�ci
            }
            boardRepresentation += "\n"; // Nowa linia po ka�dym wierszu
        }

        Debug.Log(boardRepresentation); // Wypisujemy ca�� plansz� jako tekst
    }
}

