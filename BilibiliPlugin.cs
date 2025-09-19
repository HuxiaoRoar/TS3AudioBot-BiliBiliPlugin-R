using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.CommandSystem.Commands;
using TS3AudioBot.Plugins;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Reflection;

public class BilibiliPlugin : IBotPlugin
{
    private Ts3Client _ts3Client;
    private readonly PlayManager _playManager;
	private static readonly HttpClient http = new HttpClient();
	private static string cookieFile = "bili_cookie.txt";


    private static BilibiliVideoInfo lastSearchedVideo;
    private static List<BilibiliVideoInfo> lastHistoryResult;


    private static List<(string bvid, long cid, string title)> recentHistory =	new List<(string bvid, long cid, string title)>();

	public BilibiliPlugin(PlayManager playManager, Ts3Client ts3Client)
	{
		_playManager = playManager;
        _ts3Client =  ts3Client;
        http.DefaultRequestHeaders.Remove("Referer");
		http.DefaultRequestHeaders.Add("Referer", "https://www.bilibili.com");
		http.DefaultRequestHeaders.Remove("User-Agent");
		http.DefaultRequestHeaders.Add(
			"User-Agent",
			"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0"
		);
		LoadCookie();
	}

    public async void Initialize() {
        // 获取当前正在运行的程序集(也就是 BilibiliPlugin.dll)的版本信息
        var version = Assembly.GetExecutingAssembly().GetName().Version;

        // 将版本号格式化成 "主版本.次版本.生成号" 的形式，忽略最后的修订号
        string displayVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        await _ts3Client.SendChannelMessage($"Bilibili 插件加载完毕！当前版本：v{displayVersion}");

    }

	public void Dispose() { }

    //---------------------------------------------------------------新建类---------------------------------------------------------------//
    // 用于存储单个分P的信息
    public class VideoPartInfo
    {
        public long Cid { get; set; }//分P的CID
        public string Title { get; set; }//分P的标题
        public int Index { get; set; }//分P的索引，从1开始
    }

    // 用于存储整个视频的详细信息
    public class BilibiliVideoInfo
    {
        public string Bvid { get; set; }//bv
        public string Title { get; set; }//标题
        public string Uploader { get; set; }//up主
        public string CoverUrl { get; set; }//封面
        public List<VideoPartInfo> Parts { get; set; } = new List<VideoPartInfo>();
    }

    public class AudioStreamInfo
    {
        public List<string> Urls { get; set; } = new List<string>();
        public bool IsHiRes { get; set; } = false;
    }

