using Blazored.LocalStorage;
using lumine8_GrpcService;
using Newtonsoft.Json;
using static lumine8.SingletonVariables;

namespace lumine8.Client.Identity
{
    public class AccountManager
    {
        MainProto.MainProtoClient MainClient;
        AuthenticationService authService;

        SingletonVariables variables;
        public ILocalStorageService localStorage;

        public LoginUser loginUser = new();
        public string privateKey = string.Empty;
        public CreateAccountResponse response = null;

        public string Message = "";

        public AccountManager(MainProto.MainProtoClient MainClient, AuthenticationService authService, SingletonVariables variables, ILocalStorageService localStorageService)
        {
            this.MainClient = MainClient;
            this.authService = authService;
            this.localStorage = localStorageService;
            this.variables = variables;
        }

        public async Task<bool> SignInAsync(LoginUser loginUser)
        {
            if (loginUser != null)
            {
                var auth = await MainClient.AuthenticateAsync(loginUser);

                if (auth.IsAuthenticated)
                {
                    await localStorage.SetItemAsync("LoginUser", loginUser);

                    return true;
                }
            }

            Message = "User not authorized";
            return false;
        }

        public async Task<bool> SetupAccountAsync(LoginUser loginUser)
        {
            if (loginUser != null)
            {
                if (response != null)
                {
                    loginUser.Mnemonic = response.Mnemonic;
                    loginUser.PrivateKey = response.PrivateKey;
                }

                var b = await MainClient.AuthenticateAsync(loginUser);

                if (b.IsAuthenticated)
                {
                    variables.users = await localStorage.GetItemAsync<List<LoginUser>>("Users");
                    if(variables.users == null)
                        variables.users = new();

                    variables.users.Add(loginUser);

                    await localStorage.SetItemAsync("Users", variables.users);
                    await localStorage.SetItemAsync("LoginUser", loginUser);

                    await authService.InitializeAuthenticate();

                    return authService.isAuthenticated;
                }
            }

            Message = "User is not authorized";
            return false;
        }

        public async Task SignOut(LoginUser loginUser)
        {
            await localStorage.RemoveItemAsync("LoginUser");
            this.loginUser = new();

            authService.Reset();
            await authService.InitializeAuthenticate();
        }

        public async Task<bool> SwitchAccountAsync(LoginUser loginUser)
        {
            var current = await localStorage.GetItemAsync<LoginUser>("LoginUser");

            loginUser.Mnemonic = current.Mnemonic;
            loginUser.PrivateKey = current.PrivateKey;

            var b = await SignInAsync(loginUser);
            if (b)
            {
                loginUser.Mnemonic = string.Empty;
                loginUser.PrivateKey = string.Empty;
                await localStorage.SetItemAsync("LoginUser", loginUser);
            }

            return b;
        }
    }
}
