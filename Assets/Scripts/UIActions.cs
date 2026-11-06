using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// The win and lose buttons. The uGUI Buttons call Restart and NextLevel, and this script ALSO listens
// for the same clicks and keys itself, so the buttons work even if the Event System has no input set up.
// Keys: R restarts, Enter or N goes to the next level after a win.
public class UIActions : MonoBehaviour
{
    RectTransform winRestart, winNext, loseRestart;

    public void Restart() { GameManager.Instance.Restart(); }
    public void NextLevel() { GameManager.Instance.NextLevel(); }

    void Start()
    {
        winRestart = transform.Find("WinPanel/RestartButton") as RectTransform;
        winNext = transform.Find("WinPanel/NextButton") as RectTransform;
        loseRestart = transform.Find("LosePanel/RestartButton") as RectTransform;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (!gm.PanelOpen)
        {
            if (KeyRestart()) gm.Restart();         // R restarts the level at any time
            return;
        }               // Update still runs while Time.timeScale is 0

        bool restart = KeyRestart();
        bool next = gm.Won && KeyNext();

        if (ClickThisFrame())
        {
            Vector2 pos = PointerPosition();
            if (gm.Won)
            {
                if (Hit(winNext, pos)) next = true;
                if (Hit(winRestart, pos)) restart = true;
            }
            else if (Hit(loseRestart, pos)) restart = true;
        }

        if (restart) gm.Restart();
        else if (next) gm.NextLevel();
    }

    static bool Hit(RectTransform rt, Vector2 screenPoint)
    {
        return rt != null && rt.gameObject.activeInHierarchy
            && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, null);   // overlay canvas: no camera
    }

    static bool KeyRestart()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.R)) return true;
#endif
        return false;
    }

    static bool KeyNext()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.nKey.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.N)) return true;
#endif
        return false;
    }

    static bool ClickThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0)) return true;
#endif
        return false;
    }

    static Vector2 PointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }
}
