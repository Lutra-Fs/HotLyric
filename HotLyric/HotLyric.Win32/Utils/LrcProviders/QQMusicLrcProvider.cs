﻿using Kfstorm.LrcParser;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils.LrcProviders
{
    public class QQMusicLrcProvider : ILrcProvider
    {
        public string Name => "QQMusic";

        public async Task<LyricModel?> GetByIdAsync(object id, CancellationToken cancellationToken)
        {
            if (id is string _id && !string.IsNullOrEmpty(_id))
            {
                try
                {
                    var query = new Dictionary<string, string>()
                    {
                        ["songmid"] = _id,
                        ["pcachetime"] = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}",
                        ["g_tk"] = "5381",
                        ["loginUin"] = "0",
                        ["hostUin"] = "0",
                        ["inCharset"] = "utf8",
                        ["outCharset"] = "utf-8",
                        ["notice"] = "0",
                        ["platform"] = "yqq",
                        ["needNewCode"] = "0",
                        ["format"] = "json",
                    };

                    var queryStr = string.Join("&", query.Select(c => $"{Uri.EscapeDataString(c.Key)}={Uri.EscapeDataString(c.Value)}"));

                    var json = await LrcProviderHelper.TryGetStringAsync($"http://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?{queryStr}", "https://y.qq.com/portal/player.html", cancellationToken);
                    if (string.IsNullOrEmpty(json)) return null;

                    var jobj = JObject.Parse(json);
                    var lrcBase64 = (string?)jobj?["lyric"];
                    if (string.IsNullOrEmpty(lrcBase64)) return null;

                    var lrcContent = "";
                    try
                    {
                        lrcContent = Encoding.UTF8.GetString(Convert.FromBase64String(lrcBase64));
                    }
                    catch { }

                    if (string.IsNullOrEmpty(lrcContent)) return null;

                    try
                    {
                        lrcContent = System.Net.WebUtility.HtmlDecode(lrcContent);
                    }
                    catch { }

                    var lrcFile = LrcFile.FromText(lrcContent);

                    if (lrcFile != null)
                    {
                        string? translatedContent = "";
                        ILrcFile? translated = null;
                        try
                        {
                            var translatedBase64 = (string?)jobj?["trans"];
                            if (!string.IsNullOrEmpty(translatedBase64))
                            {
                                translatedContent = Encoding.UTF8.GetString(Convert.FromBase64String(translatedBase64));
                                if (!string.IsNullOrEmpty(translatedContent))
                                {
                                    translated = LrcFile.FromText(translatedContent);
                                    if (translated.Lyrics.All(c => string.IsNullOrEmpty(c.Content)))
                                    {
                                        translated = null;
                                        translatedContent = "";
                                    }
                                }
                            }
                        }
                        catch { }

                        return new LyricModel(lrcFile, translated, lrcContent, translatedContent);
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException)) { }
            }
            return null;
        }

        public async Task<object?> GetIdAsync(string name, string[]? artists, CancellationToken cancellationToken)
        {
            const int pageSize = 20;

            try
            {
                var searchKey = LrcProviderHelper.BuildSearchKey(name, artists);
                var key = LrcProviderHelper.GetSearchKey(searchKey);

                if (string.IsNullOrEmpty(key)) return null;

                // var json = await LrcProviderHelper.TryGetStringAsync($"http://c.y.qq.com/soso/fcgi-bin/client_search_cp?format=json&n={pageSize}&p=1&w={Uri.EscapeDataString(key)}&cr=1&g_tk=5381&t=0", "https://y.qq.com", cancellationToken);
                // test for aboard use of qqmusic. the link above will return 404 error.
                var json = await LrcProviderHelper.TryGetStringAsync($"https://c.y.qq.com/splcloud/fcgi-bin/smartbox_new.fcg?key={Uri.EscapeDataString(key)}", "https://y.qq.com", cancellationToken);

                if (string.IsNullOrEmpty(json)) return null;

                var jObj = JObject.Parse(json);
                if (jObj != null
                    && jObj.ContainsKey("code")
                    && jObj?["code"]?.Type == JTokenType.Integer
                    && jObj.Value<int>("code") == 0)
                {
                    var arr = jObj["data"]?["song"]?["itemlist"] as JArray;
                    if (arr != null && arr.Count > 0)
                    {
                        var musicInfos = arr.Select(c => (
                                id: c.Value<string?>("mid"),
                                name: c.Value<string?>("name"),
                                artists: (c.Value<string?>("singer")+"/").Split("/",StringSplitOptions.RemoveEmptyEntries)))
                            .Where(c => !string.IsNullOrEmpty(c.id) && !string.IsNullOrEmpty(c.name))
                            .Select(c => new LrcProviderHelper.MusicInfomation(c.id, c.name, c.artists))
                            .ToArray();

                        var info = LrcProviderHelper.GetMostSimilarMusicInfomation(key, musicInfos, (int)Math.Ceiling(key.Length / 6d));

                        return info?.Id;
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException)) { }

            return null;
        }

    }
}
