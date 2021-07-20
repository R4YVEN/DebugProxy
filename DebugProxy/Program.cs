using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace DebugProxy
{
    class Program
    {
        public static ProxyServer proxyServer = null;
        static void Main(string[] args)
        {
            Console.WriteLine("[SkillProxy V1.0]\n", Color.Lime);
            Console.WriteLine("[INFO] Creating new Proxy...", Color.White);
            proxyServer = new ProxyServer();
            //proxyServer.CertificateManager.TrustRootCertificate(true);
            proxyServer.BeforeRequest += OnRequest;
            proxyServer.BeforeResponse += OnResponse;

            Console.WriteLine("[INFO] Creating new EndPoint on Port 8000 & 8001...", Color.White);
            var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000, true);

            proxyServer.AddEndPoint(explicitEndPoint);

            Console.WriteLine("[INFO] Starting ProxyServer...", Color.White);
            proxyServer.Start();

            var transparentEndPoint = new TransparentProxyEndPoint(IPAddress.Any, 8001, true)
            {
                GenericCertificateName = "google.com"
            };

            Console.WriteLine("[INFO] Certificate: Google!", Color.White);

            Console.WriteLine("[INFO] Setting up...", Color.White);
            proxyServer.AddEndPoint(transparentEndPoint);
            proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
            proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);

            Console.WriteLine("[INFO] Proxy started!\n", Color.White);

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

            Console.Read();
            proxyServer.BeforeRequest -= OnRequest;
            proxyServer.AfterResponse -= OnResponse;
            proxyServer.Stop();
        }
        public static async Task OnRequest(object sender, SessionEventArgs e)
        {
            var requestHeaders = e.HttpClient.Request.Headers;

            var method = e.HttpClient.Request.Method.ToUpper();
            if ((method == "GET" || method == "POST" || method == "PUT" || method == "PATCH"))
            {
                Console.WriteLine("[" + method + "] " + e.HttpClient.Request.Url);
                byte[] bodyBytes = await e.GetRequestBody();
                e.SetRequestBody(bodyBytes);

                string bodyString = await e.GetRequestBodyAsString();
                e.SetRequestBodyString(bodyString);

                e.UserData = e.HttpClient.Request;
            }
        }

        public static async Task OnResponse(object sender, SessionEventArgs e)
        {
            // read response headers
            var responseHeaders = e.HttpClient.Response.Headers;
            //if (!e.ProxySession.Request.Host.Equals("medeczane.sgk.gov.tr")) return;
            if (e.HttpClient.Request.Method == "GET" || e.HttpClient.Request.Method == "POST")
            {
                if (e.HttpClient.Response.StatusCode == 200)
                {
                    if (e.HttpClient.Response.ContentType != null && e.HttpClient.Response.ContentType.Trim().ToLower().Contains("text/html"))
                    {
                        if(e.HttpClient.Request.Url.Contains("api.intruderfps.com/rooms"))
                        {
                            Console.WriteLine("FOUND ROOM REQUEST");
                            string body = await e.GetResponseBodyAsString();
                            body = body.Replace("limitSpectating\":true", "limitSpectating\":false");
                            e.SetResponseBodyString(body);
                        }
                        
                    }
                }
            }

            if (e.UserData != null)
            {
                var request = (Request)e.UserData;
            }
        }

        

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "abdfe0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static async Task<string> betweenStrings(String text, String start, String end)
        {
            int p1 = text.IndexOf(start) + start.Length;
            int p2 = text.IndexOf(end, p1);

            if (end == "") return (text.Substring(p1));
            else return text.Substring(p1, p2 - p1);
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            proxyServer.Stop();
        }
    }
}
