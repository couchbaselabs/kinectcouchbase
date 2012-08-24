﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using Fleck;
using Microsoft.Kinect;

namespace Kinect.Server
{
    class Program
    {
        static List<IWebSocketConnection> _sockets;
        static Skeleton[] skeletons;
        static KinectSensor m_kinect;

        static bool _initialized = false;

        static void Main(string[] args)
        {
            InitilizeKinect();
            InitializeSockets();
        }

        private static void InitializeSockets()
        {
            _sockets = new List<IWebSocketConnection>();

            var server = new WebSocketServer("ws://localhost:9999");

            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("Connected to " + socket.ConnectionInfo.ClientIpAddress);
                    _sockets.Add(socket);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("Disconnected from " + socket.ConnectionInfo.ClientIpAddress);
                    _sockets.Remove(socket);
                };
                socket.OnMessage = message =>
                {
                    Console.WriteLine(message);
                };
            });

            _initialized = true;

            Console.ReadLine();
        }

        private static void InitilizeKinect()
        {
            m_kinect = KinectSensor.KinectSensors[0];
            
            m_kinect.ColorStream.Enable();

            m_kinect.DepthStream.Enable();

            m_kinect.SkeletonStream.Enable();

            m_kinect.Start();

            m_kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
        }

        static private void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (!_initialized) return;
            List<Skeleton> users = new List<Skeleton>();

            if (e.OpenSkeletonFrame().SkeletonArrayLength == 0) return;
        
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null){
                    if(skeletons == null){
                       skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            //Add to users local array
            foreach(Skeleton s in skeletons)
            {
                users.Add(s);
            }  
        
            //Begin serializing
            if (users.Count > 0)
            {
                string json = users.Serialize();
                foreach (var socket in _sockets)
                {
                    socket.Send(json);
                }
            }
        }
    }
}