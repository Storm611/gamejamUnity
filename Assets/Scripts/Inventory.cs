using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private Transform _holdPoint;

    private GameObject _currentItem;
    private IPickupable _pickupInterface;
    private IUsable _usableInterface;
    private Player _playerScript;

    [SerializeField] private float dropForce = 1f;
    [SerializeField] private GameObject PlayerMesh;

    private List<GameObject> _nearbyItems = new List<GameObject>();

    // Флаг, что у игрока уже есть предмет
    private bool _hasItem = false;

    private void Awake()
    {
        _playerScript = GetComponent<Player>();
    }

    private void Update()
    {
        // Подбор по клавише E (только если нет предмета)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!_hasItem)
                TryPickupNearest();
            else
                DropItem();
        }

        // Использование предмета в руках (только если есть предмет)
        if (_hasItem && _currentItem != null)
        {
            if (Input.GetMouseButtonDown(0)) _usableInterface?.OnPrimaryUse(gameObject);

            if (Input.GetMouseButtonDown(1)) _usableInterface?.OnSecondaryUse(gameObject);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            _playerScript.TakeDamage(10f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, есть ли на объекте интерфейс подбора
        if (other.TryGetComponent<IPickupable>(out _))
        {
            // Добавляем в список только если у игрока нет предмета
            if (!_hasItem && !_nearbyItems.Contains(other.gameObject))
            {
                _nearbyItems.Add(other.gameObject);
                // Опционально: подсветить предмет
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_nearbyItems.Contains(other.gameObject))
        {
            _nearbyItems.Remove(other.gameObject);
            // Опционально: убрать подсветку
        }
    }

    private void TryPickupNearest()
    {
        // Нельзя подобрать предмет, если уже есть
        if (_hasItem)
        {
            Debug.Log("У вас уже есть предмет! Сначала выбросьте текущий (нажмите E)");
            return;
        }

        // Убираем из списка все null (на случай если предмет уничтожился)
        _nearbyItems.RemoveAll(item => item == null);

        if (_nearbyItems.Count == 0)
        {
            Debug.Log("Нет предметов поблизости");
            return;
        }

        // Находим ближайший предмет
        GameObject nearest = GetNearestItem();

        if (nearest != null)
        {
            PickupItem(nearest);
        }
    }

    private GameObject GetNearestItem()
    {
        GameObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (var item in _nearbyItems)
        {
            if (item == null) continue;

            float dist = Vector3.Distance(transform.position, item.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = item;
            }
        }

        return nearest;
    }

    private void PickupItem(GameObject item)
    {
        // Убираем предмет из списка ближайших
        _nearbyItems.Remove(item);

        _currentItem = item;
        _hasItem = true;

        // Получаем интерфейсы
        _pickupInterface = item.GetComponent<IPickupable>();
        _usableInterface = item.GetComponent<IUsable>();

        _pickupInterface.OnPickedUp(gameObject);

        // Физическое прикрепление к руке
        item.transform.SetParent(_holdPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        // Отключаем физику
        if (item.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        // Отключаем коллайдер
        if (item.TryGetComponent<Collider>(out var col))
            col.enabled = false;
    }

    private void DropItem()
    {
        if (!_hasItem || _currentItem == null) return;

        // Включаем физику обратно
        if (_currentItem.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        if (_currentItem.TryGetComponent<Collider>(out var col))
            col.enabled = true;

        // Открепляем
        _currentItem.transform.SetParent(null);

        // Сообщаем предмету, что его выбросили
        _pickupInterface?.OnDropped();

        // Небольшой импульс вперёд при выбрасывании
        rb?.AddForce(PlayerMesh.transform.forward * 3f, ForceMode.Impulse);

        // Добавляем обратно в список, если он всё ещё в триггере
        if (_currentItem != null && !_nearbyItems.Contains(_currentItem))
        {
            _nearbyItems.Add(_currentItem);
        }

        // Сбрасываем ссылки
        _currentItem = null;
        _pickupInterface = null;
        _usableInterface = null;
        _hasItem = false;
    }

    public void ForceDrop()
    {
        if (!_hasItem || _currentItem == null) return;

        // Включаем физику обратно
        if (_currentItem.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        if (_currentItem.TryGetComponent<Collider>(out var col))
            col.enabled = true;

        // Открепляем
        _currentItem.transform.SetParent(null);

        // Сообщаем предмету, что его выбросили
        _pickupInterface?.OnDropped();

        // Импульс вперёд при выбрасывании
        if (_currentItem.TryGetComponent<Rigidbody>(out var rb2))
        {
            rb2.AddForce(PlayerMesh.transform.forward * dropForce + Vector3.up * 3f, ForceMode.Impulse);
        }

        // Сбрасываем ссылки
        _currentItem = null;
        _pickupInterface = null;
        _usableInterface = null;
        _hasItem = false;
    }

    public bool IsHandsFree() => !_hasItem;

    // Метод для получения текущего предмета (интерфейс)
    public IPickupable GetCurrentItem()
    {
        return _pickupInterface;
    }

    // Метод для получения GameObject текущего предмета
    public GameObject GetCurrentItemGameObject()
    {
        return _currentItem;
    }
}