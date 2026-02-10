using UnityEngine;

[CreateAssetMenu(fileName = "NewTicket", menuName = "BusSystem/TicketData")]
public class TicketData : ScriptableObject
{
    public string ticketName;   // ชื่อตั๋ว เช่น "ตั๋วแดง"
    public int price;           // ราคา เช่น 8 บาท
    public Color ticketColor;   // สีของตั๋ว (เอาไว้เปลี่ยนสี UI)
}