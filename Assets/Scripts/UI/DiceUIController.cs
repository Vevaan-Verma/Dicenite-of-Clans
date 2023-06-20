using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.Rendering.Universal;

public class DiceUIController : MonoBehaviour {

    [Header("References")]
    [SerializeField] private List<DiceRoller> diceRollers;
    private GameManager gameManager;
    private PlayerData playerData;
    private List<DiceRoller> rollersLeft;

    [Header("UI References")]
    [SerializeField] private CanvasGroup diceHUD;
    [SerializeField] private Image loadingScreen;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text woodText;
    [SerializeField] private TMP_Text brickText;
    [SerializeField] private TMP_Text metalText;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button kingdomButton;
    [SerializeField] private CanvasGroup testingHUD;
    [SerializeField] private GameObject rollButton;
    [SerializeField] private GameObject acceptButton;
    [SerializeField] private GameObject declineButton;

    [Header("Animations")]
    [SerializeField] private float healthLerpDuration;
    [SerializeField] private float materialLerpDuration;
    [SerializeField] private float diceHUDFadeDuration;
    [SerializeField][Range(0f, 1f)] private float diceHUDFadeOpacity;
    private Coroutine diceHUDFadeCoroutine;
    private Coroutine healthLerpCoroutine;
    private Coroutine woodLerpCoroutine;
    private Coroutine brickLerpCoroutine;
    private Coroutine metalLerpCoroutine;

    [Header("Scene Transitions")]
    [SerializeField] private string kingdomSceneName;
    [SerializeField] private float loadingFadeDuration;
    [SerializeField] private float loadingFadeOpacity;
    private Coroutine loadingFadeCoroutine;

    [Header("Testing Mode")]
    private RollRootObject importedRollData;
    private RollRootObject newRollData;
    private List<RollData> currDiceRollData;
    private bool testingModeEnabled;

    public event Action OnTestingModeToggle;

    private void Start() {

        gameManager = FindObjectOfType<GameManager>();
        playerData = FindObjectOfType<PlayerData>();

        if (loadingFadeCoroutine != null) {

            StopCoroutine(loadingFadeCoroutine);

        }

        loadingScreen.color = new Color(loadingScreen.color.r, loadingScreen.color.g, loadingScreen.color.b, 1f);
        StartFadeOutLoadingScreen(new Color(loadingScreen.color.r, loadingScreen.color.g, loadingScreen.color.b, 0f));

        healthSlider.maxValue = playerData.GetMaxHealth();
        UpdateHealthSlider();

        buildButton.GetComponentInChildren<TMP_Text>().text = "Build x" + gameManager.GetBuildersDice();
        buildButton.onClick.AddListener(RollBuildersDice);

        attackButton.GetComponentInChildren<TMP_Text>().text = "Attack x" + gameManager.GetAttackDice();
        attackButton.onClick.AddListener(RollAttackDice);

        kingdomButton.onClick.AddListener(LoadKingdomScene);

        UpdateHealthSlider();
        UpdateWoodCount();
        UpdateBrickCount();
        UpdateMetalCount();

        OnTestingModeToggle += ToggleTestingMode;

        testingHUD.gameObject.SetActive(false);

        rollButton.GetComponent<Button>().onClick.AddListener(RollTestingBuildersDice);
        acceptButton.GetComponent<Button>().onClick.AddListener(AcceptTestingRoll);
        declineButton.GetComponent<Button>().onClick.AddListener(DeclineTestingRoll);

        if (!File.Exists(gameManager.GetDiceRollFilePath())) {

            File.Create(gameManager.GetDiceRollFilePath());

        }

        importedRollData = new RollRootObject();

        using (StreamReader sr = new StreamReader(gameManager.GetDiceRollFilePath())) {

            if (sr.EndOfStream) {

                Debug.LogWarning("There are no rolls to execute! Enter developer testing mode to add some!");

            } else {

                importedRollData = JsonConvert.DeserializeObject<RollRootObject>(sr.ReadToEnd());

            }
        }

        newRollData = new RollRootObject();

    }