    //---------------------------------------------------------------辅助方法---------------------------------------------------------------//
    private void LoadCookie()
	{
		if (File.Exists(cookieFile))
		{
			string cookie = File.ReadAllText(cookieFile);
			if (!string.IsNullOrWhiteSpace(cookie))
			{
				http.DefaultRequestHeaders.Remove("Cookie");
				http.DefaultRequestHeaders.Add("Cookie", cookie);
			}
		}
	}
	private string GetCookiePath(InvokerData invoker)
	{
		return $"bili_cookie_{invoker.ClientUid}.txt";
	}
	private void SetInvokerCookie(InvokerData invoker, HttpClient client)
	{
		string cookiePath = GetCookiePath(invoker);
		if (File.Exists(cookiePath))
		{
			string cookie = File.ReadAllText(cookiePath);
			if (!string.IsNullOrWhiteSpace(cookie))
			{
				client.DefaultRequestHeaders.Remove("Cookie");
				client.DefaultRequestHeaders.Add("Cookie", cookie);
			}
		}
	}
    private async Task<string> CheckLoginStatusAsync(string qrKey, InvokerData invoker)
    {
        string checkLoginUrl =
            $"https://passport.bilibili.com/x/passport-login/web/qrcode/poll?qrcode_key={qrKey}";
        string loginStatusResponse;
        bool isLoggedIn = false;
        int time = 0;

        while (isLoggedIn == false)
        {
            loginStatusResponse = await http.GetStringAsync(checkLoginUrl);
            JObject loginStatusJson = JObject.Parse(loginStatusResponse);
            string statusCode = (string)loginStatusJson["data"]?["code"];

            // 打印出登录状态响应
            Console.WriteLine(
                "Login Status Response: " + loginStatusResponse + "statuscode:" + statusCode
            );

            // 登录成功，返回状态码 0
            if (statusCode == "0")
            {
                string fullUrl = (string)loginStatusJson["data"]?["url"];
                isLoggedIn = true;
                string cookie;
                cookie = ExtractCookieFromUrl(fullUrl);
                if (string.IsNullOrWhiteSpace(cookie))
                {
                    return "登录成功，但无法获取Cookie信息。";
                }

                // 保存登录后的cookie信息
                string cookiePath = GetCookiePath(invoker);
                File.WriteAllText(cookiePath, cookie);
                return "扫码登录成功！已将登录信息保存。";
            }

            if (statusCode == "86038")
            {
                return "登录失败，二维码已超时";
            }

            if (time >= 30)
            {
                return "登录失败，超时";
            }

            await Task.Delay(2000); // 每2秒检查一次
            time++;
        }
        return "登录失败，请检查二维码是否已扫描并确认登录。";
    }
    private string ExtractCookieFromUrl(string fullUrl)
    {
        // fullUrl 格式：
        // https://passport.biligame.com/crossDomain?DedeUserID=***\u0026DedeUserID__ckMd5=***\u0026Expires=***\u0026SESSDATA=***\u0026bili_jct=***\u0026gourl=https%3A%2F%2Fpassport.bilibili.com

        try
        {
            Uri uri = new Uri(fullUrl);
            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);

            string sessData = queryParams["SESSDATA"];
            string biliJct = queryParams["bili_jct"];

            // 检查是否提取到了这两个参数，并返回 cookie 字符串
            if (!string.IsNullOrEmpty(sessData) && !string.IsNullOrEmpty(biliJct))
            {
                return $"SESSDATA={sessData};bili_jct={biliJct};";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error extracting cookie from URL: " + ex.Message);
        }

        return null; // 如果解析失败，返回 null
    }

