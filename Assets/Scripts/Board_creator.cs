using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Newtonsoft.Json;

public class Board_creator : MonoBehaviour
{
    [SerializeField] int[,] Board = new int[9, 9], PlayerBoard =  new int[9,9]; // deklaracja tablicy
    [SerializeField] Button[] BoardButtons; // deklaracja przycisków
    [SerializeField] List<int> numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }; // lista numerów do wyboru
    [SerializeField] private int seed; // ziarno do œledzenia konkretnego losowania
    private System.Random rand; // losowanie
    //[SerializeField, Range(1, 3)] private int difficulty; // poziom trudnoœci
    private clock clockreset; // odniesienie do clock
    public SudokuAgent sudokuAgent;
    [SerializeField] TMP_Text seedtext;
    [SerializeField] private TextAsset jsonData; // Przypisz plik JSON w Inspectorze
    private SudokuDataset dataset;

    // Struktura do odwzorowania danych JSON
    private class SudokuDataset
    {
        public string[] quizzes { get; set; }
        public string[] solutions { get; set; }
    }

    void Start()
    {
        clockreset = FindObjectOfType<clock>();

        // Inicjalizuj generator liczb losowych
        if (seed == -1)
        {
            seed = Mathf.Abs((int)(System.DateTime.Now.Ticks / 10000));
        }
        rand = new System.Random(seed);

        // Za³aduj dane JSON tylko raz
        dataset = LoadSudokuDataset();
        if (dataset == null)
        {
            Debug.LogError("B³¹d: Nie uda³o siê za³adowaæ danych Sudoku.");
            return;
        }

        LoadRandomSudokuFromDataset(dataset);
        DefineButtons();
        StartButtonDeactivate();
    }

    private SudokuDataset LoadSudokuDataset()
    {
        if (jsonData == null)
        {
            Debug.LogError("jsonData is null. Please assign a valid JSON file.");
            return null;
        }

        Debug.Log("Loaded JSON Data: " + jsonData.text);

        try
        {
            SudokuDataset dataset = JsonConvert.DeserializeObject<SudokuDataset>(jsonData.text);
            if (dataset != null)
            {
                Debug.Log("Sudoku dataset loaded successfully.");
            }
            else
            {
                Debug.LogError("Deserialized dataset is null.");
            }
            return dataset;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading dataset: {e.Message}");
            return null;
        }
    }

    private void LoadRandomSudokuFromDataset(SudokuDataset dataset)
    {
        if (dataset.quizzes == null || dataset.solutions == null)
        {
            Debug.LogError("Quizzes or solutions is null.");
            return;
        }

        if (dataset.quizzes.Length == 0 || dataset.solutions.Length == 0)
        {
            Debug.LogError("Dataset is empty or missing quizzes/solutions.");
            return;
        }

        seed = rand.Next(0, dataset.quizzes.Length);
        Debug.Log($"Selected random index: {seed}");

        string quiz = dataset.quizzes[seed];
        string solution = dataset.solutions[seed];

        if (string.IsNullOrEmpty(quiz) || string.IsNullOrEmpty(solution))
        {
            Debug.LogError($"Invalid data at index {seed}. Quiz or solution is null/empty.");
            return;
        }

        FillBoardFromString(quiz, PlayerBoard);
        FillBoardFromString(solution, Board);

        UpdateBoardUI();
    }


    private void FillBoardFromString(string boardString, int[,] targetBoard)
    {
        if (boardString.Length != 81)
        {
            Debug.LogError("Invalid board string length. Expected 81 characters.");
            return;
        }

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                char value = boardString[i * 9 + j];
                targetBoard[j, i] = value == '.' ? 0 : (value - '0'); // '.' oznacza puste pole
            }
        }
    }

    private void UpdateBoardUI()
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                int buttonIndex = (i * 9) + j;
                TMP_Text buttonText = BoardButtons[buttonIndex].GetComponentInChildren<TMP_Text>();

                if (PlayerBoard[j, i] != 0)
                {
                    buttonText.text = PlayerBoard[j, i].ToString();
                    BoardButtons[buttonIndex].interactable = false; // Pola wstêpnie wype³nione nie s¹ interaktywne
                }
                else
                {
                    buttonText.text = " ";
                    BoardButtons[buttonIndex].interactable = true;
                }
            }
        }
    }

    private void DefineButtons()
    {
        for (int i = 0; i < BoardButtons.Length; i++)
        {
            int index = i;
            BoardButtons[i].onClick.AddListener(() => OnButtonClicked(index));
        }
    }

    public void OnButtonClicked(int index)
    {
        int declared = PlayerPrefs.GetInt("number") + 1;
        TMP_Text buttonText = BoardButtons[index].GetComponentInChildren<TMP_Text>();
        if (declared != 0)
        {
            if (declared == Board[index % 9, index / 9])
            {
                buttonText.text = (Board[index % 9, index / 9]).ToString();
                BoardButtons[index].interactable = false;
                Color currentColor = BoardButtons[index].image.color;
                if (currentColor.r == 0.992f && currentColor.g == 0.286f && currentColor.b == 0.286f)  //sprawdzamy czy wczeœniej pole by³o b³êdnie zaznaczone
                {
                    BoardButtons[index].image.color = new Color(0.898f, 0.678f, 0.122f, 1f);  // Pomarañczowy
                    PlayerBoard[index % 9, index / 9] = Board[index % 9, index / 9];
                    //if(IsPuzzleDone())
                        //ResetGame();
                }
                else
                {
                    BoardButtons[index].image.color = new Color(0.106f, 0.737f, 0.024f, 1f);  // Zielony    
                    PlayerBoard[index % 9, index / 9] = Board[index % 9, index / 9];
                    //if (IsPuzzleDone())
                      //  ResetGame();
                }
            }
            else
            {
                int mistaketemp = PlayerPrefs.GetInt("mistake") + 1;
                PlayerPrefs.SetInt("mistake", mistaketemp);
                buttonText.text = declared.ToString();
                BoardButtons[index].image.color = new Color(0.992f, 0.286f, 0.286f); //czerwony
            }
        }
        else
            Debug.Log("nie wybrano numeru");
    }

    void StartButtonDeactivate() // wy³¹cz guziki wype³nionych komórek
    {
        for (int i = 0; i < BoardButtons.GetLength(0); i++)
        {
            TMP_Text buttonText = BoardButtons[i].GetComponentInChildren<TMP_Text>();
            if (buttonText.text != " ")
                BoardButtons[i].interactable = false;
        }
    }

    public int[,] GetBoard() //przekazujemy tablicê agentowi
    {
        return PlayerBoard;
    }

    public int[,] GetTrueBoard() //przekazujemy tablicê agentowi
    {
        return Board;
    }

    public Button[] GetButtons() //przekazujemy tablicê agentowi
    {
        return BoardButtons;
    }

    void ShowPlayerBoard()
    {
        string boardRepresentation = ""; // Przechowuje reprezentacjê planszy jako tekst

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                boardRepresentation += PlayerBoard[j, i] + " "; // Dodajemy liczbê i spacjê dla przejrzystoœci
            }
            boardRepresentation += "\n"; // Nowa linia po ka¿dym wierszu
        }

        Debug.Log(boardRepresentation); // Wypisujemy ca³¹ planszê jako tekst
    }

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

    public bool IsInRow(int row, int number, int[,] tab) // czy wyst¹pi³a w wierszu?
    {
        for (int i = 0; i < tab.GetLength(1); i++)
        {
            if (tab[row, i] == number)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsInColumn(int column, int number, int[,] tab)// czy wyst¹pi³a w kolumnie?
    {
        for (int i = 0; i < tab.GetLength(0); i++)
        {
            if (tab[i, column] == number)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsInSector(int row, int column, int number, int[,] tab) //czy wyst¹pi³a w kwadracie 9x9
    {
        int sectorRowStart = (row / 3) * 3;
        int sectorColumnStart = (column / 3) * 3;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (tab[sectorRowStart + i, sectorColumnStart + j] == number)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void ResetGame()
    {
        // Wybierz nowy seed i zainicjalizuj generator liczb losowych
        seed = rand.Next(0, dataset.quizzes.Length);
        Debug.Log($"Wybrano nowy seed: {seed}");

        // Pobierz now¹ planszê i rozwi¹zanie
        string quiz = dataset.quizzes[seed];
        string solution = dataset.solutions[seed];

        if (string.IsNullOrEmpty(quiz) || string.IsNullOrEmpty(solution))
        {
            Debug.LogError("B³¹d: Nie mo¿na wczytaæ nowej planszy. Reset gry przerwany.");
            return;
        }

        // Aktualizuj plansze
        FillBoardFromString(quiz, PlayerBoard);
        FillBoardFromString(solution, Board);

        // Zresetuj stan przycisków
        for (int i = 0; i < BoardButtons.Length; i++)
        {
            int x = i % 9; // Kolumna
            int y = i / 9; // Wiersz
            Button button = BoardButtons[i];
            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();

            // Ustaw wartoœci przycisków na podstawie PlayerBoard
            if (PlayerBoard[x, y] != 0)
            {
                buttonText.text = PlayerBoard[x, y].ToString();
                button.interactable = false;
                button.image.color = Color.white; // Domyœlny kolor
            }
            else
            {
                buttonText.text = " ";
                button.interactable = true;
                button.image.color = Color.white;
            }
        }

        // Zresetuj zmienne gry
        PlayerPrefs.SetInt("mistake", 0);
        clockreset.ResetTime();
        seedtext.text = "Seed: " + seed.ToString();

        // Debugowanie
        Debug.Log("Gra zosta³a zresetowana.");
        ShowPlayerBoard();
        ShowTruePlayerBoard();
    }

    public bool IsPuzzleDone() //warunek zwyciêstwa
    {
        for (int i = 0; i < BoardButtons.Length; i++)
        {
            Button button = BoardButtons[i];
            if (button.interactable == true)
                return false;
        }
        Debug.Log("Wygra³eœ!!!");
        //round++;
        //PlayerPrefs.SetInt("iter", round);
        return true;
    }
}


    /*
    void Start()
    {
        clockreset = FindObjectOfType<clock>();//szukamy obiektu ze skryptem clock
        if (seed == -1) //sprawdzamy czy generowaæ nowy seed, czy mamy sprecyzowany
        {
            seed = (int)(System.DateTime.Now.Ticks / 10000); // Losowy seed
            seed = Math.Abs(seed);
        }
        rand = new System.Random(seed); //RNG na bazie seed
        PlayerPrefs.SetInt("mistake", 0); //reset b³êdów
        PlayerPrefs.SetInt("iter", 0); //reset rund
        BoardInit(Board);// wype³niamy tablicê zerami
        CreateBoard(); //wype³niamy tablicê guzików i mieszamy numery
        DefineButtons();
        PuzzleMaker();
        seedtext.text = ("Seed: " +seed).ToString();
        //ShowPlayerBoard();
    }


    void CreateBoard()
    {
        ShuffleList(numbers); // mieszamy numery
        BoardFiller(0, 0);
    }

    bool BoardFiller(int row, int column) //uzupe³nienie tablicy
    {
        if (row == 9) // czy skoñczyliœmy ostatni wiersz
        {
            return true; // Plansza zosta³a wype³niona
        }

        if (column == 9) //czy ostatnia kolumna w wierszu
        {
            return BoardFiller(row + 1, 0); // skoñczyliœmy ostatni¹ kolumnê, przechodzimy do nastêpnego wiersza
        }

        // jeœli komórka jest pe³na, przechodzimy do kolejnej
        if (Board[row, column] != 0) //czy pe³na komórka
        {
            return BoardFiller(row, column + 1); // powtórz funkcjê dla kolejnej komórki
        }

        if (Board[row, column] == 0) //jeœli komórka jest pusta
        {
            foreach (var number in numbers) //iteracja po liœcie numerów
            {
                if (!IsInRow(row, number, Board) && !IsInColumn(column, number, Board) && !IsInSector(row, column, number,Board)) //czy zgodna z zasadami
                {
                    Board[row, column] = number; // Umieszczamy liczbê
                    //Przypisz(row, column, number); // Aktualizujemy UI
                    if (BoardFiller(row, column + 1)) // Jeœli nie mo¿na dalej wype³niæ
                    {
                        return true;
                    }
                    Board[row, column] = 0; // "wymazujemy" wartoœæ z komórki
                }
            }
        }
        return false;
    }


    void BoardInit(int[,] tab) //wype³niamy tablice zerami
    {
        for (int i = 0; i < Board.GetLength(0); i++) //pêtla po ca³ej tablicy
        {
            for (int j = 0; j < Board.GetLength(1); j++)
            {
                Board[i, j] = 0;
                PlayerBoard[i, j] = 0;
                int buttonIndex = (9 * i) + j;
                TMP_Text buttonText = BoardButtons[buttonIndex].GetComponentInChildren<TMP_Text>();
                buttonText.text = " ";
            }
        }
    }

    public bool IsInRow(int row, int number, int[,] tab) // czy wyst¹pi³a w wierszu?
    {
        for (int i = 0; i < tab.GetLength(1); i++)
        {
            if (tab[row, i] == number)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsInColumn(int column, int number, int[,] tab)// czy wyst¹pi³a w kolumnie?
    {
        for (int i = 0; i < tab.GetLength(0); i++)
        {
            if (tab[i, column] == number)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsInSector(int row, int column, int number, int[,] tab) //czy wyst¹pi³a w kwadracie 9x9
    {
        int sectorRowStart = (row / 3) * 3;
        int sectorColumnStart = (column / 3) * 3;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (tab[sectorRowStart + i, sectorColumnStart + j] == number)
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

    bool IsBoardFull() //czy zainicjowany tablicê
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

    //wype³niamy losowe niektóre komórki z tablicy guzików odpowiadaj¹cymi komórkami z tablicy numerów
    void PuzzleMaker() //wersja wstêpna z pokazywaniem komórek na bazie iloœci
    {

        int show;
        if (difficulty == 1)
        {
            //show = rand.Next(79, 80);
            show = rand.Next(41, 51);
        }
        else if (difficulty == 2)
        {
            show = rand.Next(31, 41);
        }
        else
        {
            show = rand.Next(21,31);
        }
        for (int i = 0;i < show; i++)
        {

            int k = rand.Next(0,81);
            //Debug.Log("wylosowano : " + show);
            TMP_Text buttonText = BoardButtons[k].GetComponentInChildren<TMP_Text>();
            if (buttonText.text == " ")
            {
                buttonText.text = (Board[k % 9, k / 9]).ToString();
                PlayerBoard[k % 9, k / 9] = Board[k % 9, k / 9];
            }
            else
            {
                i--;
            }
            StartButtonDeactivate();
        }
    }
    
    void StartButtonDeactivate() // wy³¹cz guziki wype³nionych komórek
    {
        for (int i = 0; i < BoardButtons.GetLength(0); i++)
        {
                TMP_Text buttonText = BoardButtons[i].GetComponentInChildren<TMP_Text>();
                if (buttonText.text != " ")
                    BoardButtons[i].interactable = false;
        }
    }

    void DefineButtons() //przypisanie indeksów ka¿demu guzikowi do funkcji OnButtonClicked
    {
        for (int i = 0; i < BoardButtons.Length; i++)
        {
            int index = i;
            BoardButtons[i].onClick.AddListener(() => OnButtonClicked(index));
        }

    }

public void OnButtonClicked(int index) //logika "kolorowania guzików" i uzupe³niania planszy
    {
        int declared = PlayerPrefs.GetInt("number")+1;
        TMP_Text buttonText = BoardButtons[index].GetComponentInChildren<TMP_Text>();
        if (declared != 0)
        {
            if(declared == Board[index%9, index/9])
            {
                buttonText.text = (Board[index % 9, index / 9]).ToString();
                BoardButtons[index].interactable = false;
                Color currentColor = BoardButtons[index].image.color;
                if (currentColor.r == 0.992f && currentColor.g == 0.286f && currentColor.b == 0.286f)  //sprawdzamy czy wczeœniej pole by³o b³êdnie zaznaczone
                {
                    BoardButtons[index].image.color = new Color(0.898f, 0.678f, 0.122f, 1f);  // Pomarañczowy
                    PlayerBoard[index % 9, index / 9] = Board[index % 9, index / 9];
                }
                else
                {
                    BoardButtons[index].image.color = new Color(0.106f, 0.737f, 0.024f, 1f);  // Zielony    
                    PlayerBoard[index % 9, index / 9] = Board[index % 9, index / 9];
                }
            }
            else
            {
                int mistaketemp = PlayerPrefs.GetInt("mistake") + 1;
                PlayerPrefs.SetInt("mistake", mistaketemp);
                buttonText.text = declared.ToString();
                BoardButtons[index].image.color = new Color(0.992f, 0.286f, 0.286f); //czerwony
                // if (PlayerPrefs.GetInt("mistake")>=10)
                //ResetGame();
            }
        }
        else
            Debug.Log("nie wybrano numeru");
        /*if (IsPuzzleDone()) ju¿ niepotrzebne, teraz agent sprawdza warunek
        {
            ResetGame();
        }
        */
    //}

    /*
    public bool IsPuzzleDone() //warunek zwyciêstwa
    {
        for (int i = 0; i < BoardButtons.Length; i++)
        {
            Button button = BoardButtons[i];
            if (button.interactable == true)
                return false;
        }
        Debug.Log("Wygra³eœ!!!");
        round++;
        PlayerPrefs.SetInt("iter", round);
        return true;
    }
    */
    /*
    public void ResetGame() //
    {
        // Resetuj planszê (tablicê Board)
        BoardInit(Board);
        seed = (int)(System.DateTime.Now.Ticks / 10000); // Losowy seed
        seed = Math.Abs(seed);
        rand = new System.Random(seed); //RNG na bazie seed
        // Przywróæ stan przycisków
        for (int i = 0; i < BoardButtons.Length; i++)
        {
            Button button = BoardButtons[i];
            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            buttonText.text = " "; // Czyœæ tekst
            button.interactable = true; // Przywróæ interaktywnoœæ
            button.image.color = Color.white; // Przywróæ domyœlny kolor
            
        }

        // Wygeneruj now¹ planszê
        CreateBoard();

        // uzupe³nij losowe pola
        PuzzleMaker();

        // reset parametrów
        PlayerPrefs.SetInt("mistake", 0);
        clockreset.ResetTime();
        seedtext.text = ("Seed: " + seed).ToString();
        Debug.Log("Gra zosta³a zresetowana.");
        Debug.Log("Plansza po resecie:");
        ShowPlayerBoard();
        ShowTruePlayerBoard();
    }


  /// 
  ///  PRZYK£ADOWE KOMENDY DO MLAGENT, WYMAGA ZMIANY I POPRAWEK
  ///
    public int[,] GetBoard() //przekazujemy tablicê agentowi
    {
        return PlayerBoard;
    }

    public int[,] GetTrueBoard() //przekazujemy tablicê agentowi
    {
        return Board;
    }

    public Button[] GetButtons() //przekazujemy tablicê agentowi
    {
        return BoardButtons;
    }

    void ShowPlayerBoard()
    {
        string boardRepresentation = ""; // Przechowuje reprezentacjê planszy jako tekst

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                boardRepresentation += PlayerBoard[j, i] + " "; // Dodajemy liczbê i spacjê dla przejrzystoœci
            }
            boardRepresentation += "\n"; // Nowa linia po ka¿dym wierszu
        }

        Debug.Log(boardRepresentation); // Wypisujemy ca³¹ planszê jako tekst
    }

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
    
}*/
