using nanoFramework.CoAP.Channels;
using nanoFramework.CoAP.Helpers;
using nanoFramework.CoAP.Message;
using nanoFramework.Networking;
using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace nanoFramework.CoAP.Samples
{
    public class BasicCoAPNONClient
    {
        /// <summary>
        /// Holds an instance of the CoAP client
        /// </summary>
        private static CoAPClientChannel coapClient = null;
        /// <summary>
        /// Used for matching request / response / associated request
        /// </summary>
        private static string _mToken = "";
        /// <summary>
        /// Entry point
        /// </summary>     
        public static void Main()
        {
            NetworkHelpers.SetupAndConnectNetwork();
            Debug.WriteLine("Waiting for network up and IP address...");
            NetworkHelpers.IpAddressAvailable.WaitOne();

            string serverIP = "localhost";
            int serverPort = 5683;

            coapClient = new CoAPClientChannel();
            coapClient.Initialize(serverIP, serverPort);
            coapClient.CoAPResponseReceived += new CoAPResponseReceivedHandler(OnCoAPResponseReceived);
            coapClient.CoAPRequestReceived += new CoAPRequestReceivedHandler(OnCoAPRequestReceived);
            coapClient.CoAPError += new CoAPErrorHandler(OnCoAPError);
            //Send a NON request to get the temperature...in return we will get a NON request from the server
            CoAPRequest coapReq = new CoAPRequest(CoAPMessageType.NON,
                                                CoAPMessageCode.GET,
                                                100);//hardcoded message ID as we are using only once
            string uriToCall = "coap://" + serverIP + ":" + serverPort + "/sensors/temp";
            coapReq.SetURL(uriToCall);
            _mToken = DateTime.Today.ToString("HHmmss");//Token value must be less than 8 bytes
            coapReq.Token = new CoAPToken(_mToken);//A random token
            coapClient.Send(coapReq);
            Thread.Sleep(Timeout.Infinite);//blocks
            while (true)
            {
                Thread.Sleep(3000);
            }
        }
        /// <summary>
        /// Called when error occurs
        /// </summary>
        /// <param name="e">The exception that occurred</param>
        /// <param name="associatedMsg">The associated message (if any)</param>    
        static void OnCoAPError(Exception e, AbstractCoAPMessage associatedMsg)
        {
            //Write your error logic here
        }

        /// <summary>
        /// Called when a request is received...
        /// </summary>
        /// <param name="coapReq">The CoAPRequest object</param>
        static void OnCoAPRequestReceived(CoAPRequest coapReq)
        {

        }

        /// <summary>
        /// Called when a response is received against a sent request
        /// </summary>
        /// <param name="coapResp">The CoAPResponse object</param>
        static void OnCoAPResponseReceived(CoAPResponse coapResp)
        {
            string tokenRx = (coapResp.Token != null && coapResp.Token.Value != null) ? AbstractByteUtils.ByteToStringUTF8(coapResp.Token.Value) : "";
            if (tokenRx == _mToken)
            {
                //This response is against the NON request for getting temperature we issued earlier
                if (coapResp.Code.Value == CoAPMessageCode.CONTENT)
                {
                    //Get the temperature
                    string tempAsJSON = AbstractByteUtils.ByteToStringUTF8(coapResp.Payload.Value);
                    Hashtable tempValues = JSONResult.FromJSON(tempAsJSON);
                    int temp = Convert.ToInt32(tempValues["temp"].ToString());
                    //Now do something with this temperature received from the server
                }
                else
                {
                    //Will come here if an error occurred..
                }
            }
        }
    }
}
