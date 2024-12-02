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
    private Board_creator board; //odnoœnik do skryptu
    private banner_bar bar; //odnoœnik do skryptu
    private Button[] buttons, intension; //odnoœnik do skryptu
    private int episodeCount = 0,Actions=0, Streak =0; // Licznik epizodów
    int number = -1; //numer którym agent operuje na swojej tablicy
    private HashSet<string> invalidMoves = new HashSet<string>();

    void Awake()
    {
        Debug.unityLogger.logEnabled = false; //wy³¹cz logi na czas treningu agenta
    }

    //Inicjalizacja zachowañ agenta
    public override void Initialize()
    {
        board = FindObjectOfType<Board_creator>(); //przypisanie obiektu ze skryptem
        bar = FindObjectOfType<banner_bar>(); //przypisanie obiektu ze skryptem
        playerBoard = board.GetBoard(); //przepisz tablicê zagadki agentowi
        Board = board.GetTrueBoard(); //przepisz rozwi¹zanie do porównania przez agenta
        buttons = board.GetButtons(); //przepisz guziki na planszy do invoke
        intension = bar.GetIntensionButtons(); //przepisz guziki numerów do invoke
    }

    //po rozpoczêciu 1 sesji sudoku(nowa mapa)
    public override void OnEpisodeBegin()
    {
        episodeCount++; //do wyœwietlania epizodów
        if (episodeCount != 1)
        {
            board.ResetGame(); //jeœli to kolejny epizod, zresetuj planszê(unikamy resetowania wygenerowanej planszy na poczatku)
        }
        playerBoard = board.GetBoard(); //przypisujemy tablicê agenta
        Board = board.GetTrueBoard(); //przypisujemy pe³ne rozwi¹zanie do porównañ
        invalidMoves.Clear(); //czyœcimy b³êdne ruchy z poprzedniej planszy
        ShowPlayerBoard(); // do debugowania
        ShowTruePlayerBoard(); //do debugowania

    }


    //na co agent ma zwracaæ uwagê
    public override void CollectObservations(VectorSensor sensor)
    {
        int emptyFields = 0;
        // Przekazuj planszê gracza jako obserwacje (0 = puste pole, 1-9 = liczby)
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                sensor.AddObservation(playerBoard[i, j]);
                /*
                for (int num = 1; num <= 9; num++)
                {
                    bool isValid = IsMoveValid(i, j, num);
                    sensor.AddObservation(isValid ? 1 : 0);
                }*/
                //if (playerBoard[i, j] == 0) emptyFields++; //ile mamy pustych pól
                //sensor.AddObservation(emptyFields / 81.0f);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Akcja AI: wybór pola i liczby
        int fieldIndex = actions.DiscreteActions[0]; // Pole do modyfikacji (0-80)
        number = actions.DiscreteActions[1]; // Liczba do wstawienia (0-8)
        bool didMove = false; //sprawdzamy czy agent nie odpuœci³ tury
        //Debug.Log("Wybrany number: " + number);
        int row = fieldIndex % 9; 
        int col = fieldIndex / 9;
        Actions++; //w cmd odpowiednik Step
        string moveKey = $"{row},{col},{number}";
        //Debug.Log("Iloœæ akcji: " + Actions);
        // Jeœli pole jest puste, pozwól na ruch
        if (playerBoard[row, col] == 0)
        {
            //Debug.Log("Playerboard response : "+row + col + playerBoard[row, col]);
            if (intension[number].interactable)  // 'intension' to przyciski z banner_bar
            {
                intension[number].onClick.Invoke();
                AddReward(0.1f);
                didMove = true;
                //Debug.Log("Intension number: " + number);
            }
            number++;
            //Debug.Log("IsMoveValid number: " + number);
            if (IsMoveValid(row, col, number)) //czy ruch agenta jest zgodny z zasadami
            {
                buttons[fieldIndex].onClick.Invoke();
                playerBoard[row, col] = number; // Wstaw liczbê
                AddReward(4.0f+(0.5f*Streak)); // Nagroda za poprawny ruch
                if (Streak <= 5) 
                {
                    Streak++; //streak nagradza agenta za dokonanie poprawnych decyzji z rzêdu, co powinno go zachêciæ do dalszej eksploracji
                }
                didMove = true; 
                playerBoard = board.GetBoard();
                List<string> toRemove = new List<string>();
                foreach (var key in invalidMoves)
                {
                    if (key.StartsWith($"{row},{col},")) // Jeœli b³¹d dotyczy wype³nionego pola
                    {
                        toRemove.Add(key);
                    }
                }
                foreach (var key in toRemove)
                {
                    invalidMoves.Remove(key);
                }
                Debug.Log("Agent poprawnie oznaczy³ liczbê" +number + " w "+ row + col);
                ShowPlayerBoard();
            }
            else
            {
                buttons[fieldIndex].onClick.Invoke();
                if (invalidMoves.Contains(moveKey))
                {
                    AddReward(-1.5f); // Kara za powtórzenie b³êdnego ruchu
                }
                else
                {
                    invalidMoves.Add(moveKey); // Zapisz b³êdny ruch
                    AddReward(-1.0f); // Kara za nowy b³êdny ruch
                }
                Streak = 0;
                //Debug.Log($"Agent b³êdnie oznaczy³ liczbê {number} at field {buttons[fieldIndex]} (row: {row}, col: {col}).");
                Debug.Log("Agent niepoprawnie oznaczy³ liczbê");
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

    private bool IsMoveValid(int row, int col, int number) //sprawdzamy czy ruch jest poprawny
    {
        //Debug.Log("IsInRow: " + board.IsInRow(row, number - 1, Board));
        //Debug.Log("IsInColumn: " + board.IsInColumn(col, number - 1, Board));
        //Debug.Log("IsInSector: " + board.IsInSector(row, col, number - 1, Board));
        if (board.IsInRow(row, number,Board) ==false && board.IsInColumn(col, number, Board)==false && board.IsInSector(row, col, number, Board) ==false)
            return true;
        else
            return false;
    }

    private bool IsPuzzleSolved() //sprawdzamy czy agent ukoñczy³ planszê
    {
        // SprawdŸ, czy wszystkie pola s¹ wype³nione poprawnie
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (playerBoard[i, j] == 0)
                    return false;
            }
        }
        return true;
    }


    public override void Heuristic(in ActionBuffers actionsOut) //mo¿na u¿yæ do debugowania i sprawdzania rozwi¹zañ, na czas treningu powinno zostac puste
    {
       
    }


    ///debugowanie- poka¿ plansze gracza i poka¿ uzupe³nione sudoku
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

