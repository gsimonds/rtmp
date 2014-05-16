namespace MComms_Transmuxer.SmoothStreaming
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    using MComms_Transmuxer.Common;
    using MComms_Transmuxer.RTMP;

    public class SmoothStreamingPublisher : IDisposable
    {
        private static Dictionary<string, SmoothStreamingPublisher> publishers = new Dictionary<string, SmoothStreamingPublisher>();

        private string publishUri = null;
        private int muxId = -1;
        private Dictionary<MediaType, Guid> streams = new Dictionary<MediaType, Guid>();
        private Dictionary<Guid, MediaType> streamsRev = new Dictionary<Guid, MediaType>();
        private Dictionary<Guid, int> publishStreamId2MuxerStreamId = new Dictionary<Guid, int>();
        private Dictionary<Guid, DateTime> publishStreamId2LastActivity = new Dictionary<Guid, DateTime>();
        private bool mediaDataStarted = false;
        private DateTime lastPacketAbsoluteTime = DateTime.MinValue;
        private long lastPacketTimestamp = long.MinValue;
        private Dictionary<Guid, HttpWebRequest> webRequests = new Dictionary<Guid, HttpWebRequest>();
        private Dictionary<Guid, Stream> webRequestStreams = new Dictionary<Guid, Stream>();
        private DateTime lastExpiredStreamsChecked = DateTime.MinValue;
        private DateTime lastActivity = DateTime.Now;

        private SmoothStreamingPublisher(string publishUri)
        {
            this.publishUri = publishUri;
            this.muxId = SmoothStreamingSegmenter.MCSSF_Initialize();
            // always restart publishing point if we have new publisher instance,
            // i.e. it is either new publishing point or continuation from another server
            // which can't be continued smoothly
            this.ShutdownPublishingPoint();
            this.StartPublishingPoint();
        }

        public string PublishUri
        {
            get
            {
                return this.publishUri;
            }
        }

        #region IDisposable

        /// <summary>
        /// Releases resources.
        /// </summary>
        public void Dispose()
        {
            foreach (Stream webRequestStream in this.webRequestStreams.Values)
            {
                try
                {
                    webRequestStream.Close();
                    webRequestStream.Dispose();
                }
                catch
                {
                }
            }

            this.webRequestStreams.Clear();
            this.webRequests.Clear();

            if (this.muxId >= 0)
            {
                SmoothStreamingSegmenter.MCSSF_Uninitialize(this.muxId);
                this.muxId = -1;
            }
        }

        #endregion

        public static SmoothStreamingPublisher Create(string publishUri)
        {
            lock (SmoothStreamingPublisher.publishers)
            {
                if (SmoothStreamingPublisher.publishers.ContainsKey(publishUri))
                {
                    return SmoothStreamingPublisher.publishers[publishUri];
                }
                else
                {
                    SmoothStreamingPublisher publisher = new SmoothStreamingPublisher(publishUri);
                    SmoothStreamingPublisher.publishers.Add(publishUri, publisher);
                    return publisher;
                }
            }
        }

        public static void DeleteExpired()
        {
            lock (SmoothStreamingPublisher.publishers)
            {
                bool interrupted = false;
                do
                {
                    interrupted = false;
                    foreach (KeyValuePair<string, SmoothStreamingPublisher> pair in SmoothStreamingPublisher.publishers)
                    {
                        if ((DateTime.Now - pair.Value.lastActivity).TotalMilliseconds < 60000)
                        {
                            continue;
                        }

                        pair.Value.Dispose();
                        SmoothStreamingPublisher.publishers.Remove(pair.Key);
                        interrupted = true;
                        break; // because collection is modified
                    }
                }
                while (interrupted);
            }
        }

        public static void DeleteAll()
        {
            lock (SmoothStreamingPublisher.publishers)
            {
                while (SmoothStreamingPublisher.publishers.Count > 0)
                {
                    KeyValuePair<string, SmoothStreamingPublisher> keyValue = SmoothStreamingPublisher.publishers.First();
                    keyValue.Value.Dispose();
                    SmoothStreamingPublisher.publishers.Remove(keyValue.Key);
                }
            }
        }

        public Guid RegisterMediaType(MediaType mediaType)
        {
            lock (this)
            {
                this.lastActivity = DateTime.Now;

                MediaType existingType = null;
                try
                {
                    existingType = this.streams.Keys.First
                    (
                        e =>
                            e.ContentType == mediaType.ContentType &&
                            e.Codec == mediaType.Codec &&
                            e.Bitrate == mediaType.Bitrate &&
                            e.IsPrivateDataEqual(mediaType.PrivateData) &&
                            e.Width == mediaType.Width &&
                            e.Height == mediaType.Height &&
                            e.SampleRate == mediaType.SampleRate &&
                            e.Channels == mediaType.Channels
                    );
                }
                catch (InvalidOperationException)
                {
                    existingType = null;
                }

                if (existingType == null)
                {
                    Guid guid = Guid.NewGuid();
                    this.streams.Add(mediaType, guid);
                    this.streamsRev.Add(guid, mediaType);
                    this.publishStreamId2LastActivity.Add(guid, DateTime.Now);
                    existingType = mediaType;

                    Global.Log.DebugFormat("New media type {0} registered: {1} {2} bps", guid, mediaType.Codec, mediaType.Bitrate);

                    if (this.mediaDataStarted)
                    {
                        // need to re-create muxer, re-initialize streams and restart publishing point
                        Global.Log.DebugFormat("Need to re-initialize publishing point {0}", this.publishUri);

                        // restart publishing point
                        this.ShutdownPublishingPoint();
                        this.StartPublishingPoint();

                        this.mediaDataStarted = false;
                        this.lastPacketAbsoluteTime = DateTime.MinValue;
                        this.lastPacketTimestamp = long.MinValue;
                        this.publishStreamId2MuxerStreamId.Clear();

                        if (this.muxId >= 0)
                        {
                            SmoothStreamingSegmenter.MCSSF_Uninitialize(this.muxId);
                        }

                        this.muxId = SmoothStreamingSegmenter.MCSSF_Initialize();
                    }
                }

                return this.streams[existingType];
            }
        }

        public int AddStream(Guid streamId, Int32 streamType, Int32 bitrate, UInt16 language, Int32 extraDataSize, IntPtr extraData)
        {
            lock (this)
            {
                this.lastActivity = DateTime.Now;
                int muxStreamId = -1;

                if (this.publishStreamId2MuxerStreamId.ContainsKey(streamId))
                {
                    muxStreamId = this.publishStreamId2MuxerStreamId[streamId];
                    this.publishStreamId2LastActivity[streamId] = DateTime.Now;
                }
                else
                {
                    muxStreamId = SmoothStreamingSegmenter.MCSSF_AddStream(this.muxId, streamType, bitrate, language, extraDataSize, extraData);

                    if (muxStreamId < 0)
                    {
                        Global.Log.ErrorFormat("Stream {0} adding to muxer failed", streamId);
                        UnregisterStream(streamId);
                    }
                    else
                    {
                        Global.Log.DebugFormat("Stream {0} added to muxer successfully", streamId);
                        this.publishStreamId2MuxerStreamId.Add(streamId, muxStreamId);
                    }
                }

                return muxStreamId;
            }
        }

        public int GetMuxId(Guid streamId)
        {
            lock (this)
            {
                if (!this.publishStreamId2MuxerStreamId.ContainsKey(streamId))
                {
                    Global.Log.DebugFormat("Need to re-register stream {0}", streamId);
                    return -1;
                }
                else
                {
                    return this.muxId;
                }
            }
        }

        public MediaType GetMediaType(Guid streamId)
        {
            lock (this)
            {
                if (this.streamsRev.ContainsKey(streamId))
                {
                    return this.streamsRev[streamId];
                }
                else
                {
                    return null;
                }
            }
        }

        public void GetSynchronizationInfo(out DateTime lastAbsoluteTime, out long lastTimestamp)
        {
            lock (this)
            {
                lastAbsoluteTime = this.lastPacketAbsoluteTime;
                lastTimestamp = this.lastPacketTimestamp;
            }
        }

        public void PushData(Guid streamId, DateTime absoluteTime, long timestamp, byte[] buffer, int offset, int length)
        {
            this.lastActivity = DateTime.Now;

            // 3 retries
            for (int i = 0; i < 3; ++i)
            {
                Stream webRequestStream = null;

                lock (this)
                {
                    this.publishStreamId2LastActivity[streamId] = DateTime.Now;
                    this.UnregisterExpiredStreams();

                    if (this.publishStreamId2MuxerStreamId.Count < this.streams.Count)
                    {
                        // we haven't added all streams yet, dropping media data
                        Global.Log.DebugFormat("Dropping media data of stream {0} because header is not written yet", streamId);
                        return;
                    }
                    else
                    {
                        if (!this.mediaDataStarted)
                        {
                            PushHeader();
                        }
                    }

                    if (absoluteTime != DateTime.MinValue)
                    {
                        this.lastPacketAbsoluteTime = absoluteTime;
                    }

                    if (timestamp != long.MinValue)
                    {
                        this.lastPacketTimestamp = timestamp;
                    }

                    if (!this.webRequests.ContainsKey(streamId))
                    {
                        this.CreateWebRequest(streamId);
                    }

                    webRequestStream = this.webRequestStreams[streamId];
                }

                try
                {
                    webRequestStream.Write(buffer, offset, length);
                    webRequestStream.Flush();
                }
                catch (Exception)
                {
                    try
                    {
                        webRequestStream.Close();
                    }
                    catch
                    {
                    }

                    lock (this)
                    {
                        this.webRequestStreams.Remove(streamId);
                        this.webRequests.Remove(streamId);
                    }
                }
            }
        }

        public void CompareHeader()
        {
            lock (this)
            {
                // make sure publishing point is started
                this.StartPublishingPoint();

                // compare publishing point streams with our streams
                if (!this.IsPublishingPointManifestMatching())
                {
                    // we need to restart publishing point with new header
                    this.mediaDataStarted = false;
                    this.lastPacketAbsoluteTime = DateTime.MinValue;
                    this.lastPacketTimestamp = long.MinValue;
                    this.ShutdownPublishingPoint();
                    this.StartPublishingPoint();
                }
            }
        }

        private void CreateWebRequest(Guid streamId)
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

        private void StartPublishingPoint()
        {
            Stream reqStream = null;
            HttpWebResponse webResp = null;

            try
            {
                string streamPublishUri = string.Format("{0}/state", this.publishUri);

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(streamPublishUri);
                webRequest.Credentials = new NetworkCredential(Properties.Settings.Default.IisMediaRestApiUserId, Properties.Settings.Default.IisMediaRestApiUserPwd);
                webRequest.Method = "GET";
                webResp = (HttpWebResponse)webRequest.GetResponse();

                string sXmlState;
                using (StreamReader sr = new StreamReader(webResp.GetResponseStream()))
                {
                    sXmlState = sr.ReadToEnd();
                }

                webResp.Close();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(sXmlState);

                XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("atom", "http://www.w3.org/2005/Atom");
                ns.AddNamespace("ssm", "http://schemas.microsoft.com/iis/media/2011/03/streaming/management");

                string state = "Idle";
                XmlNode nodeState = doc.SelectSingleNode("//ssm:SmoothStreaming/ssm:State/ssm:Value", ns);
                if (nodeState != null)
                {
                    state = nodeState.InnerXml;
                }

                if (state != "Starting" && state != "Started")
                {
                    if (state != "Idle")
                    {
                        // need to shutdown publishing point first
                        nodeState.InnerXml = "Idle";

                        webRequest = (HttpWebRequest)WebRequest.Create(streamPublishUri);
                        webRequest.Credentials = new NetworkCredential(Properties.Settings.Default.IisMediaRestApiUserId, Properties.Settings.Default.IisMediaRestApiUserPwd);
                        webRequest.Method = "PUT";
                        webRequest.ContentType = "application/atom+xml";
                        reqStream = webRequest.GetRequestStream();
                        doc.Save(reqStream);
                        webResp = (HttpWebResponse)webRequest.GetResponse();

                        using (StreamReader sr = new StreamReader(webResp.GetResponseStream()))
                        {
                            string sResp = sr.ReadToEnd();
                        }

                        reqStream.Close();
                        webResp.Close();
                    }

                    // start publishing point
                    nodeState.InnerXml = "Started";

                    webRequest = (HttpWebRequest)WebRequest.Create(streamPublishUri);
                    webRequest.Credentials = new NetworkCredential(Properties.Settings.Default.IisMediaRestApiUserId, Properties.Settings.Default.IisMediaRestApiUserPwd);
                    webRequest.Method = "PUT";
                    webRequest.ContentType = "application/atom+xml";
                    reqStream = webRequest.GetRequestStream();
                    doc.Save(reqStream);
                    webResp = (HttpWebResponse)webRequest.GetResponse();

                    using (StreamReader sr = new StreamReader(webResp.GetResponseStream()))
                    {
                        string sResp = sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Log.InfoFormat("Failed to start publishing point {0}: {1}", this.publishUri, ex.ToString());
            }

            if (reqStream != null)
            {
                try
                {
                    reqStream.Close();
                }
                catch
                {
                }
            }

            if (webResp != null)
            {
                try
                {
                    webResp.Close();
                }
                catch
                {
                }
            }
        }

        private void ShutdownPublishingPoint()
        {
            Stream reqStream = null;
            HttpWebResponse webResp = null;

            try
            {
                string streamPublishUri = string.Format("{0}/state", this.publishUri);

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(streamPublishUri);
                webRequest.Credentials = new NetworkCredential(Properties.Settings.Default.IisMediaRestApiUserId, Properties.Settings.Default.IisMediaRestApiUserPwd);
                webRequest.Method = "GET";
                webResp = (HttpWebResponse)webRequest.GetResponse();

                string sXmlState;
                using (StreamReader sr = new StreamReader(webResp.GetResponseStream()))
                {
                    sXmlState = sr.ReadToEnd();
                }

                webResp.Close();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(sXmlState);

                XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("atom", "http://www.w3.org/2005/Atom");
                ns.AddNamespace("ssm", "http://schemas.microsoft.com/iis/media/2011/03/streaming/management");

                string state = "Idle";
                XmlNode nodeState = doc.SelectSingleNode("//ssm:SmoothStreaming/ssm:State/ssm:Value", ns);
                if (nodeState != null)
                {
                    state = nodeState.InnerXml;
                }

                if (state != "Idle")
                {
                    nodeState.InnerXml = "Idle";

                    webRequest = (HttpWebRequest)WebRequest.Create(streamPublishUri);
                    webRequest.Credentials = new NetworkCredential(Properties.Settings.Default.IisMediaRestApiUserId, Properties.Settings.Default.IisMediaRestApiUserPwd);
                    webRequest.Method = "PUT";
                    webRequest.ContentType = "application/atom+xml";
                    reqStream = webRequest.GetRequestStream();
                    doc.Save(reqStream);
                    webResp = (HttpWebResponse)webRequest.GetResponse();

                    using (StreamReader sr = new StreamReader(webResp.GetResponseStream()))
                    {
                        string sResp = sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Log.InfoFormat("Failed to shutdown publishing point {0}: {1}", this.publishUri, ex.ToString());
            }

            if (reqStream != null)
            {
                try
                {
                    reqStream.Close();
                }
                catch
                {
                }
            }

            if (webResp != null)
            {
                try
                {
                    webResp.Close();
                }
                catch
                {
                }
            }
        }

        private bool IsPublishingPointManifestMatching()
        {
            bool matching = false;
            Stream reqStream = null;
            HttpWebResponse webResp = null;

            try
            {
                string streamPublishUri = string.Format("{0}/Manifest", this.publishUri);

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(streamPublishUri);
                webRequest.Method = "GET";
                webResp = (HttpWebResponse)webRequest.GetResponse();

                string sXmlState;
                using (StreamReader sr = new StreamReader(webResp.GetResponseStream()))
                {
                    sXmlState = sr.ReadToEnd();
                }

                webResp.Close();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(sXmlState);

                matching = true;
                List<Guid> recognizedStreams = new List<Guid>();

                XmlNodeList videoNodes = doc.SelectNodes("//StreamIndex[@Type='video']/QualityLevel");
                foreach (XmlNode node in videoNodes)
                {
                    int bitrate = 0;
                    string privateData = null;
                    string fourCC = null;
                    int width = 0;
                    int height = 0;

                    try
                    {
                        bitrate = int.Parse(node.Attributes["Bitrate"].Value);
                        privateData = node.Attributes["CodecPrivateData"].Value.ToLower();
                        fourCC = node.Attributes["FourCC"].Value.ToLower();
                        width = int.Parse(node.Attributes["MaxWidth"].Value);
                        height = int.Parse(node.Attributes["MaxHeight"].Value);
                    }
                    catch
                    {
                        matching = false;
                        break;
                    }

                    if (bitrate == 0 ||
                        string.IsNullOrEmpty(privateData) ||
                        string.IsNullOrEmpty(fourCC) ||
                        (fourCC != "h264" && fourCC != "avc1") ||
                        width == 0 ||
                        height == 0)
                    {
                        matching = false;
                        break;
                    }

                    bool found = false;
                    foreach (KeyValuePair<MediaType, Guid> pair in streams)
                    {
                        if (recognizedStreams.Contains(pair.Value)) continue;
                        if ((double)Math.Abs(bitrate - pair.Key.Bitrate) / Math.Max(bitrate, pair.Key.Bitrate) > 0.1) continue;
                        if (width != pair.Key.Width) continue;
                        if (height != pair.Key.Height) continue;
                        if (privateData != pair.Key.PrivateDataIisString) continue;

                        found = true;
                        recognizedStreams.Add(pair.Value);
                        break;
                    }

                    if (!found)
                    {
                        matching = false;
                        break;
                    }
                }

                if (matching)
                {
                    XmlNodeList audioNodes = doc.SelectNodes("//StreamIndex[@Type='audio']/QualityLevel");
                    foreach (XmlNode node in audioNodes)
                    {
                        int bitrate = 0;
                        int audioTag = 0;
                        int channels = 0;
                        int samplingRate = 0;

                        try
                        {
                            bitrate = int.Parse(node.Attributes["Bitrate"].Value);
                            audioTag = int.Parse(node.Attributes["AudioTag"].Value);
                            channels = int.Parse(node.Attributes["Channels"].Value);
                            samplingRate = int.Parse(node.Attributes["SamplingRate"].Value);
                        }
                        catch
                        {
                            matching = false;
                            break;
                        }

                        if (bitrate == 0 ||
                            audioTag != 255 || // AAC
                            channels == 0 ||
                            samplingRate == 0)
                        {
                            matching = false;
                            break;
                        }

                        bool found = false;
                        foreach (KeyValuePair<MediaType, Guid> pair in streams)
                        {
                            if (recognizedStreams.Contains(pair.Value)) continue;
                            if ((double)Math.Abs(bitrate - pair.Key.Bitrate) / Math.Max(bitrate, pair.Key.Bitrate) > 0.1) continue;
                            if (channels != pair.Key.Channels) continue;
                            if (samplingRate != pair.Key.SampleRate) continue;

                            found = true;
                            recognizedStreams.Add(pair.Value);
                            break;
                        }

                        if (!found)
                        {
                            matching = false;
                            break;
                        }
                    }
                }

            }
            catch (WebException webex)
            {
                if (webex.Response != null && webex.Response.GetType() == typeof(HttpWebResponse))
                {
                    HttpWebResponse resp = webex.Response as HttpWebResponse;
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        matching = true;
                    }
                    else
                    {
                        Global.Log.InfoFormat("Unexpected status code: {0}, exception: {1}", resp.StatusCode, webex.ToString());
                    }
                }
                else
                {
                    Global.Log.InfoFormat("Unexpected web exception: {0}", webex.ToString());
                }
            }
            catch (Exception ex)
            {
                Global.Log.InfoFormat("Unexpected exception: {0}", ex.ToString());
            }

            if (reqStream != null)
            {
                try
                {
                    reqStream.Close();
                }
                catch
                {
                }
            }

            if (webResp != null)
            {
                try
                {
                    webResp.Close();
                }
                catch
                {
                }
            }

            return matching;
        }

        private void UnregisterExpiredStreams()
        {
            if ((DateTime.Now - this.lastExpiredStreamsChecked).TotalMilliseconds < 1000)
            {
                return;
            }

            bool interrupted = false;
            do
            {
                interrupted = false;
                foreach (KeyValuePair<Guid, DateTime> pair in this.publishStreamId2LastActivity)
                {
                    if ((DateTime.Now - pair.Value).TotalMilliseconds < 30000)
                    {
                        continue;
                    }

                    this.UnregisterStream(pair.Key);
                    interrupted = true;
                    break; // because collection is modified
                }
            }
            while (interrupted);

            this.lastExpiredStreamsChecked = DateTime.Now;
        }

        private void UnregisterStream(Guid streamId)
        {
            Global.Log.DebugFormat("Unregistering media type {0}", streamId);

            if (this.webRequestStreams.ContainsKey(streamId))
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

            MediaType mt = this.streamsRev[streamId];
            this.streams.Remove(mt);
            this.streamsRev.Remove(streamId);
            this.publishStreamId2MuxerStreamId.Remove(streamId);
            this.publishStreamId2LastActivity.Remove(streamId);
        }

        private void PushHeader()
        {
            foreach (KeyValuePair<Guid, int> pair in publishStreamId2MuxerStreamId)
            {
                int headerSize = 0;
                IntPtr headerPtr = IntPtr.Zero;
                int res = SmoothStreamingSegmenter.MCSSF_GetHeader(this.muxId, pair.Value, out headerSize, out headerPtr);
                if (res < 0)
                {
                    Global.Log.ErrorFormat("MCSSF_GetHeader failed for stream {0}, result {1}", pair.Key, res);
                    UnregisterStream(pair.Key);
                    throw new CriticalStreamException(string.Format("MCSSF_GetHeader failed {0}", res));
                }

                if (headerSize > Global.MediaAllocator.BufferSize)
                {
                    // increase buffer sizes
                    Global.MediaAllocator.Reallocate(headerSize * 3 / 2, Global.MediaAllocator.BufferCount);
                }

                PacketBuffer header = Global.MediaAllocator.LockBuffer();
                Marshal.Copy(headerPtr, header.Buffer, 0, headerSize);

                header.ActualBufferSize = headerSize;
                try
                {
                    if (header.ActualBufferSize > 0)
                    {
                        this.PushHeaderData(pair.Key, header.Buffer, 0, header.ActualBufferSize);
                    }
                }
                catch (Exception ex)
                {
                    // header push failed, unregistering the stream
                    Global.Log.ErrorFormat("Push header failed for stream {0}: {1}", pair.Key, ex.ToString());
                    UnregisterStream(pair.Key);
                    header.Release();
                    throw;
                }

                header.Release();
            }

            this.mediaDataStarted = true;
        }

        private void PushHeaderData(Guid streamId, byte[] buffer, int offset, int length)
        {
            if (!this.webRequests.ContainsKey(streamId))
            {
                this.CreateWebRequest(streamId);
            }

            Stream webRequestStream = this.webRequestStreams[streamId];
            webRequestStream.Write(buffer, offset, length);
            webRequestStream.Flush();
        }
    }
}
