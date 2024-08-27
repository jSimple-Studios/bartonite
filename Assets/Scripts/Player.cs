using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{
    Vector2 mouseDelta;
    GameObject cam;
    float sensitivity = .1f; // put in gamemanager later
    Vector2 rotation;
    GameManager gm;
    CharacterController ctrl;
    public float speed = 1;
    Vector2 rawInputMovement;
    public float yVel;
    public float yPrev;
    public bool isGrounded;
    public float jumpSpeed;
    public float height;
    bool jumpGo;

    void Start() {
        cam = GetComponentInChildren<Camera>().gameObject;
        gm = GameManager.Singleton;
        ctrl = GetComponent<CharacterController>();
        if (isLocalPlayer) {
            print("setting local player settings");
            gameObject.layer = LayerMask.NameToLayer("LocalPlayer");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        } else {
            cam.SetActive(false);
        }
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
            yVel += gm.sceneGravity * Time.deltaTime;
            ctrl.Move(new Vector3(0, yVel * Time.deltaTime,0));
            
            // movement
            Vector3 moveDir = transform.forward * rawInputMovement.y + transform.right * rawInputMovement.x;
            ctrl.Move(speed * Time.deltaTime * moveDir);
        }
        
    }

    void FixedUpdate() {
        RaycastHit hit;
        // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 1 << 8;

        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        layerMask = ~layerMask;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hit, height, layerMask)) {
            isGrounded = true;
            print("jump cast hit");
        } else {
            isGrounded = false;
            print("jump cast missed");
        }
        if (isGrounded && jumpGo) {
            yVel = jumpSpeed;
            // print("Jump!");
            isGrounded = false;
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
