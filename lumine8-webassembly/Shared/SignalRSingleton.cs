using lumine8_GrpcService;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;

namespace lumine8
{
    public class SignalRSingleton
    {
        public HubConnection hub { get; set; }
        NavigationManager navigation { get; set; }

        public ObservableCollection<Message> messages { get; set; } = new();
        public ObservableCollection<MessageOnMessage> onMessages { get; set; } = new();

        public SignalRSingleton(HubConnection hub, SingletonVariables variables)
        {
            this.hub = hub;
            this.navigation = navigation;

            if (hub.State != HubConnectionState.Connected)
            {
                hub = new HubConnectionBuilder()
                    .WithAutomaticReconnect()
                    .WithUrl($"{variables.uri}/postreal")
                    .Build();
                hub.StartAsync();
            }
        }
    }
}
