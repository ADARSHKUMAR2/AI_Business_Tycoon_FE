using System;

namespace AIBusinessTycoon.Data
{
    /// <summary>
    /// Generic wrapper for API responses from the backend.
    /// </summary>
    [Serializable]
    public class APIResponse<T>
    {
        public bool success;
        public string message;
        public T data;
        
        public APIResponse()
        {
            success = false;
            message = "";
            data = default(T);
        }
    }
}
