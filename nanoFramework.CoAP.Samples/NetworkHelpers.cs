//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using nanoFramework.Runtime.Events;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using Windows.Devices.WiFi;

namespace nanoFramework.Networking
{
    internal class NetworkHelpers
    {
        private static string c_SSID = "REPLACE-WITH-YOUR-SSID";
        private static string c_AP_PASSWORD = "REPLACE-WITH-YOUR-WIFI-KEY";

        static public ManualResetEvent IpAddressAvailable = new ManualResetEvent(false);      
        private static WiFiAdapter wifi;

        public static void SetupAndConnectNetwork()
        {           
            wifi = WiFiAdapter.FindAllAdapters()[0];          
            WiFiConnectionResult wiFiConnectionResult = wifi.Connect(c_SSID, WiFiReconnectionKind.Automatic, c_AP_PASSWORD);

            NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();

            if (nis.Length > 0)
            {
                NetworkInterface ni = nis[0];
                ni.EnableAutomaticDns();
                ni.EnableDhcp();

                CheckIP();

                if(!NetworkHelpers.IpAddressAvailable.WaitOne(5000, false))
                {
                    throw new NotSupportedException("ERROR: IP address is not assigned to the network interface.");
                }
            }
            else
            {
                throw new NotSupportedException("ERROR: there is no network interface configured.\r\nOpen the 'Edit Network Configuration' in Device Explorer and configure one.");
            }
        }

        private static void CheckIP()
        {

            Debug.WriteLine("Checking for IP");

            NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[wifi.NetworkInterface];
            if (ni.IPv4Address != null && ni.IPv4Address.Length > 0)
            {
                if (ni.IPv4Address[0] != '0')
                {
                    Debug.WriteLine($"We have and IP: {ni.IPv4Address}");
                    IpAddressAvailable.Set();
                }
            }
        }   
    }
}