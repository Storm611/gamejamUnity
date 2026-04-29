using UnityEngine;

public class Player : MonoBehaviour
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
        healthbar.UpdateHealthBar(maxHealth,health);
    }

    void Update()
    {
        
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health < 0)
        {
            health = 0;
        }
        healthbar.UpdateHealthBar(maxHealth,health);
    }
}
