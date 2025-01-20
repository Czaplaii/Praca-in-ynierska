using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Collections.Generic;
using Unity.Barracuda;
using System;

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
        //Time.timeScale = 0.1f; //spowolnienie czasu pozwalaj¹ce obserwowaæ i œledziæ zachowania agenta
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
        PlayerPrefs.SetInt("iter", 0);
    }

    //po rozpoczêciu 1 sesji sudoku(nowa mapa)
    public override void OnEpisodeBegin()
    {
        episodeCount++; //do wyœwietlania epizodów
        PlayerPrefs.SetInt("iter", episodeCount);
        if (episodeCount == 2)
        {
            board.ChangeMode();
            board.ResetGame();

        }
        else if (episodeCount > 2)
        {
            board.ResetGame(); //jeœli to kolejny epizod, zresetuj planszê(unikamy resetowania wygenerowanej planszy na poczatku)
        } 
        playerBoard = board.GetBoard(); //przypisujemy tablicê agenta
        Board = board.GetTrueBoard(); //przypisujemy pe³ne rozwi¹zanie do porównañ
        invalidMoves.Clear(); //czyœcimy b³êdne ruchy z poprzedniej planszy
        ShowPlayerBoard(); // do debugowania
        ShowTruePlayerBoard(); //do debugowania
    }

    public void PrintAvailableActions()
    {
        string possibleActionsLog = "Mo¿liwe akcje:\n"; // Inicjalizacja logu
        // Iteracja przez wszystkie komórki na planszy
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                int number = playerBoard[row, col]; // Pobieramy liczbê z tablicy
                if (number == 0) // Tylko dla pustych komórek
                {
                    possibleActionsLog += $"Pozycja ({row}, {col}) - Dostêpne akcje: ";

                    // Sprawdzamy dostêpnoœæ liczb od 1 do 9
                    for (int i = 1; i <= 9; i++)
                    {
                        // Sprawdzamy, czy liczba jest dostêpna w tym wierszu, kolumnie lub sektorze
                        if (!board.IsInRow(row, i, playerBoard) &&
                            !board.IsInColumn(col, i, playerBoard) &&
                            !board.IsInSector(row, col, i, playerBoard))
                        {
                            // Jeœli liczba jest dostêpna, dodajemy j¹ do mo¿liwych akcji
                            possibleActionsLog += $"{i} ";
                        }
                    }

                    possibleActionsLog += "\n";
                }
            }
        }

        // Wyœwietlamy log z mo¿liwymi akcjami
        Debug.Log(possibleActionsLog);
    }


    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {

        int[] numberCounts = new int[9]; // Tablica do zliczania wyst¹pieñ ka¿dej liczby
        //maskowanie zajêtych komórek
        for (int col = 0; col < 9; col++)
        {
            for (int row = 0; row < 9; row++)
            {
                int index = col * 9 + row;

                if (playerBoard[row, col] != 0) // Komórka zajêta
                {
                    actionMask.SetActionEnabled(0, index, false); //maskowanie liczb
                    int value = playerBoard[row, col]; //przypisz wyst¹pienie
                    numberCounts[value - 1]++; //inkrementuj wyst¹pienie danej liczby
                }
            }
        }
        string debug2 = "Zliczanie liczb:\n";
        //maskowanie liczb gdy wyst¹pi ich 9
        for (int i = 0; i < numberCounts.Length; i++)
        {
            debug2 += $"Liczba {i + 1}: {numberCounts[i]}\n";
            if (numberCounts[i] >= 9) //jeœli dana liczba wyst¹pi 9 razy w tablicy
            {
                actionMask.SetActionEnabled(1, i, false); //zamaskuj t¹ liczbê przed agentem
            }
        }
        //Debug.Log(debug2);

        for (int i = 0; i < 9; i++)
        {
            if (LastInCol(i))
            {
                for (int j = 0; j < 81; j++)
                {
                    int row = j / 9;  // obliczamy numer wiersza
                    //Debug.Log(row + "=row, i = " + i);
                    if (row == i)  // jeœli to jest element w wierszu i, pomijamy
                        continue;
                    else
                    {
                        if (playerBoard[row, (j % 9)] == 0)
                        {
                            actionMask.SetActionEnabled(0, j, false);
                            /*for (i = 0; i < 9; i++)
                            {
                                //if (i != predicted)
                                //  actionMask.SetActionEnabled(1, i, false);
                            }*/
                            //Debug.Log("zablokowane: " + col+" " + (j / 9) + " - " + playerBoard[col, (j / 9)]);
                        }
                    }

                }
                break;
            }
            else if (LastInRow(i))
            {
                for (int j = 0; j < 81; j++)
                {
                    int col = j % 9;  // obliczamy numer wiersza
                    if (col == i)  // jeœli to jest element w wierszu i, pomijamy
                        continue;
                    else
                    {
                        if (playerBoard[col, (j / 9)] == 0)
                        {
                            actionMask.SetActionEnabled(0, j, false);
                            //for (i = 0; i < 9; i++)
                            //Debug.Log("zablokowane: " + col+" " + (j / 9) + " - " + playerBoard[col, (j / 9)]);
                        }
                    }
                }
                break;
            }
        }
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

        for (int sectorCol = 0; sectorCol < 3; sectorCol++) // Iteracja przez 3 sektory w pionie
        {
            for (int sectorRow = 0; sectorRow < 3; sectorRow++) // Iteracja przez 3 sektory w poziomie
            {
                int sectorSum = 0;

                // Oblicz sumê dla bie¿¹cego sektora 3x3
                for (int i = 0; i < 3; i++)  // Przechodzimy przez 3 wiersze w obrêbie sektora
                {
                    for (int j = 0; j < 3; j++)  // Przechodzimy przez 3 kolumny w obrêbie sektora
                    {
                        int row = sectorRow * 3 + i;  // Indeks wiersza w ca³ej planszy
                        int col = sectorCol * 3 + j;  // Indeks kolumny w ca³ej planszy
                        sectorSum += playerBoard[row, col];  // Dodajemy wartoœæ do sumy sektora
                    }
                }
                sensor.AddObservation(sectorSum); // Dodajemy sumê sektora jako obserwacjê
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
                AddReward(5.0f+(1f*Streak)); // Nagroda za poprawny ruch
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
                /*if (ObviousMistake(row, col, number, playerBoard))
                {
                    Debug.Log("Liczba wyst¹pi³a w wierszu, sektorze lub kolumnie");
                    AddReward(-5f); // Kara za oczywisty b³¹d
                }*/
                buttons[fieldIndex].onClick.Invoke();
                if (invalidMoves.Contains(moveKey))
                {
                        AddReward(-2f);
                }
                else
                {
                    invalidMoves.Add(moveKey); // Zapisz b³êdny ruch
                        AddReward(-1f);
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
            AddReward(500.0f); // Nagroda za ukoñczenie planszy
            didMove= true;
            EndEpisode();
        }

        // Jeœli agent nie wykona³ ruchu, na³ó¿ dodatkow¹ karê
        if (!didMove)
        {
            AddReward(-5f); // Kara za brak ruchu
        }
        // Iteracja po elementach
        /*foreach (var name in invalidMoves)
        {
            Debug.Log("HashSet: " + name);
        }*/
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

    bool ObviousMistake(int row,int col,int number, int[,] tab) //funkcja nak³adaj¹ca dodatkow¹ karê, jeœli liczba wyst¹pi³a w wierszu, kolumnie, sektorze
    {
        if (board.IsInRow(row, number, tab) || board.IsInColumn(col, number, tab) || board.IsInSector(row, col, number, tab))
        {
            Debug.Log("IsInRow: " + board.IsInRow(row, number, tab));
            Debug.Log("IsInColumn: " + board.IsInColumn(col, number, tab));
            Debug.Log("IsInSector: " + board.IsInSector(row,col, number, tab));
            return true;
        }
        else
        {
            return false;
        }
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

        //Debug.Log(boardRepresentation); // Wypisujemy ca³¹ planszê jako tekst
    }

    bool LastInCol(int col)
    {
        int count = 0;
        int sum = 0;
        bool lic = false;
        for (int j = 0; j < 9; j++)
        {
            //Debug.Log("jestem na: " + playerBoard[j, col]);
            sum = sum + playerBoard[j, col];
            if(playerBoard[j, col] > 0)
                count++;
        }
        //Debug.Log("count: " + count);
        //Debug.Log("sum: " + sum);
        if(count == 8)
            lic = true;
        return lic;
    }

    bool LastInRow(int row)
    {
        int count = 0;
        int sum = 0;
        bool lic = false;
        for (int j = 0; j < 9; j++)
        {
            //Debug.Log("jestem na: " + playerBoard[row, j]);
            sum = sum + playerBoard[row, j];
            if (playerBoard[row, j] > 0)
                count++;
        }
        //Debug.Log("count: " + count);
        //Debug.Log("sum: " + sum);
        if (count == 8)
            lic = true;
        return lic;
    }

    //jeœli rz¹d, to true
    int lastnumber(bool row, int type)
    {
        int sum = 0;
        int predicted = 0;
        for (int j = 0; j < 9; j++)
        {
            if (row) 
            {
                sum = sum + playerBoard[type, j];
                Debug.Log("suma wiersza" + sum);
            }
            else
            {
                sum = sum + playerBoard[j, type];
                Debug.Log("suma kolumny" + sum);
            }
        }
        predicted = 45 - sum - 1;
        return predicted;
    }

    public override void Heuristic(in ActionBuffers actionsOut) //mo¿na u¿yæ do debugowania i sprawdzania rozwi¹zañ, na czas treningu powinno zostac puste
    {
        //losowe liczby
        /*
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Random.Range(0,81);
        discreteActions[1] = Random.Range(0,8);
        */
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
}

