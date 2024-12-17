using UnityEngine;

public class DamageCollider : MonoBehaviour
{
    public int damage = 10; // Урон, наносимый при соприкосновении
    private BossController bossController; // Ссылка на контроллер босса

    private void Start()
    {
        // Найти контроллер босса, чтобы управлять активностью коллайдера
        bossController = GetComponentInParent<BossController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Наносим урон игроку
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage(damage);
            }

            // Деактивируем коллайдер после нанесения урона, если нужно
            if (bossController != null)
            {
                bossController.DeactivateDamageCollider();
            }
        }
    }
}