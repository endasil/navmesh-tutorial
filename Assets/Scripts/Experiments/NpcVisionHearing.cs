using TMPro;

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

public sealed class NpcVisionHearing : MonoBehaviour
{
    // Navigation
    public NavMeshAgent navMeshAgent;

    // Speeds
    public float defaultSpeed = 1f;          // Speed during idle/random patrol
    public float playerFoundSpeed = 2f;      // Speed when chasing the player
    public float animationSpeed = 0.5f;      // Animator multiplier based on velocity

    // References
    public Transform head;
    public Player player;                    // Reference to player object
    private Animator animator;               // Animator component on child object
    private TextMeshPro indicator;           // TMP text for feedback searching, seen, heard ("?", "!", "~")

    // Patrol behavior
    public float maxWalkDistance = 20f;      // Max distance for random patrol target
    public float minWalkDistance = 5f;       // Min distance to avoid too small steps

    public AIBehavior aiBehavior;            // Current behavior mode of the NPC

    // Vision parameters
    public float sideVisionAngle = 45f;      // Field of view angle (half cone)
    public float visionLength = 10f;         // Max vision range

    // Hearing parameters
    public float hearingRange = 3f;          // Detection range without vision
    private bool investigating;              // Flag: NPC is chasing a recent detection

    // Idle look-around system
    public bool isWaiting = false;           // True when NPC is in idle wait
    private Vector3 bestIdleDir = Vector3.forward;  // Current best direction to face during idle
    private float waitEndTime;               // When idle ends
    private float holdEndTime;               // When initial idle rotation phase ends

    // AI behavior options
    public enum AIBehavior
    {
        RandomWalk,     // Roams randomly
        FollowPlayer,   // Constantly chases player
        WalkNChase      // Patrols, but reacts when player seen/heard
    }

    void Start()
    {
        // Cache components
        animator = GetComponentInChildren<Animator>();
        indicator = GetComponentInChildren<TextMeshPro>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        // Validate dependencies
        Assert.IsNotNull(navMeshAgent);
        Assert.IsNotNull(animator);
        Assert.IsNotNull(indicator);
        head = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
    }

    void Update()
    {
        // Update animation based on agent movement speed
        animator.SetFloat("Velocity", navMeshAgent.velocity.magnitude * animationSpeed);

        // --- Behavior Control ---

        if (aiBehavior == AIBehavior.FollowPlayer)
        {
            // Unpause the agent if it was stopped (e.g. from switching behavior in inspector)
            if (navMeshAgent.isStopped && !isWaiting)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.updateRotation = true;
            }

            // Rotate to face player and move toward them
            TurnTowardsTarget();
            navMeshAgent.SetDestination(player.transform.position);
            return;
        }

        if (aiBehavior == AIBehavior.RandomWalk)
        {
            RandomWalk();
            return;
        }

        if (aiBehavior == AIBehavior.WalkNChase)
        {
            // Allow movement only if not currently waiting
            if (navMeshAgent.isStopped && !isWaiting)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.updateRotation = true;
            }

