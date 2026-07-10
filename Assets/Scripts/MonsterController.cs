using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class MonsterController : MonoBehaviour {
    [Header("References")]
    public Camera playerCamera;
    public Transform handsViewModel; // attach hands mesh under camera
    public Transform bonePrefab;
    public Transform boneSpawn;
    public LayerMask grabLayer;
    public AudioClip eatSfx;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float gravity = -20f;
    public float jumpSpeed = 7f;
    public float slideSpeed = 10f;
    public float slideDuration = 0.8f;

    CharacterController cc;
    Vector3 velocity;
    bool sliding = false;
    float slideTimer = 0f;

    [Header("Wall/Branch")]
    public float grabRange = 1.2f;
    public float wallJumpForce = 6f;
    public float wallPushCooldown = 0.2f;
    float wallPushTimer = 0f;

    [Header("Combat")]
    public int eatPoints = 0;
    public int pointsToUnlockBones = 5;
    public float boneSpeed = 25f;

    void Start() {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update() {
        HandleMovement();
        HandleActions();
        if (wallPushTimer > 0) wallPushTimer -= Time.deltaTime;
    }

    void HandleMovement() {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");
        Vector3 forward = Quaternion.Euler(0, playerCamera.transform.eulerAngles.y, 0) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0, playerCamera.transform.eulerAngles.y, 0) * Vector3.right;
        Vector3 move = (forward * inputZ + right * inputX).normalized;

        bool running = Input.GetKey(KeyCode.LeftShift);
        float speed = running ? runSpeed : walkSpeed;

        if (sliding) {
            slideTimer += Time.deltaTime;
            move = transform.forward;
            speed = slideSpeed;
            if (slideTimer >= slideDuration) { sliding = false; }
        } else if (Input.GetKeyDown(KeyCode.LeftControl)) {
            sliding = true; slideTimer = 0f;
        }

        if (cc.isGrounded && velocity.y < 0) velocity.y = -2f;
        if (Input.GetButtonDown("Jump") && cc.isGrounded) {
            velocity.y = jumpSpeed;
        }

        // wall/branch push: if near a wall and jump pressed, push off in camera look direction
        if (Input.GetButtonDown("Jump") && !cc.isGrounded && wallPushTimer <= 0) {
            if (Physics.SphereCast(playerCamera.transform.position, 0.3f, playerCamera.transform.forward, out RaycastHit hit, grabRange, grabLayer)) {
                Vector3 pushDir = (playerCamera.transform.forward + Vector3.up * 0.2f).normalized;
                velocity = pushDir * wallJumpForce;
                wallPushTimer = wallPushCooldown;
            }
        }

        Vector3 horizontal = move * speed;
        Vector3 final = horizontal + new Vector3(0, velocity.y, 0);
        velocity.y += gravity * Time.deltaTime;
        cc.Move(final * Time.deltaTime);
    }

    void HandleActions() {
        if (Input.GetMouseButtonDown(0)) { MeleeAttack(); }
        if (Input.GetMouseButtonDown(1) && eatPoints >= pointsToUnlockBones) { ShootBone(); eatPoints = 0; }
    }

    void MeleeAttack() {
        // simple sphere check in front of camera
        if (Physics.SphereCast(playerCamera.transform.position, 0.5f, playerCamera.transform.forward, out RaycastHit hit, 2f)) {
            if (hit.collider.CompareTag("Human")) {
                // play eat animation / sfx and award points
                eatPoints++;
                if (eatSfx) AudioSource.PlayClipAtPoint(eatSfx, transform.position);
                // optional: trigger human hide/pool
            }
        }
    }

    void ShootBone() {
        var b = Instantiate(bonePrefab, boneSpawn.position, Quaternion.identity);
        b.GetComponent<Rigidbody>().linearVelocity = playerCamera.transform.forward * boneSpeed;
    }
}

