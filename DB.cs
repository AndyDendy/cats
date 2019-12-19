using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
namespace Myronov
{
    public class DB
    {
        Timer aTimer = new Timer(2000);
        DataTable data;
        string path;
        public DB(string path)
        {
            this.path = path;
            data = DTfromText(path);
            data.TableName = Path.GetFileNameWithoutExtension(path);
            aTimer = new System.Timers.Timer(180000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            File.Copy(path, Path.GetDirectoryName(path) + "/" + name + "meta.xml");
            File.Delete(path);
            DataTable meta = data;
            int clen = meta.Columns.Count;
            for (int i = 0; i < meta.Rows.Count; i++)
            {
                int c = clen;
                object[] arr = meta.Rows[i].ItemArray;
                for (int j = 0; j < clen; j++)
                {
                    if (arr[j].ToString() == "") { c--; }
                }
                if (c == 0) { meta.Rows[i].Delete(); if (i - 1 != -1) i--; }
            }
            meta.WriteXml(path);
            File.Delete(Path.GetDirectoryName(path) + "/" + name + "meta.xml");
        }
        public static DataTable DTfromText(string text)
        {
            try
            {
                DataTable dt;
                DataSet dataSet = new DataSet();
                FileStream stream = new FileStream(text, FileMode.Open);
                dataSet.ReadXml(stream, XmlReadMode.InferTypedSchema);
                stream.Close();
                dt = dataSet.Tables[0];
                return dt;
            }
            catch
            {
                return null;
            }
        }
        public bool Access(CookieCollection cl)
        {
            try
            {
                Cookie cookie = cl[0];
                string[] arr = cookie.Value.Replace("id=", "").Split('!');

                if (arr[1] == UniversalF(new int[] { 0 }, new string[] { arr[0] }, new int[] { 2 })[0])
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        public bool Create(string[] arr)
        {
            bool c = true;
            Parallel.ForEach(data.AsEnumerable(), drow =>
            {
                object[] r = drow.ItemArray;
                if (r[0].ToString() == arr[0]) c = false;
            });
            if (c)
            {
                data.Rows.Add(arr);
            }
            return c;
        }
        public bool Delete(string [] keys, string [] heads)
        {
            try
            {
                int len = data.Rows.Count, hlen=heads.Length;
                bool res = true;
                for (int i = 0; i < len; i++)
                {
                    for(int j=0;j<hlen;j++)
                    {
                        if (data.Rows[i][heads[j]].ToString() != keys[j]) res = false;
                    }
                    if (res) for (int j = 0; j < hlen; j++) data.Rows[i][j] = "";
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public string All()
        {
            return JsonConvert.SerializeObject(data);
        }
        public string[] UniversalF(int[] authinds, string[] authwords, int[] exportinds)
        {
            bool ok = true;
            int len = data.Rows.Count, reql = authinds.Length, respl = exportinds.Length;
            string[] values = new string[reql];
            string[] value = new string[respl];
            for (int i = 0; i < len; i++)
            {
                for (int j = 0; j < reql; j++)
                {
                    values[j] = data.Rows[i].ItemArray[authinds[j]].ToString();
                }
                for (int j = 0; j < reql; j++)
                {
                    if (authwords[j] != values[j])
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    for (int j = 0; j < respl; j++)
                    {
                        value[j] = data.Rows[i].ItemArray[exportinds[j]].ToString();
                    }

                    return value;
                }
                ok = true;
            }
            return value;
        }
        //public bool UniversalM(int[] authinds, string[] authwords, int[] importinds, string[] importwords)
        //{
        //    back:
        //    try
        //    {

        //        bool ok = true;
        //        int len = data.Rows.Count, reql = authinds.Length, respl = importinds.Length;
        //        string[] values = new string[reql];
        //        for (int i = 0; i < len; i++)
        //        {
        //            for (int j = 0; j < reql; j++)
        //            {
        //                values[j] = data.Rows[i].ItemArray[authinds[j]].ToString();
        //            }
        //            for (int j = 0; j < reql; j++)
        //            {
        //                if (authwords[j] != values[j])
        //                {
        //                    ok = false;
        //                    break;
        //                }
        //            }
        //            if (ok)
        //            {
        //                for (int j = 0; j < respl; j++)
        //                {
        //                    data.Rows[i][importinds[j]] = importwords[j];
        //                }
        //                return true;
        //            }
        //            ok = true;
        //        }
        //    }
        //    catch(Exception exs)
        //    {
        //        goto back;
        //    }
        //    return false;
        //}

        public bool UniversalM(string [] keys, string [] heads)
        {
            bool res = true;
            int len = data.Rows.Count, hlen=heads.Length;
            for(int i=0;i<len;i++)
            {
                
                string[] arr = data.Rows[i].ItemArray.Select((x)=>x.ToString()).ToArray();
                for(int j=0;j<hlen;j++)
                {
                    if (data.Rows[i][heads[j]].ToString() != keys[j]) { res = false; break; }
                }
                if(res) return true;
            }
            return false;
        }

        public bool Update(string[] keys, string[] heads, string [] newkeys, string[]heads2)
        {
        back:
            try
            {
                bool res = true;
                int len = data.Rows.Count, hlen = heads.Length, nlen = newkeys.Length;
                for (int i = 0; i < len; i++)
                {
                    res = true;
                    string[] arr = data.Rows[i].ItemArray.Select((x) => x.ToString()).ToArray();
                    for (int j = 0; j < hlen; j++)
                    {
                        if (data.Rows[i][heads[j]].ToString() != keys[j]) { res = false; break; }
                    }
                    if (res)
                    {
                        for (int j = 0; j < nlen; j++)
                        {
                            data.Rows[i][heads2[j]] = newkeys[j];
                        }
                    }
                }
            }
            catch (Exception exs){ goto back; }
            return false;
        }
        public string[] UniversalF(string[] keys, string[] heads,string [] expheads)
        {
            bool res = true;

            int len = data.Rows.Count, hlen = heads.Length, elen=expheads.Length;
            string[] resarr = new string[elen];
            for (int i = 0; i < len; i++)
            {

                string[] arr = data.Rows[i].ItemArray.Select((x) => x.ToString()).ToArray();
                for (int j = 0; j < hlen; j++)
                {
                    if (data.Rows[i][heads[j]] != keys[j]) { res = false; break; }
                }
                if (res)
                {
                    for (int j = 0; j < elen; j++)
                    {
                        resarr[j] = data.Rows[i][expheads[j]].ToString();   
                    }
                    return resarr;
                }
            }
            return null;
        }
        public void Close()
        {
            string name = Path.GetFileNameWithoutExtension(path);
            File.Copy(path, Path.GetDirectoryName(path) + "/" + name + "meta.xml");
            File.Delete(path);
            data.WriteXml(path);
            File.Delete(Path.GetDirectoryName(path) + "/" + name + "meta.xml");
        }
    }
}
