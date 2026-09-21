using System;
using System.Collections.Generic;

namespace AIBusinessTycoon.Data
{
    /// <summary>
    /// Phase 3: Represents a single piece of trash on the store floor.
    /// Mirrors backend TrashItem model.
    /// trash_id is used to call DELETE .../trash/{trash_id} when cleaned.
    /// position_x/y are world-space coordinates (floats, not grid ints).
    /// </summary>
    [Serializable]
    public class TrashItem
    {
        public string trash_id;
        public float  position_x;
        public float  position_y;
        public string dropped_at;

        public Vector3Data WorldPosition => new Vector3Data(position_x, 0f, position_y);
    }

    /// <summary>
    /// Phase 3: Request payload when a customer drops trash.
    /// Sent to POST .../trash
    /// </summary>
    [Serializable]
    public class SpawnTrashRequest
    {
        public float position_x;
        public float position_y;

        public SpawnTrashRequest(float x, float y)
        {
            position_x = x;
            position_y = y;
        }
    }

    /// <summary>
    /// Simple serializable Vector3 wrapper.
    /// </summary>
    [Serializable]
    public class Vector3Data
    {
        public float x, y, z;
        public Vector3Data(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public UnityEngine.Vector3 ToVector3() => new UnityEngine.Vector3(x, y, z);
    }
}
