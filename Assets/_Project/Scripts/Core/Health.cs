using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Параметры HP")]
    public int maxHp = 100;
    private int _currentHp;

    [Header("Визуал полоски")]
    public Transform hpBarForeground; // Зеленая плашка, которую мы будем сжимать

    private void OnEnable()
    {
        EventBus.Subscribe<DamageEvent>(OnDamageReceived);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DamageEvent>(OnDamageReceived);
    }

    private void Start()
    {
        _currentHp = maxHp;
        UpdateHealthBar();
    }

    // Публичный метод, чтобы EnemyAI мог принудительно задать HP из JSON
    public void InitializeHp(int maxHealth)
    {
        maxHp = maxHealth;
        _currentHp = maxHp;
        UpdateHealthBar();
    }

    private void OnDamageReceived(DamageEvent data)
    {
        // Проверяем, что урон нанесен именно ЭТОМУ объекту
        if (data.Target == gameObject)
        {
            _currentHp = _currentHp - data.Amount;
            
            // Защита: здоровье не должно падать ниже нуля
            if (!(_currentHp > 0))
            {
                _currentHp = 0;
            }

            Debug.Log(gameObject.name + " получил урон: " + data.Amount + " | Осталось HP: " + _currentHp);
            UpdateHealthBar();

            if (!(_currentHp > 0))
            {
                Die();
            }
        }
    }

    private void UpdateHealthBar()
    {
        if (hpBarForeground == null) return;

        // 1. Считаем процент здоровья (от 0.0f до 1.0f)
        float hpPercent = (float)_currentHp / maxHp;
    
        // Защита, чтобы масштаб не стал отрицательным
        if (!(hpPercent >= 0f)) hpPercent = 0f;

        // Исходная максимальная ширина полоски из префаба (у нас это 0.8f)
        float maxBarWidth = 0.8f; 

        // 2. Вычисляем новый масштаб для зеленой полоски
        Vector3 newScale = hpBarForeground.localScale;
        newScale.x = maxBarWidth * hpPercent; // Сжимаем ширину
        hpBarForeground.localScale = newScale;

        // 3. МАТЕМАТИЧЕСКИЙ СДВИГ PIVOT:
        // Сдвигаем позицию X влево пропорционально потере здоровья.
        // Когда HP = 100%, сдвиг равен 0 (полоска ровно по центру перекрывает черный фон).
        // Когда HP уменьшается, левый край остается на месте, а правый уезжает влево!
        Vector3 newPos = hpBarForeground.localPosition;
        newPos.x = - (maxBarWidth * (1f - hpPercent)) / 2f;
        hpBarForeground.localPosition = newPos;
    }


    private void Die()
    {
        Debug.Log(gameObject.name + " погиб!");
        
        // Если погиб враг — он просто уничтожается. 
        // (В будущем здесь можно запускать анимацию смерти или давать опыт)
        if (GetComponent<EnemyAI>() != null)
        {
            Destroy(gameObject);
        }
    }
}
