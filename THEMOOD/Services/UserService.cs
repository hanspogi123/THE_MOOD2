using Firebase.Auth;

namespace THEMOOD.Services
{
    public class UserService
    {
        private static UserService _instance;
        private string _currentToken;
        private User _currentUser;

        public static UserService Instance => _instance ??= new UserService();

        private UserService() { }

        public void SetCurrentUser(string token, User user)
        {
            _currentToken = token;
            _currentUser = user;
        }

        public string GetCurrentToken()
        {
            return _currentToken;
        }

        public User GetCurrentUser()
        {
            return _currentUser;
        }

        public void ClearUser()
        {
            _currentToken = null;
            _currentUser = null;
        }
    }
} 