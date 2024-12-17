using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour
{
    public float baseDamage = 1f;
    private HashSet<Collider> damagedEnemies = new HashSet<Collider>(); // Хранит врагов, которым уже нанесен урон в текущей атаке
    private bool isAttacking = false; // Флаг для отслеживания атаки

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, если меч в атаке и попадает в врага, которому еще не нанесен урон
        if (isAttacking && other.CompareTag("Enemy") && !damagedEnemies.Contains(other))
        {
            damagedEnemies.Add(other); // Добавляем врага в список уже получивших урон
            EnemyAI enemyAI = other.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                float damage = CalculateDamage();
                enemyAI.TakeDamage(damage);
                enemyAI.ReactToAttack();
            }
        }
    }

    // Включение триггера для атаки
    public void StartAttack()
    {
        isAttacking = true;
        damagedEnemies.Clear(); // Очищаем список врагов, чтобы нанести урон только один раз за атаку
        GetComponent<Collider>().isTrigger = true; // Включаем триггер на время атаки
    }

    // Отключение триггера после атаки
    public void EndAttack()
    {
        isAttacking = false;
        GetComponent<Collider>().isTrigger = false; // Отключаем триггер после завершения атаки
    }

    private float CalculateDamage()
    {
        PlayerController playerController = GetComponentInParent<PlayerController>();
        if (playerController == null) return baseDamage;

        switch (playerController.currentAttack)
        {
            case 1:
                return baseDamage;
            case 2:
                return baseDamage * 2;
            case 3:
                return baseDamage * 3;
            default:
                return baseDamage;
        }
    }
}