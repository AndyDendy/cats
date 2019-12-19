using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Myronov
{
    public class TCP
    {
        static DataTable online = new DataTable();
        private readonly TcpListener core;
        public string ip;
        public string abspath;
        DB data;
        public TCP()
        {
            DataTable dt;
            dt = DB.DTfromText("manifest.xml");

            ip = dt.Rows[0]["wsip"].ToString();
            int port = int.Parse(dt.Rows[0]["wsport"].ToString());
            core = new TcpListener(IPAddress.Parse(ip), port);

            abspath = dt.Rows[0]["db"].ToString();
            data = new DB(abspath + "/" + "messages.xml");
            if (online.Columns.Count == 0) { online.Columns.Add("id"); online.Columns.Add("stat"); }
        }
        public void Log(string text)
        {

        }
        public void Stop()
        {
            Log("TCP core has been stopped;");
            data.Close();
            core.Stop();
        }
        public void Start()
        {
            Log($"TCP core has been launched  on {ip};");
            core.Start();
            ThreadPool.QueueUserWorkItem(zero =>
            {
                try
                {
                    while (true)
                    {
                        ThreadPool.QueueUserWorkItem(core_context =>
                        {
                            TcpClient context = core_context as TcpClient;

                            if (context != null)
                            {
                                Process(context);
                            }
                            else
                            {
                                return;
                            }
                        }, core.AcceptTcpClient());
                    }
                }
                catch
                {
                    Log("Fatal error;");
                }
            });
        }
        bool Handshaking(ref TcpClient client, ref NetworkStream stream)
        {
            bool done = false;
            while (!done)
            {
                while (!stream.DataAvailable) ;

                while (client.Available < 3) ;

                byte[] bytes = new byte[client.Available];
                stream.Read(bytes, 0, client.Available);
                string s = Encoding.UTF8.GetString(bytes);

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                {
                    Console.WriteLine("=====Handshaking from client=====\n{0}", s);
                    string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                    string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                    string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                    byte[] response = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Connection: Upgrade\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Sec-WebSocket-Accept: " + swkaSha1Base64 +
                        "\r\n\r\n");
                    stream.Write(response, 0, response.Length);
                    Log("handshake done;");
                    done = true;
                }
                
            }
            return done;
        }
        string Receive(ref TcpClient client, ref NetworkStream stream)
        {
            while (!stream.DataAvailable) ;

            while (client.Available < 3) ;

            string par = string.Empty;
            byte[] bytes = new byte[client.Available];
            stream.Read(bytes, 0, client.Available);
            string s = Encoding.UTF8.GetString(bytes);
            bool fin = (bytes[0] & 0b10000000) != 0,
                    mask = (bytes[1] & 0b10000000) != 0;

            int opcode = bytes[0] & 0b00001111,
                msglen = bytes[1] - 128,
                offset = 2;

            if (msglen == 126)
            {
                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                offset = 4;
            }
            else if (msglen == 127)
            {
                Console.WriteLine("TODO: msglen == 127, needs qword to store msglen");
            }

            if (msglen == 0)
                Console.WriteLine("msglen == 0");
            else if (mask)
            {
                byte[] decoded = new byte[msglen];
                byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                offset += 4;

                for (int i = 0; i < msglen; ++i)
                    decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                string text = Encoding.UTF8.GetString(decoded);
                Log(text);
                par = text;
            }
            else
                Console.WriteLine("mask bit not set");
            return par;
        }
        public bool Send(ref NetworkStream stream, string content)
        {
            try
            {
                List<byte> lb = new List<byte>();
                lb.Add(0x81);
                lb.Add(Convert.ToByte(content.Length));//dlina!!!!!!!!!!!!!!
                lb.AddRange(Encoding.UTF8.GetBytes(content));
                stream.Write(lb.ToArray(), 0, lb.ToArray().Length);
                Log(content + " has been send;");
                return true;
            }
            catch
            {
                Log("Aborted connection;");
                return false;
            }
        }
        async void Process(TcpClient client)
        {
            bool acc = false;
            string id="";
            NetworkStream stream = client.GetStream();
            if (Handshaking(ref client, ref stream))
            {
                while (client.Connected)
                {
                    string m = Receive(ref client, ref stream);
                    try
                    {
                        if (m.Substring(0, 3) == "id=")
                        {
                            HttpWebRequest http = (HttpWebRequest)WebRequest.Create("http://" + ip + "/validate/");
                            http.CookieContainer = new CookieContainer();
                            Cookie cookie = new Cookie("id", m.Substring(3));
                            cookie.Domain = ip;
                            http.CookieContainer.Add(cookie);
                            http.Method = "GET";

                            HttpWebResponse resp = (HttpWebResponse)http.GetResponse();
                            Stream str = resp.GetResponseStream();
                            StreamReader reader = new StreamReader(str);
                            string stat = reader.ReadToEnd();
                            reader.Close();

                            if (stat == "ok")
                            {
                                id = m.Substring(3).Split('!')[0];
                                acc = true;
                                int len = online.Rows.Count;
                                bool ex = false;
                                for (int i = 0; i < len; i++)
                                {
                                    if (online.Rows[i]["id"].ToString() == id)
                                    {
                                        ex = true;
                                        online.Rows[i]["stat"] = "+";
                                        break;
                                    }
                                }
                                if (ex)
                                {

                                }
                                else
                                {
                                    online.Rows.Add(m.Substring(3).Split('!')[0], "+");
                                }
                                break;
                            }
                        }
                    }
                    catch { client.Close(); }
                }

                if (acc)
                {
                    await Task.WhenAny(
                        Task.Run(() =>
                        {
                            while (client.Connected)
                            {
                                int len = online.Rows.Count;
                                string res = string.Empty;
                                for (int i = 0; i < len; i++)
                                {
                                    if (online.Rows[i]["stat"].ToString() == "+")
                                    {
                                        res += $"<button type=\"button\" class=\"btn btn-primary btn - lg btn - success\" onclick=\"butF('{online.Rows[i]["id"]}')\">{online.Rows[i]["id"]}</button>";
                                    }
                                    else
                                    {
                                        res += $"<button type=\"button\" class=\"btn btn-primary btn - lg btn - danger\" onclick=\"butF('{online.Rows[i]["id"]}')\">{online.Rows[i]["id"]}</button>";
                                    }
                                }

                                if(!Send(ref stream, res))
                                {
                                    client.Close();
                                    break;
                                }
                                Task.Delay(3000);
                            }
                        }),
                        Task.Run(() =>
                        {
                            while (client.Connected)
                            {
                                try
                                {
                                    string mes = Receive(ref client, ref stream);
                                }
                                catch
                                {
                                    //client.Close();
                                    //break;
                                }
                                Task.Delay(2000);
                            }
                        })
                        );
                }
                int len2 = online.Rows.Count;
                for (int i = 0; i < len2; i++)
                {
                    if (online.Rows[i]["id"].ToString()== id) online.Rows[i]["stat"] = "-";
                }
            }
            client.Close();
        }
    }
}