            WalkNChase();
        }
    }

    private void TurnTowardsTarget()
    {
        // Rotate towards the agent's steering direction
        Vector3 toCorner = navMeshAgent.steeringTarget - transform.position;
        toCorner.y = 0f;

        Quaternion targetRot = Quaternion.LookRotation(toCorner, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            navMeshAgent.angularSpeed * Time.deltaTime);
    }

    private void RandomWalk()
    {
        // If close to target, rotate to face next corner
        if (!isWaiting && !navMeshAgent.pathPending)
        {
            if (navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance + 1f)
            {
                Vector3 dir = navMeshAgent.steeringTarget - transform.position;

                // Prevent jitter or redundant LookRotation from micro-movements
                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, 240f * Time.deltaTime);
                }
            }
        }

        // Handle idle look-around behavior
        if (UpdateIdleLookAround()) return;

        // If idle done and no path queued, pick new random location
        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
        {
            navMeshAgent.autoBraking = true;
            navMeshAgent.stoppingDistance = 0.4f;
            navMeshAgent.SetDestination(GetRandomPosition());
            StartIdleLookAround();
        }
    }

    // Waiting behavior: NPC pauses to look around before walking again
    private void StartIdleLookAround()
    {
        bestIdleDir = transform.forward;
        isWaiting = true;
        navMeshAgent.isStopped = true;
        navMeshAgent.updateRotation = false;
        waitEndTime = Time.time + Random.Range(2f, 4f);
        holdEndTime = Time.time + Random.Range(1f, 3f);
        indicator.text = "?";
        navMeshAgent.speed = defaultSpeed;
    }

    private bool UpdateIdleLookAround()
    {
        if (!isWaiting) return false;

        if (Time.time < holdEndTime)
        {
            // First phase: hold current facing direction
            Quaternion holdRot = Quaternion.LookRotation(bestIdleDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, holdRot, 240f * Time.deltaTime);

            // Reconfirm stopped state if changed externally, like ai mode in inspector
            if (!navMeshAgent.isStopped)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.updateRotation = false;
            }

            return true;
        }

        // Second phase: turn toward next move direction
        Vector3 faceDir = GetFacingToNextCorner();
        if (faceDir.sqrMagnitude > 0.0001f)
            bestIdleDir = faceDir.normalized;

        Quaternion lookRot2 = Quaternion.LookRotation(bestIdleDir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot2, 240f * Time.deltaTime);

        // Done waiting? Resume movement
        if (Time.time >= waitEndTime)
        {
            isWaiting = false;
            navMeshAgent.isStopped = false;
            navMeshAgent.updateRotation = true;
            return false;
        }

        return true;
    }

    private Vector3 GetFacingToNextCorner()
    {
        // Try steering target first
        Vector3 dir = navMeshAgent.steeringTarget - transform.position;
        
        if (dir.sqrMagnitude > 0.0001f) return dir;

        // Fallback to desired velocity
        Vector3 dv = navMeshAgent.desiredVelocity;
        if (dv.sqrMagnitude > 0.0001f) return dv;

        // Recalculate path to estimate direction
        var tmp = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, navMeshAgent.destination, NavMesh.AllAreas, tmp))
        {
            if (tmp.corners != null && tmp.corners.Length >= 2)
                return tmp.corners[1] - transform.position;
        }

        // Absolute fallback: face current forward
        return transform.forward;
    }

    public void WalkNChase()
    {
        // Face destination when approaching
        if (!isWaiting && !navMeshAgent.pathPending)
        {
            if (navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance + 1f)
            {
                Vector3 dir = navMeshAgent.steeringTarget - transform.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, 240f * Time.deltaTime);
                }
            }
        }

        // Handle idle look-around if active
        if (UpdateIdleLookAround()) return;

        // Check if player is in vision or hearing range
        float distance = Vector3.Distance(player.transform.position, transform.position);
        bool playerDetected = false;

        if (distance < visionLength && IsWithinViewCone())
        {
            indicator.text = "!";
            playerDetected = true;
        }
        else if (distance <= hearingRange)
        {
            indicator.text = "~";
            playerDetected = true;
        }

        if (playerDetected)
        {
            // Stop waiting and chase the player
            isWaiting = false;
            navMeshAgent.isStopped = false;
            navMeshAgent.updateRotation = true;

            navMeshAgent.speed = playerFoundSpeed;
            navMeshAgent.SetDestination(player.transform.position);
            investigating = true;
            return;
        }

        // Continue chasing if still moving toward last known position
        if (investigating && navMeshAgent.hasPath && navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
        {
            navMeshAgent.speed = playerFoundSpeed;
            indicator.text = ".";
            return;
        }

        // No target detected or arrived, resume patrol
        investigating = false;
        navMeshAgent.speed = defaultSpeed;
        indicator.text = "?";

        if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            navMeshAgent.autoBraking = true;
            navMeshAgent.stoppingDistance = 0.4f;
            navMeshAgent.SetDestination(GetRandomPosition());
            StartIdleLookAround();
        }
    }

    public bool IsWithinViewCone()
    {
        Vector3 direction = (player.transform.position - transform.position).normalized;

        // Visualize line of sight in scene view
        Debug.DrawLine(transform.position + new Vector3(0, 0.5f, 0), player.transform.position + new Vector3(0, 0.5f, 0));

        float cosAngle = Vector3.Dot(direction, transform.forward);
        float angle = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;

        if (angle <= sideVisionAngle)
        {
            // Raycast to check line of sight
            if (Physics.Raycast(transform.position + new Vector3(0, 0.5f, 0), direction, out var hit, visionLength))
            {
                Debug.Log($"Raycast hit {hit.transform.name}");
                if (hit.transform.CompareTag("Player"))
                    return true;
            }
        }

        return false;
    }

    private Vector3 GetRandomPosition()
    {
        // Generate random point within min-max range in XZ plane
        float randomX = Random.Range(-maxWalkDistance, maxWalkDistance);
        float randomZ = Random.Range(-maxWalkDistance, maxWalkDistance);

        // Clamp values to avoid too short or too long paths
        randomX = Mathf.Sign(randomX) * Mathf.Clamp(Mathf.Abs(randomX), minWalkDistance, maxWalkDistance);
        randomZ = Mathf.Sign(randomZ) * Mathf.Clamp(Mathf.Abs(randomZ), minWalkDistance, maxWalkDistance);

        Vector3 desiredPosition = transform.position + new Vector3(randomX, 0, randomZ);

        // Snap to nearest walkable NavMesh position
        if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            return hit.position;

        // Fallback: stay in place
        return transform.position;
    }


    
}
