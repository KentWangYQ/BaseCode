﻿using Common.OpenAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Common.Util
{
    public class HttpHelper
    {
        #region Property
        protected HttpClient httpClient;
        protected string BaseUrl { get; set; }
        #endregion

        #region Init
        public HttpHelper()
        {
            httpClient = new HttpClient(new HttpClientHandler()
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true
            }); ;
        }

        public HttpHelper(string baseUrl)
        {
            this.BaseUrl = baseUrl;

            httpClient = new HttpClient(new HttpClientHandler()
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true
            })
            {
                BaseAddress = new Uri(this.BaseUrl)
            };
        }

        public HttpHelper(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
        #endregion

        #region HttpGet
        public HttpResponseMessage HttpGet(string api, dynamic parameters)
        {
            return this.HttpGetAsync(api, parameters).Result;
        }

        public Task<HttpResponseMessage> HttpGetAsync(string api, dynamic parameters)
        {
            var paramType = ((object)parameters).GetType();

            if (!(paramType.Name.Contains("AnonymousType") && paramType.IsSealed && paramType.Namespace == null))
                throw new ArgumentException("Only dynamic type is supported");

            var dict = paramType.GetProperties().Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string))
                .ToDictionary(k => k.Name, v => string.Format("{0}", v.GetValue(parameters, null)));

            return this.HttpGetAsync(api, dict);
        }

        public HttpResponseMessage HttpGet(string api, Dictionary<string, object> parameters = null)
        {
            return this.HttpGetAsync(api, parameters).Result;
        }

        public virtual Task<HttpResponseMessage> HttpGetAsync(string api, Dictionary<string, object> parameters = null)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();

            var queryString = string.Join("&",
                parameters.Where(p => p.Value == null || p.Value.GetType().IsValueType || p.Value.GetType() == typeof(string))
                .Select(p => string.Format("{0}={1}", Uri.EscapeDataString(p.Key), Uri.EscapeDataString(p.Value.ToString()))));

            api = string.Format("{1}{0}{2}", api.IndexOf("?") < 0 ? "?" : "&", api, queryString).Trim('&', '?');

            return httpClient.GetAsync(api);
        }
        #endregion

        #region HttpPost
        public HttpResponseMessage HttpPost(string api, dynamic parameters)
        {
            return HttpPostAsync(api, parameters).Result;
        }

        public Task<HttpResponseMessage> HttpPostAsync(string api, dynamic parameters)
        {
            var paramType = ((object)parameters).GetType();
            if (!(paramType.Name.Contains("AnonymousType") && paramType.IsSealed && paramType.Namespace == null))
            {
                throw new ArgumentException("只支持匿名类型参数");
            }

            var dict = paramType.GetProperties().ToDictionary(k => k.Name, v => v.GetValue((object)parameters, null));

            return HttpPostAsync(api, dict);
        }


        public HttpResponseMessage HttpPost(string api, Dictionary<string, object> parameters)
        {

            return HttpPostAsync(api, parameters).Result;
        }
        public virtual Task<HttpResponseMessage> HttpPostAsync(string api, Dictionary<string, object> parameters)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();

            var dict = new Dictionary<string, object>(parameters.ToDictionary(k => k.Key, v => v.Value));

            HttpContent httpContent = null;

            if (dict.Count(p => p.Value.GetType() == typeof(byte[]) || p.Value.GetType() == typeof(System.IO.FileInfo)) > 0)
            {
                var content = new MultipartFormDataContent();

                foreach (var param in dict)
                {
                    var dataType = param.Value.GetType();
                    if (dataType == typeof(byte[]))	//byte[]
                    {
                        content.Add(new ByteArrayContent((byte[])param.Value), param.Key, GetNonceString());
                    }
                    else if (dataType == typeof(MemoryFileContent))	//内存文件
                    {
                        var mcontent = (MemoryFileContent)param.Value;
                        content.Add(new ByteArrayContent(mcontent.Content), param.Key, mcontent.FileName);
                    }
                    else if (dataType == typeof(System.IO.FileInfo))	//本地文件
                    {
                        var file = (System.IO.FileInfo)param.Value;
                        content.Add(new ByteArrayContent(System.IO.File.ReadAllBytes(file.FullName)), param.Key, file.Name);
                    }
                    else /*if (dataType.IsValueType || dataType == typeof(string))*/	//其他类型
                    {
                        content.Add(new StringContent(string.Format("{0}", param.Value)), param.Key);
                    }
                }

                httpContent = content;
            }
            else
            {
                var content = new FormUrlEncodedContent(dict.ToDictionary(k => k.Key, v => string.Format("{0}", v.Value)));
                httpContent = content;
            }

            return httpClient.PostAsync(api, httpContent);
        }
        #endregion

        #region Common Method
        private string GetNonceString(int length = 8)
        {
            var sb = new StringBuilder();

            var rnd = new Random();
            for (var i = 0; i < length; i++)
            {

                sb.Append((char)rnd.Next(97, 123));

            }

            return sb.ToString();

        }
        #endregion
    }
}
