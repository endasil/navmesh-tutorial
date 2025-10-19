using UnityEngine;
using UnityEngine.AI;

public sealed class Npc : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    public AIMode aiMode;
    public Player player;
    public float animationSpeed = 0.5f;
    public float maxWalkDistance = 50f;

    public enum AIMode
    {
        RandomWalk,
        FollowPlayer
    }
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        animator.SetFloat("Velocity", agent.velocity.magnitude * animationSpeed);
        if(aiMode == AIMode.FollowPlayer)
        {
            agent.SetDestination(player.transform.position);
        }

        if (aiMode == AIMode.RandomWalk && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.SetDestination(RandomPosition());
        }
    }

    private Vector3 RandomPosition()
    {
        float randomX = Random.Range(-maxWalkDistance, maxWalkDistance);
        float randomZ = Random.Range(-maxWalkDistance, maxWalkDistance);
        Vector3 desiredPosition = transform.position + new Vector3(randomX, 0, randomZ);
        if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 6f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position;
    }
}
