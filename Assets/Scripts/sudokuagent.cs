using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class SudokuAgent : Agent
{
    [SerializeField] private Board_creator boardCreator;

    public override void CollectObservations(VectorSensor sensor)
    {
        // Pobierz plansz� Sudoku jako obserwacje
        int[,] board = boardCreator.GetBoard();
        foreach (int cell in board)
        {
            sensor.AddObservation(cell); // Ka�da kom�rka (0-9)
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Odczytaj akcje agenta
        int row = actions.DiscreteActions[0];
        int col = actions.DiscreteActions[1];
        int number = actions.DiscreteActions[2] + 1; // Liczby 1-9

        // Pr�buj wstawi� liczb� na plansz�
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
            AddReward(50.0f); // Nagroda za rozwi�zanie planszy
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Podaj heurystyczne warto�ci dla test�w (opcjonalnie)
        var actions = actionsOut.DiscreteActions;
        actions[0] = Random.Range(0, 9); // Rz�d
        actions[1] = Random.Range(0, 9); // Kolumna
        actions[2] = Random.Range(0, 9); // Liczba
    }
}
