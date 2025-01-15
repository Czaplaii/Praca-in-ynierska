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
    public int[,] Board = new int[9, 9]; // Pe�ne rozwi�zanie sudoku (ukryte przed AI)
    private int[,] playerBoard = new int[9, 9]; // Plansza gracza (to widzi i modyfikuje AI)
    private Board_creator board; //odno�nik do skryptu
    private banner_bar bar; //odno�nik do skryptu
    private Button[] buttons, intension; //odno�nik do skryptu
    private int episodeCount = 0,Actions=0, Streak =0; // Licznik epizod�w
    int number = -1; //numer kt�rym agent operuje na swojej tablicy
    private HashSet<string> invalidMoves = new HashSet<string>();
    private IDiscreteActionMask actionMask; //maskowanie
    private List<int>[,] validMoves = new List<int>[9, 9];

    void Awake()
    {
        Debug.unityLogger.logEnabled = false; //wy��cz logi na czas treningu agenta
        //Time.timeScale = 0.01f; //spowolnienie czasu pozwalaj�ce obserwowa� i �ledzi� zachowania agenta
    }

    //Inicjalizacja zachowa� agenta
    public override void Initialize()
    {
        board = FindObjectOfType<Board_creator>(); //przypisanie obiektu ze skryptem
        bar = FindObjectOfType<banner_bar>(); //przypisanie obiektu ze skryptem
        playerBoard = board.GetBoard(); //przepisz tablic� zagadki agentowi
        Board = board.GetTrueBoard(); //przepisz rozwi�zanie do por�wnania przez agenta
        buttons = board.GetButtons(); //przepisz guziki na planszy do invoke
        intension = bar.GetIntensionButtons(); //przepisz guziki numer�w do invoke
        PlayerPrefs.SetInt("iter", 0);
    }

    //po rozpocz�ciu 1 sesji sudoku(nowa mapa)
    public override void OnEpisodeBegin()
    {
        episodeCount++; //do wy�wietlania epizod�w
        if (episodeCount != 1)
        {
            board.ResetGame(); //je�li to kolejny epizod, zresetuj plansz�(unikamy resetowania wygenerowanej planszy na poczatku)
        }
        PlayerPrefs.SetInt("iter", episodeCount);
        playerBoard = board.GetBoard(); //przypisujemy tablic� agenta
        Board = board.GetTrueBoard(); //przypisujemy pe�ne rozwi�zanie do por�wna�
        invalidMoves.Clear(); //wyczy�� niepoprawne ruchy z poprzedniego epizodu
        InitializeValidMoves(); // Dodaj inicjalizacj� mo�liwych ruch�w
        ShowPlayerBoard(); // do debugowania
        ShowTruePlayerBoard(); //do debugowania

    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        string debugLog = "Maskowanie:\n"; // Pocz�tek loga z opisem
        int[] numberCounts = new int[9]; // Tablica do zliczania wyst�pie� ka�dej liczby
        for (int col = 0; col < 9; col++)
        {
            for (int row = 0; row < 9; row++)
            {
                int index = col * 9 + row;

                if (playerBoard[row, col] != 0) // Kom�rka zaj�ta
                {
                    actionMask.SetActionEnabled(0, index, false); //maskowanie liczb
                    debugLog += "1 "; // Zablokowana kom�rka
                    int value = playerBoard[row, col]; //przypisz wyst�pienie
                    numberCounts[value - 1]++; //inkrementuj wyst�pienie danej liczby
                }
                else
                {
                    debugLog += "0 "; // Wolna kom�rka
                }
            }
            debugLog += "\n"; // Nowa linia dla nast�pnego wiersza
        }
        Debug.Log(debugLog);

        string debug2 = "Zliczanie liczb:\n";
        for (int i = 0; i < numberCounts.Length; i++)
        {
            debug2 += $"Liczba {i + 1}: {numberCounts[i]}\n";
            if(numberCounts[i] >= 9) //je�li dana liczba wyst�pi 9 razy w tablicy
            {
                actionMask.SetActionEnabled(1, i, false); //zamaskuj t� liczb� przed agentem
            }
        }
        Debug.Log(debug2);

    }


    //na co agent ma zwraca� uwag�
    public override void CollectObservations(VectorSensor sensor)
    {
        int emptyFields = 0;
        // Przekazuj plansz� gracza jako obserwacje (0 = puste pole, 1-9 = liczby)
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                sensor.AddObservation(playerBoard[i, j]);
                if (playerBoard[i, j] == 0) 
                emptyFields++; //ile mamy pustych p�l
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

                // Oblicz sum� dla bie��cego sektora 3x3
                for (int i = 0; i < 3; i++)  // Przechodzimy przez 3 wiersze w obr�bie sektora
                {
                    for (int j = 0; j < 3; j++)  // Przechodzimy przez 3 kolumny w obr�bie sektora
                    {
                        int row = sectorRow * 3 + i;  // Indeks wiersza w ca�ej planszy
                        int col = sectorCol * 3 + j;  // Indeks kolumny w ca�ej planszy
                        sectorSum += playerBoard[row, col];  // Dodajemy warto�� do sumy sektora
                    }
                }
                sensor.AddObservation(sectorSum); // Dodajemy sum� sektora jako obserwacj�
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Akcja AI: wyb�r pola i liczby
        int fieldIndex = actions.DiscreteActions[0]; // Pole do modyfikacji (0-80)
        int number = actions.DiscreteActions[1]; // Liczba do wstawienia (0-8)
        bool didMove = false; // Sprawdzamy, czy agent wykona� ruch

        int row = fieldIndex % 9;
        int col = fieldIndex / 9;
        Actions++; // Zliczanie akcji agenta

        string moveKey = $"{row},{col},{number}";

        // Je�li pole jest puste, pozw�l na ruch
        if (playerBoard[row, col] == 0)
        {
            if (intension[number].interactable)
            {
                intension[number].onClick.Invoke();
                AddReward(0.1f); // Nagroda za pr�b� eksploracji
                didMove = true;
            }

            if (IsMoveValid(row, col, number)) // Czy ruch jest poprawny?
            {
                buttons[fieldIndex].onClick.Invoke();
                playerBoard[row, col] = number; // Wstaw liczb�
                AddReward((2f + 1f * Streak));

                UpdateValidMoves(row, col, number); // Zaktualizuj mo�liwe ruchy

                // Nagrody za cz�ciowe cele
                if (IsColFull(col)) AddReward(15f);
                if (IsRowFull(row)) AddReward(10f);
                if (IsSectorFull(row, col)) AddReward(20f);

                Streak = Math.Min(Streak + 1, 5); // Ograniczenie maksymalnego streaka
                didMove = true;
            }
            else // Ruch niezgodny z zasadami
            {
                buttons[fieldIndex].onClick.Invoke();
                if (invalidMoves.Contains(moveKey))
                {
                    AddReward(-1f * invalidMoves.Count); // Dynamiczna kara za powt�rzenie b��du
                }
                else
                {
                    invalidMoves.Add(moveKey); //dodaj wykorzystany niepoprawny ruch
                    AddReward(-0.5f);
                }
                Streak = 0;
                didMove = true;
            }
        }
        else // Pole jest ju� zaj�te
        {
            AddReward(-4f); // Kara za wyb�r zaj�tego pola
            didMove = true;
        }

        // Nagroda za uko�czenie planszy
        if (IsPuzzleSolved())
        {
            AddReward(500.0f); // Du�a nagroda za uko�czenie gry
            EndEpisode();
            return;
        }

        // Nagroda za post�p wype�niania planszy (proporcjonalna)
        float completionRate = CountFilledCells() / 81f;
        AddReward(completionRate * 5f);

        // Kara za brak ruchu
        if (!didMove)
        {
            AddReward(-2f);
        }
    }

    private int CountFilledCells()
    {
        int count = 0;
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (playerBoard[i, j] != 0) // Je�li kom�rka jest wype�niona
                {
                    count++;
                }
            }
        }
        return count;
    }

    private void InitializeValidMoves()
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                validMoves[i, j] = new List<int>();
                if (playerBoard[i, j] == 0) // Je�li kom�rka jest pusta
                {
                    for (int num = 1; num <= 9; num++)
                    {
                        if (IsMoveValid(i, j, num)) // Sprawd�, czy liczba jest poprawna
                        {
                            validMoves[i, j].Add(num);
                        }
                    }
                }
            }
        }
    }

    private void UpdateValidMoves(int row, int col, int number)
    {
        // Usuni�cie mo�liwo�ci dla konkretnej kom�rki
        validMoves[row, col].Clear(); // Kom�rka jest ju� wype�niona, wi�c brak mo�liwych ruch�w

        // Przejd� przez wiersz, kolumn� i sektor, aby zaktualizowa� mo�liwe ruchy
        for (int i = 0; i < 9; i++)
        {
            // Usu� wstawion� liczb� z mo�liwych warto�ci w tym wierszu i kolumnie
            validMoves[row, i]?.Remove(number);
            validMoves[i, col]?.Remove(number);
        }

        // Usu� wstawion� liczb� z sektora
        int startRow = (row / 3) * 3;
        int startCol = (col / 3) * 3;
        for (int i = startRow; i < startRow + 3; i++)
        {
            for (int j = startCol; j < startCol + 3; j++)
            {
                validMoves[i, j]?.Remove(number);
            }
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

    private bool IsPuzzleSolved() //sprawdzamy czy agent uko�czy� plansz�
    {
        // Sprawd�, czy wszystkie pola s� wype�nione poprawnie
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


    public override void Heuristic(in ActionBuffers actionsOut) //mo�na u�y� do debugowania i sprawdzania rozwi�za�, na czas treningu powinno zostac puste
    {
        //losowe liczby
        /*
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Random.Range(0,81);
        discreteActions[1] = Random.Range(0,8);
        */
        // WYPISZ CA�� KOLUMN� DOBRZE
        
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

        // WYPISZ CA�Y RZ�D DOBRZE
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
        // WYPISZ CA�Y SEKTOR
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
                Debug.Log("Kolumna jeszcze nie jest sko�czona");
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
                Debug.Log("Rz�d nie jest sko�czony");
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
                    Debug.Log("Sektor nie jest sko�czony");
                    return false;
                }
            }
        }
        return true;
    }

    void RemoveInvalidMoves(int row, int col)
    {
        string rowColKey = $"{row},{col},";
        // Usuwanie wszystkich wpis�w pasuj�cych do wzorca
        invalidMoves.RemoveWhere(move => move.StartsWith(rowColKey));
    }

    bool ObviousMistake(int row,int col,int number, int[,] tab) //funkcja nak�adaj�ca dodatkow� kar�, je�li liczba wyst�pi�a w wierszu, kolumnie, sektorze
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

    ///debugowanie- poka� plansze gracza i poka� uzupe�nione sudoku
    void ShowTruePlayerBoard()
    {
        string boardRepresentation2 = ""; // Przechowuje reprezentacj� planszy jako tekst

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
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

