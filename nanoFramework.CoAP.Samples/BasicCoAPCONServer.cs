using nanoFramework.CoAP.Channels;
using nanoFramework.CoAP.Helpers;
using nanoFramework.CoAP.Message;
using nanoFramework.Networking;
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace nanoFramework.CoAP.Samples
{
    public class BasicCoAPCONServer
    {      
        /// <summary>
        /// Holds the server channel instance
        /// </summary>
        private static CoAPServerChannel coapServer = null;

        public static void Main()
        {
            NetworkHelpers.SetupAndConnectNetwork();
            Debug.WriteLine("Waiting for network up and IP address...");
            NetworkHelpers.IpAddressAvailable.WaitOne();

            StartServer();

            while (true)
            {
                Thread.Sleep(3000);
            }
        }

        /// <summary>
        /// Start the server
        /// </summary>
        private static void StartServer()
        {
            coapServer = new CoAPServerChannel();
            coapServer.Initialize(null, 5683);
            coapServer.CoAPResponseReceived += new CoAPResponseReceivedHandler(OnCoAPResponseReceived);
            coapServer.CoAPRequestReceived += new CoAPRequestReceivedHandler(OnCoAPRequestReceived);
            coapServer.CoAPError += new CoAPErrorHandler(OnCoAPError);
        }

        /// <summary>
        /// Called when an error occurs.Not used in this sample
        /// </summary>
        /// <param name="e">The exception</param>
        /// <param name="associatedMsg">Associated message if any, else null</param>
        private static void OnCoAPError(Exception e, AbstractCoAPMessage associatedMsg)
        {
            //put error handling code here
        }
        /// <summary>
        /// Called when a CoAP request is received...we will only support CON requests 
        /// of type GET... the path is sensors/temp
        /// </summary>
        /// <param name="coapReq">CoAPRequest object</param>
        private static void OnCoAPRequestReceived(CoAPRequest coapReq)
        {
            string reqPath = (coapReq.GetPath() != null) ? coapReq.GetPath().ToLower() : "";

            if (coapReq.MessageType.Value == CoAPMessageType.CON)
            {
                if (coapReq.Code.Value != CoAPMessageCode.GET)
                {
                    CoAPResponse resp = new CoAPResponse(CoAPMessageType.ACK,
                                                        CoAPMessageCode.METHOD_NOT_ALLOWED,
                                                        coapReq /*Copy all necessary values from request in the response*/);
                    //When you use the constructor that accepts a request, then automatically
                    //the message id , token and remote sender values are copied over to the response
                    coapServer.Send(resp);
                }
                else if (reqPath != "sensors/temp")
                {
                    //We do not understand this..
                    CoAPResponse resp = new CoAPResponse(CoAPMessageType.ACK,
                                                            CoAPMessageCode.NOT_FOUND,
                                                            coapReq /*Copy all necessary values from request in the response*/);
                    coapServer.Send(resp);
                }
                else
                {
                    Debug.WriteLine(coapReq.ToString());
                    CoAPResponse resp = new CoAPResponse(CoAPMessageType.ACK,
                                                        CoAPMessageCode.CONTENT,
                                                        coapReq /*Copy all necessary values from request in the response*/);
                    //The payload will be JSON
                    Hashtable ht = new Hashtable();
                    ht.Add("temp", GetRoomTemperature());
                    string jsonStr = JSONResult.ToJSON(ht);
                    resp.AddPayload(jsonStr);
                    //Tell recipient about the content-type of the response
                    resp.AddOption(CoAPHeaderOption.CONTENT_FORMAT, AbstractByteUtils.GetBytes(CoAPContentFormatOption.APPLICATION_JSON));
                    //send it
                    coapServer.Send(resp);
                }
            }
        }
        /// <summary>
        /// Called when CoAP response is received (ACK, RST). Not used in this sample
        /// </summary>
        /// <param name="coapResp">CoAPResponse</param>
        private static void OnCoAPResponseReceived(CoAPResponse coapResp)
        {
            //not used in this sample
        }

        /// <summary>
        /// A dummy method to simulate reading temperature from a connected
        /// sensor to this machine
        /// </summary>
        /// <returns>int</returns>
        private static int GetRoomTemperature()
        {
            int temp = (DateTime.Today.Second < 15) ? 25 : DateTime.Today.Second;
            return temp;
        }
    }
}
