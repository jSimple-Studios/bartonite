using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public InputActionAsset actions;
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
    public float sceneGravity = -9.81f;

    void Start() {
        DontDestroyOnLoad(gameObject);
    }
    
}
