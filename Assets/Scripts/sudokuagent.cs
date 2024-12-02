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
    private Board_creator board; //odno�nik do skryptu
    private banner_bar bar; //odno�nik do skryptu
    private Button[] buttons, intension; //odno�nik do skryptu
    private int episodeCount = 0,Actions=0, Streak =0; // Licznik epizod�w
    int number = -1; //numer kt�rym agent operuje na swojej tablicy
    private HashSet<string> invalidMoves = new HashSet<string>();

    void Awake()
    {
        Debug.unityLogger.logEnabled = false; //wy��cz logi na czas treningu agenta
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
    }

    //po rozpocz�ciu 1 sesji sudoku(nowa mapa)
    public override void OnEpisodeBegin()
    {
        episodeCount++; //do wy�wietlania epizod�w
        if (episodeCount != 1)
        {
            board.ResetGame(); //je�li to kolejny epizod, zresetuj plansz�(unikamy resetowania wygenerowanej planszy na poczatku)
        }
        playerBoard = board.GetBoard(); //przypisujemy tablic� agenta
        Board = board.GetTrueBoard(); //przypisujemy pe�ne rozwi�zanie do por�wna�
        invalidMoves.Clear(); //czy�cimy b��dne ruchy z poprzedniej planszy
        ShowPlayerBoard(); // do debugowania
        ShowTruePlayerBoard(); //do debugowania

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
                /*
                for (int num = 1; num <= 9; num++)
                {
                    bool isValid = IsMoveValid(i, j, num);
                    sensor.AddObservation(isValid ? 1 : 0);
                }*/
                //if (playerBoard[i, j] == 0) emptyFields++; //ile mamy pustych p�l
                //sensor.AddObservation(emptyFields / 81.0f);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Akcja AI: wyb�r pola i liczby
        int fieldIndex = actions.DiscreteActions[0]; // Pole do modyfikacji (0-80)
        number = actions.DiscreteActions[1]; // Liczba do wstawienia (0-8)
        bool didMove = false; //sprawdzamy czy agent nie odpu�ci� tury
        //Debug.Log("Wybrany number: " + number);
        int row = fieldIndex % 9; 
        int col = fieldIndex / 9;
        Actions++; //w cmd odpowiednik Step
        string moveKey = $"{row},{col},{number}";
        //Debug.Log("Ilo�� akcji: " + Actions);
        // Je�li pole jest puste, pozw�l na ruch
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
                playerBoard[row, col] = number; // Wstaw liczb�
                AddReward(4.0f+(0.5f*Streak)); // Nagroda za poprawny ruch
                if (Streak <= 5) 
                {
                    Streak++; //streak nagradza agenta za dokonanie poprawnych decyzji z rz�du, co powinno go zach�ci� do dalszej eksploracji
                }
                didMove = true; 
                playerBoard = board.GetBoard();
                List<string> toRemove = new List<string>();
                foreach (var key in invalidMoves)
                {
                    if (key.StartsWith($"{row},{col},")) // Je�li b��d dotyczy wype�nionego pola
                    {
                        toRemove.Add(key);
                    }
                }
                foreach (var key in toRemove)
                {
                    invalidMoves.Remove(key);
                }
                Debug.Log("Agent poprawnie oznaczy� liczb�" +number + " w "+ row + col);
                ShowPlayerBoard();
            }
            else
            {
                buttons[fieldIndex].onClick.Invoke();
                if (invalidMoves.Contains(moveKey))
                {
                    AddReward(-1.5f); // Kara za powt�rzenie b��dnego ruchu
                }
                else
                {
                    invalidMoves.Add(moveKey); // Zapisz b��dny ruch
                    AddReward(-1.0f); // Kara za nowy b��dny ruch
                }
                Streak = 0;
                //Debug.Log($"Agent b��dnie oznaczy� liczb� {number} at field {buttons[fieldIndex]} (row: {row}, col: {col}).");
                Debug.Log("Agent niepoprawnie oznaczy� liczb�");
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
       
    }


    ///debugowanie- poka� plansze gracza i poka� uzupe�nione sudoku
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

