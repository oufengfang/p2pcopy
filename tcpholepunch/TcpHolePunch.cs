﻿using System;
using System.Net;
using System.Net.Sockets;

namespace p2pcopy.tcpholepunch
{
    static class TcpHolePunch
    {
        static internal Socket PeerConnect(
            string remoteAddr,
            int remotePort,
            int localPort)
        {
            Connector connector = new Connector();
            connector.Run(remoteAddr, remotePort, localPort);

            Acceptor acceptor = new Acceptor();
            acceptor.Run(localPort);

            while (true)
            {
                System.Threading.Thread.Sleep(100);

                if (acceptor.Accepted())
                {
                    Console.WriteLine("Acceptor correctly accepted a connection");
                    return acceptor.GetSocket();
                }

                if (connector.Connected())
                {
                    Console.WriteLine("Connector correctly connected");
                    return connector.GetSocket();
                }

                if (connector.Retry())
                {
                    Console.WriteLine("Retrying connection");
                    connector = new Connector();
                    connector.Run(remoteAddr, remotePort, localPort);
                }
            }
        }

        class Connector
        {
            internal void Run(string remoteAddr, int port, int localPort)
            {
                mSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                mSocket.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress,
                    true);

                mSocket.Bind(new IPEndPoint(IPAddress.Any, localPort));

                mSocket.BeginConnect(remoteAddr, port,
                    (IAsyncResult ar) =>
                    {
                        Socket s = ar.AsyncState as Socket;

                        if (s.Connected)
                        {
                            Console.WriteLine("The socket is connected! {0}", s.RemoteEndPoint.ToString());
                        }
                        else
                        {
                            Console.WriteLine("Connect didn't work!");

                            s.Close();
                            lock (mLock)
                            {
                                mbRetry = true;
                            }
                            return;
                        }

                        (ar.AsyncState as Socket).EndConnect(ar);
                        lock (mLock)
                        {
                            Console.WriteLine("Connection happened!");
                            mbConnected = true;
                        }
                    },
                    mSocket);
            }

            internal bool Connected()
            {
                lock (mLock)
                {
                    return mbConnected;
                }
            }

            internal bool Retry()
            {
                lock (mLock)
                {
                    return mbRetry;
                }
            }

            internal Socket GetSocket()
            {
                lock (mLock)
                {
                    return mSocket;
                }
            }

            Socket mSocket;
            bool mbConnected = false;
            bool mbRetry = false;
            object mLock = new object();
        }

        class Acceptor
        {
            internal void Run(int localPort)
            {
                Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);

                sock.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress,
                    true);

                sock.Bind(new IPEndPoint(IPAddress.Any, localPort));

                sock.Listen(1);

                sock.BeginAccept(
                    (IAsyncResult ar) =>
                    {
                        Console.WriteLine("Connection accepted!");
                        lock (mLock)
                        {
                            mSocket = (Socket)ar.AsyncState;
                            mbAccepted = true;
                        }
                    },
                    null);
            }

            internal bool Accepted()
            {
                lock (mLock)
                {
                    return mbAccepted;
                }
            }

            internal Socket GetSocket()
            {
                lock(mLock)
                {
                    return mSocket;
                }
            }

            Socket mSocket;
            bool mbAccepted = false;
            object mLock = new object();
        }
    }
}