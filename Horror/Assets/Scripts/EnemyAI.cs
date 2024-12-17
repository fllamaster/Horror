using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public float maxHealth = 10f;
    private float currentHealth;
    public HealthBar healthBar;

    public Transform[] patrolPoints;
    public float fieldOfViewAngle = 110f;
    public float viewDistance = 10f;
    public float attackRadius = 1.5f;
    public float chaseRadius = 15f;
    public float attackCooldown = 1f;
    public float searchDuration = 2f;
    public float turnSpeed = 5f; // Speed at which the enemy turns towards the player

    private NavMeshAgent agent;
    private int currentPatrolIndex;
    private float lastAttackTime;
    private Animator anim;
    private bool isChasing;
    private bool isSearching;
    private bool isAttacking;

    private PlayerController playerController;
    private Transform player;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // Initialize health
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
        }

        // Find player by tag "Player"
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerController = playerObject.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogError("Player with tag 'Player' not found!");
        }

        currentPatrolIndex = 0;
        isChasing = false;
        isSearching = false;
        isAttacking = false;

        GoToNextPatrolPoint();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            ReactToAttack();
        }
    }

    void Die()
    {
        Debug.Log("Enemy died!");
        Destroy(gameObject);
    }

    public void ReactToAttack()
    {
        isChasing = true;
        ChasePlayer();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(player.position, transform.position);

        if (isChasing)
        {
            if (distanceToPlayer <= chaseRadius)
            {
                if (distanceToPlayer <= attackRadius)
                {
                    StopAndAttackPlayer();
                }
                else
                {
                    ChasePlayer();
                }
            }
            else
            {
                isChasing = false;
                GoToNextPatrolPoint();
            }
        }
        else if (CanSeePlayer())
        {
            if (distanceToPlayer <= attackRadius)
            {
                StopAndAttackPlayer();
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            if (!isSearching && agent.remainingDistance < 0.5f)
            {
                StartCoroutine(SearchAtPatrolPoint());
            }
            else if (!isSearching)
            {
                Patrol();
            }
        }
    }

    bool CanSeePlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer < fieldOfViewAngle / 2f || isChasing)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer.normalized, out hit, viewDistance))
            {
                if (hit.collider.transform == player)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0 || !agent.isOnNavMesh)
            return;

        agent.destination = patrolPoints[currentPatrolIndex].position;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;

        anim.SetBool("isRunning", false);
        anim.SetBool("isWalking", true);
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0 || !agent.isOnNavMesh)
        {
            return;
        }

        if (agent.remainingDistance < 0.5f && !isSearching)
        {
            StartCoroutine(SearchAtPatrolPoint());
        }
        else if (!isSearching)
        {
            anim.SetBool("isRunning", false);
            anim.SetBool("isWalking", true);
        }
    }

    void ChasePlayer()
    {
        if (!agent.isOnNavMesh) return;

        if (isSearching)
        {
            isSearching = false;
            anim.ResetTrigger("Search");
        }

        if (agent.isStopped || isAttacking)
        {
            agent.isStopped = false;
            isAttacking = false;
        }

        agent.SetDestination(player.position);

        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);

        if (agent.velocity.magnitude > 0.1f)
        {
            anim.SetBool("isRunning", true);
            anim.SetBool("isWalking", false);
        }
        else
        {
            anim.SetBool("isRunning", false);
            anim.SetBool("isWalking", false);
        }

        isChasing = true;
    }

    void StopAndAttackPlayer()
    {
        if (!agent.isOnNavMesh) return;

        if (isSearching)
        {
            isSearching = false;
            anim.ResetTrigger("Search");
        }

        agent.isStopped = true;
        anim.SetBool("isRunning", false);
        anim.SetBool("isWalking", false);

        if (Time.time > lastAttackTime + attackCooldown)
        {
            AttackPlayer();
        }
    }

    void AttackPlayer()
    {
        anim.SetTrigger("Attack");
        if (playerController != null)
        {
            playerController.TakeDamage(Random.Range(1f, 3f));
        }
        lastAttackTime = Time.time;
        isAttacking = true;

        if (Vector3.Distance(player.position, transform.position) <= attackRadius)
        {
            Invoke(nameof(StopAndAttackPlayer), attackCooldown);
        }
    }

    IEnumerator SearchAtPatrolPoint()
    {
        if (!agent.isOnNavMesh) yield break;

        agent.isStopped = true;
        isSearching = true;

        anim.SetBool("isWalking", false);
        anim.SetBool("isRunning", false);
        anim.SetTrigger("Search");

        yield return new WaitForSeconds(searchDuration);

        isSearching = false;
        agent.isStopped = false;
        GoToNextPatrolPoint();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * transform.forward * viewDistance;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * transform.forward * viewDistance;
        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);

        Gizmos.color = Color.blue;
        Vector3 attackPosition = transform.position + transform.forward * attackRadius;
        Gizmos.DrawWireSphere(attackPosition, attackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
    }
}