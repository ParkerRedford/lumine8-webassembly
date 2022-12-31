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

        public SignalRSingleton(HubConnection hub, NavigationManager navigation)
        {
            this.hub = hub;
            this.navigation = navigation;

            hub = new HubConnectionBuilder()
                .WithAutomaticReconnect()
                .WithUrl(navigation.ToAbsoluteUri("/postreal"))
                .Build();
            hub.StartAsync();
        }
    }
}
