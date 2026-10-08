using System;

namespace AIBusinessTycoon.Data
{
    [Serializable]
    public class RegisterRequest
    {
        public string name;
        public string email;
        public string password;
    }

    [Serializable]
    public class LoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class GuestLoginRequest
    {
        public string name;
    }

    [Serializable]
    public class AuthResponse
    {
        public string player_id;
        public string name;
        public string email;
        public bool is_guest;
    }
}
