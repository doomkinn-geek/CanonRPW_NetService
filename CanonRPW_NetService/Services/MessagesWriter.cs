using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CanonRPWService.Services
{
    public static class MessagesWriter
    {
        public static void SendMessage(string message, NetworkStream networkStream)
        {   
            if(networkStream == null)
            {
                throw new ArgumentNullException("Network stream is closed");
            }
            // turn the string message into a byte[] (encode)
            byte[] messageBytes = Encoding.ASCII.GetBytes(message); // a UTF-8 encoder would be 'better', as this is the standard for network communications
            // determine length of message
            int length = messageBytes.Length;
            // convert the length into bytes using BitConverter (encode)
            byte[] lengthBytes = System.BitConverter.GetBytes(length);
            // flip the bytes if we are a little-endian system: reverse the bytes in lengthBytes to do so
            if (System.BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }
            // send length
            networkStream.Write(lengthBytes, 0, lengthBytes.Length);
            // send message
            networkStream.Write(messageBytes, 0, length);
        }
    }
}
