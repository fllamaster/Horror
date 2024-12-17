using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    //Third Person Controller References
    [SerializeField]
    private Animator playerAnim;

    // Equip-Unequip parameters
    [SerializeField]
    private GameObject sword;
    [SerializeField]
    private GameObject swordOnShoulder;
    public bool isEquipping;
    public bool isEquipped;

    // Blocking Parameters
    public bool isBlocking;

    // Kick Parameters
    public bool isKicking;

    // Attack Parameters
    public bool isAttacking;
    private float timeSinceAttack;
    public int currentAttack = 0;

    // Health-related variables
    public float maxHealth = 100f;
    private float currentHealth;
    public HealthBar healthBar;

    // Combat-related references
    public Collider swordCollider;
    private Sword swordScript;

    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);

        // Initialize combat-related components
        playerAnim = GetComponent<Animator>();

        // Get the Sword script
        swordScript = sword.GetComponent<Sword>();

        // Initially disable the sword collider trigger
        if (swordCollider != null)
        {
            swordCollider.isTrigger = false;
        }
    }

    private void Update()
    {
        timeSinceAttack += Time.deltaTime;

        Attack();
        Equip();
        Block();
        Kick();
    }

    private void Equip()
    {
        // Проверка нажатия клавиши экипировки и текущего состояния
        if (Input.GetKeyDown(KeyCode.R) && !isEquipping)
        {
            isEquipping = true;
            playerAnim.SetTrigger("Equip");
        }
    }

    public void ActiveWeapon()
    {
        // Переключение состояния экипировки
        if (!isEquipped)
        {
            sword.SetActive(true);
            swordOnShoulder.SetActive(false);
            isEquipped = true;
        }
        else
        {
            sword.SetActive(false);
            swordOnShoulder.SetActive(true);
            isEquipped = false;
        }
    }

    public void Equipped()
    {
        // Сброс состояния экипировки
        isEquipping = false;
    }

    private void Block()
    {
        if (Input.GetKey(KeyCode.Mouse1))
        {
            playerAnim.SetBool("Block", true);
            isBlocking = true;
        }
        else
        {
            playerAnim.SetBool("Block", false);
            isBlocking = false;
        }
    }

    public void Kick()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            playerAnim.SetBool("Kick", true);
            isKicking = true;
        }
        else
        {
            playerAnim.SetBool("Kick", false);
            isKicking = false;
        }
    }

    private void Attack()
    {
        if (Input.GetMouseButtonDown(0) && timeSinceAttack > 0.8f)
        {
            if (!isEquipped)
                return;

            currentAttack++;
            isAttacking = true;

            if (currentAttack > 3)
                currentAttack = 1;

            // Reset
            if (timeSinceAttack > 1.0f)
                currentAttack = 1;

            // Call Attack Triggers
            playerAnim.SetTrigger("Attack" + currentAttack);
            swordScript.StartAttack(); // Включаем атаку

            // Reset Timer
            timeSinceAttack = 0;
        }
    }

    public void ResetAttack()
    {
        isAttacking = false;
        swordScript.EndAttack(); // Выключаем атаку
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player died!");
        // Logic for player death, e.g., reload level or end game
        // You can call a method to reload the scene or show a game over screen
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        healthBar.SetHealth(currentHealth);
    }
}