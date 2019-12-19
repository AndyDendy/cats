using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Myronov
{
    public class HTTP
    {
        private readonly HttpListener core = new HttpListener();
        public string abspath;
        DB data;
        public HTTP()
        {
            core.IgnoreWriteExceptions = true;

            DataTable dt;
            dt = DB.DTfromText("manifest.xml");
            string[] pref = dt.Rows[0]["headers"].ToString().Replace("\r\n", "").Split(';');
            foreach (string s in pref)
            {
                try
                {
                    core.Prefixes.Add(s);
                }
                catch
                {

                }
            }
            abspath = dt.Rows[0]["www"].ToString();
            data = new DB(dt.Rows[0]["db"].ToString() + "/" + "users.xml");
        }
        public void Start()
        {
            core.Start();
            ThreadPool.QueueUserWorkItem(zero =>
            {
                while (core.IsListening)
                {

                    try
                    {
                        ThreadPool.QueueUserWorkItem(core_context =>
                        {

                            try
                            {
                                HttpListenerContext context = core_context as HttpListenerContext;

                                if (context == null)
                                {
                                    return;
                                }
                                else
                                {
                                    LoadPage(ref context);
                                    context.Response.OutputStream.Close();
                                }
                            }
                            catch
                            {

                            }

                        }, core.GetContext());
                    }
                    catch
                    {

                    }
                }
            });
        }

        public void LoadPage(ref HttpListenerContext context)
        {
            Uri uri = new Uri(context.Request.Url.AbsoluteUri);
            string[] query = uri.Segments;
            string path = uri.AbsolutePath.Replace(uri.Host, "");

            byte[] buf = new byte[0];
            Stream stream = context.Request.InputStream;
            StreamReader reader = new StreamReader(stream);
            string request = reader.ReadToEnd();
            reader.Close();

            string response = string.Empty;
            if (!Path.GetExtension(path).Contains("."))
            {
                try
                {
                    switch (query[1].Replace("/", ""))
                    {
                        case "login":
                            {
                                string[] arr = request.Split('&', '=');
                                Random rnd = new Random();
                                int code = rnd.Next(-10000000, 10000001);
                                if (data.UniversalF(new int[] { 0 }, new string[] { arr[1] }, new int[] { 1 })[0] == arr[3] && data.Update(new string[] {arr[1] }, new string[] { "name" },new string[] {code.ToString()},new string[] { "cookie"})/* data.UniversalM(new int[] { 0 }, new string[] { arr[1] }, new int[] { 2 }, new string[] { cd })*/)
                                {
                                    context.Response.SetCookie(new Cookie("id", $"{arr[1]}!{code}"));
                                    context.Response.Redirect("/catsite.html");
                                }
                            }
                            break;
                        case "signup":
                            {
                                
                                string[] arr = request.Split('&', '=');
                                if (data.UniversalF(new int[] { 0 }, new string[] { arr[1] }, new int[] { 0 })[0] == arr[1])
                                {
                                    context.Response.Redirect("/");
                                }
                                else
                                {
                                    Random rnd = new Random();
                                    int code = rnd.Next(-10000000, 10000001);
                                    if (data.Create(new string[] {arr[1],arr[5],code.ToString(),arr[7],arr[3] }))
                                    {
                                        context.Response.SetCookie(new Cookie("id", $"{arr[1]}!{code}"));
                                        context.Response.Redirect("/catsite.html");
                                    }
                                }
                            }
                            break;
                        case "delete":
                            if (data.Access(context.Request.Cookies)) {
                                string[] arr = request.Split('&', '=');
                                //data.Delete();
                            }
                            break;
                        case "update":
                            if (data.Access(context.Request.Cookies)) {

                            }
                            break;
                        case "all":
                            if (data.Access(context.Request.Cookies)) { response = data.All(); }
                            break;
                        case "validate":
                            if (data.Access(context.Request.Cookies)) { response = "ok"; }
                            break;
                        case "location":
                            if (data.Access(context.Request.Cookies)) { response = data.UniversalF( new int[] { 0 }, new string[] { request }, new int[] { 3 } )[0]; }
                            break;
                        case "admin":
                            break;
                        default:
                            break;
                    }
                }
                catch
                {
                    response = File.ReadAllText(abspath + "/" + "index.html");
                    context.Response.ContentType = "text/html";
                }
                buf = Encoding.UTF8.GetBytes(response);
            }
            else
            {
                MemoryStream mem = new MemoryStream();
                string ext = Path.GetExtension(path);
                switch (ext)
                {
                    case ".html":
                        context.Response.ContentType = "text/html";
                        if (data.Access(context.Request.Cookies) && File.Exists(abspath + "/" + path))
                        {
                            response = File.ReadAllText(abspath + "/" + path);
                            buf = Encoding.UTF8.GetBytes(response);
                        }
                        else
                        {
                            context.Response.StatusCode = 403;
                        }
                        break;
                    case ".json":
                        response = File.ReadAllText(abspath + "/" + path);
                        context.Response.ContentType = "application/json";
                        buf = Encoding.UTF8.GetBytes(response);
                        break;
                    case ".jpg":
                        context.Response.ContentType = "image/jpeg";
                        Image.FromFile(abspath + "/" + path).Save(mem, ImageFormat.Jpeg);
                        buf = mem.ToArray();
                        break;
                    case ".png":
                        context.Response.ContentType = "image/png";
                        Image.FromFile(abspath + "/" + path).Save(mem, ImageFormat.Png);
                        buf = mem.ToArray();
                        break;
                    case ".ico":
                        break;
                    case ".js":
                        response = File.ReadAllText(abspath + "/" + path);
                        context.Response.ContentType = "application/javascript";
                        buf = Encoding.UTF8.GetBytes(response);
                        break;
                    case ".css":
                        response = File.ReadAllText(abspath + "/" + path);
                        context.Response.ContentType = "text/css";
                        buf = Encoding.UTF8.GetBytes(response);
                        break;
                    default:
                        response = File.ReadAllText(abspath + "/" + path);
                        context.Response.ContentType = "font/" + ext.Replace(".", "");
                        buf = Encoding.UTF8.GetBytes(response);
                        break;
                }
            }
            context.Response.ContentLength64 = buf.Length;
            context.Response.OutputStream.Write(buf, 0, buf.Length);
        }
        public void Stop()
        {
            data.Close();
            core.Stop();
            core.Close();
        }
    }
}
