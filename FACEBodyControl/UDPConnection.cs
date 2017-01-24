using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace FACEBodyControl
{
    public class UDPConnection
    {
        private byte[] data;
        public byte[] Data
        {
            get { return data; }
        }

        private IPEndPoint localEndPoint;
        private EndPoint remoteEndPoint;
        private Socket socket;

        public UDPConnection()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            socket.Bind(localEndPoint);
            //listeningPort = ((IPEndPoint)(socket.LocalEndPoint)).Port; //The actual port it is listening on
        }

        public UDPConnection(int localPort)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            localEndPoint = new IPEndPoint(IPAddress.Any, localPort);            
            socket.Bind(localEndPoint);
        }

        public byte[] ListenOnUDPConnection(IPAddress remoteIp)
        {
            IPEndPoint sender = new IPEndPoint(remoteIp, 0);
            remoteEndPoint = (EndPoint)(sender);

            while (true)
            {
                data = new byte[1024];
                //socket.ReceiveFrom(data, ref remoteEndPoint);
                socket.BeginReceiveFrom(data, 0, data.Length, SocketFlags.None, ref remoteEndPoint,
                    new AsyncCallback(CheckForFailuresCallback), (object)this);
                return data;
            }
        }

        private bool serviceMissing = false;

        void CheckForFailuresCallback(IAsyncResult result) 
        { 
            EndPoint remoteEndPoint = new IPEndPoint(0, 0); 
            try 
            { 
                int bytesRead = socket.EndReceiveFrom(result, ref remoteEndPoint); 
            } 
            catch (SocketException e) 
            { 
                if (e.ErrorCode == 10054) 
                    serviceMissing = true; 
            } 
        }


        public void DisconnectUDPConnection()
        {
            socket.Disconnect(true);
        }

        public T DeserializeMsg<T>(Byte[] data) where T : struct
        {
            int objsize = Marshal.SizeOf(typeof(T));
            IntPtr buff = Marshal.AllocHGlobal(objsize);

            Marshal.Copy(data, 0, buff, objsize);

            T retStruct = (T)Marshal.PtrToStructure(buff, typeof(T));
            Marshal.FreeHGlobal(buff);

            return retStruct;
        }

        public byte[] SerializeMsg<T>(T msg) where T : struct
        {
            int objsize = Marshal.SizeOf(typeof(T));
            byte[] ret = new Byte[objsize];

            IntPtr buff = Marshal.AllocHGlobal(objsize);
            Marshal.StructureToPtr(msg, buff, true);
            Marshal.Copy(buff, ret, 0, objsize);
            Marshal.FreeHGlobal(buff);

            return ret;
        }

        public byte[] GetBytes<TStruct>(TStruct data) where TStruct : struct
        {
            int structSize = Marshal.SizeOf(typeof(TStruct));
            byte[] buffer = new byte[structSize];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Marshal.StructureToPtr(data, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return buffer;
        }

        //IPAddress localIP = GetInterfaceAddress();
        internal static IPAddress GetInterfaceAddress()
        {
            IPAddress[] addresses = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
            foreach (IPAddress a in addresses)
            {
                if (a.AddressFamily == AddressFamily.InterNetwork)
                    return a;
            }
            return null;
        }
    }
}

