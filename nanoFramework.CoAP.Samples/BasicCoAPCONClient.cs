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
    public class BasicCoAPCONClient
    {      
        /// <summary>
        /// Holds an instance of the CoAP client
        /// </summary>
        private static CoAPClientChannel coapClient = null;
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
        private static void SetupClient()
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
        private static void SendRequest()
        {
            string urlToCall = "coap://localhost:5683/sensors/temp";
            UInt16 mId = coapClient.GetNextMessageID();//Using this method to get the next message id takes care of pending CON requests
            CoAPRequest tempReq = new CoAPRequest(CoAPMessageType.CON, CoAPMessageCode.GET, mId);
            tempReq.SetURL(urlToCall);

            /*Uncomment the two lines below to use non-default values for timeout and retransmission count*/
            /*Dafault value for timeout is 2 secs and retransmission count is 4*/
            //tempReq.Timeout = 10;
            //tempReq.RetransmissionCount = 5;

            coapClient.Send(tempReq);
        }
        /// <summary>
        /// We should receive the temperature from sever in the response
        /// </summary>
        /// <param name="coapResp">CoAPResponse</param>
        private static void OnCoAPResponseReceived(CoAPResponse coapResp)
        {
            if (coapResp.MessageType.Value == CoAPMessageType.ACK &&
                coapResp.Code.Value == CoAPMessageCode.CONTENT)
            {
                //We got the temperature..it will be in payload in JSON
                string payload = AbstractByteUtils.ByteToStringUTF8(coapResp.Payload.Value);
                Hashtable keyVal = JSONResult.FromJSON(payload);
                int temp = Convert.ToInt32(keyVal["temp"].ToString());
                //do something with the temperature now
            }
        }
        /// <summary>
        /// Not doing anything now
        /// </summary>
        /// <param name="coapReq"></param>
        private static void OnCoAPRequestReceived(CoAPRequest coapReq)
        {
            //nothing for now
        }
        /// <summary>
        /// Handle error
        /// </summary>
        /// <param name="e">The exception that occurred</param>
        /// <param name="associatedMsg">The associated message (if any)</param>
        private static void OnCoAPError(Exception e, AbstractCoAPMessage associatedMsg)
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
