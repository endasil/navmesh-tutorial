using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPC that can wander randomly or follow the player.  Stops to turn when the corner is sharp.
/// The result of a cross product is a vector that is normal to the plane described by vector a and b
/// Use warp for instantiated entities.
/// 28 years later
/// </summary>
public class NpcScriptTurningOverride: MonoBehaviour
{
    public Player player;
    [SerializeField] private float maxSpeed = 3.0f;
    public float turnThreshold = 10f;        // degrees

    public enum NPCMode { FollowPlayer, RandomWalk }
    public NPCMode npcMode = NPCMode.RandomWalk;
    public float animationSpeed = 0.5f; 
    private Vector3 _previousPosition;
    private Animator animator;
    private NavMeshAgent _agent;

    void Start()
    {
        player = FindFirstObjectByType<Player>();
        animator = GetComponentInChildren<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = maxSpeed;
        _previousPosition = transform.position;
    }

    void Update()
    {
        ActAccordingToMode();

        if (!_agent.hasPath) return;

        /* ---------- turn-in-place logic -------------------------------- */
        Vector3 toCorner = _agent.steeringTarget - transform.position;
        toCorner.y = 0f;

        float angle = toCorner.sqrMagnitude < 0.0001f
            ? 0f
            : Vector3.Angle(transform.forward, toCorner);

        Quaternion targetRot = Quaternion.LookRotation(toCorner, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
              transform.rotation,
              targetRot,
              _agent.angularSpeed * Time.deltaTime);
        
        //if (angle > turnThreshold || _agent.isStopped == true && angle > 60)                                // manual rotation
        //{
        //    Debug.Log("manual rotate");
        //    _agent.isStopped = true;  // Pause follow path
        //    _agent.updateRotation = false;              // we handle turning

        //    //Quaternion targetRot = Quaternion.LookRotation(toCorner, Vector3.up);
        //    transform.rotation = Quaternion.RotateTowards(
        //        transform.rotation,
        //        targetRot,
        //        _agent.angularSpeed * Time.deltaTime);
        //}
        //else
        //{
        //    if (_agent.isStopped)
        //    {
        //        Debug.Log("Starting moving");
        //        _agent.isStopped = false;
        //        _agent.updateRotation = true;              // navmesh agent takes over again
        //    }
        //}
        /* ---------- animation ------------------------------------------ */
        animator.SetFloat("Velocity", _agent.velocity.magnitude* animationSpeed);
    }

    /* ------------------------------------------------------------------ */
    private void ActAccordingToMode()
    {

        if(Vector3.Distance(transform.position, player.transform.position) < _agent.stoppingDistance)
        {
            return;
        }
        if (npcMode == NPCMode.FollowPlayer && player)
        {
            
            if (!_agent.SetDestination(player.transform.position))
                Debug.LogWarning($"{name}: failed to set destination to player");
        }
        else if (npcMode == NPCMode.RandomWalk)
        {
            HandleRandomWalk();
        }
    }

    private void HandleRandomWalk()
    {

        if (!_agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name}: not on NavMesh");
            return;
        }

        if (_agent.remainingDistance > _agent.stoppingDistance) return;

        if (GetRandomPointOnNavMesh(transform.position, 10f, out Vector3 randomPoint))
        {
            if (!_agent.SetDestination(randomPoint))
                Debug.LogWarning($"{name}: failed to set destination to random point");
            // else
               //  Debug.Log($"{name}: wandering to {randomPoint}");
        }
        
    }

    /* ------------------------------------------------------------------ */
    public bool GetRandomPointOnNavMesh(Vector3 center, float radius, out Vector3 target)
    {
        for (int i = 0; i < 50; i++)
        {
            
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _agent.height, NavMesh.AllAreas))
            {
                target = hit.position;
                return true;
            }
        }

        target = Vector3.zero;
        return false;
    }
}
