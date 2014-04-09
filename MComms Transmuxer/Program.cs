namespace MComms_Transmuxer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceProcess;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using MComms_Transmuxer.RTMP;

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
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
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new TransmuxerService() 
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
