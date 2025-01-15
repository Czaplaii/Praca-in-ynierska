using UnityEngine;
using UnityEditor;
using static Board_creator;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(HorizontalArrayExample))]
public class HorizontalArrayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        HorizontalArrayExample example = (HorizontalArrayExample)target;

        // Wyœwietlanie Sudoku
        EditorGUILayout.LabelField("Sudoku Grid", EditorStyles.boldLabel);
        for (int y = 0; y < 9; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < 9; x++)
            {
                int value = example.GetSudokuValue(x, y);
                int newValue = EditorGUILayout.IntField(value, GUILayout.Width(40));
                if (newValue != value)
                {
                    example.SetSudokuValue(x, y, newValue);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        // Wyœwietlanie Puzzle
        EditorGUILayout.LabelField("Puzzle Grid", EditorStyles.boldLabel);
        for (int y = 0; y < 9; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < 9; x++)
            {
                int value = example.GetPuzzleValue(x, y);
                int newValue = EditorGUILayout.IntField(value, GUILayout.Width(40));
                if (newValue != value)
                {
                    example.SetPuzzleValue(x, y, newValue);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
