namespace MComms_Transmuxer
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.ServiceProcess;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;
    using MComms_Transmuxer.RTMP;

    public partial class TransmuxerService : ServiceBase
    {
        RtmpServer server = null;

        public TransmuxerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Global.Allocator = new PacketBufferAllocator(10240, 10240);
            server = new RtmpServer();
            server.Start();
            Global.Log.Info("MComms Transmuxer started in service mode");
        }

        protected override void OnStop()
        {
            server.Stop();
        }
    }
}
