using UnityEngine;

public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        // สั่งให้รูปภาพหันหน้าไปทางเดียวกับกล้องหลักเสมอ
        if (Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
        }
    }
}