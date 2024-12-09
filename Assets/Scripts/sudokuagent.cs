using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Collections.Generic;
using Unity.Barracuda;

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
                if (playerBoard[i, j] == 0) 
                emptyFields++; //ile mamy pustych pól
            }
        }
        sensor.AddObservation(emptyFields);

        for (int i = 0; i < 9; i++)
        {
            int rowSum = 0;
            int colSum = 0;
            for (int j = 0; j < 9; j++)
            {
                rowSum += playerBoard[i, j];  // Suma wiersza
                colSum += playerBoard[j, i];  // Suma kolumny
            }
            sensor.AddObservation(rowSum);
            sensor.AddObservation(colSum);
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
                if (IsColFull(row))
                {
                    Debug.Log("Kolumna jest pe³na");
                    AddReward(20f);
                }
                if (IsRowFull(col))
                {
                    Debug.Log("Pe³ny Rz¹d");
                    AddReward(20f);
                }
                if (IsSectorFull(row, col))
                {
                    Debug.Log("Sektor jest skoñczony");
                    AddReward(20f);
                }
                didMove = true; 
                playerBoard = board.GetBoard();

                RemoveInvalidMoves(row, col); //funkcja usuwaj¹ca z hashsetu informacje o b³êdach z tej komórki

                Debug.Log("Agent poprawnie oznaczy³ liczbê" +number + " w "+ row + col);
                //ShowPlayerBoard();
            }
            else
            {
                buttons[fieldIndex].onClick.Invoke();
                if (invalidMoves.Contains(moveKey))
                {
                    AddReward(-2f); // Kara za powtórzenie b³êdnego ruchu
                }
                else
                {
                    invalidMoves.Add(moveKey); // Zapisz b³êdny ruch
                    AddReward(-1f); // Kara za nowy b³êdny ruch
                }
                Streak = 0;
                //Debug.Log("Agent niepoprawnie oznaczy³ liczbê, poprawna odpowiedŸ dla" +row + col+" = " + Board[row,col]);
                didMove = true;
            }
        }
        else
        {
            //Debug.Log($"Agent wybra³ oznaczone pole" + row + " " +col + " = " + Board[row,col]);
            AddReward(-4f); // Kara za próbê zmiany zajêtego pola
            didMove = true;
        }

        // SprawdŸ, czy gra zosta³a ukoñczona
        if (IsPuzzleSolved())
        {
            AddReward(150.0f); // Nagroda za ukoñczenie planszy
            didMove= true;
            EndEpisode();
        }

        // Jeœli agent nie wykona³ ruchu, na³ó¿ dodatkow¹ karê
        if (!didMove)
        {
            AddReward(-5f); // Kara za brak ruchu
        }
        // Iteracja po elementach
        foreach (var name in invalidMoves)
        {
            Debug.Log("HashSet: " + name);
        }

    }

    private bool IsMoveValid(int row, int col, int number) //sprawdzamy czy ruch jest poprawny
    {
        //Debug.Log("IsInRow: " + board.IsInRow(row, number - 1, Board));
        //Debug.Log("IsInColumn: " + board.IsInColumn(col, number - 1, Board));
        //Debug.Log("IsInSector: " + board.IsInSector(row, col, number - 1, Board));
        /*if (board.IsInRow(row, number,Board) ==false && board.IsInColumn(col, number, Board)==false && board.IsInSector(row, col, number, Board) ==false)
            return true;
        else
            return false;
        */
        if (Board[row, col] == number)
        {
            return true;
        }
        else
        {
            return false;
        }
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
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Random.Range(0,8);
        discreteActions[1] = Random.Range(0,8);
        // WYPISZ CA£¥ KOLUMNÊ DOBRZE
        /*
            var discreteActions = actionsOut.DiscreteActions;
                for (int j = 0; j < 9; j++)
                {
                    if (playerBoard[0, j] == 0)
                    {
                        discreteActions[0] = (j*9);
                        Debug.Log(discreteActions[0]);
                        discreteActions[1] = (Board[0, j])-1;
                        Debug.Log(discreteActions[1]);
                        return; 
                    }
                }
        */

        // WYPISZ CA£Y RZ¥D DOBRZE
        /*
            var discreteActions = actionsOut.DiscreteActions;
                for (int j = 0; j < 9; j++)
                {
                    if (playerBoard[j, 0] == 0)
                    {
                        discreteActions[0] = (j);
                        //Debug.Log(discreteActions[0]);
                        discreteActions[1] = (Board[j, 0])-1;
                        //Debug.Log(discreteActions[1]);
                        return; 
                    }
                }

        */
        // WYPISZ CA£Y SEKTOR
        /*
        
        /*
        int currentSector = 2; // Numer sektora (od 0 do 8)
        Debug.Log("Heuristic called");

        int sectorColumnStart = (currentSector / 3) * 3;
        int sectorRowStart = (currentSector % 3) * 3;
        var discreteActions = actionsOut.DiscreteActions;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int row = sectorRowStart + i;
                int col = sectorColumnStart + j;

                if (playerBoard[row, col] == 0)
                {
                    discreteActions[0] = col * 9 + row;
                    discreteActions[1] = Board[row, col] - 1;
                    Debug.Log($"Heuristic Action: fieldIndex={discreteActions[0]}, value={discreteActions[1] + 1}");
                    currentSector = (currentSector + 1) % 9;
                    return;
                }
            }
        }
        */
    }

    bool IsColFull(int row)
    {
        for (int i = 0; i < 9; i++)
        {
            if (playerBoard[row, i] == 0)
            {
                Debug.Log("Kolumna jeszcze nie jest skoñczona");
                return false;
            }
        }
        return true;
    }

    bool IsRowFull(int col)
    {
        for (int i = 0; i < 9; i++)
        {
            if (playerBoard[i, col] == 0)
            {
                Debug.Log("Rz¹d nie jest skoñczony");
                return false;
            }
        }
        return true;
    }

    bool IsSectorFull(int row, int col)
    {
        int sectorRowStart = (row / 3) * 3;
        int sectorColumnStart = (col / 3) * 3;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (playerBoard[sectorRowStart + i, sectorColumnStart + j] == 0)
                {
                    Debug.Log("Sektor nie jest skoñczony");
                    return false;
                }
            }
        }
        return true;
    }

    void RemoveInvalidMoves(int row, int col)
    {
        string rowColKey = $"{row},{col},";
        // Usuwanie wszystkich wpisów pasuj¹cych do wzorca
        invalidMoves.RemoveWhere(move => move.StartsWith(rowColKey));
    }

    ///debugowanie- poka¿ plansze gracza i poka¿ uzupe³nione sudoku
    void ShowTruePlayerBoard()
    {
        string boardRepresentation2 = ""; // Przechowuje reprezentacjê planszy jako tekst

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
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

