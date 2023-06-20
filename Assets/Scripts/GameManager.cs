using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    [Header("Settings")]
    [SerializeField] private float diceStillTime;
    [SerializeField] private int buildersDice;
    [SerializeField] private int attackDice;
    [SerializeField] private int gridWidth;
    [SerializeField] private int gridHeight;
    [SerializeField] private string diceRollFilePath;

    public enum MaterialType {

        Wood, Brick, Metal

    }

    private void Start() {

        DontDestroyOnLoad(gameObject);

    }

    public void ClearAllDice() {

        foreach (DiceController diceController in FindObjectsOfType<DiceController>()) {

            Destroy(diceController.gameObject);

        }
    }

    public float GetDiceStillTime() {

        return diceStillTime;

    }

    public int GetBuildersDice() {

        return buildersDice;

    }

    public int GetAttackDice() {

        return attackDice;

    }

    public int GetGridWidth() {

        return gridWidth;

    }

    public int GetGridHeight() {

        return gridHeight;

    }

    public string GetDiceRollFilePath() {

        return diceRollFilePath;

    }
}