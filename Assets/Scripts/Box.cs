using UnityEngine;

public class Box : MonoBehaviour
{
    public float health;
    public float maxHealth = 50f;

    [Header("Спавн предметов")]
    [SerializeField] private GameObject _stickPrefab;
    [SerializeField] private int _minSticksToSpawn = 1;
    [SerializeField] private int _maxSticksToSpawn = 3;
    [SerializeField] private float _spawnForce = 5f;

    [Header("Эффекты")]
    [SerializeField] private AudioClip _damageSound;
    [SerializeField] private AudioClip _destroySound;

    private AudioSource _audioSource;

    private void Awake()
    {
        health = maxHealth;
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    // Такой же простой метод TakeDamage как у Enemy
    public void TakeDamage(float damage)
    {
        health -= damage;

        // Звук удара
        if (_damageSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_damageSound);
        }

        Debug.Log($"Коробка получила {damage} урона. Осталось здоровья: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Debug.Log("Коробка разрушена!");

        // Звук разрушения
        if (_destroySound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_destroySound);
        }

        // Спавним палки
        SpawnSticks();

        // Уничтожаем коробку
        Destroy(gameObject);
    }

    private void SpawnSticks()
    {
        if (_stickPrefab == null) return;

        int sticksToSpawn = Random.Range(_minSticksToSpawn, _maxSticksToSpawn + 1);

        for (int i = 0; i < sticksToSpawn; i++)
        {
            // Случайная позиция вокруг коробки
            Vector3 randomOffset = Random.insideUnitSphere * 1.5f;
            randomOffset.y = Mathf.Abs(randomOffset.y);
            Vector3 spawnPosition = transform.position + randomOffset;

            // Спавним палку
            GameObject newStick = Instantiate(_stickPrefab, spawnPosition, Random.rotation);

            // Добавляем силу для разлёта
            Rigidbody stickRb = newStick.GetComponent<Rigidbody>();
            if (stickRb != null)
            {
                Vector3 randomForce = Random.insideUnitSphere * _spawnForce;
                randomForce.y = Mathf.Abs(randomForce.y) * 2f;
                stickRb.AddForce(randomForce, ForceMode.Impulse);
            }
        }
    }
}