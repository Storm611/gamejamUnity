using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health;
    public float maxHealth = 100;

    [SerializeField] private Healthbar healthbar;

    private void Awake()
    {
        health = maxHealth;
    }

    void Start()
    {
        healthbar.UpdateHealthBar(maxHealth, health);
    }

    void Update()
    {

    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
            return;
        }
        healthbar.UpdateHealthBar(maxHealth, health);
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
