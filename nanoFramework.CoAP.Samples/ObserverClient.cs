using nanoFramework.CoAP.Channels;
using nanoFramework.CoAP.Exceptions;
using nanoFramework.CoAP.Helpers;
using nanoFramework.CoAP.Message;
using nanoFramework.Networking;
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace nanoFramework.CoAP.Samples
{
    public class ObserverClient
    {
        /// <summary>
        /// Holds an instance of the CoAP client
        /// </summary>
        private static CoAPClientChannel coapClient = null;
        /// <summary>
        /// Will keep a count of how many notifications we received so far
        /// </summary>
        private static int countOfNotifications = 0;
        /// <summary>
        /// The last observed sequence number
        /// </summary>
        private static int lastObsSeq = 0;
        /// <summary>
        /// Last time the observable notification was received.
        /// </summary>
        private static DateTime lastObsRx = DateTime.MinValue;
        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main()
        {
            NetworkHelpers.SetupAndConnectNetwork();
            Debug.WriteLine("Waiting for network up and IP address...");
            NetworkHelpers.IpAddressAvailable.WaitOne();

            SetupClient();            
            SendRequest();

            while (true)
            {
                Thread.Sleep(3000);
            }
        }
        /// <summary>
        /// Setup the client
        /// </summary>
        public static  void SetupClient()
        {
            coapClient = new CoAPClientChannel();
            coapClient.Initialize("localhost", 5683);
            coapClient.CoAPError += new CoAPErrorHandler(OnCoAPError);
            coapClient.CoAPRequestReceived += new CoAPRequestReceivedHandler(OnCoAPRequestReceived);
            coapClient.CoAPResponseReceived += new CoAPResponseReceivedHandler(OnCoAPResponseReceived);
        }
        /// <summary>
        /// Send the request to get the temperature
        /// </summary>
        public static void SendRequest()
        {
            string urlToCall = "coap://localhost:5683/sensors/temp/observe";
            UInt16 mId = coapClient.GetNextMessageID();//Using this method to get the next message id takes care of pending CON requests
            CoAPRequest tempReq = new CoAPRequest(CoAPMessageType.CON, CoAPMessageCode.GET, mId);
            tempReq.SetURL(urlToCall);
            //Important::Add the Observe option
            tempReq.AddOption(CoAPHeaderOption.OBSERVE, null);//Value of observe option has no meaning in request
            tempReq.AddTokenValue(DateTime.Today.ToString("HHmmss"));//must be <= 8 bytes
            /*Uncomment the two lines below to use non-default values for timeout and retransmission count*/
            /*Dafault value for timeout is 2 secs and retransmission count is 4*/
            //tempReq.Timeout = 10;
            //tempReq.RetransmissionCount = 5;

            coapClient.Send(tempReq);
        }
        /// <summary>
        /// We should receive an ACK to indicate we successfully registered with
        /// the server to observe the resource
        /// </summary>
        /// <param name="coapResp">CoAPResponse</param>
        static void OnCoAPResponseReceived(CoAPResponse coapResp)
        {
            if (coapResp.MessageType.Value == CoAPMessageType.ACK &&
                coapResp.Code.Value == CoAPMessageCode.EMPTY)
                Debug.WriteLine("Registered successfully to observe temperature");
            else
                Debug.WriteLine("Failed to register for temperature observation");
        }
        /// <summary>
        /// Going forward, we will receive temperature notifications from 
        /// server in a CON request
        /// </summary>
        /// <param name="coapReq">CoAPRequest</param>
        static void OnCoAPRequestReceived(CoAPRequest coapReq)
        {            
            if (coapReq.MessageType.Value == CoAPMessageType.CON)
            {
                //Extract the temperature..but first, check if the notification is fresh
                //The server sends a 4-digit sequence number
                int newObsSeq = AbstractByteUtils.ToUInt16(coapReq.Options.GetOption(CoAPHeaderOption.OBSERVE).Value);
                if ((lastObsSeq < newObsSeq && ((newObsSeq - lastObsSeq) < (System.Math.Pow(2.0, 23.0)))) ||
                    (lastObsSeq > newObsSeq && ((lastObsSeq - newObsSeq) > (System.Math.Pow(2.0, 23.0)))) ||
                    DateTime.Today > lastObsRx.AddSeconds(128))
                {
                    //The value received from server is new....read the new temperature
                    //We got the temperature..it will be in payload in JSON
                    string payload = AbstractByteUtils.ByteToStringUTF8(coapReq.Payload.Value);
                    Hashtable keyVal = JSONResult.FromJSON(payload);
                    int temp = Convert.ToInt32(keyVal["temp"].ToString());
                    //do something with the temperature now  
                    Debug.WriteLine(coapReq.ToString());
                }
                //update how many notifications received
                countOfNotifications++;
                if (countOfNotifications > 5)
                {
                    //We are no longer interested...send RST to de-register
                    CoAPResponse resp = new CoAPResponse(CoAPMessageType.RST, CoAPMessageCode.EMPTY, coapReq.ID.Value);
                    resp.RemoteSender = coapReq.RemoteSender;
                    resp.Token = coapReq.Token;//Do not forget this...this is how messages are correlated                    
                    coapClient.Send(resp);
                }
                else
                {
                    //we are still interested...send ACK
                    CoAPResponse resp = new CoAPResponse(CoAPMessageType.ACK, CoAPMessageCode.EMPTY, coapReq.ID.Value);
                    resp.RemoteSender = coapReq.RemoteSender;
                    resp.Token = coapReq.Token;//Do not forget this...this is how messages are correlated       
                    coapClient.Send(resp);
                }
                lastObsSeq = newObsSeq;
                lastObsRx = DateTime.Today;
            }
        }
        /// <summary>
        /// Handle error
        /// </summary>
        /// <param name="e">The exception that occurred</param>
        /// <param name="associatedMsg">The associated message (if any)</param>
        static void OnCoAPError(Exception e, AbstractCoAPMessage associatedMsg)
        {
            if (e.GetType() == typeof(UndeliveredException))
            {
                //Some CON message never got delivered...
                CoAPRequest undeliveredCONReq = (CoAPRequest)associatedMsg;
                //Now take action on this underlivered request
            }
        }
    }
}
