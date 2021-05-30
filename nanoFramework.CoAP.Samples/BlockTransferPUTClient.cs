using nanoFramework.CoAP.Channels;
using nanoFramework.CoAP.Message;
using nanoFramework.Networking;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace nanoFramework.CoAP.Samples
{
    public static class BlockTransferPUTClient
    {
        /// <summary>
        /// What is our block size in terms of bytes
        /// </summary>
        private const int BLOCK_SIZE_BYTES = 16;
        /// <summary>
        /// We want to transfer 35 bytes, in 16 byte blocks..3 transfers
        /// </summary>
        private static byte[] _dataToTransfer = new byte[35];
        /// <summary>
        /// Holds the sequence number of the block
        /// </summary>
        private static UInt32 _blockSeqNo = 0;
        /// <summary>
        /// Holds the client channel instance
        /// </summary>
        private static CoAPClientChannel coapClient = null;
        /// <summary>
        /// Holds how many bytes got transferred to server
        /// </summary>
        private static int _totalBytesTransferred = 0;
        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main()
        {
            NetworkHelpers.SetupAndConnectNetwork();
            Debug.WriteLine("Waiting for network up and IP address...");
            NetworkHelpers.IpAddressAvailable.WaitOne();

            SetupClient();
            TransferToServerInBlocks(); //Initiate the first transfer

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
        /// Check if we have data to send
        /// </summary>
        /// <returns>bool</returns>
        private static bool HasDataToSend()
        {
            //In this example, we are only transferring on large data set...
            return (_totalBytesTransferred < _dataToTransfer.Length);
        }
        /// Transfer data to server in blocks
        /// </summary>
        /// <returns>true if there are more blocks to be transferred</returns>
        private static void TransferToServerInBlocks()
        {
            CoAPRequest blockPUTReq = new CoAPRequest(CoAPMessageType.CON, CoAPMessageCode.PUT, coapClient.GetNextMessageID());
            blockPUTReq.SetURL("coap://localhost:5683/largedata/blockput");
            blockPUTReq.AddTokenValue(DateTime.Today.ToString("HHmm"));
            //Get needed bytes from source            
            int copyBeginIdx = (int)(_blockSeqNo * BLOCK_SIZE_BYTES);
            int bytesToCopy = ((copyBeginIdx + BLOCK_SIZE_BYTES) < _dataToTransfer.Length) ? BLOCK_SIZE_BYTES : (_dataToTransfer.Length - copyBeginIdx);
            byte[] blockToSend = new byte[bytesToCopy];
            Array.Copy(_dataToTransfer, copyBeginIdx, blockToSend, 0, bytesToCopy);
            //Calculate how many more bytes left to transfer
            bool hasMore = (_totalBytesTransferred + bytesToCopy < _dataToTransfer.Length);
            //Add the bytes to the payload
            blockPUTReq.Payload = new CoAPPayload(blockToSend);
            //Now add block option to the request
            blockPUTReq.SetBlockOption(CoAPHeaderOption.BLOCK1, new CoAPBlockOption(_blockSeqNo, hasMore, CoAPBlockOption.BLOCK_SIZE_16));
            //send
            coapClient.Send(blockPUTReq);
            //Updated bytes transferred
            _totalBytesTransferred += bytesToCopy;
        }
        /// <summary>
        /// Once a response is received, check if that has block option set.
        /// If yes, server has responded back. Ensure you check the more flag.
        /// If that falg was set in the response, that means, server is still
        /// getting the data that you sent in the previous PUT request. So wait
        /// for a final ACK
        /// </summary>
        /// <param name="coapResp">CoAPResponse</param>
        private static void OnCoAPResponseReceived(CoAPResponse coapResp)
        {
            if (coapResp.MessageType.Value == CoAPMessageType.ACK)
            {
                CoAPBlockOption returnedBlockOption = coapResp.GetBlockOption(CoAPHeaderOption.BLOCK1);
                if (returnedBlockOption != null && !returnedBlockOption.HasMoreBlocks)
                {
                    //send the next block
                    _blockSeqNo++;
                    if (HasDataToSend()) TransferToServerInBlocks();
                }
            }
        }
        /// <summary>
        /// Not used in this sample
        /// </summary>
        /// <param name="coapReq"></param>
        private static void OnCoAPRequestReceived(CoAPRequest coapReq)
        {

        }
        /// <summary>
        /// Not used in this sample
        /// </summary>
        /// <param name="e"></param>
        /// <param name="associatedMsg"></param>
        private static void OnCoAPError(Exception e, AbstractCoAPMessage associatedMsg)
        {

        }
    }
}
