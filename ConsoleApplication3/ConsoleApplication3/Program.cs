
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Threading;

namespace ConsoleApplication3
{
    class AppConfig
    {
        public static string GetAppConfig(string strKey)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (key == strKey)
                {
                    return config.AppSettings.Settings[strKey].Value.ToString();
                }
            }
            return null;
        }




        class Program
        {

            static Program p = new Program();
            static void Main(string[] args)
            {
                int j = 0;
                int sleepingtime = int.Parse(AppConfig.GetAppConfig("sleepingtime"));
                try
                {
                    tessnet2.Tesseract ocr = new tessnet2.Tesseract();//声明一个OCR类
                    ocr.SetVariable("tessedit_char_whitelist", "0123456789"); //设置识别变量，当前只能识别数字。
                    ocr.Init(AppConfig.GetAppConfig("tessdata"), "eng", true); //应用当前语言包。注，Tessnet2是支持多国语的。语言包下载链接：http://code.google.com/p/tesseract-ocr/downloads/list

                    while (true)
                    {
                        //登录界面的url
                        string loginPageUrl = AppConfig.GetAppConfig("loginurl");
                        //创建请求以获取验证码id                    
                        System.Net.HttpWebRequest SeekpicRequest = System.Net.HttpWebRequest.Create(loginPageUrl) as System.Net.HttpWebRequest;
                        SeekpicRequest.Method = "GET";
                        //发送请求
                        System.Net.HttpWebResponse SeekpicResponse = (System.Net.HttpWebResponse)SeekpicRequest.GetResponse();
                        //响应头
                        string heads = SeekpicResponse.Headers.ToString();
                        //cook信息
                        string cooks = SeekpicResponse.Headers["Set-Cookie"];
                        string[] strArray = cooks.Split(new string[] { "uid=" }, StringSplitOptions.RemoveEmptyEntries);
                        //uid信息
                        string uid = strArray[1].Substring(0, 32);
                        //token
                        string token = cooks.Substring(10, 24);
                        //获取验证码图片的请求，取得验证码id后取得验证码并进行图像识别
                        string picUrl = AppConfig.GetAppConfig("prepicurl") + uid;
                        System.Net.WebRequest GetpicRequest = System.Net.WebRequest.Create(picUrl);
                        System.Net.WebResponse GetpicResponse = GetpicRequest.GetResponse();
                        //获取图片流
                        System.IO.Stream stream = GetpicResponse.GetResponseStream();
                        //img bmp 代表图片信息 可以解析
                        System.Drawing.Image img = System.Drawing.Image.FromStream(stream);
                        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(img);
                        List<tessnet2.Word> result = ocr.DoOCR(bmp, Rectangle.Empty);//执行识别操作
                        string code = result[0].Text;
                        string loginUri = AppConfig.GetAppConfig("loginuri");
                        //用户名
                        string username = AppConfig.GetAppConfig("account");
                        //密码123456 加密过的 加密方式不明。           
                        string password = AppConfig.GetAppConfig("password");
                        //authcode 要从图片中解析
                        string authcode = code;
                        //formDate 用户登录时需要的信息
                        string postDataStr = "username=" + username + "&password=" + password + "&authcode=" + authcode + "&uid=" + uid + "&_csrf=" + token;
                        //请求中加入以上信息，字符串拼接
                        string postUrl = loginUri + "?" + postDataStr;
                        //发送请求
                        System.Net.HttpWebRequest loginrequest = System.Net.HttpWebRequest.Create(postUrl) as System.Net.HttpWebRequest;
                        //登录时用Post请求
                        loginrequest.Method = "POST";
                        //响应
                        System.Net.HttpWebResponse loginresponse = (System.Net.HttpWebResponse)loginrequest.GetResponse();
                        //获取响应流 并从中获取信息
                        System.IO.Stream responseStream = loginresponse.GetResponseStream();
                        System.IO.StreamReader reader = new System.IO.StreamReader(responseStream, Encoding.GetEncoding("UTF-8"));
                        string srcString = reader.ReadToEnd();
                        string status = srcString.Substring(30, 2);
                        string codestatus = srcString.Substring(20, 6);
                        Console.WriteLine("当前时间为：" + DateTime.Now);
                        Console.WriteLine(srcString);
                        Console.WriteLine("------------------------------------------------------------------");
                        //检验错误代码
                        WriteLog.WriteError(srcString.ToString());
                        if (status != "成功")
                        {
                            j++;
                        }

                        else
                        {
                            j = 0;
                        }
                        if (j >= 5)
                        {
                            Console.WriteLine("check the web servlet");
                        }

                        //释放资源 
                        stream.Close();
                        responseStream.Close();
                        reader.Close();
                        SeekpicResponse.Close();
                        GetpicResponse.Close();
                        loginresponse.Close();
                        Thread.Sleep(sleepingtime);
                    }
                }
                catch (Exception ex)
                {
                    WriteLog.WriteError(ex.ToString());
                    try
                    {
                        string loginPageUrl = AppConfig.GetAppConfig("testweb");
                        System.Net.HttpWebRequest SeekpicRequest = System.Net.HttpWebRequest.Create(loginPageUrl) as System.Net.HttpWebRequest;
                        SeekpicRequest.Method = "GET";
                        //发送请求
                        System.Net.HttpWebResponse SeekpicResponse = (System.Net.HttpWebResponse)SeekpicRequest.GetResponse();
                        Console.WriteLine("web is broken");

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("fail to login baidu,check the internet");
                        WriteLog.WriteError("fail to login baidu,check the internet".ToString());
                    }
                }
            }
            public static string GetAppConfig(string strKey)
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                foreach (string key in config.AppSettings.Settings.AllKeys)
                {
                    if (key == strKey)
                    {
                        return config.AppSettings.Settings[strKey].Value.ToString();
                    }
                }
                return null;
            }

        }

        /*将程序运行错误打印到txt文档
         * @message 
         */
        public class WriteLog
        {
            private static StreamWriter streamWriter; //写文件  

            public static void WriteError(string message)
            {

                //string filePath = AppDomain.CurrentDomain.BaseDirectory + "Log";
                string filePath = AppConfig.GetAppConfig("logaddress") + "LoginLog";
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                string logPath = AppConfig.GetAppConfig("logaddress") + "LoginLog\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
                try
                {
                    using (StreamWriter sw = File.AppendText(logPath))
                    {
                        sw.WriteLine("消息：" + message);
                        sw.WriteLine("时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        sw.WriteLine("**************************************************");
                        sw.WriteLine();
                        sw.Flush();
                        sw.Close();
                        sw.Dispose();
                    }
                }
                catch (IOException e)
                {
                    using (StreamWriter sw = File.AppendText(logPath))
                    {
                        sw.WriteLine("异常：" + e.Message);
                        sw.WriteLine("时间：" + DateTime.Now.ToString("yyy-MM-dd HH:mm:ss"));
                        sw.WriteLine("**************************************************");
                        sw.WriteLine();
                        sw.Flush();
                        sw.Close();
                        sw.Dispose();
                    }
                }
            }
        }


    }

}


