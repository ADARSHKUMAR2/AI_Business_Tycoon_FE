using UnityEngine;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Identifies a building as a temporary event business.
    /// Added dynamically by GameManager when spawning event businesses.
    /// </summary>
    public class EventBusinessMarker : MonoBehaviour
    {
        public string EventId { get; set; }
    }
}
