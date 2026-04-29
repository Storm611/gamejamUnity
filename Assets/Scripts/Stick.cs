using UnityEngine;
using System.Collections;

public class Stick : MonoBehaviour, IPickupable, IUsable, IWeapon
{
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _attackRange = 3f;
    private GameObject PlayerMesh;

    [SerializeField] private float _throwDamage = 15f;
    [SerializeField] private float _throwSpinSpeed = 720f;
    [SerializeField] private int _durability = 20;

    private bool _isThrown = false;
    private GameObject _thrower;
    private Rigidbody _rb;
    private Quaternion _originalRotation;
    private bool _isRotating = false;
    private float _rotationTimer = 0f;
    private float _rotationDuration = 0.2f;

    // Переменные для способности кручения
    [SerializeField] private float _spinDamage = 8f;
    [SerializeField] private float _spinRange = 4f;
    [SerializeField] private int _spinHitCount = 3;
    [SerializeField] private float _spinDuration = 1.5f;
    [SerializeField] private float _spinSpeed = 720f;

    private bool _isSpinning = false;
    private float _spinTimer = 0f;
    private int _hitsDone = 0;
    private float _lastHitTime = 0f;
    [SerializeField] private float _hitCooldown = 0.3f;

    // Флаг, что палка уже сломана
    private bool _isBroken = false;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _originalRotation = transform.rotation;
    }

    private void Update()
    {
        if (_isBroken) return; // Если сломана - не обрабатываем

        // Обрабатываем анимацию обычного поворота
        if (_isRotating)
        {
            _rotationTimer += Time.deltaTime;
            float t = _rotationTimer / _rotationDuration;

            if (t >= 1f)
            {
                transform.rotation = _originalRotation * Quaternion.Euler(0, 90, 0);
                _isRotating = false;
            }
        }

        // Обрабатываем кручение
        if (_isSpinning)
        {
            _spinTimer += Time.deltaTime;

            // Вращаем палку вокруг игрока
            if (_thrower != null)
            {
                // Вращаем палку вокруг игрока по кругу
                float angle = _spinSpeed * Time.deltaTime;
                transform.RotateAround(_thrower.transform.position, Vector3.up, angle);

                // Также вращаем саму палку для эффекта
                transform.Rotate(Vector3.up, angle * 2);
            }

            // Периодически наносим урон
            if (Time.time >= _lastHitTime + _hitCooldown && _hitsDone < _spinHitCount)
            {
                PerformSpinAttack();
                _lastHitTime = Time.time;
            }

            // Завершаем кручение и ломаем палку
            if (_spinTimer >= _spinDuration || _hitsDone >= _spinHitCount)
            {
                BreakStick();
            }
        }
    }

    public void OnPickedUp(GameObject picker)
    {
        if (_isBroken) return;

        transform.rotation = Quaternion.Euler(0, 0, 0);
        _thrower = picker;
       // transform.rotation = _originalRotation;
        _isRotating = false;
        _isSpinning = false;

        // Уведомляем игрока о том, что палка поднята
        PlayerMovement playerMovement = picker.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.SetCurrentStick(this);
        }
    }

    public void OnDropped()
    {
        if (_isBroken) return;

        StopSpin();

        // Очищаем ссылку у игрока
        if (_thrower != null)
        {
            PlayerMovement playerMovement = _thrower.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ClearCurrentStick();
            }
        }

        _thrower = null;
    }

    private void DurabilityCost(int _cost)
    {
        _durability -= _cost;
        if (_durability <= 0)
        {
            BreakStick();
        }
    }

    private void PerformSpinAttack()
    {
        if (_thrower == null) return;

        PlayerMesh = _thrower.GetComponent<PlayerMovement>().PlayerMesh;
        if (PlayerMesh == null) return;

        // Находим всех врагов в радиусе кручения
        Collider[] hitColliders = Physics.OverlapSphere(_thrower.transform.position, _spinRange);
        bool hitAnyEnemy = false;

        foreach (var collider in hitColliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy == null)
                enemy = collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(_spinDamage);
                hitAnyEnemy = true;
                Debug.Log($"Кручение нанесло {_spinDamage} урона врагу {enemy.name}");
            }
        }

        if (hitAnyEnemy)
        {
            _hitsDone++;
            Debug.Log($"Удар кручением {_hitsDone}/{_spinHitCount}");
        }
    }

    private void StartSpin()
    {
        if (_isSpinning || _isBroken) return;

        _isSpinning = true;
        _spinTimer = 0f;
        _hitsDone = 0;
        _lastHitTime = -_hitCooldown;

        // Отключаем физику при кручении
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        // Делаем коллайдер триггером
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        Debug.Log("Начало кручения палкой!");
    }

    private void StopSpin()
    {
        if (!_isSpinning) return;

        _isSpinning = false;

        // Возвращаем коллайдер в нормальное состояние
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        // Включаем физику только если предмет не в руках
        if (_thrower == null || _thrower.GetComponent<Inventory>()?.GetCurrentItemGameObject() != gameObject)
        {
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        Debug.Log("Кручение завершено!");
    }

    // Новый метод для поломки палки
    private void BreakStick()
    {
        if (_isBroken) return;

        _isBroken = true;
        _isSpinning = false;
        _isRotating = false;

        Debug.Log("Палка сломалась от использования!");

        // Если палка в руках у игрока - принудительно выбрасываем
        if (_thrower != null)
        {
            Inventory inventory = _thrower.GetComponent<Inventory>();
            if (inventory != null && inventory.GetCurrentItemGameObject() == gameObject)
            {
                inventory.ForceDrop();
            }
        }

        // Визуальный эффект поломки (опционально)
        // Можно добавить партиклы, звук разлома и т.д.

        // Создаём эффект разлетающихся щепок (опционально)
        CreateBreakEffect();

        // Уничтожаем палку
        Destroy(gameObject, 0.1f); // Небольшая задержка для эффектов
    }

    // Эффект поломки (опционально)
    private void CreateBreakEffect()
    {
        // Можно добавить:
        // 1. Партиклы щепок
        // 2. Звук разлома
        // 3. Визуальный эффект

        Debug.Log("Эффект поломки палки!");

        // Пример создания простых визуальных эффектов
        if (_rb != null)
        {
            // Разбрасываем обломки (если есть модель обломков)
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            _rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }
    }

    public void OnPrimaryUse(GameObject user)
    {
        if (_isBroken) return;
        if (_isSpinning) return;

        PlayerMesh = user.GetComponent<PlayerMovement>().PlayerMesh;

        Ray ray = new Ray(PlayerMesh.transform.position, PlayerMesh.transform.forward);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * _attackRange, Color.red, 1f);

        if (Physics.Raycast(ray, out hit, _attackRange))
        {
            // Проверяем на Enemy
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy == null)
                enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
                DurabilityCost(1);
                Debug.Log($"Нанесён урон {_damage} врагу {enemy.name}");
                StartRotation();
                return; // Важно: выходим, чтобы не проверять дальше
            }

            // ДОБАВИТЬ: Проверяем на Box
            Box box = hit.collider.GetComponent<Box>();
            if (box == null)
                box = hit.collider.GetComponentInParent<Box>();

            if (box != null)
            {
                box.TakeDamage(_damage);
                DurabilityCost(1);
                Debug.Log($"Нанесён урон {_damage} коробке {box.name}");
                StartRotation();
            }
        }
    }

    public void OnSecondaryUse(GameObject user)
    {
        if (_isBroken) return; // Если сломана - нельзя использовать

        // Начинаем кручение вместо броска
        StartSpin();
    }

    public void StartSpinFromPlayer(GameObject player)
    {
        if (_isSpinning || _isBroken) return;

        _thrower = player;
        StartSpin();
    }

    public bool IsSpinning()
    {
        return _isSpinning;
    }

    public bool IsBroken()
    {
        return _isBroken;
    }

    private void StartRotation()
    {
        _originalRotation = transform.rotation;
        _rotationTimer = 0f;
        _isRotating = true;
        transform.rotation *= Quaternion.Euler(0, 90, 0);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isBroken) return;

        if (!_isThrown) return;
        if (_thrower != null && collision.gameObject == _thrower) return;

        // Проверяем на врага
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy == null)
            enemy = collision.gameObject.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(_throwDamage);
            DurabilityCost(1);
            Debug.Log($"Бросок нанёс {_throwDamage} урона врагу {enemy.name}");
            _isThrown = false;
            return;
        }

        // Проверяем на коробку
        Box box = collision.gameObject.GetComponent<Box>();
        if (box == null)
            box = collision.gameObject.GetComponentInParent<Box>();

        if (box != null)
        {
            box.TakeDamage(_throwDamage);
            DurabilityCost(1);
            Debug.Log($"Бросок нанёс {_throwDamage} урона коробке {box.name}");
            _isThrown = false;
            return;
        }

        // Если ничего не нашли
        Debug.Log($"Ни враг, ни коробка не найдены на {collision.gameObject.name} и его родителях");

        _isThrown = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (_isSpinning && _thrower != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_thrower.transform.position, _spinRange);
        }
    }
}