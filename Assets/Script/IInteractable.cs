using UnityEngine;

/// <summary>
/// Interface สำหรับวัตถุทุกอย่างที่ Player สามารถ interact ได้
/// </summary>
public interface IInteractable
{
    bool CanInteract();
    void Interact();
    string GetPromptText();
}
