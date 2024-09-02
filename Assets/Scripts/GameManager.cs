using Mirror;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public GameObject[] toggleVisOnJoin;
    public RawImage charge;
    public static GameManager Singleton {
        get {
            if (_singleton is null) {
                _singleton = FindObjectOfType<GameManager>();
                print("GameManager is NULL");
            }
            return _singleton;
        }
    }
    static GameManager _singleton;
    static NetworkManager nm;
    public float sceneGravity = -9.81f;
    public Image heathBar;
    public TMPro.TMP_Text addrLabel;
    public TMPro.TMP_Text streakLabel;
    public TMPro.TMP_InputField addrBox;
    public GameObject ultText;

    int UILayer;
    void Start() {
        DontDestroyOnLoad(gameObject);
        nm = GetComponent<NetworkManager>();
        UILayer = LayerMask.NameToLayer("UI");
    }

    public void Suffer() {
        if (addrBox.text != "") {
            nm.StartClient();
            addrLabel.text = "bartonite on " + nm.networkAddress;
        } else {
            nm.StartHost();
            addrLabel.text = "hosting bartonite";
        }
        foreach (var obj in toggleVisOnJoin) {
            obj.SetActive(!obj.activeSelf);
        }
    }

    public void Quit() {
        if (addrBox.text != "") {
            nm.StopClient();
        } else {
            nm.StopHost();
        }
        foreach (var obj in toggleVisOnJoin) {
            obj.SetActive(!obj.activeSelf);
        }
    }

    //Returns 'true' if we touched or hovering on Unity UI element.
    public bool IsPointerOverUIElement() {
        List<RaycastResult> eventSystemRaycastResults = GetEventSystemRaycastResults();
        for (int index = 0; index < eventSystemRaycastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaycastResults[index];
            if (curRaysastResult.gameObject.layer == UILayer)
                return true;
        }
        return false;
    }


    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}