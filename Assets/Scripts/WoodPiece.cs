using UnityEngine;

// Добавляем интерфейс IWeapon
public class WoodPiece : MonoBehaviour, IPickupable, IUsable, IWeapon
{
    [Header("Атака")]
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private int _durability = 10;

    [Header("Бросок")]
    [SerializeField] private float _throwDamage = 15f;
    [SerializeField] private float _throwForce = 10f;
    [SerializeField] private float _throwSpinSpeed = 360f;

    [Header("Эффекты")]
    [SerializeField] private GameObject _breakParticleEffect;
    [SerializeField] private AudioClip _hitSound;
    [SerializeField] private AudioClip _breakSound;

    private GameObject _holder;
    private Rigidbody _rb;
    private bool _isThrown = false;
    private bool _isBroken = false;
    private AudioSource _audioSource;

    // Для анимации удара
    private Quaternion _originalRotation;
    private bool _isRotating = false;
    private float _rotationTimer = 0f;
    private float _rotationDuration = 0.15f;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && (_hitSound != null || _breakSound != null))
            _audioSource = gameObject.AddComponent<AudioSource>();

        _originalRotation = transform.rotation;
    }

    private void Update()
    {
        if (_isBroken) return;

        // Анимация поворота при ударе
        if (_isRotating)
        {
            _rotationTimer += Time.deltaTime;
            float t = _rotationTimer / _rotationDuration;

            if (t >= 1f)
            {
                transform.rotation = _originalRotation;
                _isRotating = false;
            }
        }
    }

    public void OnPickedUp(GameObject picker)
    {
        if (_isBroken) return;

        _holder = picker;
        _isThrown = false;
        transform.rotation = _originalRotation;
        _isRotating = false;

        // Отключаем физику при поднятии
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        // Уведомляем игрока через интерфейс
        PlayerMovement playerMovement = picker.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.SetCurrentWeapon(this); // Используем SetCurrentWeapon вместо SetCurrentStick
        }
    }

    public void OnDropped()
    {
        if (_isBroken) return;

        // Очищаем ссылку у игрока
        if (_holder != null)
        {
            PlayerMovement playerMovement = _holder.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ClearCurrentWeapon();
            }
        }

        _holder = null;

        // Включаем физику обратно
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
        }
    }

    public void OnPrimaryUse(GameObject user)
    {
        if (_isBroken) return;

        // Получаем точку откуда бьем
        PlayerMovement playerMovement = user.GetComponent<PlayerMovement>();
        if (playerMovement == null || playerMovement.PlayerMesh == null) return;

        GameObject playerMesh = playerMovement.PlayerMesh;

        // Создаем луч от игрока вперед
        Ray ray = new Ray(playerMesh.transform.position, playerMesh.transform.forward);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * _attackRange, Color.red, 0.5f);

        if (Physics.Raycast(ray, out hit, _attackRange))
        {
            // Проверяем врага
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy == null)
                enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
                DurabilityCost(1);
                PlayHitEffect();
                StartRotationAnimation();
                Debug.Log($"Нанесён урон {_damage} врагу {enemy.name}");
                return;
            }

            // Проверяем коробку
            Box box = hit.collider.GetComponent<Box>();
            if (box == null)
                box = hit.collider.GetComponentInParent<Box>();

            if (box != null)
            {
                box.TakeDamage(_damage);
                DurabilityCost(1);
                PlayHitEffect();
                StartRotationAnimation();
                Debug.Log($"Нанесён урон {_damage} коробке {box.name}");
            }
        }
    }

    public void OnSecondaryUse(GameObject user)
    {
        if (_isBroken) return;

        // Бросаем палку
        Throw(user);
    }

    public void Throw(GameObject thrower)
    {
        if (_isBroken) return;

        // Открепляем от игрока
        if (_holder != null)
        {
            PlayerMovement playerMovement = _holder.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ClearCurrentWeapon();
            }
        }

        _holder = null;
        _isThrown = true;

        // Включаем физику
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;

            // Добавляем силу броска
            Vector3 throwDirection = thrower.transform.forward + Vector3.up * 0.2f;
            _rb.AddForce(throwDirection * _throwForce, ForceMode.Impulse);

            // Добавляем вращение
            _rb.AddTorque(Random.onUnitSphere * _throwSpinSpeed);
        }

        Debug.Log("WoodPiece брошен!");
    }

    private void DurabilityCost(int cost)
    {
        _durability -= cost;
        if (_durability <= 0)
        {
            BreakWoodPiece();
        }
    }

    private void BreakWoodPiece()
    {
        if (_isBroken) return;

        _isBroken = true;

        Debug.Log("WoodPiece сломался!");

        // Эффект поломки
        if (_breakParticleEffect != null)
        {
            Instantiate(_breakParticleEffect, transform.position, Quaternion.identity);
        }

        // Звук поломки
        if (_breakSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_breakSound);
        }

        // Если wood piece в руках - принудительно выкидываем
        if (_holder != null)
        {
            Inventory inventory = _holder.GetComponent<Inventory>();
            if (inventory != null && inventory.GetCurrentItemGameObject() == gameObject)
            {
                inventory.ForceDrop();
            }
        }

        // Создаем эффект разлетающихся обломков
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.AddForce(Random.onUnitSphere * 3f, ForceMode.Impulse);
            _rb.AddTorque(Random.onUnitSphere * 5f, ForceMode.Impulse);
        }

        // Уничтожаем объект
        Destroy(gameObject, 0.2f);
    }

    private void PlayHitEffect()
    {
        if (_hitSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_hitSound);
        }
    }

    private void StartRotationAnimation()
    {
        _originalRotation = transform.rotation;
        _rotationTimer = 0f;
        _isRotating = true;

        // Поворачиваем wood piece для эффекта удара
        transform.rotation *= Quaternion.Euler(0, 90, 0);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isBroken) return;
        if (!_isThrown) return;
        if (_holder != null && collision.gameObject == _holder) return;

        // Проверяем врага
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy == null)
            enemy = collision.gameObject.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(_throwDamage);
            DurabilityCost(1);
            PlayHitEffect();
            Debug.Log($"Бросок нанёс {_throwDamage} урона врагу {enemy.name}");
            _isThrown = false;
            return;
        }

        // Проверяем коробку
        Box box = collision.gameObject.GetComponent<Box>();
        if (box == null)
            box = collision.gameObject.GetComponentInParent<Box>();

        if (box != null)
        {
            box.TakeDamage(_throwDamage);
            DurabilityCost(1);
            PlayHitEffect();
            Debug.Log($"Бросок нанёс {_throwDamage} урона коробке {box.name}");
            _isThrown = false;
            return;
        }

        _isThrown = false;
    }

    public bool IsBroken()
    {
        return _isBroken;
    }
}