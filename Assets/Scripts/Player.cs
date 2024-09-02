using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


public class Player : NetworkBehaviour {
    Vector2 mouseDelta;
    GameObject cam;
    float sensitivity = .1f; // put in gamemanager later
    Vector2 rotation;
    GameManager gm;
    CharacterController ctrl;
    public float speed;
    public float glideSpeed;
    Vector2 rawInputMovement;
    public float yPrev;
    public bool isGrounded;
    public float jumpSpeed;
    public float reGlideSpeed;
    public float height;
    bool jumpGo;
    public Vector3 vel;
    float yVel;
    bool doGrav;
    Vector2 smoothInput;
    public bool flight;
    public GameObject mesh;
    public Animator animator;
    bool cancelanim;
    public bool gliding = true;
    [SyncVar] public int charge;
    [SyncVar] public int streak;
    bool paused;
    public GameObject hat;
    [SyncVar] public int health;
    public int maxhealth;
    Collider prevAtk;
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
        if (isLocalPlayer) {
            if (Application.isFocused) {
                // rotation
                // TODO: allow for controller input as well
                mouseDelta = Mouse.current.delta.ReadValue();
                rotation.y += mouseDelta.x * sensitivity;
                rotation.x -= mouseDelta.y * sensitivity;
                rotation.x = Mathf.Clamp(rotation.x, -90, 90);
                transform.localEulerAngles = new Vector3(0, rotation.y, 0);
                cam.transform.localEulerAngles = new Vector3(rotation.x, 0, 0);
                if (Input.GetKey(KeyCode.Escape)) {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    paused = true;
                }
                if (Input.anyKey && !Input.GetKey(KeyCode.Escape) && !gm.IsPointerOverUIElement()) {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    paused = false;
                }
            }

            // gravity
            if (doGrav && !flight) yVel += gm.sceneGravity * Time.deltaTime;
            if (doGrav && flight) yVel += gm.sceneGravity / 10 * Time.deltaTime;
            
            // movement
            if (Vector3.Distance(smoothInput,rawInputMovement) >= .2)
                smoothInput = Vector2.Lerp(smoothInput, rawInputMovement, 5f * Time.deltaTime);
            else smoothInput = rawInputMovement;
            Vector3 vel;
            if (gliding) vel = transform.forward * smoothInput.y * glideSpeed + transform.right * smoothInput.x * glideSpeed + new Vector3(0, yVel, 0);
            else vel = transform.forward * smoothInput.y * speed + transform.right * smoothInput.x * speed + new Vector3(0, yVel, 0);
            if (smoothInput.magnitude >= .2f && !cancelanim) animator.SetInteger("state", 1);
            else if (!cancelanim) animator.SetInteger("state", 0);

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
                gliding = false;
            } else {
                isGrounded = false;
            }
            if (isGrounded && jumpGo) {
                yVel = jumpSpeed;
                isGrounded = false;
            }
        } else {
            if (jumpGo) yVel = Mathf.Lerp(yVel, jumpSpeed, 0.1f);
        }
        if (isLocalPlayer) {
            // update ui
            if (charge < 5 && charge >= 0) {
                gm.charge.texture = Resources.Load<Texture>("charge-" + charge);
                gm.ultText.SetActive(false);
            } else if (charge < 0) {
                gm.charge.texture = Resources.Load<Texture>("charge-" + 0);
            } else { 
                gm.charge.texture = Resources.Load<Texture>("charge-" + 5);
                gm.ultText.SetActive(true);
            }

            gm.heathBar.fillAmount = (float)health / maxhealth;

            gm.streakLabel.text = "streak: " + streak;
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
        if (value.ReadValue<float>() == 0 && isLocalPlayer) jumpGo = false;
        else jumpGo = true;
    }

    public void OnAtkPrim(InputAction.CallbackContext value){
        if (value.ReadValueAsButton() && !cancelanim && isLocalPlayer) {
            StartCoroutine(IAtkPrim());
        }
    }

    IEnumerator IAtkPrim() {
        animator.SetInteger("state", 2);
        cancelanim = true;
        hat.layer = LayerMask.NameToLayer("Default");
        yield return new WaitForSecondsRealtime(1.8f);
        cancelanim = false;
        hat.layer = LayerMask.NameToLayer("LocalPlayer");
    }

    public void OnAtkSec(InputAction.CallbackContext value){
        if (value.ReadValueAsButton() && isLocalPlayer) {   
            StartCoroutine(IAtkPrim());
        }
    }

    public void OnReGlide(InputAction.CallbackContext value){
        if (value.ReadValueAsButton() && charge == 5 && isLocalPlayer) {
            StartCoroutine(IReGlide());
        }
    }

    [Client] IEnumerator IReGlide() {
        yVel = reGlideSpeed;
        for (int i = 0; i < 5; i++) {
            ChargeMod(-1);
            yield return new WaitForSecondsRealtime(.2f);
        }
        ClearCharge();
        gliding = true;
        yVel = 0;
    }

    [Command] void ChargeMod(int amount) {
            charge += amount;
    }

    [Command] void ClearCharge() { charge = 0; } // WHY THE FUCK IS IT NEGATIVE?! FIX THAT SHIT!

    void OnTriggerEnter(Collider col) {
        StartCoroutine(IHatCol(col));
    }

    IEnumerator IHatCol(Collider col) {
        if (prevAtk != col && col.GetComponentInParent<Player>() != this) {
            prevAtk = col;
            if (col.gameObject.name == "hat")
                CmdDmg(1, col.GetComponentInParent<Player>());
            yield return new WaitForSecondsRealtime(.8f);
            prevAtk = null;
        }
    }

    IEnumerator _Die() {
        cancelanim = true;
        animator.SetInteger("state", 4);
        yVel = reGlideSpeed;
        for (int i = 0; i < 6; i++) {
            yVel = reGlideSpeed / i;
            yield return new WaitForSecondsRealtime(.2f);
        }
        cancelanim = false;
        gliding = true;
        yVel = 0;
    }

    [ClientRpc] void Die() {
            StartCoroutine(_Die());
    }

    [Command] void CmdDmg(int amount, Player origin) {
        health -= amount;
        if (health == 0) {
            if (origin.charge < 5)
                origin.charge++;
            origin.streak++;
            streak = 0;
            charge = 0;
            health = maxhealth;
            Die();
        }
    }
}
