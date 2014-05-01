namespace MComms_Transmuxer.SmoothStreaming
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    public class SmoothStreamingPublisher
    {
        private string publishUri = null;
        private Dictionary<int, HttpWebRequest> webRequests = new Dictionary<int, HttpWebRequest>();
        private Dictionary<int, Stream> webRequestStreams = new Dictionary<int, Stream>();

        public SmoothStreamingPublisher(string publishUri)
        {
            // TODO: create publishing point using REST API for IIS Media Services?
            this.publishUri = publishUri;
        }

        public void PushData(int streamId, byte[] buffer, int offset, int length)
        {
            // 3 retries
            for (int i = 0; i < 3; ++i)
            {
                if (!this.webRequests.ContainsKey(streamId))
                {
                    this.CreateWebRequest(streamId);
                }

                try
                {
                    this.webRequestStreams[streamId].Write(buffer, offset, length);
                    this.webRequestStreams[streamId].Flush();
                }
                catch (Exception)
                {
                    try
                    {
                        this.webRequestStreams[streamId].Close();
                    }
                    catch
                    {
                    }

                    this.webRequestStreams.Remove(streamId);
                    this.webRequests.Remove(streamId);
                }
            }
        }

        private void CreateWebRequest(int streamId)
        {
            string streamPublishUri = string.Format("{0}/Streams({1})", this.publishUri, streamId);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(streamPublishUri);
            webRequest.Method = "POST";
            webRequest.SendChunked = true;
            webRequest.KeepAlive = true;
            this.webRequests.Add(streamId, webRequest);
            this.webRequestStreams.Add(streamId, webRequest.GetRequestStream());

            Global.Log.InfoFormat("Created web request {0}", streamPublishUri);
        }
    }
}
