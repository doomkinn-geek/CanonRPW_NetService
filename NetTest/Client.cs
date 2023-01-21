using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetTest
{
    public class Client : IDisposable
    {
        private TcpClient tcpClient { get; }

        public Client(TcpClient _tcpClient)
        {
            tcpClient = _tcpClient;
        }

        public void Close()
        {
            tcpClient.Close();
        }

        /// <summary> Sends a length-prepended (Pascal) string over the network </summary>
        public void SendMessage(string message)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            // we won't use a binary writer, because the endianness is unhelpful

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

        /// <summary> Reads a number of bytes from the stream </summary>
        private byte[] ReadBytes(int count)
        {
            NetworkStream networkStream = tcpClient.GetStream();

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
        public string ReadMessage()
        {
            // read length bytes, and flip if necessary
            byte[] lengthBytes = ReadBytes(sizeof(int)); // int is 4 bytes
            if (System.BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            // decode length
            int length = System.BitConverter.ToInt32(lengthBytes, 0);

            // read message bytes
            byte[] messageBytes = ReadBytes(length);

            // decode the message
            string message = System.Text.Encoding.ASCII.GetString(messageBytes);

            return message;
        }

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                tcpClient.Dispose();
            }

            disposed = true;
        }
    }
}
