namespace MComms_Transmuxer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceProcess;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;
    using MComms_Transmuxer.RTMP;

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Global.Log.Info("Starting MComms Transmuxer...");

            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, Global.RtmpMaxConnections * 100);
            Global.MediaAllocator = new PacketBufferAllocator(Global.OneMediaBufferSize, Global.RtmpMaxConnections);
            Global.SegmentAllocator = new PacketBufferAllocator(Global.SegmentBufferSize, Global.RtmpMaxConnections / 50);

            if (System.Environment.UserInteractive)
            {
                if (args.Length > 0)
                {
                    switch (args[0].ToLower())
                    {
                        case "-standalone":
                            {
                                RtmpServer server = new RtmpServer();
                                server.Start();

                                Global.Log.Info("MComms Transmuxer started in UI mode");

                                while (true)
                                {
                                    Thread.Sleep(1);
                                }

                                server.Stop();
                                break;
                            }
                    }
                }
            }
            else
            {
                Global.Log.Debug("MComms Transmuxer started in service mode");
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new TransmuxerService() 
                };
                ServiceBase.Run(ServicesToRun);
            }

            Global.Log.Info("MComms Transmuxer stopped");
        }
    }
}