    private void Update() {

        if ((Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.T)) || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.LeftShift) && Input.GetKey(KeyCode.T)) || (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.T)) || (Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.LeftShift) && Input.GetKey(KeyCode.T)) || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.T)) || (Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.T))) {

            OnTestingModeToggle?.Invoke();

        }
    }

    private void OnApplicationQuit() {

        if (testingModeEnabled) {

            SaveRollData();

        }
    }

    private void RollBuildersDice() {

        if (importedRollData.rollData.Count == 0) {

            Debug.LogWarning("There are no rolls to execute! Enter developer testing mode to add some!");
            return;

        }

        DisableRollButtons();

        gameManager.ClearAllDice();
        StartFadeOutDiceHUD(diceHUDFadeOpacity);

        int rollIndex = UnityEngine.Random.Range(0, importedRollData.rollData.Count);

        for (int i = 0; i < gameManager.GetBuildersDice(); i++) {

            diceRollers[importedRollData.rollData[rollIndex][i].GetDiceRoller()].RollBuildersDice(importedRollData.rollData[rollIndex][i].GetDiceRotation(), importedRollData.rollData[rollIndex][i].GetDiceVelocity());

        }
    }

    private void RollAttackDice() {

        DisableRollButtons();

        rollersLeft = new List<DiceRoller>();

        foreach (DiceRoller roller in diceRollers) {

            rollersLeft.Add(roller);

        }

        gameManager.ClearAllDice();
        StartFadeOutDiceHUD(diceHUDFadeOpacity);

        int randInt;

        for (int i = 0; i < gameManager.GetAttackDice(); i++) {

            randInt = UnityEngine.Random.Range(0, rollersLeft.Count);
            rollersLeft[randInt].RollAttackDice();
            rollersLeft.RemoveAt(randInt);

        }
    }

    private void LoadKingdomScene() {

        StartFadeOutDiceHUD(0f);

        if (loadingFadeCoroutine != null) {

            StopCoroutine(loadingFadeCoroutine);

        }

        loadingScreen.color = new Color(loadingScreen.color.r, loadingScreen.color.g, loadingScreen.color.b, 0f);

        loadingFadeCoroutine = StartCoroutine(FadeLoadingScreen(loadingScreen.color, new Color(loadingScreen.color.r, loadingScreen.color.g, loadingScreen.color.b, loadingFadeOpacity), true, kingdomSceneName));

    }

    protected void StartFadeOutLoadingScreen(Color targetColor) {

        if (loadingFadeCoroutine != null) {

            StopCoroutine(loadingFadeCoroutine);

        }

        loadingFadeCoroutine = StartCoroutine(FadeLoadingScreen(loadingScreen.color, targetColor, false, ""));

    }

    private IEnumerator FadeLoadingScreen(Color startColor, Color targetColor, bool fadeIn, string sceneName) {

        float currentTime = 0f;
        loadingScreen.gameObject.SetActive(true);

        while (currentTime < loadingFadeDuration) {

            currentTime += Time.deltaTime;
            loadingScreen.color = Color.Lerp(startColor, targetColor, currentTime / loadingFadeDuration);
            yield return null;

        }

        loadingScreen.color = targetColor;
        loadingFadeCoroutine = null;

        if (fadeIn) {

            SceneManager.LoadSceneAsync(sceneName);

        } else {

            loadingScreen.gameObject.SetActive(false);

        }
    }

    public void UpdateHealthSlider() {

        if (healthLerpCoroutine != null) {

            StopCoroutine(healthLerpCoroutine);

        }

        healthLerpCoroutine = StartCoroutine(LerpHealthSlider((int) healthSlider.value, playerData.GetHealth()));

    }

    private IEnumerator LerpHealthSlider(int startHealth, int targetHealth) {

        float currentTime = 0f;

        while (currentTime < healthLerpDuration) {

            currentTime += Time.deltaTime;
            healthSlider.value = Mathf.Lerp(startHealth, targetHealth, currentTime / healthLerpDuration);
            healthText.text = healthSlider.value + "";
            yield return null;

        }

        healthSlider.value = targetHealth;
        healthText.text = healthSlider.value + "";
        healthLerpCoroutine = null;

    }

    public void UpdateWoodCount() {

        if (woodLerpCoroutine != null) {

            StopCoroutine(woodLerpCoroutine);

        }

        int.TryParse(woodText.text, out int woodCount);
        woodLerpCoroutine = StartCoroutine(LerpWoodCount(woodCount, playerData.GetWoodCount()));

    }

    private IEnumerator LerpWoodCount(int startWood, int targetWood) {

        float currentTime = 0f;

        while (currentTime < materialLerpDuration) {

            currentTime += Time.deltaTime;
            woodText.text = (int) Mathf.Lerp(startWood, targetWood, currentTime / materialLerpDuration) + "";
            yield return null;

        }

        woodText.text = targetWood + "";
        woodLerpCoroutine = null;

    }

    public void UpdateBrickCount() {

        if (brickLerpCoroutine != null) {

            StopCoroutine(brickLerpCoroutine);

        }

        int.TryParse(brickText.text, out int brickCount);
        brickLerpCoroutine = StartCoroutine(LerpBrickCount(brickCount, playerData.GetBrickCount()));

    }

    private IEnumerator LerpBrickCount(int startBrick, int targetBrick) {

        float currentTime = 0f;

        while (currentTime < materialLerpDuration) {

            currentTime += Time.deltaTime;
            brickText.text = (int) Mathf.Lerp(startBrick, targetBrick, currentTime / materialLerpDuration) + "";
            yield return null;

        }

        brickText.text = targetBrick + "";
        brickLerpCoroutine = null;

    }

    public void UpdateMetalCount() {

        if (metalLerpCoroutine != null) {

            StopCoroutine(metalLerpCoroutine);

        }

        int.TryParse(metalText.text, out int metalCount);
        metalLerpCoroutine = StartCoroutine(LerpMetalCount(metalCount, playerData.GetMetalCount()));

    }

    private IEnumerator LerpMetalCount(int startMetal, int targetMetal) {

        float currentTime = 0f;

        while (currentTime < materialLerpDuration) {

            currentTime += Time.deltaTime;
            metalText.text = (int) Mathf.Lerp(startMetal, targetMetal, currentTime / materialLerpDuration) + "";
            yield return null;

        }

        metalText.text = targetMetal + "";
        metalLerpCoroutine = null;

    }

    public void EnableRollButtons() {

        buildButton.interactable = true;
        attackButton.interactable = true;

    }

    public void DisableRollButtons() {

        buildButton.interactable = false;
        attackButton.interactable = false;

    }

    public void StartFadeInDiceHud() {

        if (diceHUDFadeCoroutine != null) {

            StopCoroutine(diceHUDFadeCoroutine);

        }

        diceHUDFadeCoroutine = StartCoroutine(FadeDiceHUD(diceHUD.alpha, 1f, true));

    }

    public void StartFadeOutDiceHUD(float targetOpacity) {

        if (diceHUDFadeCoroutine != null) {

            StopCoroutine(diceHUDFadeCoroutine);

        }

        kingdomButton.GetComponent<SlideUIButton>().DisableSlideIn();
        diceHUDFadeCoroutine = StartCoroutine(FadeDiceHUD(diceHUD.alpha, targetOpacity, false));

    }

    private IEnumerator FadeDiceHUD(float startOpacity, float targetOpacity, bool fadeIn) {

        float currentTime = 0f;

        while (currentTime < diceHUDFadeDuration) {

            currentTime += Time.deltaTime;
            diceHUD.alpha = Mathf.Lerp(startOpacity, targetOpacity, currentTime / diceHUDFadeDuration);
            yield return null;

        }

        diceHUD.alpha = targetOpacity;
        diceHUDFadeCoroutine = null;

        if (fadeIn) {

            kingdomButton.GetComponent<SlideUIButton>().EnableSlideIn();

        }
    }

    public bool GetTestingModeState() {

        return testingModeEnabled;

    }

    private void ToggleTestingMode() {

        gameManager.ClearAllDice();
        testingModeEnabled = !testingModeEnabled;

        if (testingModeEnabled) {

            Debug.LogWarning("Developer testing mode enabled!");
            diceHUD.gameObject.SetActive(false);
            testingHUD.gameObject.SetActive(true);
            rollButton.SetActive(true);
            acceptButton.SetActive(false);
            declineButton.SetActive(false);

        } else {

            SaveRollData();
            Debug.LogWarning("Developer testing mode disabled!");
            testingHUD.gameObject.SetActive(false);
            diceHUD.gameObject.SetActive(true);
            EnableRollButtons();
            StartFadeInDiceHud();

        }
    }

    private void RollTestingBuildersDice() {

        rollersLeft = new List<DiceRoller>();

        foreach (DiceRoller roller in diceRollers) {

            rollersLeft.Add(roller);

        }

        gameManager.ClearAllDice();

        currDiceRollData = new List<RollData>();
        int randInt;

        for (int i = 0; i < gameManager.GetBuildersDice(); i++) {

            RollData rollData = new RollData();
            randInt = UnityEngine.Random.Range(0, rollersLeft.Count);
            rollData = rollersLeft[randInt].RollTestingBuildersDice(rollData);
            currDiceRollData.Add(rollData);
            rollersLeft.RemoveAt(randInt);

        }

        rollButton.SetActive(false);
        acceptButton.GetComponent<Button>().interactable = false;
        declineButton.GetComponent<Button>().interactable = false;
        acceptButton.SetActive(true);
        declineButton.SetActive(true);

    }

    public void FinishTestingRolls() {

        acceptButton.GetComponent<Button>().interactable = true;
        declineButton.GetComponent<Button>().interactable = true;

    }

    private void AcceptTestingRoll() {

        newRollData.rollData.Add(currDiceRollData);
        gameManager.ClearAllDice();
        acceptButton.SetActive(false);
        declineButton.SetActive(false);
        rollButton.SetActive(true);

    }

    private void DeclineTestingRoll() {

        gameManager.ClearAllDice();
        acceptButton.SetActive(false);
        declineButton.SetActive(false);
        rollButton.SetActive(true);

    }

    private void SaveRollData() {

        using (StreamWriter sw = new StreamWriter(gameManager.GetDiceRollFilePath())) {

            importedRollData.rollData.AddRange(newRollData.rollData);
            newRollData.rollData.Clear();

            sw.Write(JsonConvert.SerializeObject(importedRollData, Formatting.Indented, new JsonSerializerSettings {

                ReferenceLoopHandling = ReferenceLoopHandling.Ignore

            }));
        }
    }
}
