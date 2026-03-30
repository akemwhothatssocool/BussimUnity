using System.Collections.Generic;
using UnityEngine;

public class CityManager : MonoBehaviour
{
    [Header("⚙️ ความเร็ว")]
    public float scrollSpeed = -10f;

    [Header("📐 ความกว้างบล็อก")]
    [Tooltip("ต้องตรงกับขนาดจริงของ prefab แกน X")]
    public float blockWidth = 100f;

    [Header("🛑 ค่าหยุด/ออกตัวเนียน")]
    public float decelerationRate = 5f;
    public float accelerationRate = 3f;

    public float _currentSpeed;
    private float _targetSpeed;
    private List<Transform> _blocks = new List<Transform>();
    private int _blockCount;
    private float _baseScrollSpeed;
    private bool _isScrollPaused;

    void Awake()
    {
        _baseScrollSpeed = scrollSpeed;
    }

    public void PauseScroll()
    {
        _isScrollPaused = true;
        _targetSpeed = 0f;
    }

    public void ResumeScroll()
    {
        _isScrollPaused = false;
        _targetSpeed = GetEffectiveScrollSpeed();
    }

    void Start()
    {
        foreach (Transform child in transform)
            _blocks.Add(child);

        _blockCount = _blocks.Count;

        if (_blockCount < 2)
        {
            Debug.LogError("[CityManager] ต้องมีลูกอย่างน้อย 2 ตัว!");
            enabled = false;
            return;
        }

        _blocks.Sort((a, b) => a.position.x.CompareTo(b.position.x));

        scrollSpeed = GetEffectiveScrollSpeed();
        _currentSpeed = scrollSpeed;
        _targetSpeed = scrollSpeed;
    }

    void Update()
    {
        // ✅ Lerp ความเร็วให้เนียน
        float rate = (_targetSpeed == 0f) ? decelerationRate : accelerationRate;
        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, rate * Time.deltaTime);

        // หยุดสนิทถ้าใกล้ 0
        if (Mathf.Abs(_currentSpeed) < 0.01f && _targetSpeed == 0f)
            _currentSpeed = 0f;

        if (_currentSpeed == 0f) return;

        float move = -_currentSpeed * Time.deltaTime;

        for (int i = 0; i < _blockCount; i++)
            _blocks[i].Translate(move, 0f, 0f, Space.World);

        for (int i = 0; i < _blockCount; i++)
        {
            if (move > 0 && _blocks[i].position.x > blockWidth * 3)
            {
                float leftmostX = GetLeftmostX();
                _blocks[i].position = new Vector3(leftmostX - blockWidth, _blocks[i].position.y, _blocks[i].position.z);
            }
            else if (move < 0 && _blocks[i].position.x < -blockWidth * 3)
            {
                float rightmostX = GetRightmostX();
                _blocks[i].position = new Vector3(rightmostX + blockWidth, _blocks[i].position.y, _blocks[i].position.z);
            }
        }
    }

    private float GetRightmostX()
    {
        float max = float.MinValue;
        foreach (Transform t in _blocks)
            if (t.position.x > max) max = t.position.x;
        return max;
    }

    private float GetLeftmostX()
    {
        float min = float.MaxValue;
        foreach (Transform t in _blocks)
            if (t.position.x < min) min = t.position.x;
        return min;
    }

    public void ApplyEngineSpeedBonus(float bonus)
    {
        scrollSpeed = GetEffectiveScrollSpeed(bonus);

        if (_isScrollPaused)
        {
            _targetSpeed = 0f;
            return;
        }

        _currentSpeed = scrollSpeed;
        _targetSpeed = scrollSpeed;
    }

    float GetEffectiveScrollSpeed(float overrideBonus = float.NaN)
    {
        float direction = Mathf.Approximately(_baseScrollSpeed, 0f) ? Mathf.Sign(scrollSpeed) : Mathf.Sign(_baseScrollSpeed);
        if (Mathf.Approximately(direction, 0f))
            direction = -1f;

        float baseMagnitude = Mathf.Abs(_baseScrollSpeed);
        float bonus = float.IsNaN(overrideBonus)
            ? (GameManager.Instance != null ? Mathf.Max(0f, GameManager.Instance.engineSpeedBonus) : 0f)
            : Mathf.Max(0f, overrideBonus);

        return direction * (baseMagnitude + bonus);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        for (int i = 0; i < _blocks.Count; i++)
        {
            Vector3 center = _blocks[i].position + Vector3.right * blockWidth * 0.5f;
            Gizmos.DrawWireCube(center, new Vector3(blockWidth, 10f, 10f));
        }
    }
#endif
}
