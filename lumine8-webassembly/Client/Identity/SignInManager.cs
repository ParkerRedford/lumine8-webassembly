using Blazored.LocalStorage;
using Grpc.Core;
using lumine8_GrpcService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace lumine8.Client.Identity
{
    public class SignInManager
    {
        MainProto.MainProtoClient MainClient;
        AuthenticationService authService;
        public ILocalStorageService localStorage;

        public LoginUser loginUser = new();
        public Metadata headers = new();

        public string Message = "";

        public SignInManager(MainProto.MainProtoClient MainClient, AuthenticationService authService, ILocalStorageService localStorage)
        {
            this.MainClient = MainClient;
            this.authService = authService;
            this.localStorage = localStorage;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
            Initialize();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
        }

        public async Task Initialize()
        {
            loginUser = await localStorage.GetItemAsync<LoginUser>("Login");
        }

        public async Task<bool> SignInAsync(LoginUser loginUser)
        {
            if (loginUser != null)
            {
                var auth = await MainClient.AuthenticateAsync(loginUser);

                if (auth.IsAuthenticated)
                {
                    await localStorage.SetItemAsync("Login", loginUser);
                    return true;
                }
            }

            Message = "User is not authorized";
            return false;
        }

        public async Task SignOut(LoginUser loginUser)
        {
            await authService.InitializeAuthenticate();

            await localStorage.RemoveItemAsync($"Login");
        }
    }
}
