using Blazored.LocalStorage;
using Grpc.Core;
using lumine8_GrpcService;

namespace lumine8.Client.Identity
{
    public partial class AuthenticationService
    {
        MainProto.MainProtoClient MainClient;
        ISyncLocalStorageService localStorage;

        SingletonVariables variables;

        public AuthenticationService(MainProto.MainProtoClient MainClient, ISyncLocalStorageService localStorageService, SingletonVariables variables)
        {
            this.MainClient = MainClient;
            this.localStorage = localStorageService;
            this.variables = variables;
        }

        public bool isAuthenticated = false;
        public Metadata headers = new();
        public string Message = "";
        public LoginUser loginUser = new();

        public void Reset()
        {
            isAuthenticated = false;
            loginUser = new();
        }

        public Task InitializeAuthenticate()
        {
            loginUser = localStorage.GetItem<LoginUser>("LoginUser");

            if (loginUser != null
                && !string.IsNullOrWhiteSpace(loginUser?.Username)
                && !string.IsNullOrWhiteSpace(loginUser?.PrivateKey))
            {
                return Task.Run(async () =>
                {
                    var b = await MainClient.AuthenticateAsync(loginUser);
                    isAuthenticated = b.IsAuthenticated;
                    if (isAuthenticated)
                    {
                        headers = new()
                        {
                            { "Username", loginUser.Username },
                            { "PrivateKey", loginUser.PrivateKey }
                        };
                    }
                    else
                    {
                        var us = variables.users.Where(x => x.Username == loginUser.Username).AsEnumerable();
                        foreach (var u in us)
                            variables.users.Remove(u);

                        localStorage.SetItem("Users", variables.users);
                    }
                });
            }

            return Task.CompletedTask;
            //return Task.Run(() => { });
        }
    }
}