    //---------------------------------------------------------------核心方法---------------------------------------------------------------//
    private async Task<string> EnqueueAudio(BilibiliVideoInfo videoInfo, VideoPartInfo partInfo, InvokerData invoker)
    {
        try
        {
            // 调用新方法获取音频流信息
            var streamInfo = await GetAudioStreamInfoAsync(videoInfo, partInfo, invoker);

            if (streamInfo.Urls == null || streamInfo.Urls.Count == 0)
            {
                return "未能获取到任何有效的音频链接。";
            }

            foreach (var url in streamInfo.Urls)
            {
                if (string.IsNullOrWhiteSpace(url))
                    continue;
                try
                {
                    string proxyUrl = $"http://localhost:32181/?{WebUtility.UrlEncode(url)}";
                    await _playManager.Enqueue(invoker, proxyUrl);
                    Console.WriteLine($"已通过代理加入队列：{proxyUrl}");
                    string qualityTag = streamInfo.IsHiRes ? " (Hi-Res)" : "";
                    string partTag = (!string.IsNullOrWhiteSpace(partInfo.Title)) ? $"（{partInfo.Index}P：{partInfo.Title}）" : "";
                    
                    await _ts3Client.SendChannelMessage($"{videoInfo.Bvid}{qualityTag} 添加成功！已将《{videoInfo.Title}》{partTag}添加到播放队列。");
                   // return $"{videoInfo.Bvid}{qualityTag} 添加成功！已将《{videoInfo.Title}》{partTag}添加到播放队列。";
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"添加到队列失败：{url}\n原因: {ex.Message}");
                }
            }

            return "所有音频链接加入队列失败。";
        }
        catch (Exception ex)
        {
            return "加入队列失败：" + ex.Message;
        }
    }

    private async Task<string> PlayAudio(BilibiliVideoInfo videoInfo, VideoPartInfo partInfo, InvokerData invoker)
    {
        try
        {
            var streamInfo = await GetAudioStreamInfoAsync(videoInfo, partInfo, invoker);

            if (streamInfo.Urls == null || streamInfo.Urls.Count == 0)
            {
                return "未能获取到任何有效的音频链接。";
            }


            // 使用本地代理进行播放
            foreach (var url in streamInfo.Urls)
            {
                if (string.IsNullOrWhiteSpace(url))
                    continue;
                try
                {
                    string proxyUrl = $"http://localhost:32181/?{WebUtility.UrlEncode(url)}";

                    await SetAvatarAsync(videoInfo.CoverUrl);
                    await SetBotNameAsync(videoInfo.Title);
                    await _playManager.Play(invoker, proxyUrl);
                    Console.WriteLine($"{videoInfo.Title}播放成功：{proxyUrl}");
                    string qualityTag = streamInfo.IsHiRes ? " (Hi-Res)" : "";
                    
                    string partTag = (!string.IsNullOrWhiteSpace(partInfo.Title))? $"（{partInfo.Index}P：{partInfo.Title}）" : "";
                    string partJump = (!string.IsNullOrWhiteSpace(partInfo.Title)) ? $"/?p={partInfo.Index}" : "";   
                    await _ts3Client.SendChannelMessage($"正在播放{qualityTag}：{videoInfo.Uploader} 投稿的《{videoInfo.Title}》{partTag}{System.Environment.NewLine}链接：https://www.bilibili.com/video/{videoInfo.Bvid}{partJump}");
                    return null;
                    //  return $"正在播放{qualityTag}：{videoInfo.Uploader} 投稿的《{videoInfo.Title}》{partTag}{System.Environment.NewLine}链接：https://www.bilibili.com/video/{videoInfo.Bvid}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"播放失败：{url}\n原因: {ex.Message}");
                }
            }

            return "所有音频链接播放失败。";
        }
        catch (Exception ex)
        {
            return "播放失败：" + ex.Message;
        }
    }



    //---------------------------------------------------------------虎啸添加---------------------------------------------------------------//
    private async Task<BilibiliVideoInfo> GetVideoInfo(string bvid)
    {
        try
        {
            string viewApi = $"https://api.bilibili.com/x/web-interface/view?bvid={bvid}";
            string viewJson = await http.GetStringAsync(viewApi);
            JObject viewData = JObject.Parse(viewJson)["data"] as JObject;

            if (viewData == null)
            {
                // 如果无法获取视频数据，可以抛出异常或返回 null
                throw new Exception("未获取到视频信息，请检查 BV 号是否正确。");
            }

            var videoInfo = new BilibiliVideoInfo
            {
                Bvid = viewData["bvid"]?.ToString(),
                Title = viewData["title"]?.ToString(),
                Uploader = viewData["owner"]?["name"]?.ToString(),
                CoverUrl = viewData["pic"]?.ToString()
            };

            JArray pages = viewData["pages"] as JArray;
            if (pages != null && pages.Count > 1)
            {
                // 多分P视频
                for (int i = 0; i < pages.Count; i++)
                {
                    videoInfo.Parts.Add(new VideoPartInfo
                    {
                        Cid = (long)pages[i]["cid"],
                        Title = pages[i]["part"]?.ToString(),
                        Index = i + 1
                    });
                }
            }
            else
            {
                // 单P视频
                videoInfo.Parts.Add(new VideoPartInfo
                {
                    Cid = (long)viewData["cid"],
                    Title = "", // 单P视频没有分P标题，直接使用主标题
                    Index = 1 
                });
            }

            return videoInfo;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取视频信息时出错 (bvid: {bvid}): {ex.Message}");
            // 向上抛出异常，让调用方处理
            throw;
        }
    }

    private async Task<string> SetAvatarAsync(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return "图片URL为空，无法设置头像。";
        }

        int size = 500; // 默认尺寸

        try
        {
            // 1. 不下载图片，仅获取图片信息流
            using (var response = await http.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    // 2. 使用 SixLabors.ImageSharp.Image.IdentifyAsync 获取尺寸
                    var imageInfo = await Image.IdentifyAsync(stream);
                    if (imageInfo != null)
                    {
                        // 3. 二者取最小值
                        size = Math.Min(imageInfo.Width, imageInfo.Height);
                    }
                    else
                    {
                        Console.WriteLine("警告：获取图片宽高失败，将使用默认尺寸 500。");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告：获取图片信息失败，将使用默认尺寸 500。原因: {ex.Message}");
            // 发生任何异常（如网络请求失败），都继续使用默认尺寸
        }

        // 4. 拼接B站特定格式的图片处理URL
        // B站的格式是 @<高度>h_<宽度>w_1c，我们要做成正方形，所以宽高用同一个最小值
        string formattedUrl = $"{imageUrl}@{size}h_{size}w_1c";

        try
        {

            await MainCommands.CommandBotAvatarSet(_ts3Client, formattedUrl);
            return null;

        }
        catch (Exception ex)
        {
            return $"错误：修改头像失败。原因: {ex.Message}";
        }
    }

    private async Task<string> SetBotNameAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "标题为空，无法设置机器人名称。";
        }

        try
        {
            // 1. 检查标题长度，如果超过30个字符，则截断为27个字符并加上"..."
            string botName = title.Length > 30 ? title.Substring(0, 27) + "..." : title;

            // 2. 调用客户端API修改名称 
             await _ts3Client.ChangeName(botName);
            // 3. 成功后，返回 null
            return null;
        }
        catch (Exception ex)
        {
            // 4. 失败时，返回错误信息
            return $"错误：修改机器人名称失败。原因: {ex.Message}";
        }
    }

    private async Task<AudioStreamInfo> GetAudioStreamInfoAsync(BilibiliVideoInfo videoInfo, VideoPartInfo partInfo, InvokerData invoker)
    {
        var streamInfo = new AudioStreamInfo();

        try
        {
            // 1. 创建一个临时的、携带用户个人Cookie的HttpClient
            var userClient = new HttpClient();
            userClient.DefaultRequestHeaders.Add("Referer", "https://www.bilibili.com");
            userClient.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/128.0.0.0"
            );
            // 调用我们现有的 SetInvokerCookie 方法来设置当前用户的 Cookie
            SetInvokerCookie(invoker, userClient);

            // 2. 使用这个临时客户端请求播放链接API
            string playApi = $"https://api.bilibili.com/x/player/playurl?cid={partInfo.Cid}&bvid={videoInfo.Bvid}&fnval=16&fourk=1";
            string playJson = await userClient.GetStringAsync(playApi);
            JObject playData = JObject.Parse(playJson);
            //Console.WriteLine(playApi);


            // 3. 优先尝试获取 Hi-Res (flac) 音频            
            var flacAudio = playData.SelectToken("data.dash.flac.audio") as JObject;
            if (flacAudio != null && flacAudio.HasValues)
            {
                Console.WriteLine("发现 Hi-Res 音频流，优先尝试...");
                streamInfo.IsHiRes = true;
                // 提取所有可能的URL
                if (flacAudio["baseUrl"] != null) streamInfo.Urls.Add(flacAudio["baseUrl"].ToString());
                if (flacAudio["backupUrl"] is JArray backupUrls) streamInfo.Urls.AddRange(backupUrls.Select(u => u.ToString()));
            }

            // 4. 如果没有Hi-Res，则回退到获取普通的DASH音频
            if (!streamInfo.IsHiRes == true) Console.WriteLine("正在获取标准 DASH 音频...");
                JArray audioArray = playData["data"]?["dash"]?["audio"] as JArray;
                if (audioArray != null)
                {
                    // 选择码率最高的音轨
                    JObject bestAudio = audioArray.OrderByDescending(a => (long)a["bandwidth"]).FirstOrDefault() as JObject;
                    if (bestAudio != null)
                    {
                        // 提取所有可能的URL
                        if (bestAudio["baseUrl"] != null) streamInfo.Urls.Add(bestAudio["baseUrl"].ToString());
                        if (bestAudio["backupUrl"] is JArray backupUrls) streamInfo.Urls.AddRange(backupUrls.Select(u => u.ToString()));
                    }
                }
            
            return streamInfo;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取音频流信息失败: {ex.Message}");
            // 即使失败，也返回一个空的 streamInfo 对象，避免后续代码出错
            return streamInfo;
        }
    }
    private async Task<string> HandleVideoRequest(InvokerData invoker,string input,
        Func<BilibiliVideoInfo, VideoPartInfo, InvokerData, Task<string>> actionAsync
    )
    {
        string bvid = input;
        int requestedPartIndex = -1;

        // 1. 解析输入，判断是否为 BVxxxx-P 的格式
        if (input.Contains("-"))
        {
            var parts = input.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int pIndex))
            {
                bvid = parts[0];
                requestedPartIndex = pIndex;
            }
        }

        try
        {
            // 2. 获取视频信息
            var videoInfo = await GetVideoInfo(bvid);
            lastSearchedVideo = videoInfo; // 缓存视频信息，以便二次选择

            // 3. 根据解析结果和视频信息执行不同逻辑

            // --- 情况A: 用户指定了分P (输入了 BVxxxx-P) ---
            if (requestedPartIndex > 0)
            {
                // 尝试找到用户指定的分P
                var partToProcess = videoInfo.Parts.FirstOrDefault(p => p.Index == requestedPartIndex);

                if (partToProcess != null)
                {
                    // 1. 该视频有对应分P，直接执行动作（播放或入队）
                    return await actionAsync(videoInfo, partToProcess, invoker);
                }
                else
                {
                    if (videoInfo.Parts.Count == 1)
                    {
                        // 2. 视频只有1P，但用户指定了无效P数
                        string response = $"该视频只有一个分P，即将为您播放。\n\n";
                        response += await actionAsync(videoInfo, videoInfo.Parts.First(), invoker);
                        return response;
                    }
                    else
                    {
                        // 3. 视频有分P，但输入数字不在范围
                        string reply = $"分P选择错误！您输入的 ‘{requestedPartIndex}’ 不在有效范围内 (1 - {videoInfo.Parts.Count})。\n";
                        reply += $"视频《{videoInfo.Title}》包含 {videoInfo.Parts.Count} 个分P：\n";
                        foreach (var part in videoInfo.Parts)
                        {
                            reply += $"{part.Index}. {part.Title}\n";
                        }
                        reply += $"\n请使用命令 !b vp [编号] 或 !b addp [编号] 重新选择。";
                        return reply;
                    }
                }
            }
            // --- 情况B: 用户未指定分P (只输入了 BVxxxx) ---
            else
            {
                if (videoInfo.Parts.Count > 1)
                {
                    // 逻辑不变：多P视频，返回列表让用户二次选择
                    string reply = $"视频《{videoInfo.Title}》包含 {videoInfo.Parts.Count} 个分P：\n";
                    foreach (var part in videoInfo.Parts)
                    {
                        reply += $"{part.Index}. {part.Title}\n";
                    }
                    reply += "\n请使用命令 !b vp [编号] 播放对应分P，或使用 !b addp [编号] 添加到队列。";
                    return reply;
                }
                else
                {
                    // 逻辑不变：单P视频，直接执行动作
                    return await actionAsync(videoInfo, videoInfo.Parts.First(), invoker);
                }
            }
        }
        catch (Exception ex)
        {
            return "处理视频请求时出错：" + ex.Message;
        }
    }



    //---------------------------------------------------------------指令块---------------------------------------------------------------//


    [Command("b status")]
    public async Task<string> BilibiliStatus(InvokerData invoker)
    {
        // 准备一个临时的 HttpClient 来发送请求
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Referer", "https://www.bilibili.com");
        client.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/128.0.0.0"
        );

        SetInvokerCookie(invoker, client);
        Console.WriteLine(invoker.ClientUid);
        string proxyStatus;
        try
        {
            // 尝试快速连接代理来检查其状态
            using (var proxyClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
            {
                var response = await proxyClient.GetAsync("http://localhost:32181/");
                proxyStatus = "http://localhost:32181/ (运行中)";
            }
        }

        catch (Exception)
        {
            proxyStatus = "未启动代理，建议启动！";
        }

        try
        {
            // 访问 Bilibili API
            string navJson = await client.GetStringAsync("https://api.bilibili.com/x/web-interface/nav");
            JObject navData = JObject.Parse(navJson);

            var reply = new System.Text.StringBuilder();
            reply.AppendLine("B站登录状态");
            reply.AppendLine();
            reply.AppendLine($"代理接口：{proxyStatus}");
            bool isLogin = false;
            
            JObject data = null;

            if (navData["code"]?.ToString() == "0" && navData["data"] != null)
            {
                data = navData["data"] as JObject;
                isLogin = (bool?)data?["isLogin"] ?? false;
            

            }
            if (isLogin)
            {
                string uname = data["uname"]?.ToString() ?? "未知用户";
                string mid = data["mid"]?.ToString() ?? "N/A";
                string vipLabel = data["vip_label"]?["text"]?.ToString();
                long vipDueDateUnix = (long?)data["vipDueDate"] ?? 0;

                reply.Append($"当前用户：{uname} [https://space.bilibili.com/{mid}]");

                if (!string.IsNullOrEmpty(vipLabel))
                {
                    reply.AppendLine(); // 换行
                    string dueDateString = "N/A";
                    if (vipDueDateUnix > 0)
                    {
                        // 从Unix毫秒时间戳转换为正常日期
                        DateTimeOffset dueDate = DateTimeOffset.FromUnixTimeMilliseconds(vipDueDateUnix);
                        dueDateString = dueDate.ToString("yyyy-MM-dd");
                    }
                    reply.Append($"会员状态：{vipLabel} (到期：{dueDateString})");
                }
            }
            else
            {
                reply.Append("当前用户：未登录");
            }

            return reply.ToString();
        }
        catch (Exception ex)
        {
            return $"检查状态时发生错误：{ex.Message}";
        }
    }

    [Command("b qr")]
	public async Task<string> BilibiliQrLogin(InvokerData invoker)
	{
		try
		{
			// 1. 请求生成二维码的key
			string keyUrl = "https://passport.bilibili.com/x/passport-login/web/qrcode/generate";
			string keyResponse = await http.GetStringAsync(keyUrl);
			JObject keyJson = JObject.Parse(keyResponse);

			string qrKey = keyJson["data"]?["qrcode_key"]?.ToString();
			string qrUrl = keyJson["data"]?["url"]?.ToString();

			Console.WriteLine(
				"Key Response: " + keyResponse + "\n qrUrl:" + qrUrl + "\n qrKey:" + qrKey
			);

			if (string.IsNullOrEmpty(qrUrl))
			{
				return "获取二维码失败，请稍后再试，返回的Url为空。";
			}

			qrUrl = qrUrl.Replace(@"\u0026", "&"); // 解决\u0026替换问题

			// 生成二维码
			var qrCodeUrl =
				$"[URL]https://api.2dcode.biz/v1/create-qr-code?data={Uri.EscapeDataString(qrUrl)}[/URL]";

			//轮询
			_ = CheckLoginStatusAsync(qrKey, invoker);
			// 3. 返回二维码图片的URL或其他方式将二维码显示给用户
			// 如果是发送二维码图片到 TS3AudioBot，可以将 qrCodeUrl 提供给用户
			return $"请扫描二维码进行登录： {qrCodeUrl}";
		}
		catch (Exception ex)
		{
			return $"二维码登录失败：{ex.Message}";
		}
	}

	[Command("b login")]
	public async Task<string> BilibiliLogin(InvokerData invoker, string cookie)
	{
		if (string.IsNullOrWhiteSpace(cookie))
			return "用法: !b login [SESSDATA=xxx; bili_jct=xxx; ...]";

		string cookiePath = GetCookiePath(invoker);
		File.WriteAllText(cookiePath, cookie);

		try
		{
			// 使用新的 HttpClient（避免 Header 污染）
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Cookie", cookie);

			string userJson = await client.GetStringAsync(
				"https://api.bilibili.com/x/web-interface/nav"
			);
			JObject userObj = JObject.Parse(userJson);
			string uname = userObj["data"]?["uname"]?.ToString();

			if (!string.IsNullOrEmpty(uname))
				return $"登录成功，账号绑定为：{uname}";

			return "Cookie 已设置，但未能确认登录状态，请检查 Cookie 是否有效。";
		}
		catch (Exception ex)
		{
			return "登录状态确认失败：" + ex.Message;
		}
	}



	[Command("b history")]
	public async Task<string> BilibiliHistory(InvokerData invoker)
	{
		try
		{
			// 新建HttpClient，避免全局Cookie污染
			var client = new HttpClient();
			string cookiePath = GetCookiePath(invoker);
			if (File.Exists(cookiePath))
			{
				string cookie = File.ReadAllText(cookiePath);
				if (!string.IsNullOrWhiteSpace(cookie))
				{
					client.DefaultRequestHeaders.Add("Cookie", cookie);
				}
			}

            string url = "https://api.bilibili.com/x/web-interface/history/cursor?ps=10&type=archive";
			string json = await client.GetStringAsync(url);
			JObject data = JObject.Parse(json)["data"] as JObject;
			JArray list = data?["list"] as JArray;

			if (list == null || list.Count == 0)
				return "未获取到历史记录，请确认当前用户是否登录账号。";

            lastHistoryResult = new List<BilibiliVideoInfo>();
            string reply = "最近观看的视频：\n";

			for (int i = 0; i < list.Count; i++)
			{
				JObject item = (JObject)list[i];
				JObject history = item["history"] as JObject;

                // 提取分P标题，优先使用 show_title
                string partTitle = item["show_title"]?.ToString();

                long pageNumber = (long?)history?["page"] ?? 0;

                // 创建并填充视频信息对象
                var videoInfo = new BilibiliVideoInfo
                {
                    Bvid = history?["bvid"]?.ToString(),
                    Title = item["title"]?.ToString(),
                    Uploader = item["author_name"]?.ToString(),
                    CoverUrl = item["cover"]?.ToString(),
                    // 历史记录只关心播放的那一个P

                    Parts = new List<VideoPartInfo>
                {
                    new VideoPartInfo
                    {
                        Cid = (long?)history?["cid"] ?? 0,
                        Title = partTitle,
                        Index = (int)pageNumber
                    }
                }
                };

                // 检查关键信息是否存在
                if (!string.IsNullOrWhiteSpace(videoInfo.Bvid) && videoInfo.Parts.First().Cid > 0)
                {
                    lastHistoryResult.Add(videoInfo);

                    // 根据您的要求格式化输出
                    string displayTitle = videoInfo.Title;
                    // 如果分P标题和主标题不同，则拼接显示
                    if (!string.IsNullOrWhiteSpace(partTitle) )
                    {
                        displayTitle += $"({pageNumber}P：{partTitle})";
                    }
                    reply += $"{lastHistoryResult.Count}. {displayTitle}\n";
                }
            }
            reply += "\n使用 !b h [编号] 播放对应视频。\n使用 !b addh [编号] 添加到下一播放。";
            return reply;
            
        }
        catch (Exception ex)
		{
			return "获取历史记录失败：" + ex.Message;
		}
	}

	[Command("b h")]
	public async Task<string> BilibiliHistoryPlay(InvokerData invoker, int index)
	{
        
        if (lastHistoryResult == null || lastHistoryResult.Count == 0)
                return "历史记录为空，请先使用 !b history 加载最近观看的视频。";

            if (index < 1 || index > lastHistoryResult.Count)
                return $"请输入有效编号（1 - {lastHistoryResult.Count}）。";

            // 根据索引直接获取完整的 BilibiliVideoInfo 对象
            var videoToPlay = lastHistoryResult[index - 1];
            // 因为我们知道 Parts 列表里只有一个元素，直接取 First()
            var partToPlay = videoToPlay.Parts.First();

            // 将完整的对象传递给 PlayAudio
            return await PlayAudio(videoToPlay, partToPlay, invoker);
  	}

	[Command("b addh")]
	public async Task<string> BilibiliHistoryAdd(InvokerData invoker, int index)
	{
        if (lastHistoryResult == null || lastHistoryResult.Count == 0)
            return "历史记录为空，请先使用 !b history 加载最近观看的视频。";

        if (index < 1 || index > lastHistoryResult.Count)
            return $"请输入有效编号（1 - {lastHistoryResult.Count}）。";

        var videoToAdd = lastHistoryResult[index - 1];
        var partToAdd = videoToAdd.Parts.First();

        // 将完整的对象传递给 EnqueueAudio
        return await EnqueueAudio(videoToAdd, partToAdd, invoker);
    }
 
	[Command("b v")]
	public async Task<string> BilibiliBvCommand(InvokerData invoker, string input)
	{
        if (string.IsNullOrWhiteSpace(input))
            return "请提供 BV 号，例如：!b v BV1UT42167xb 或 !b v BV1UT42167xb-3";

        // 调用新的核心处理方法，并把“播放”这个动作传进去
        return await HandleVideoRequest(invoker, input, PlayAudio);

    }

	[Command("b vp")]
	public async Task<string> BilibiliPlayPart(InvokerData invoker, int partIndex)
	{
        if (lastSearchedVideo == null)
            return "请先使用 !b v [BV号] 获取视频信息。";

        var partToPlay = lastSearchedVideo.Parts.FirstOrDefault(p => p.Index == partIndex);
        if (partToPlay == null)
            return $"请输入有效编号（1 - {lastSearchedVideo.Parts.Count}）。";

        return await PlayAudio(lastSearchedVideo, partToPlay, invoker);
    }

	[Command("b add")]
	public async Task<string> BilibiliAddCommand(InvokerData invoker, string input)
	{
        if (string.IsNullOrWhiteSpace(input))
            return "请提供 BV 号，例如：!b add BV1UT42167xb 或 !b add BV1UT42167xb-3";

        // 调用新的核心处理方法，并把“添加到队列”这个动作传进去
        return await HandleVideoRequest(invoker, input, EnqueueAudio);
    }

	[Command("b addp")]
	public async Task<string> BilibiliAddPart(InvokerData invoker, int partIndex)
	{
        if (lastSearchedVideo == null)
            return "请先使用 !b add [BV号] 获取视频信息。";

        var partToAdd = lastSearchedVideo.Parts.FirstOrDefault(p => p.Index == partIndex);
        if (partToAdd == null)
            return $"请输入有效编号（1 - {lastSearchedVideo.Parts.Count}）。";

        return await EnqueueAudio(lastSearchedVideo, partToAdd, invoker);
    }

}
