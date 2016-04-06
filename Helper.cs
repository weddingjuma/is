﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using iSpyApplication.Utilities;

namespace iSpyApplication
{
    public static class Helper
    {
        public class ListItem
        {
            public string Name { get; set; }

            public bool Restricted { get; set; }

            public string Value { get; set; }

            public int Index { get; set; }

            public override string ToString()
            {
                return Name;
            }

            public ListItem(string name = "", string value = "", int index = 0)
            {
                Name = name;
                Value = value;
                Index = index;
            }
        }

        public static Rectangle GetArea(Rectangle container, int imageW, int imageH)
        {
            int contH = container.Height;
            int contW = container.Width;
            int x = container.X;
            int y = container.Y;
            if (contH > 0 && contW > 0)
            {
                double arw = Convert.ToDouble(contW) / Convert.ToDouble(imageW);
                double arh = Convert.ToDouble(contH) / Convert.ToDouble(imageH);
                int w;
                int h;
                if (arh <= arw)
                {
                    w = Convert.ToInt32(((Convert.ToDouble(contW) * arh) / arw));
                    h = contH;
                }
                else
                {
                    w = contW;
                    h = Convert.ToInt32((Convert.ToDouble(contH) * arw) / arh);
                }
                int x2 = x + ((contW - w) / 2);
                int y2 = y + ((contH - h) / 2);
                return new Rectangle(x2, y2, w, h);
            }
            return container;
        }

        public static double CalculateTrigger(double percent)
        {
            const double minimum = 0.00000001;
            const double maximum = 1;
            return minimum + ((maximum - minimum)/100)*Convert.ToDouble(percent);
        }

        public static DateTime Now => DateTime.UtcNow;

        public static bool HasFeature(Enums.Features feature)
        {
            return ((1L & FeatureSet) != 0) || (((long)feature & FeatureSet) != 0);
        }

        public static string AvailableActionsJson
        {
            get
            {
                const string t = "{{ \"value\": \"{0}\", \"text\": \"{1}\" }},";
                var r = "";
                r += string.Format(t, "EXE", "Execute File");
                r += string.Format(t, "URL", "Call URL");
                r += string.Format(t, "NM", "Network Message");
                r += string.Format(t, "S", "Play Sound");
                r += string.Format(t, "ATC", "Sound Through Camera");
                r += string.Format(t, "SW", "Show Window");
                r += string.Format(t, "B", "Beep");
                r += string.Format(t, "M", "Maximise");
                r += string.Format(t, "SOO", "Switch Object On");
                r += string.Format(t, "SOF", "Switch Object Off");
                r += string.Format(t, "TA", "Trigger Alert On");
                if (MainForm.Conf.UseSMTP || MainForm.Conf.Subscribed)
                    r += string.Format(t, "E", "Send Email");
                if (MainForm.Conf.Subscribed)
                {
                    r += string.Format(t, "SMS", "Send SMS");
                    r += string.Format(t, "TM", "Send Twitter Message");
                }
                return r.Trim(',');

            }
        }

        private static long FeatureSet
        {
            get
            {
                var o = MainForm.Conf.Permissions.FirstOrDefault(p => p.name == MainForm.Group);
                if (o == null)
                    return 1; //group missing - assign all permissions
                return o.featureset;
            }
        }
        public static string ZeroPad(int i)
        {
            if (i < 10)
                return "0" + i;
            return i.ToString(CultureInfo.InvariantCulture);
        }

        public static Dictionary<string, string> GetDictionary(string cfg, char delim)
        {
            var d = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(cfg))
            {
                var l = cfg.Split(delim);
                foreach (var t in l)
                {
                    var nv = t.Split('=');
                    if (nv.Length == 2)
                    {
                        if (!d.ContainsKey(nv[0]))
                            d.Add(nv[0], nv[1]);
                    }
                }
            }
            return d;
        }

        public static Boolean IsAlphaNumeric(string strToCheck)
        {
            Regex rg = new Regex(@"^[a-zA-Z0-9\s,]*$");
            return rg.IsMatch(strToCheck);
        }

