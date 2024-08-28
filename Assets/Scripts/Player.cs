using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using System.Transactions;

public class Player : NetworkBehaviour {
    Vector2 mouseDelta;
    GameObject cam;
    float sensitivity = .1f; // put in gamemanager later
    Vector2 rotation;
    GameManager gm;
    CharacterController ctrl;
    public float speed = 1;
    Vector2 rawInputMovement;
    public float yPrev;
    public bool isGrounded;
    public float jumpSpeed;
    public float height;
    bool jumpGo;
    public Vector3 vel;
    float yVel;
    bool doGrav;
    Vector2 smoothInput;
    public bool flight;
    public GameObject mesh;

    void Start() {
        doGrav = false;
        cam = GetComponentInChildren<Camera>().gameObject;
        gm = GameManager.Singleton;
        ctrl = GetComponent<CharacterController>();
        if (isLocalPlayer) {
            print("setting local player settings");
            gameObject.layer = LayerMask.NameToLayer("LocalPlayer");
            foreach (Transform go in mesh.GetComponentInChildren<Transform>())
                go.gameObject.layer = LayerMask.NameToLayer("LocalPlayer");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        } else {
            cam.SetActive(false);
        }
        StartCoroutine(GravTimer());
    }

    IEnumerator GravTimer() {
        yield return new WaitForSeconds(2); // might need to increase if pc is less beef
        doGrav = true;
    }

    void Update() {
        // TODO: server authority
        if (isLocalPlayer && Application.isFocused) {
            // rotation
            // TODO: allow for controller input as well
            mouseDelta = Mouse.current.delta.ReadValue();
            rotation.y += mouseDelta.x * sensitivity;
            rotation.x -= mouseDelta.y * sensitivity;
            rotation.x = Mathf.Clamp(rotation.x, -90, 90);
            transform.localEulerAngles = new Vector3(0, rotation.y, 0);
            cam.transform.localEulerAngles = new Vector3(rotation.x, 0, 0);

            // gravity
            if (doGrav && !flight) yVel += gm.sceneGravity * Time.deltaTime;
            if (doGrav && flight) yVel += gm.sceneGravity / 10 * Time.deltaTime;
            
            // movement
            if (Vector3.Distance(smoothInput,rawInputMovement) >= .2)
                smoothInput = Vector2.Lerp(smoothInput, rawInputMovement, 5f * Time.deltaTime);
            else smoothInput = rawInputMovement;
            Vector3 vel = transform.forward * smoothInput.y * speed + transform.right * smoothInput.x * speed + new Vector3(0, yVel, 0);
            
            ctrl.Move(vel * Time.deltaTime);
        }
        
    }

    void FixedUpdate() {
        if (!flight) {
            RaycastHit hit;
            // Bit shift the index of the layer (8) to get a bit mask
            int layerMask = 1 << 8;

            // This would cast rays only against colliders in layer 8.
            // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
            layerMask = ~layerMask;
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hit, height, layerMask)) {
                isGrounded = true;
                // print("jump cast hit");
            } else {
                isGrounded = false;
                // print("jump cast missed");
            }
            if (isGrounded && jumpGo) {
                yVel = jumpSpeed;
                // print("Jump!");
                isGrounded = false;
            }
        } else {
            if (jumpGo) yVel = Mathf.Lerp(yVel, jumpSpeed, 0.1f);
        }
    }

    public void OnMovementX(InputAction.CallbackContext value)
    {
        float inputMovement = value.ReadValue<float>();
        rawInputMovement = new Vector2(inputMovement, rawInputMovement.y);
    }

    public void OnMovementY(InputAction.CallbackContext value)
    {
        float inputMovement = value.ReadValue<float>();
        rawInputMovement = new Vector2(rawInputMovement.x, inputMovement);
    }

    public void OnJump(InputAction.CallbackContext value){
        // print(value.ReadValue<float>());
        if (value.ReadValue<float>() == 0) jumpGo = false;
        else jumpGo = true;
    }
}
