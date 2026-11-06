using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Objective { CollectCoins, ReachGoal, Survive }

// One GameManager per level: it owns the score, the lives, the objective, and the win and lose states.
// Lecture 06's GameManager (score, win, restart) plus Lecture 07's lives, respawn and Lose().
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Objective")]
    public Objective objective = Objective.CollectCoins;
    public int coinsRequired = 8;        // CollectCoins: win at this score. ReachGoal / Survive: needed as well.
    public float surviveSeconds = 0f;    // Survive: the clock to outlast
    public float timeLimit = 0f;         // > 0: reach the goal before it runs out, or lose
    public string levelTitle = "Level";
    [TextArea] public string intro = "";

    [Header("Lives")]
    public int lives = 3;
    public float respawnDelay = 0.6f;
    public float spawnProtection = 1.5f;

    [Header("Scene references (set by the level builder)")]
    public Health player;
    public Transform respawnPoint;
    public TMP_Text scoreText;
    public TMP_Text livesText;
    public TMP_Text objectiveText;
    public TMP_Text timerText;
    public TMP_Text messageText;
    public GameObject winPanel;
    public GameObject losePanel;
    public TMP_Text winDetail;
    public TMP_Text loseDetail;
    public GameObject nextButton;
    public TMP_Text nextButtonLabel;
    public string nextSceneName = "";

    enum State { Playing, Won, Lost }
    State state = State.Playing;
    bool loading;
    public bool PanelOpen => state != State.Playing;
    public bool Won => state == State.Won;
    int score;
    int totalCoins;
    float elapsed;
    bool respawning;
    int lastShownSecond = -1;
    Coroutine messageRoutine;

    void Awake()
    {
        Instance = this;      // Lecture 06 slide 29: set in Awake, so every Start can use it
        Time.timeScale = 1f;  // a level always starts running, even after a frozen win or loss
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex < 0)
            Debug.LogWarning("This scene is not in the Build Profiles scene list. Restart and Next Level still work in the editor, but add every level to the list.");
        totalCoins = FindObjectsByType<Coin>(FindObjectsSortMode.None).Length;
        if (coinsRequired > totalCoins)
            Debug.LogWarning($"{levelTitle}: coinsRequired ({coinsRequired}) is higher than the coins in the level ({totalCoins}). It can't be won.");

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        UpdateUI();
        if (!string.IsNullOrEmpty(intro)) ShowMessage(intro, 6f);
    }

    void Update()
    {
        if (state != State.Playing) return;
        elapsed += Time.deltaTime;

        if (timeLimit > 0f && elapsed >= timeLimit)
        {
            Lose("Time's up!");
            return;
        }
        if (objective == Objective.Survive) CheckWin();
        UpdateTimerText();
    }

    // ---------------------------------------------------------------- score and win
    public void AddScore(int amount)
    {
        if (state != State.Playing) return;
        score += amount;
        UpdateUI();
        CheckWin();
    }

    void CheckWin()
    {
        if (state != State.Playing) return;
        switch (objective)
        {
            case Objective.CollectCoins:
                if (score >= coinsRequired) Win();
                break;
            case Objective.Survive:
                if (elapsed >= surviveSeconds && score >= coinsRequired) Win();
                break;
            // ReachGoal is decided by the Goal, in ReachGoal() below.
        }
    }

    public void ReachGoal()
    {
        if (state != State.Playing || objective != Objective.ReachGoal) return;
        if (score >= coinsRequired) Win();
        else ShowMessage($"The goal needs {coinsRequired - score} more coin(s)", 2f);
    }

    void Win()
    {
        state = State.Won;
        string detail = $"Coins: {score} / {totalCoins}\nTime: {FormatTime(elapsed)}\nLives left: {lives}";
        SetText(winDetail, detail);
        if (nextButton != null) nextButton.SetActive(!string.IsNullOrEmpty(nextSceneName));
        if (winPanel != null) winPanel.SetActive(true);
        HideMessage();
        Time.timeScale = 0f;
    }

    // ---------------------------------------------------------------- lives, respawn, lose
    // Health calls this at <= 0 hearts; a KillZone calls it when you fall.
    public void PlayerDied()
    {
        if (state != State.Playing || respawning) return;
        lives--;
        UpdateUI();
        if (lives <= 0) Lose("You ran out of lives");
        else StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        respawning = true;
        yield return new WaitForSeconds(respawnDelay);   // a beat to see it happen; also lets a queued knockback finish
        if (state == State.Playing)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            pc.Respawn(respawnPoint.position);           // standing still, at the respawn point
            player.Restore(spawnProtection);             // full hearts, and a moment of protection
        }
        respawning = false;
        UpdateUI();
    }

    void Lose(string reason)
    {
        state = State.Lost;
        SetText(loseDetail, reason + $"\nCoins: {score} / {totalCoins}");
        if (losePanel != null) losePanel.SetActive(true);
        HideMessage();
        Time.timeScale = 0f;
    }

    public void SetCheckpoint(Transform point)
    {
        if (respawnPoint == point) return;
        respawnPoint = point;
        ShowMessage("Checkpoint!", 1.5f);
    }

    // ---------------------------------------------------------------- restart and next level
    public void Restart()
    {
        if (loading) return;
        Debug.Log("Restart: reloading " + SceneManager.GetActiveScene().name);
        Time.timeScale = 1f;   // first: timeScale survives a scene load
        Load(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        if (loading) return;
        Debug.Log("Next level: " + (string.IsNullOrEmpty(nextSceneName) ? "(same scene)" : nextSceneName));
        Time.timeScale = 1f;
        Load(string.IsNullOrEmpty(nextSceneName) ? SceneManager.GetActiveScene().name : nextSceneName);
    }

    // Loads by NAME, so it works whatever the scene's build index is. A scene that is not in the Build Profiles
    // scene list cannot be loaded normally; inside the editor we fall back to loading it straight from its file.
    void Load(string sceneName)
    {
        loading = true;
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }
#if UNITY_EDITOR
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets(sceneName + " t:Scene"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
            {
                Debug.LogWarning($"'{sceneName}' is not in the Build Profiles scene list; loading it from {path}. Add all levels to the scene list before you submit.");
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
        }
#endif
        Debug.LogError($"Cannot load scene '{sceneName}'. Add it in File > Build Profiles > Scene List.");
        loading = false;
        Time.timeScale = 0f;
    }

    // ---------------------------------------------------------------- UI (text is set only when it changes)
    public void UpdateUI()
    {
        string coinLabel = objective == Objective.CollectCoins ? "Coins" : "Coins (needed)";
        SetText(scoreText, $"{coinLabel}: {score} / {coinsRequired}");

        int hearts = player != null ? Mathf.Max(player.Current, 0) : 0;
        int maxHearts = player != null ? player.Max : 0;
        SetText(livesText, $"Lives: {lives}   Health: {hearts} / {maxHearts}");

        switch (objective)
        {
            case Objective.CollectCoins:
                SetText(objectiveText, $"{levelTitle}: collect {coinsRequired} coins");
                break;
            case Objective.ReachGoal:
                SetText(objectiveText, $"{levelTitle}: reach the goal with {coinsRequired} coins");
                break;
            case Objective.Survive:
                SetText(objectiveText, $"{levelTitle}: collect {coinsRequired} coins and survive {surviveSeconds:0}s");
                break;
        }
        UpdateTimerText();
    }

    void UpdateTimerText()
    {
        if (timerText == null) return;
        if (timeLimit > 0f) SetSecond("Time", timeLimit - elapsed);
        else if (objective == Objective.Survive) SetSecond("Survive", surviveSeconds - elapsed);
        else if (timerText.gameObject.activeSelf) timerText.gameObject.SetActive(false);
    }

    void SetSecond(string label, float secondsLeft)
    {
        int shown = Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
        if (shown == lastShownSecond) return;      // only when the shown second changes
        lastShownSecond = shown;
        timerText.gameObject.SetActive(true);
        timerText.text = $"{label} {shown / 60}:{shown % 60:00}";
    }

    static void SetText(TMP_Text t, string s)
    {
        if (t != null && t.text != s) t.text = s;
    }

    public void ShowMessage(string message, float seconds = 2.5f)
    {
        if (messageText == null || state != State.Playing) return;
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(HideMessageAfter(seconds));
    }

    IEnumerator HideMessageAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        messageText.gameObject.SetActive(false);
        messageRoutine = null;
    }

    void HideMessage()
    {
        if (messageText != null) messageText.gameObject.SetActive(false);
    }

    static string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return $"{s / 60}:{s % 60:00}";
    }
}
