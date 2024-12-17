using System.Collections;
using UnityEngine;

public class TentacleColliderController : MonoBehaviour
{
    public float growthDuration = 3f;  // Время, за которое коллайдер достигает максимального размера
    public float maxColliderRadius = 3f;  // Максимальный радиус коллайдера
    public Transform player;  // Ссылка на игрока для проверки столкновений
    private SphereCollider tentacleCollider;

    private void Start()
    {
        // Добавляем коллайдер и настраиваем начальные параметры
        tentacleCollider = gameObject.AddComponent<SphereCollider>();
        tentacleCollider.isTrigger = true;
        tentacleCollider.radius = 0.5f;  // Начальный размер коллайдера

        // Запускаем процесс увеличения радиуса коллайдера
        StartCoroutine(GrowCollider());
    }

    private IEnumerator GrowCollider()
    {
        float elapsedTime = 0f;
        float initialRadius = tentacleCollider.radius;

        while (elapsedTime < growthDuration)
        {
            float progress = elapsedTime / growthDuration;
            // Линейно увеличиваем радиус коллайдера до максимального значения
            tentacleCollider.radius = Mathf.Lerp(initialRadius, maxColliderRadius, progress);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Устанавливаем финальный радиус после завершения роста
        tentacleCollider.radius = maxColliderRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Логика обработки попадания игрока в область щупальца
            Debug.Log("Игрок попал под атаку щупальца!");
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage(10);  // Уменьшаем здоровье игрока
            }
        }
    }

    public void SetLifetime(float lifetime)
    {
        // Уничтожаем щупальце через заданное время жизни
        Destroy(gameObject, lifetime);
    }
}