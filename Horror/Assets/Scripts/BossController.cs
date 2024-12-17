using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    public Animator animator;
    public Transform player;
    public GameObject tentaclePrefab;
    public Transform ballSpawnPoint; // Точка спавна мяча
    public GameObject damageCollider; // Пустышка с коллайдером, которая наносит урон игроку

    public float speed = 3f;  // Скорость передвижения босса
    public float runSpeed = 6f; // Скорость бега при атаке
    public float attackRadius = 10f;  // Радиус телепатической атаки
    public float meleeAttackRadius = 3f;  // Радиус атаки руками
    public float ballAttackRadius = 4f;  // Радиус атаки мячом
    public float meleeAttackOffset = 1.5f; // Смещение радиуса атаки комбо вперед
    public float chaseRadius = 15f;  // Радиус преследования
    public float stopChaseRadius = 1.5f; // Радиус, в котором босс останавливается перед атакой
    public float tentacleLifetime = 5f;  // Время жизни щупальца
    public float tentacleSpawnRadius = 5f;  // Радиус спавна щупалец вокруг игрока
    public int tentacleCount = 3;  // Количество спавнящихся щупалец за раз
    public float attackCooldown = 5f;  // Задержка между атаками
    public int meleeAttackDamage = 20; // Урон от удара руками
    private float tentacleSpawnYOffset = -0.4f; // Смещение по оси Y для спавна щупалец

    private enum BossState { Idle, Walk, Run, Attack, Charge, Overload, Death }
    private BossState currentState;

    private float lastAttackTime;
    private bool isAttacking;

    void Start()
    {
        currentState = BossState.Idle;
        lastAttackTime = Time.time;
        isAttacking = false;

        // Деактивируем пустышку с коллайдером при старте
        if (damageCollider != null)
        {
            damageCollider.SetActive(false);
        }
    }

    void Update()
    {
        if (currentState == BossState.Death || currentState == BossState.Charge || currentState == BossState.Overload)
            return;

        if (CanSeePlayer())
        {
            float distanceToPlayer = Vector3.Distance(player.position, transform.position);

            if (distanceToPlayer <= stopChaseRadius)
            {
                // Босс стоит и смотрит на игрока
                LookAtPlayer();
                animator.SetBool("IsWalking", false);

                // Проверяем задержку перед следующей атакой
                if (!isAttacking && Time.time - lastAttackTime > attackCooldown)
                {
                    ChooseRandomAttack();
                }
            }
            else if (distanceToPlayer > stopChaseRadius && distanceToPlayer <= chaseRadius)
            {
                // Босс преследует игрока
                if (currentState != BossState.Attack)
                {
                    MoveTowardsPlayer();
                }
            }
            else
            {
                animator.SetBool("IsWalking", false);
            }
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }
    }

    private void MoveTowardsPlayer()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        if (distance > stopChaseRadius)
        {
            currentState = BossState.Walk;
            animator.SetBool("IsWalking", true);

            // Движение к игроку
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            // Поворачиваемся лицом к игроку
            LookAtPlayer();
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;  // Убираем влияние высоты
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    private bool CanSeePlayer()
    {
        // Проверка прямой линии видимости между врагом и игроком
        Vector3 directionToPlayer = player.position - transform.position;
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer.normalized, out hit, chaseRadius))
        {
            // Убеждаемся, что луч попадает именно в игрока
            if (hit.collider.transform == player)
            {
                return true;
            }
        }
        return false;
    }

    private void ChooseRandomAttack()
    {
        isAttacking = true;
        int attackChoice = Random.Range(0, 3); // Выбор случайного числа: 0, 1 или 2

        if (attackChoice == 0)
        {
            StartCoroutine(PerformAttack("AttackTelepathy", Attack1_Telepathy()));
        }
        else if (attackChoice == 1)
        {
            StartCoroutine(RunAndAttackWithCombo());
        }
        else
        {
            StartCoroutine(RunAndAttackWithBall());
        }
    }

    private IEnumerator PerformAttack(string trigger, IEnumerator attackRoutine)
    {
        currentState = BossState.Attack;
        animator.SetTrigger(trigger);
        animator.SetBool("IsAttacking", true);

        // Выполнение выбранной атаки
        yield return StartCoroutine(attackRoutine);

        // Переход в состояние ожидания после завершения атаки
        EndAttack();
    }

    private IEnumerator RunAndAttackWithCombo()
    {
        currentState = BossState.Run;
        animator.SetBool("IsRunning", true);

        // Босс бежит к игроку с анимацией бега
        while (Vector3.Distance(transform.position, player.position) > meleeAttackRadius)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * runSpeed * Time.deltaTime;
            LookAtPlayer();
            yield return null;
        }

        // Останавливаемся перед атакой
        animator.SetBool("IsRunning", false);
        LookAtPlayer();  // Поворачиваемся лицом к игроку перед атакой

        // Запуск комбо атаки
        StartCoroutine(PerformAttack("AttackCombo", ComboAttack()));
    }

    private IEnumerator ComboAttack()
    {
        // Логика комбо атаки руками
        Vector3 attackPosition = transform.position + transform.forward * meleeAttackOffset; // Позиция радиуса атаки впереди босса

        // Проверяем игрока в радиусе впереди босса
        Collider[] hitColliders = Physics.OverlapSphere(attackPosition, meleeAttackRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == player)
            {
                DealDamageToPlayer();
                break; // Наносим урон один раз за атаку
            }
        }

        yield return new WaitForSeconds(1f); // Задержка для демонстрации выполнения комбо
    }

    private IEnumerator RunAndAttackWithBall()
    {
        currentState = BossState.Run;
        animator.SetBool("IsRunning", true);

        // Босс бежит к игроку с анимацией бега
        while (Vector3.Distance(transform.position, player.position) > ballAttackRadius)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * runSpeed * Time.deltaTime;
            LookAtPlayer();
            yield return null;
        }

        // Останавливаемся перед атакой
        animator.SetBool("IsRunning", false);
        LookAtPlayer();  // Поворачиваемся лицом к игроку перед атакой

        // Запуск атаки мячом на близком расстоянии
        StartCoroutine(PerformAttack("ThrowBall", CloseRangeBallAttack()));
    }

    private IEnumerator CloseRangeBallAttack()
    {
        // Запуск анимации броска мяча
        animator.SetTrigger("ThrowBall");

        // Ожидание завершения броска
        yield return new WaitForSeconds(1f); // Время на выполнение атаки

    }

    private IEnumerator Attack1_Telepathy()
    {
        for (int i = 0; i < tentacleCount; i++)
        {
            // Спавн щупалец ниже по Y на заданное смещение
            Vector3 spawnPosition = player.position + (Random.insideUnitSphere * tentacleSpawnRadius);
            spawnPosition.y = player.position.y + tentacleSpawnYOffset;  // Смещение по оси Y

            // Случайный поворот по оси Y
            Quaternion spawnRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            GameObject tentacle = Instantiate(tentaclePrefab, spawnPosition, spawnRotation);
            TentacleColliderController tentacleController = tentacle.GetComponent<TentacleColliderController>();
            tentacleController.player = player;  // Передаем ссылку на игрока
            tentacleController.SetLifetime(tentacleLifetime);  // Устанавливаем время жизни щупальца
        }

        yield return new WaitForSeconds(tentacleLifetime);
    }

    // Метод для нанесения урона игроку, вызывается через события анимации
    public void DealDamageToPlayer()
    {
        if (Vector3.Distance(transform.position, player.position) <= meleeAttackRadius)
        {
            // Наносим урон игроку
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage(meleeAttackDamage);
            }
        }
    }

    private void EndAttack()
    {
        // Возвращаемся в состояние ожидания и разрешаем выбирать атаки
        animator.SetBool("IsAttacking", false);
        currentState = BossState.Idle;
        lastAttackTime = Time.time;  // Устанавливаем время последней атаки после завершения
        isAttacking = false;  // Сбрасываем флаг атаки
    }

    public void TakeDamage(int damage)
    {
        // Логика уменьшения здоровья
        // Переход в состояние смерти при достижении нуля
    }

    // Методы для управления пустышкой с коллайдером

    // Метод для активации пустышки с коллайдером
    public void ActivateDamageCollider()
    {
        if (damageCollider != null)
        {
            damageCollider.SetActive(true);
        }
    }

    // Метод для деактивации пустышки с коллайдером
    public void DeactivateDamageCollider()
    {
        if (damageCollider != null)
        {
            damageCollider.SetActive(false);
        }
    }

    // Отображение радиусов преследования и атаки в редакторе
    void OnDrawGizmosSelected()
    {
        // Радиус преследования
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        // Радиус остановки и слежения
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stopChaseRadius);

        // Радиус телепатической атаки
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // Радиус атаки руками (впереди босса)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + transform.forward * meleeAttackOffset, meleeAttackRadius);

        // Радиус атаки мячом
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, ballAttackRadius);
    }
}
