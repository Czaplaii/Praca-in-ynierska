using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class SudokuAgent : Agent
{
    [SerializeField] private Board_creator boardCreator;

    public override void CollectObservations(VectorSensor sensor)
    {
        // Pobierz planszê Sudoku jako obserwacje
        int[,] board = boardCreator.GetBoard();
        foreach (int cell in board)
        {
            sensor.AddObservation(cell); // Ka¿da komórka (0-9)
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Odczytaj akcje agenta
        int row = actions.DiscreteActions[0];
        int col = actions.DiscreteActions[1];
        int number = actions.DiscreteActions[2] + 1; // Liczby 1-9

        // Próbuj wstawiæ liczbê na planszê
        bool success = boardCreator.MakeMove(row, col, number);

        if (success)
        {
            AddReward(5.0f); // Nagroda za poprawny ruch
        }
        else
        {
            AddReward(-5.0f); // Kara za niepoprawny ruch
        }

        if (boardCreator.IsPuzzleDone())
        {
            AddReward(50.0f); // Nagroda za rozwi¹zanie planszy
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Podaj heurystyczne wartoœci dla testów (opcjonalnie)
        var actions = actionsOut.DiscreteActions;
        actions[0] = Random.Range(0, 9); // Rz¹d
        actions[1] = Random.Range(0, 9); // Kolumna
        actions[2] = Random.Range(0, 9); // Liczba
    }
}
