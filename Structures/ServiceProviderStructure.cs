using System;
using System.Net;
using System.Net.Sockets;

namespace LogicAppAdvancedTool.Structures
{
    public class ServiceProviderValidator
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public ServiceProviderType ServiceProvider { get; set; }
        public string Endpoint { get; set; }
        public int Port { get; set; }
        public IPAddress IP { get; private set; }
        public bool IsIPAddress { get; private set; }
        public bool IsEndpointEmpty { get; private set; }

        public string NameResolutionResult { get; private set; }
        public string TcpConnectionResult { get; private set; }

        public ServiceProviderValidator(string Name, string DisplayName, ServiceProviderType ServiceProvider, string Endpoint, int Port)
        {
            this.Name = Name;
            this.DisplayName = DisplayName;
            this.ServiceProvider = ServiceProvider;
            this.Endpoint = Endpoint;
            this.Port = Port;

            IPAddress ip;
            IsIPAddress = IPAddress.TryParse(Endpoint, out ip);

            if (IsIPAddress)
            {
                this.IP = ip;
            }

            IsEndpointEmpty = String.IsNullOrEmpty(Endpoint);
        }

        public void Validate()
        {
            if (IsEndpointEmpty)
            {
                NameResolutionResult = "No Endpoint found";
                TcpConnectionResult = "No Endpoint found";

                return;
            }

            if (!IsIPAddress)
            {
                try
                {
                    IPAddress[] ipAddresses = Dns.GetHostAddresses(Endpoint, AddressFamily.InterNetwork);

                    IP = ipAddresses[0];   //assume only 1 IP will return

                    NameResolutionResult = "Passed";
                }
                catch
                {
                    NameResolutionResult = "Failed";
                    TcpConnectionResult = "N/A (DNS test failed)";

                    return;
                }
            }
            else
            {
                NameResolutionResult = "N/A (Only IP)";
            }


            try
            {
                TcpClient tcpClient = new TcpClient();
                if (tcpClient.ConnectAsync(IP.ToString(), Port).Wait(1000))
                {
                    TcpConnectionResult = "Passed";
                }
                else
                {
                    TcpConnectionResult = "Failed";
                }
            }
            catch (Exception ex)
            {
                TcpConnectionResult = "Failed";
            }

        }
    }
}