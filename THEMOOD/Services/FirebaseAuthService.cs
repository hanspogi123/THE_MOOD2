using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Firebase.Auth;
using System.Threading.Tasks;

namespace THEMOOD.Services
{
    public class FirebaseAuthService
    {
        private const string ApiKey = "AIzaSyChdaQxyY7pa4LgoNMlGG7pKWBD85B0ta8";
        private readonly FirebaseAuthProvider authProvider;

        public FirebaseAuthService()
        {
            authProvider = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
        }

        public async Task<FirebaseAuthLink> SignUp(string email, string password)
        {
            return await authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
        }

        public async Task UpdateUserProfile(string userId, string displayName)
        {
            // For now, we'll just store the display name in the user object
            // The actual profile update will be handled by Firebase automatically
            // when the user signs in next time
            var user = await authProvider.GetUserAsync(userId);
            if (user != null)
            {
                // The display name will be updated when the user signs in
                // We don't need to do anything else here
            }
        }

        public async Task<FirebaseAuthLink> SignIn(string email, string password)
        {
            return await authProvider.SignInWithEmailAndPasswordAsync(email, password);
        }

        public async Task<string> GetFreshToken(FirebaseAuthLink authLink)
        {
            await authLink.GetFreshAuthAsync();
            return authLink.FirebaseToken;
        }

        public async Task SignOut()
        {
            // Firebase Auth doesn't require explicit sign out on the server side
            // We just need to clear any local auth state
            authProvider.Dispose();
        }

        public async Task<User> GetCurrentUser(string token)
        {
            return await authProvider.GetUserAsync(token);
        }
    }
}
