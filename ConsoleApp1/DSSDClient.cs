using CanonRPWService.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CanonRPWService
{
    public class DSSDClient
    {
        private TcpClient _client;
        private StreamReader _sReader;
        private StreamWriter _sWriter;

        private Boolean _isConnected;

        public DSSDClient(String ipAddress, int portNum)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(ipAddress, portNum);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            HandleCommunication();
        }

        public void HandleCommunication()
        {
            _sReader = new StreamReader(_client.GetStream(), Encoding.ASCII);
            _sWriter = new StreamWriter(_client.GetStream(), Encoding.ASCII);

            _isConnected = true;
            String sData = null;
            while (_isConnected)
            {
                Console.Write("> ");
                sData = Console.ReadLine();
                //sData = "test string \nwith special symbols \ncouple of special symbols \r\r\r\n\n\n";

                // write data and make sure to flush, or the buffer will continue to 
                // grow, and your data might not be sent when you want it, and will
                // only be sent once the buffer is filled.
                //_sWriter.WriteLine(sData);
                //_sWriter.Flush();

                MessagesWriter.SendMessage(sData, _client.GetStream());

                Thread.Sleep(50);
                if(_client.Available > 0)
                {
                    //sData = _sReader.ReadLine();
                    sData = MessagesReader.ReadMessage(_client.GetStream());
                    Console.WriteLine(sData);
                }

                // if you want to receive anything
                // String sDataIncomming = _sReader.ReadLine();
            }
        }
    }
}
