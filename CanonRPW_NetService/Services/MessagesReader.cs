using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CanonRPWService.Services
{
    public static class MessagesReader
    {
        private static byte[] ReadBytes(int count, NetworkStream networkStream)
        {
            byte[] bytes = new byte[count]; // buffer to fill (and later return)
            int readCount = 0; // bytes is empty at the start
            // while the buffer is not full
            while (readCount < count)
            {
                // ask for no-more than the number of bytes left to fill our byte[]
                int left = count - readCount; // we will ask for `left` bytes
                int r = networkStream.Read(bytes, readCount, left); // but we are given `r` bytes (`r` <= `left`)

                if (r == 0)
                { // I lied, in the default configuration, a read of 0 can be taken to indicate a lost connection
                    throw new Exception("Lost Connection during read");
                }
                readCount += r; // advance by however many bytes we read
            }
            return bytes;
        }

        /// <summary> Reads the next message from the stream </summary>
        public static string ReadMessage(NetworkStream networkStream)
        {
            if (networkStream == null)
            {
                throw new ArgumentNullException("Network stream is closed");
            }
            // read length bytes, and flip if necessary
            byte[] lengthBytes = ReadBytes(sizeof(int), networkStream); // int is 4 bytes
            if (System.BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }
            // decode length
            int length = System.BitConverter.ToInt32(lengthBytes, 0);
            // read message bytes
            byte[] messageBytes = ReadBytes(length, networkStream);
            // decode the message
            string message = System.Text.Encoding.ASCII.GetString(messageBytes);
            return message;
        }
    }
}
