using BepInEx.Logging;
using ComponentAce.Compression.Libs.zlib;
using SIT.Tarkov.Core;
using SIT.Tarkov.Coop.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BackendConfig = GClass523;

namespace SIT.Coop.Core.Web
{
    public static class WebCallHelper
    {

		static BindingFlags privateFlags = BindingFlags.NonPublic | BindingFlags.Instance;
		static BindingFlags publicFlags = BindingFlags.Public | BindingFlags.Instance;

		static BindingFlags privateStaticFlags = BindingFlags.NonPublic | BindingFlags.Static;
		static BindingFlags publicStaticFlags = BindingFlags.Public | BindingFlags.Static;

		static string backendUrl = BackendConfig.Config.BackendUrl;

		static ManualLogSource Logger;
		static ManualLogSource GetLogger()
        {
            if(Logger == null)
				Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(ModulePatch));

			return Logger;

		}
		
		public static string GetBackEndUrl()
        {
			var coreBackendUrl = SIT.Tarkov.Core.PatchConstants.GetBackendUrl();

			GetLogger().LogInfo("Getting BackendUrl");
			GetLogger().LogInfo(backendUrl);

			return backendUrl;
			
		}


		private static Stream Send(string url, string method = "GET", string data = null, bool compress = true)
		{
			Debug.LogError($"[{method}] {url}");
			var backendUrl = GetBackEndUrl();
			if (string.IsNullOrEmpty(backendUrl))
				return null;

			string uriString = url;
			if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
			{
				uriString = GetBackEndUrl() + url;
			}
			UnityEngine.Debug.LogError(uriString);

			WebRequest webRequest = WebRequest.Create(new Uri(uriString));
			webRequest.Headers.Add("Cookie", "PHPSESSID=" + Tarkov.Coop.Core.PatchConstants.BackEndSession.Profile.AccountId);
			webRequest.Headers.Add("SessionId", Tarkov.Coop.Core.PatchConstants.BackEndSession.Profile.AccountId);
			webRequest.Headers.Add("Accept-Encoding", "deflate");
			webRequest.Method = method;
			if (method != "GET" && !string.IsNullOrEmpty(data))
			{
				byte[] array = (compress ? SimpleZlib.CompressToBytes(data, 9) : Encoding.UTF8.GetBytes(data));
				webRequest.ContentType = "application/json";
				webRequest.ContentLength = array.Length;
				if (compress)
				{
					webRequest.Headers.Add("Content-Encoding", "deflate");
				}
				using (Stream stream = webRequest.GetRequestStream())
				{
					stream.Write(array, 0, array.Length);
				}
			}
			try
			{
				return webRequest.GetResponse().GetResponseStream();
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
			return null;
		}

		public static string GetJson(string url, bool compress = true, string data = null)
		{
			using (Stream stream = Send(url, "GET", data, compress))
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					if (stream == null)
					{
						return "";
					}
					stream.CopyTo(memoryStream);
					return SimpleZlib.Decompress(memoryStream.ToArray());
				}
			}
		}

		public static string PostJson(string url, bool compress = true, string data = null)
		{
			using (Stream stream = Send(url, "POST", data, compress))
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					if (stream == null)
					{
						return "";
					}
					stream.CopyTo(memoryStream);
					return SimpleZlib.Decompress(memoryStream.ToArray());
				}
			}
		}

		public static async Task<string> PostJsonAsync(string url, bool compress = true, string data = null)
		{
			return await Task.Run(() => PostJson(url, compress, data));
		}
	}
}
