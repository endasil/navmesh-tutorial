using UnityEngine;

public class Player : MonoBehaviour
{
    private Animator animator;
    private Vector3 _previousPosition;
    private CharacterController characterController;

    public float RotationSpeed = 369f;
    public float PushImpulse = 1f;
    public float movementSpeed = 6f;
    public float animationSpeed = 30f;
    // Grab controls (kept names)
    public KeyCode GrabKey = KeyCode.E;
    public float MaxGrabDistance = 2.5f;

    private Rigidbody _grabbedRB;
    public Vector3 holdOffset = new Vector3(0f, 1.1f, 1.2f);

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        _previousPosition = transform.position;
    }

    void Update()
    {
        /* movement + rotate -------------------------------------------------- */
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        Vector3 movement = transform.forward * (vertical * movementSpeed * Time.deltaTime);
        movement.y = Physics.gravity.y;
        characterController.Move(movement);

        /* animation velocity ------------------------------------------------- */
        // animator.SetFloat("Vertical", animationSpeed * movementSpeed * vertical * Time.deltaTime);
        animator.SetFloat("Vertical", vertical); // *moveSpeedMod*animationSpeed
        transform.Rotate(Vector3.up * (horizontal * RotationSpeed * Time.deltaTime));

        /* grab / release ----------------------------------------------------- */
        if (Input.GetKeyDown(GrabKey)) TryGrab();
        if (Input.GetKeyUp(GrabKey)) ReleaseGrab();
    }

    void FixedUpdate()
    {
        if (!_grabbedRB) return;

        // convert (x,y,z) in player space to world space
        Vector3 targetPos = transform.TransformPoint(holdOffset);

        // move the cube to that position
        _grabbedRB.MovePosition(targetPos);
    }

    
    private void TryGrab()
    {
        if (_grabbedRB) return;

        Vector3 origin = transform.position + Vector3.up * holdOffset.y;
        const float radius = 0.3f;
        // sphere-cast forward to pick a rigidbody to levitate
        if (Physics.SphereCast(origin, radius, transform.forward,
                               out RaycastHit hit, MaxGrabDistance,
                               Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            Rigidbody rb = hit.rigidbody;
            if (rb == null || rb.isKinematic) return;

            _grabbedRB = rb;
            _grabbedRB.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _grabbedRB.linearVelocity = Vector3.zero;
        }
    }

    private void ReleaseGrab() => _grabbedRB = null;
}
