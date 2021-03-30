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
    public class ObservableServer
    {
        private const string OBSERVED_RESOURCE_URI = "coap://127.0.0.1:5683/sensors/temp/observe";

        /// <summary>
        /// Holds the server channel instance
        /// </summary>
        private static CoAPServerChannel coapServer = null;

        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main()
        {
            NetworkHelpers.SetupAndConnectNetwork(false);
            Debug.WriteLine("Waiting for network up and IP address...");
            NetworkHelpers.IpAddressAvailable.WaitOne();

            StartServer();

            int lastMeasuredTemp = 0;
            /*
                * Read the temperature every 10 seconds and notify all listeners
                * if there is a change
                */
            while (true)
            {
                int temp = GetRoomTemperature();
                if (temp != lastMeasuredTemp)
                {
                    NotifyListeners(temp);
                    lastMeasuredTemp = temp;
                }

                Thread.Sleep(30000);
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
            //Add all the resources that this server allows observing
            coapServer.ObserversList.AddObservableResource(OBSERVED_RESOURCE_URI);
        }
        /// <summary>
        /// Called when an error occurs.Not used in this sample
        /// </summary>
        /// <param name="e">The exception</param>
        /// <param name="associatedMsg">Associated message if any, else null</param>
        static void OnCoAPError(Exception e, AbstractCoAPMessage associatedMsg)
        {
            //put error handling code here
        }
        /// <summary>
        /// Called when a CoAP request is received...we will only support CON requests 
        /// of type GET... the path is sensors/temp
        /// </summary>
        /// <param name="coapReq">CoAPRequest object</param>
        static void OnCoAPRequestReceived(CoAPRequest coapReq)
        {
            string reqPath = (coapReq.GetPath() != null) ? coapReq.GetPath().ToLower() : "";
            /*We have skipped error handling in code below to just focus on observe option*/
            if (coapReq.MessageType.Value == CoAPMessageType.CON && coapReq.Code.Value == CoAPMessageCode.GET)
            {
                if (!coapReq.IsObservable() /*Does the request have "Observe" option*/)
                {
                    /*Request is not to observe...we do not support anything no-observable*/
                    CoAPResponse resp = new CoAPResponse(CoAPMessageType.ACK,
                                                            CoAPMessageCode.NOT_IMPLEMENTED,
                                                            coapReq /*Copy all necessary values from request in the response*/);
                    coapServer.Send(resp);
                }
                else if (!coapServer.ObserversList.IsResourceBeingObserved(coapReq.GetURL()) /*do we support observation on this path*/)
                {
                    //Observation is not supported on this path..just to tell you how to check
                    CoAPResponse resp = new CoAPResponse(CoAPMessageType.ACK,
                                                            CoAPMessageCode.NOT_FOUND,
                                                            coapReq /*Copy all necessary values from request in the response*/);
                    coapServer.Send(resp);
                }
                else
                {
                    //This is a request to observe this resource...register this client
                    coapServer.ObserversList.AddResourceObserver(coapReq);

                    /*Request contains observe option and path is correct*/
                    CoAPResponse resp = new CoAPResponse(CoAPMessageType.ACK,
                                                        CoAPMessageCode.EMPTY,
                                                        coapReq /*Copy all necessary values from request in the response*/);

                    //send it..tell client we registered it's request to observe
                    coapServer.Send(resp);
                }
            }
        }
        /// <summary>
        /// Called when CoAP response is received (ACK, RST). Not used in this sample
        /// </summary>
        /// <param name="coapResp">CoAPResponse</param>
        static void OnCoAPResponseReceived(CoAPResponse coapResp)
        {
            //If we receive a RST, then remve that client from notifications
            if (coapResp.MessageType.Value == CoAPMessageType.RST)
            {
                coapServer.ObserversList.RemoveResourceObserver(coapResp);
            }
        }
        /// <summary>
        /// Notify listeners of the new temperature
        /// </summary>
        /// <param name="temp">The temperature</param>
        static private void NotifyListeners(int temp)
        {
            ArrayList resObservers = coapServer.ObserversList.GetResourceObservers(OBSERVED_RESOURCE_URI);
            if (resObservers == null || resObservers.Count == 0) return;

            //The next observe sequence number
            UInt16 obsSeq = (UInt16)Convert.ToInt16(DateTime.Today.ToString("mmss"));//Will get accomodated in 24-bits limit and will give good sequence

            foreach (CoAPRequest obsReq in resObservers)
            {
                UInt16 mId = coapServer.GetNextMessageID();
                CoAPRequest notifyReq = new CoAPRequest(CoAPMessageType.CON, CoAPMessageCode.PUT, mId);
                notifyReq.RemoteSender = obsReq.RemoteSender;
                notifyReq.Token = obsReq.Token;
                //Add observe option with sequence number
                notifyReq.AddOption(CoAPHeaderOption.OBSERVE, AbstractByteUtils.GetBytes(obsSeq));

                //The payload will be JSON
                Hashtable ht = new Hashtable();
                ht.Add("temp", temp);
                string jsonStr = JSONResult.ToJSON(ht);
                notifyReq.AddPayload(jsonStr);
                //Tell recipient about the content-type of the response
                notifyReq.AddOption(CoAPHeaderOption.CONTENT_FORMAT, AbstractByteUtils.GetBytes(CoAPContentFormatOption.APPLICATION_JSON));
                //send it
                coapServer.Send(notifyReq);
            }
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
