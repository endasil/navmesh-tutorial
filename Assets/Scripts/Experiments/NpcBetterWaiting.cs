using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Final
{
    public class NpcBetterWaiting : MonoBehaviour
    {
        private Animator animator;
        private NavMeshAgent navAgent;
        public Player player;
        public float animationSpeed = 0.5f;
        public AgentMode agentMode = AgentMode.FollowPlayer;
        private List<Vector3> failedPoints = new List<Vector3>();
        public float waitTime = 1; // unused by the new phased wait, kept to avoid changing fields in use elsewhere
        public bool isWaiting = true;
        public float debugRayLen = 2f;
        private Vector3 bestIdleDir = Vector3.forward;
        

        // phased wait timers
        private float waitEndTime;  // absolute time when the total wait ends
        private float holdEndTime;  // absolute time when the initial facing-hold ends

        public enum AgentMode
        {
            RandomWalker,
            FollowPlayer,
        }

        void Start()
        {
            animator = GetComponentInChildren<Animator>();
            navAgent = GetComponent<NavMeshAgent>();
        }

        void Update()
        {
            animator.SetFloat("Velocity", navAgent.velocity.magnitude * animationSpeed);
            if (agentMode == AgentMode.RandomWalker) RandomWalk();
            if (agentMode == AgentMode.FollowPlayer) FollowPlayer();
        }

        private void FollowPlayer()
        {
            if (Vector3.Distance(transform.position, player.transform.position) <= navAgent.stoppingDistance) return;
            navAgent.SetDestination(player.transform.position);
        }

        void RandomWalk()
        {
            // pre-rotate a bit on approach so arrival looks natural
            if (!isWaiting && !navAgent.pathPending)
            {
                if (navAgent.remainingDistance < Mathf.Max(1.0f, navAgent.stoppingDistance * 3f))
                {
                    Vector3 dir = navAgent.steeringTarget - transform.position;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, 240f * Time.deltaTime);
                    }
                }
            }

            // waiting with two phases: hold facing, then face next corner
            if (isWaiting)
            {
                // phase 1 - hold current facing for 1-3 seconds
                if (Time.time < holdEndTime)
                {
                    Quaternion holdRot = Quaternion.LookRotation(bestIdleDir, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, holdRot, 240f * Time.deltaTime);

                    // guarantee a real stop and prevent agent-driven rotation
                    if (!navAgent.isStopped)
                    {
                        navAgent.isStopped = true;
                        navAgent.updateRotation = false;
                    }
                    return;
                }

                // phase 2 - face the next path corner until the total wait ends
                Vector3 faceDir = GetFacingToNextCorner();
                if (faceDir.sqrMagnitude > 0.0001f) bestIdleDir = faceDir.normalized;

                Quaternion lookRot2 = Quaternion.LookRotation(bestIdleDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot2, 240f * Time.deltaTime);

                if (Time.time >= waitEndTime)
                {
                    isWaiting = false;
                    navAgent.isStopped = false;
                    navAgent.updateRotation = true;
                    
                }
                return;
            }

            // choose a new random destination and start a phased wait
            if (navAgent.remainingDistance <= navAgent.stoppingDistance && !navAgent.pathPending)
            {
                if (GetRandomPointOnNavMesh(transform.position, 10f, out Vector3 randomPoint))
                {
                    navAgent.autoBraking = true;
                    navAgent.stoppingDistance = 0.4f;
                    navAgent.SetDestination(randomPoint);

                    float totalWait = Random.Range(3f, 6f);
                    float holdSecs = Random.Range(1f, 3f);

                    bestIdleDir = transform.forward; // lock current facing for the initial hold
                    isWaiting = true;

                    navAgent.isStopped = true;
                    navAgent.updateRotation = false;

                    waitEndTime = Time.time + totalWait;
                    holdEndTime = Time.time + holdSecs;
                }
            }
        }

        private Vector3 GetFacingToNextCorner()
        {
            // prefer steeringTarget if available
            Vector3 dir = navAgent.steeringTarget - transform.position;
            if (dir.sqrMagnitude > 0.0001f) return dir;

            // fallback to desired velocity
            Vector3 dv = navAgent.desiredVelocity;
            if (dv.sqrMagnitude > 0.0001f) return dv;

            return transform.forward;
        }

        public bool GetRandomPointOnNavMesh(Vector3 center, float radius, out Vector3 target)
        {
            for (int i = 0; i < 50; i++)
            {
                Vector3 randomPoint = center + Random.insideUnitSphere * radius;
                target = new Vector3(randomPoint.x, transform.position.y, randomPoint.z);
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navAgent.height * 2f, NavMesh.AllAreas))
                {
                    target = hit.position;
                    return true;
                }
            }
            target = Vector3.zero;
            return false;
        }

        void OnDrawGizmos()
        {
            Vector3 o = transform.position + Vector3.up * 0.6f;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(o, o + transform.forward * debugRayLen);
            o = transform.position + Vector3.up * 0.5f;
            Gizmos.color = Color.orange;
            Gizmos.DrawLine(o, o + bestIdleDir * debugRayLen);
        }
    }
}
