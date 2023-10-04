using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 치트, UI, 랭킹, 게임오버
public class GameManager : MonoBehaviour
{
    public static GameManager Inst { get; private set; }

    void Awake() => Inst = this;
    [SerializeField] NotificationPanel notificationPanel;
    void Start()
    {
        StartGame();
    }

    void Update()
    {
#if UNITY_EDITOR
        InputCheatKey();
#endif
    }

    void InputCheatKey()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TurnManager.Inst.myTurn = true;
            TurnManager.OnAddCard?.Invoke(true);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TurnManager.Inst.myTurn = false;
            TurnManager.OnAddCard?.Invoke(false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TurnManager.Inst.EndTurn();
        }
    }

    public void StartGame()
    {
        StartCoroutine(TurnManager.Inst.StartGameCo());
    }

    public void Notification(string message)
    {
        notificationPanel.Show(message);
    }
}
