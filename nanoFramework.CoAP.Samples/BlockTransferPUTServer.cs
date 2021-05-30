using nanoFramework.CoAP.Channels;
using nanoFramework.CoAP.Message;
using nanoFramework.Networking;
using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace nanoFramework.CoAP.Samples
{
    public class BlockTransferPUTServer
    {
        /// <summary>
        /// The server channel
        /// </summary>
        private static CoAPServerChannel _server = null;
        /// <summary>
        /// Holds the received bytes along with the sequence number
        /// </summary>
        private static Hashtable _rxBytes = null;
        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main()
        {
            NetworkHelpers.SetupAndConnectNetwork();        
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
            _server = new CoAPServerChannel();
            _server.Initialize(null, 5683);

            _server.CoAPError += new CoAPErrorHandler(OnCoAPError);
            _server.CoAPRequestReceived += new CoAPRequestReceivedHandler(OnCoAPRequestReceived);
            _server.CoAPResponseReceived += new CoAPResponseReceivedHandler(OnCoAPResponseReceived);
        }
        /// <summary>
        /// Not using this for now
        /// </summary>
        /// <param name="coapResp">CoAPResponse</param>
        static void OnCoAPResponseReceived(CoAPResponse coapResp)
        {
        }
        /// <summary>
        /// This is where we will get the PUT request
        /// </summary>
        /// <param name="coapReq">CoAPRequest</param>
        static void OnCoAPRequestReceived(CoAPRequest coapReq)
        {
            string path = coapReq.GetPath();
            /*Error checking not done to simplify example*/
            if (coapReq.MessageType.Value == CoAPMessageType.CON &&
                coapReq.Code.Value == CoAPMessageCode.PUT &&
                path == "largedata/blockput")
            {
                if (_rxBytes == null) _rxBytes = new Hashtable();
                CoAPBlockOption rxBlockOption = coapReq.GetBlockOption(CoAPHeaderOption.BLOCK1);
                if (rxBlockOption != null)
                {
                    byte[] rxBytes = coapReq.Payload.Value;
                    if (_rxBytes.Contains(rxBlockOption.SequenceNumber))
                        _rxBytes[rxBlockOption.SequenceNumber] = rxBytes;//Update
                    else
                        _rxBytes.Add(rxBlockOption.SequenceNumber, rxBytes);//Add
                    //Now send an ACK
                    CoAPBlockOption ackBlockOption = new CoAPBlockOption(rxBlockOption.SequenceNumber,
                                                        false /*incidate to client that we have guzzled all the bytes*/,
                                                        rxBlockOption.SizeExponent);
                    CoAPResponse resp = new CoAPResponse(CoAPMessageType.ACK, CoAPMessageCode.CONTENT, coapReq.ID.Value);
                    resp.Token = coapReq.Token;
                    resp.RemoteSender = coapReq.RemoteSender;
                    resp.SetBlockOption(CoAPHeaderOption.BLOCK1, ackBlockOption);
                    _server.Send(resp);
                }
            }
        }
        /// <summary>
        /// Not using this for now
        /// </summary>
        /// <param name="e">The exception that occurred</param>
        /// <param name="associatedMsg">The associated message that caused the exception (if any)</param>
        static void OnCoAPError(Exception e, AbstractCoAPMessage associatedMsg)
        {
        }
        /// <summary>
        /// This method is called after all data is received
        /// </summary>
        private void ProcessReceivedData()
        {
            //do some processing here
        }
    }
}
