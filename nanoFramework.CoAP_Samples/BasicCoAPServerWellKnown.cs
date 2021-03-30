using nanoFramework.CoAPSharp.Channels;
using nanoFramework.CoAPSharp.Helpers;
using nanoFramework.CoAPSharp.Message;
using nanoFramework.Networking;
using System;
using System.Diagnostics;
using System.Threading;

namespace CoAPSharpSamples
{
    public class BasicCoAPServerWellKnown
    {      
        /// <summary>
        /// Holds the server channel instance
        /// </summary>
        private static CoAPServerChannel _server = null;

        public static void Main()
        {
            NetworkHelpers.SetupAndConnectNetwork(false);
            Debug.WriteLine("Waiting for network up and IP address...");
            NetworkHelpers.IpAddressAvailable.WaitOne();


            //Debug.WriteLine("Waiting for valid Date & Time...");
            //NetworkHelpers.DateTimeAvailable.WaitOne();

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
            _server.CoAPResponseReceived += new CoAPResponseReceivedHandler(OnCoAPResponseReceived);
            _server.CoAPRequestReceived += new CoAPRequestReceivedHandler(OnCoAPRequestReceived);
            _server.CoAPError += new CoAPErrorHandler(OnCoAPError);
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
        /// Called when a CoAP request is received (NON, CON)
        /// </summary>
        /// <param name="coapReq">CoAPRequest object</param>
        private static void OnCoAPRequestReceived(CoAPRequest coapReq)
        {
            string reqPath = (coapReq.GetPath() != null) ? coapReq.GetPath().ToLower() : "";
            /*
                * For well-know path, we should support both CON and NON.
                * For NON request, we send back the details in another NON message
                * For CON request, we send back the details in an ACK
                */
            /*Well known should be a GET*/
            if (coapReq.Code.Value != CoAPMessageCode.GET)
            {
                if (coapReq.MessageType.Value == CoAPMessageType.CON)
                {
                    CoAPResponse resp = new CoAPResponse(CoAPMessageType.ACK,
                                                        CoAPMessageCode.METHOD_NOT_ALLOWED,
                                                        coapReq /*Copy all necessary values from request in the response*/);
                    //When you use the constructor that accepts a request, then automatically
                    //the message id , token and remote sender values are copied over to the response
                    _server.Send(resp);
                }
                else
                {
                    //For NON, we can only send back a RST
                    CoAPResponse resp = new CoAPResponse(CoAPMessageType.RST,
                                                        CoAPMessageCode.METHOD_NOT_ALLOWED,
                                                        coapReq /*Copy all necessary values from request in the response*/);
                    //When you use the constructor that accepts a request, then automatically
                    //the message id , token and remote sender values are copied over to the response
                    _server.Send(resp);
                }
            }
            else
            {
                //Message type is GET...check the path..this server only supports well-known path
                if (reqPath != ".well-known/core")
                {
                    if (coapReq.MessageType.Value == CoAPMessageType.CON)
                    {
                        CoAPResponse resp = new CoAPResponse(CoAPMessageType.ACK,
                                                            CoAPMessageCode.NOT_FOUND,
                                                            coapReq /*Copy all necessary values from request in the response*/);
                        _server.Send(resp);
                    }
                    else
                    {
                        //For NON, we can only send back a RST
                        CoAPResponse resp = new CoAPResponse(CoAPMessageType.RST,
                                                            CoAPMessageCode.NOT_FOUND,
                                                            coapReq /*Copy all necessary values from request in the response*/);
                        _server.Send(resp);
                    }
                }
                else
                {
                    //Request is GET and path is right
                    if (coapReq.MessageType.Value == CoAPMessageType.CON)
                    {
                        CoAPResponse resp = new CoAPResponse(CoAPMessageType.ACK,
                                                            CoAPMessageCode.CONTENT,
                                                            coapReq /*Copy all necessary values from request in the response*/);
                        //Add response payload
                        resp.AddPayload(GetSupportedResourceDescriptions());
                        //Tell recipient about the content-type of the response
                        resp.AddOption(CoAPHeaderOption.CONTENT_FORMAT, AbstractByteUtils.GetBytes(CoAPContentFormatOption.APPLICATION_LINK_FORMAT));
                        _server.Send(resp);
                    }
                    else
                    {
                        //Its a NON, send a NON back...in CoAPSharp, NON is always considered as request
                        CoAPResponse resp = new CoAPResponse(CoAPMessageType.NON,
                                                            CoAPMessageCode.CONTENT,
                                                            coapReq.ID.Value);
                        //Copy over other needed values from the reqeust
                        resp.Token = coapReq.Token;
                        resp.RemoteSender = coapReq.RemoteSender;
                        resp.AddPayload(GetSupportedResourceDescriptions());
                        //Tell recipient about the content-type of the response
                        resp.AddOption(CoAPHeaderOption.CONTENT_FORMAT, AbstractByteUtils.GetBytes(CoAPContentFormatOption.APPLICATION_LINK_FORMAT));
                        //send it
                        _server.Send(resp);
                    }
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
        /// Get the resource descriptions in CoRE link format.
        /// In this sample, we assume that this server has a temperature sensor
        /// and a pressure sensor. Calling each of the URLs will return the temperature
        /// and pressure in JSON format
        /// </summary>
        /// <returns>string</returns>
        private static string GetSupportedResourceDescriptions()
        {
            string resDesc = "<sensors/temp>;ct=" + CoAPContentFormatOption.APPLICATION_JSON +
                                ";title=Temperature Sensor"; //temperature sensor
            resDesc += ","; //A comma is used to separate each entry
            resDesc += "<sensors/pressure>;ct=" + CoAPContentFormatOption.APPLICATION_JSON +
                                ";title=Pressure Sensor"; //pressure sensor
            return resDesc;
        }

    }
}