        public static void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);

            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                if (name != null)
                {
                    string dest = Path.Combine(destFolder, name);
                    File.Copy(file, dest, true);
                }
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                if (name != null)
                {
                    string dest = Path.Combine(destFolder, name);
                    CopyFolder(folder, dest);
                }
            }
        }

        public static void SetTitle(Form f)
        {
            string ttl = $"iSpy v{Application.ProductVersion}";
            if (Program.Platform != "x86")
                ttl = $"iSpy 64 v{Application.ProductVersion}";

            if (MainForm.Conf.WSUsername != "")
            {
                ttl += $" ({MainForm.Conf.WSUsername})";
            }
            f.Text = ttl;
        }

        public static string GetMotionDataPoints(StringBuilder  motionData)
        {
            var elements = motionData.ToString().Trim(',').Split(',');
            if (elements.Length <= 1200)
                return String.Join(",", elements);
            
            var interval = (elements.Length / 1200d);
            var newdata = new StringBuilder(motionData.Length);
            var iIndex = 0;
            double dMax = 0;
            var tMult = 1;
            double target = 0;

            for(var i=0;i<elements.Length;i++)
            {
                try
                {
                    var dTemp = Convert.ToDouble(elements[i]);
                    if (dTemp > dMax)
                    {
                        dMax = dTemp;
                        iIndex = i;
                    }
                    if (i > target)
                    {
                        newdata.Append(elements[iIndex] + ",");
                        tMult++;
                        target = tMult*interval;
                        dMax = 0;

                    }
                }
                catch (Exception)
                {
                    //extremely long recordings can break
                    break;
                }
            }
            string r = newdata.ToString().Trim(',');
            newdata.Clear();
            return r;

        }

        internal static bool ArchiveFile(string filename)
        {

            if (!string.IsNullOrEmpty(MainForm.Conf.Archive) && Directory.Exists(MainForm.Conf.Archive))
            {
                string fn = filename.Substring(filename.LastIndexOf("\\", StringComparison.Ordinal) + 1);
                if (File.Exists(filename))
                {
                    try
                    {
                        if (!File.Exists(MainForm.Conf.Archive + fn))
                            File.Copy(filename, MainForm.Conf.Archive + fn);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogExceptionToFile(ex);
                    }
                }
            }
            return false;

        }

        internal static bool ArchiveAndDelete(string filename)
        {

            if (!string.IsNullOrEmpty(MainForm.Conf.Archive) && Directory.Exists(MainForm.Conf.Archive))
            {
                string fn = filename.Substring(filename.LastIndexOf("\\", StringComparison.Ordinal) + 1);
                if (File.Exists(filename))
                {
                    try
                    {
                        if (!File.Exists(MainForm.Conf.Archive + fn))
                            File.Copy(filename, MainForm.Conf.Archive + fn);
                        File.Delete(filename);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogExceptionToFile(ex);
                    }
                }
            }
            return false;

        }

        internal static string GetMediaDirectory(int ot, int oid)
        {
            int i = 0;
            switch (ot)
            {
                case 1:
                    {
                        var o = MainForm.Microphones.FirstOrDefault(p => p.id == oid);
                        if (o != null)
                            i = o.settings.directoryIndex;
                    }
                    break;
                case 2:
                    {
                        var o = MainForm.Cameras.FirstOrDefault(p => p.id == oid);
                        if (o != null)
                            i = o.settings.directoryIndex;
                    }
                    break;
            }
            var o2 = MainForm.Conf.MediaDirectories.FirstOrDefault(p => p.ID == i);
            if (o2 != null)
                return o2.Entry;
            return MainForm.Conf.MediaDirectories[0].Entry;
        }

        internal static string GetMediaDirectory(int directoryIndex)
        {
            var o2 = MainForm.Conf.MediaDirectories.FirstOrDefault(p => p.ID == directoryIndex);
            if (o2 != null)
                return o2.Entry;
            return MainForm.Conf.MediaDirectories[0].Entry;
        }

        public static string GetFullPath(int ot, int oid)
        {
            string d = GetMediaDirectory(ot, oid);
            if (!d.EndsWith("\\"))
                d += "\\";
            return  d+ (ot==1?"audio":"video")+"\\"+GetDirectory(ot, oid) + "\\";
        }

        public static string GetDirectory(int objectTypeId, int objectId)
        {
            if (objectTypeId == 1)
            {
                var m = MainForm.Microphones.SingleOrDefault(p => p.id == objectId);
                if (m != null)
                    return m.directory;
                throw new Exception("could not find directory for mic " + objectId);
            }
            var c = MainForm.Cameras.SingleOrDefault(p => p.id == objectId);
            if (c != null)
                return c.directory;
            throw new Exception("could not find directory for cam " + objectId);
        }

        public static void DeleteAllContent(int objectTypeId, int objectid)
        {
            var dir = GetMediaDirectory(objectTypeId, objectid);
            var dirName = GetDirectory(objectTypeId, objectid);
            if (objectTypeId == 1)
            {
                var lFi = new List<FileInfo>();
                var dirinfo = new DirectoryInfo(dir + "audio\\" +
                                              dirName + "\\");

                lFi.AddRange(dirinfo.GetFiles());
                lFi = lFi.FindAll(f => f.Extension.ToLower() == ".mp3");

                foreach (FileInfo fi in lFi)
                {
                    try
                    {
                        FileOperations.Delete(fi.FullName);
                    }
                    catch(Exception ex)
                    {
                        Logger.LogExceptionToFile(ex);
                    }
                }

            }
            if (objectTypeId == 2)
            {
                var lFi = new List<FileInfo>();
                var dirinfo = new DirectoryInfo(dir + "video\\" +
                                              dirName + "\\");

                lFi.AddRange(dirinfo.GetFiles());
                lFi = lFi.FindAll(f => f.Extension.ToLower() == ".mp4" || f.Extension.ToLower() == ".avi");

                foreach (FileInfo fi in lFi)
                {
                    try
                    {
                        FileOperations.Delete(fi.FullName);
                    }
                    catch(Exception ex)
                    {
                        Logger.LogExceptionToFile(ex);
                    }
                }
                System.Array.ForEach(Directory.GetFiles(dir + "video\\" +
                                              dirName + "\\thumbs\\"), delegate(string path)
                                              {
                                                  try
                                                  {
                                                      FileOperations.Delete(path);
                                                  }
                                                  catch
                                                  {
                                                      // ignored
                                                  }
                                              });

            }

        }
        // returns the number of milliseconds since Jan 1, 1970 (useful for converting C# dates to JS dates)
        public static double UnixTicks(this DateTime dt)
        {
            var d1 = new DateTime(1970, 1, 1);
            var d2 = dt.ToUniversalTime();
            var ts = new TimeSpan(d2.Ticks - d1.Ticks);
            return ts.TotalMilliseconds;
        }

        public static double UnixTicks(this long ticks)
        {
            var d1 = new DateTime(1970, 1, 1);
            var d2 = new DateTime(ticks);
            var ts = new TimeSpan(d2.Ticks - d1.Ticks);
            return ts.TotalMilliseconds;
        }


        public static bool CanAlert(string groupname, int resetInterval)
        {
            if (string.IsNullOrEmpty(groupname) || resetInterval == 0)
                return true;

            var ag = AlertGroups.FirstOrDefault(p => p.Name == groupname);
            if (ag == null)
            {
                ag = new AlertGroup(groupname);
                AlertGroups.Add(ag);
                return true;
            }
            if ((Now - ag.LastReset).TotalSeconds >= resetInterval)
            {
                ag.LastReset = Now;
                return true;
            }
            ag.LastReset = Now;
            return false;

        }

        public static readonly List<AlertGroup> AlertGroups = new List<AlertGroup>();

        public static MachineType GetDllMachineType(string dllPath)
        {
            //see http://www.microsoft.com/whdc/system/platform/firmware/PECOFF.mspx
            //offset to PE header is always at 0x3C
            //PE header starts with "PE\0\0" =  0x50 0x45 0x00 0x00
            //followed by 2-byte machine type field (see document above for enum)
            var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
            var br = new BinaryReader(fs);
            fs.Seek(0x3c, SeekOrigin.Begin);
            Int32 peOffset = br.ReadInt32();
            fs.Seek(peOffset, SeekOrigin.Begin);
            UInt32 peHead = br.ReadUInt32();
            if (peHead != 0x00004550) // "PE\0\0", little-endian
                throw new Exception("Can't find PE header");
            var machineType = (MachineType)br.ReadUInt16();
            br.Close();
            fs.Close();
            return machineType;
        }

        public enum MachineType : ushort
        {
            IMAGE_FILE_MACHINE_UNKNOWN = 0x0,
            IMAGE_FILE_MACHINE_AM33 = 0x1d3,
            IMAGE_FILE_MACHINE_AMD64 = 0x8664,
            IMAGE_FILE_MACHINE_ARM = 0x1c0,
            IMAGE_FILE_MACHINE_EBC = 0xebc,
            IMAGE_FILE_MACHINE_I386 = 0x14c,
            IMAGE_FILE_MACHINE_IA64 = 0x200,
            IMAGE_FILE_MACHINE_M32R = 0x9041,
            IMAGE_FILE_MACHINE_MIPS16 = 0x266,
            IMAGE_FILE_MACHINE_MIPSFPU = 0x366,
            IMAGE_FILE_MACHINE_MIPSFPU16 = 0x466,
            IMAGE_FILE_MACHINE_POWERPC = 0x1f0,
            IMAGE_FILE_MACHINE_POWERPCFP = 0x1f1,
            IMAGE_FILE_MACHINE_R4000 = 0x166,
            IMAGE_FILE_MACHINE_SH3 = 0x1a2,
            IMAGE_FILE_MACHINE_SH3DSP = 0x1a3,
            IMAGE_FILE_MACHINE_SH4 = 0x1a6,
            IMAGE_FILE_MACHINE_SH5 = 0x1a8,
            IMAGE_FILE_MACHINE_THUMB = 0x1c2,
            IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x169,
        }

        public static bool UnmanagedDllIs64Bit(string dllPath)
        {
            try
            {
                switch (GetDllMachineType(dllPath))
                {
                    case MachineType.IMAGE_FILE_MACHINE_AMD64:
                    case MachineType.IMAGE_FILE_MACHINE_IA64:
                        return true;
                    case MachineType.IMAGE_FILE_MACHINE_I386:
                        return false;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public class AlertGroup
        {
            public DateTime LastReset;
            public readonly String Name;

            public AlertGroup(string name)
            {
                LastReset = Now;
                Name = name;
            }
        }

        public static string AlertSummary(objectsActionsEntry e)
        {
            string t = "Unknown";
            bool b;
            switch (e.type.ToUpperInvariant())
            {
                case "EXE":
                    t = LocRm.GetString("ExecuteFile") + ": " + e.param1;
                    break;
                case "URL":
                    t = LocRm.GetString("CallURL") + ": " + e.param1;
                    Boolean.TryParse(e.param2, out b);
                    if (b)
                        t += " (POST grab)";
                    break;
                case "NM":
                    t = e.param1 + " " + e.param2 + ":" + e.param3 + " (" + e.param4 + ")";
                    break;
                case "S":
                    t = LocRm.GetString("PlaySound") + ": " + e.param1;
                    break;
                case "ATC":
                    t = LocRm.GetString("SoundThroughCamera") + ": " + e.param1;
                    break;
                case "SW":
                    t = LocRm.GetString("ShowWindow");
                    break;
                case "B":
                    t = LocRm.GetString("Beep");
                    break;
                case "M":
                    t = LocRm.GetString("Maximise");
                    break;
                case "MO":
                    t = LocRm.GetString("SwitchMonitorOn");
                    break;
                case "TA":
                    {
                        string[] op = e.param1.Split(',');
                        string n = "[removed]";
                        int id = Convert.ToInt32(op[1]);
                        switch (op[0])
                        {
                            case "1":
                                objectsMicrophone om = MainForm.Microphones.FirstOrDefault(p => p.id == id);
                                if (om != null)
                                    n = om.name;
                                break;
                            case "2":
                                objectsCamera oc = MainForm.Cameras.FirstOrDefault(p => p.id == id);
                                if (oc != null)
                                    n = oc.name;
                                break;
                        }
                        t = LocRm.GetString("TriggerAlertOn") + " " + n;
                    }
                    break;
                case "SOO":
                    {
                        string[] op = e.param1.Split(',');
                        string n = "[removed]";
                        int id = Convert.ToInt32(op[1]);
                        switch (op[0])
                        {
                            case "1":
                                objectsMicrophone om = MainForm.Microphones.FirstOrDefault(p => p.id == id);
                                if (om != null)
                                    n = om.name;
                                break;
                            case "2":
                                objectsCamera oc = MainForm.Cameras.FirstOrDefault(p => p.id == id);
                                if (oc != null)
                                    n = oc.name;
                                break;
                        }
                        t = LocRm.GetString("SwitchObjectOn") + " " + n;
                    }
                    break;
                case "SOF":
                    {
                        string[] op = e.param1.Split(',');
                        string n = "[removed]";
                        int id;
                        int.TryParse(op[1], out id);
                        switch (op[0])
                        {
                            case "1":
                                objectsMicrophone om = MainForm.Microphones.FirstOrDefault(p => p.id == id);
                                if (om != null)
                                    n = om.name;
                                break;
                            case "2":
                                objectsCamera oc = MainForm.Cameras.FirstOrDefault(p => p.id == id);
                                if (oc != null)
                                    n = oc.name;
                                break;
                        }
                        t = LocRm.GetString("SwitchObjectOff") + " " + n;
                    }
                    break;
                case "E":
                    t = LocRm.GetString("SendEmail") + ": " + e.param1;
                    if (e.param2 != "")
                    {
                        bool.TryParse(e.param2, out b);
                        if (b)
                            t += " (include grab)";
                    }

                    break;
                case "SMS":
                    t = LocRm.GetString("SendSMS") + ": " + e.param1;
                    break;
                case "TM":
                    t = LocRm.GetString("SendTwitterMessage");
                    break;
            }

            return t;
        }

        public static bool TestHttpurl(string source, string cookies, string login, string password)
        {
            bool b = false;
            HttpStatusCode sc = 0;

            HttpWebRequest req;
            var res = ConnectionFactory.GetResponse(source, cookies, "", "", login, password, "GET", "", false, out req);
            if (res != null)
            {
                sc = res.StatusCode;
                if (sc == HttpStatusCode.OK)
                {
                    string ct = res.ContentType.ToLower();
                    if (ct.IndexOf("text", StringComparison.Ordinal) == -1)
                    {
                        b = true;
                    }
                }
                res.Close();
            }

            Logger.LogMessageToFile("Status " + sc + " at " + source, "Uri Checker");

            return b;
        }

        public static bool TestRtspurl(Uri uri, string login, string password)
        {
            bool b = false;
            try
            {

                var request = "OPTIONS " + uri + " RTSP/1.0\r\n" +
                              "CSeq: 1\r\n" +
                              "User-Agent: iSpy\r\n" +
                              "Accept: */*\r\n";

                if (!string.IsNullOrEmpty(login))
                {
                    var authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(login + ":" + password));
                    request += "Authorization: Basic " + authInfo + "\r\n";
                }

                request += "\r\n";

                IPAddress host = IPAddress.Parse(uri.DnsSafeHost);
                var hostep = new IPEndPoint(host, uri.Port);

                var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { ReceiveTimeout = 2000 };
                sock.Connect(hostep);

                var response = sock.Send(Encoding.UTF8.GetBytes(request));
                if (response > 0)
                {
                    var bytesReceived = new byte[200];
                    var bytes = sock.Receive(bytesReceived, bytesReceived.Length, 0);
                    string resp = Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    if (resp.IndexOf("200 OK", StringComparison.Ordinal) != -1)
                    {
                        b = true;
                    }
                    Logger.LogMessageToFile("RTSP attempt: " + resp + " at " + uri, "Uri Checker");
                }
                sock.Close();
            }
            catch (Exception ex)
            {
                Logger.LogErrorToFile(ex.Message, "Uri Checker");
            }
            return b;
        }

        public static readonly ScheduleAction[] Actions =
            {
                new ScheduleAction("Power: ON",0,ScheduleAction.ActionTypeID.All),
                new ScheduleAction("Power: OFF",1,ScheduleAction.ActionTypeID.All),
                new ScheduleAction("Recording: Start",2,ScheduleAction.ActionTypeID.CameraAndMic),
                new ScheduleAction("Recording: Stop",3,ScheduleAction.ActionTypeID.CameraAndMic),
                new ScheduleAction("Mode: Record on Detect",4,ScheduleAction.ActionTypeID.CameraAndMic),
                new ScheduleAction("Mode: Record on Alert",5,ScheduleAction.ActionTypeID.CameraAndMic),
                new ScheduleAction("Mode: No Recording",6,ScheduleAction.ActionTypeID.CameraAndMic),
                new ScheduleAction("Alerts: ON",7,ScheduleAction.ActionTypeID.All),
                new ScheduleAction("Alerts: OFF",8,ScheduleAction.ActionTypeID.All),
                new ScheduleAction("Alert Action: ON",9,ScheduleAction.ActionTypeID.All,"Action"),
                new ScheduleAction("Alert Action: OFF",10,ScheduleAction.ActionTypeID.All,"Action"),
                new ScheduleAction("Time lapse: ON",11,ScheduleAction.ActionTypeID.CameraOnly),
                new ScheduleAction("Time lapse: OFF",12,ScheduleAction.ActionTypeID.CameraOnly),
                new ScheduleAction("FTP Images: ON",13,ScheduleAction.ActionTypeID.CameraOnly),
                new ScheduleAction("FTP Images: OFF",14,ScheduleAction.ActionTypeID.CameraOnly),
                new ScheduleAction("FTP Recordings: ON",15,ScheduleAction.ActionTypeID.CameraOnly),
                new ScheduleAction("FTP Recordings: OFF",16,ScheduleAction.ActionTypeID.CameraOnly),
                new ScheduleAction("Grabs: ON",17,ScheduleAction.ActionTypeID.CameraOnly),
                new ScheduleAction("Grabs: OFF",18,ScheduleAction.ActionTypeID.CameraOnly),
                new ScheduleAction("PTZ Scheduler: ON",19,ScheduleAction.ActionTypeID.CameraOnly),
                new ScheduleAction("PTZ Scheduler: OFF",20,ScheduleAction.ActionTypeID.CameraOnly),
                new ScheduleAction("Messaging: ON",21,ScheduleAction.ActionTypeID.All),
                new ScheduleAction("Messaging: OFF",22,ScheduleAction.ActionTypeID.All),
                new ScheduleAction("PTZ Tracking: ON",23,ScheduleAction.ActionTypeID.CameraOnly),
                new ScheduleAction("PTZ Tracking: OFF",24,ScheduleAction.ActionTypeID.CameraOnly)
            };
        public static string[] WebRestrictedAlertTypes = { "S", "EXE" };
        public static string ScheduleDescription(int id)
        {
            return Actions.Single(p => p.ID == id).ToString();
        }

        internal static bool CanArchive
        {
            get
            {
                if (!string.IsNullOrEmpty(MainForm.Conf.Archive))
                {
                    try
                    {
                        if (Directory.Exists(MainForm.Conf.Archive))
                            return true;
                    }
                    catch
                    {
                        //invalid location
                    }

                }
                return false;
            }
        }

        public class ScheduleAction
        {
            private readonly string _action;
            public readonly string ParameterName;
            public readonly int ID;
            public readonly ActionTypeID TypeID;

            public enum ActionTypeID
            {
                All, CameraOnly, CameraAndMic
            }

            public ScheduleAction(string action, int id, ActionTypeID typeID)
            {
                _action = action;
                ID = id;
                TypeID = typeID;
            }

            public ScheduleAction(string action, int id, ActionTypeID typeID, string param)
            {
                _action = action;
                ID = id;
                TypeID = typeID;
                ParameterName = param;
            }

            public override string ToString()
            {
                return _action;
            }
        }

        #region Nested type: FrameAction

        public struct FrameAction
        {
            public byte[] Content;
            public int DataLength;
            public readonly double Level;
            public readonly DateTime TimeStamp;
            public readonly Enums.FrameType FrameType;

            public FrameAction(Bitmap frame, double level, DateTime timeStamp)
            {
                Level = level;
                TimeStamp = timeStamp;
                using (var ms = new MemoryStream())
                {
                    frame.Save(ms, MainForm.Encoder, MainForm.EncoderParams);
                    Content = ms.GetBuffer();
                }
                FrameType = Enums.FrameType.Video;
                DataLength = Content.Length;
            }

            public FrameAction(byte[] frame,  int bytesRecorded, double level, DateTime timeStamp)
            {
                Content = frame;
                Level = level;
                TimeStamp = timeStamp;
                FrameType = Enums.FrameType.Audio;
                DataLength = bytesRecorded;
            }

            public void Nullify()
            {
                Content = null;
                DataLength = 0;
            }

        }

       
        #endregion
    }
}