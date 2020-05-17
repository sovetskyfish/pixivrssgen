using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace PixivRssGen
{
    class Program
    {
        [ServiceContract]
        public interface IPixivRecommendation
        {
            [OperationContract]
            [WebGet]
            Task<Rss20FeedFormatter> Recommendation();

            [OperationContract]
            [WebGet]
            Task<Rss20FeedFormatter> Following();

            [OperationContract]
            [WebGet(UriTemplate = "Image/{*Path}")]
            Task<Stream> Image(string Path);
        }

        public class PixivRecommendationService : IPixivRecommendation
        {
            public async Task<Stream> Image(string Path)
            {
                return await (await api.RequestCall("GET", $"https://i.pximg.net/{Path}",
                    new Dictionary<string, string>() { { "Referer", "https://app-api.pixiv.net/" } })).
                    Content.ReadAsStreamAsync();
            }

            public async Task<Rss20FeedFormatter> Following()
            {
                SyndicationFeed feed = new SyndicationFeed("Pixiv关注动态", "此RSS Feed展示用户关注的画师的新作品", new Uri("http://localhost:80/PixivRSS/Following"));
                List<SyndicationItem> items = new List<SyndicationItem>();
                //为RSS源添加内容
                var recommendation = await new PixivCS.PixivAppAPI(api).GetIllustFollowAsync();
                foreach (var item in recommendation.Illusts)
                {
                    Uri validImage = new Uri($"http://localhost:80/PixivRSS/Image/{new Uri("https://i.pximg.net/").MakeRelativeUri(item.ImageUrls.Medium)}");
                    SyndicationItem syndicationItem = new SyndicationItem
                        (
                            item.Title,
                            $"<img src=\"{validImage}\" alt=\"{item.Title}\"/><br/>{item.Caption}",
                            validImage,
                            item.Id.ToString(),
                            DateTimeOffset.Parse(item.CreateDate)
                        );
                    items.Add(syndicationItem);
                }
                feed.Items = items;
                Console.WriteLine($"提供了{items.Count}条关注动态");
                return new Rss20FeedFormatter(feed);
            }

            public async Task<Rss20FeedFormatter> Recommendation()
            {
                SyndicationFeed feed = new SyndicationFeed("Pixiv推荐", "此RSS Feed展示针对用户的Pixiv图片推荐", new Uri("http://localhost:80/PixivRSS/Recommendation"));
                List<SyndicationItem> items = new List<SyndicationItem>();
                //为RSS源添加内容
                var recommendation = await new PixivCS.PixivAppAPI(api).GetIllustRecommendedAsync();
                foreach (var item in recommendation.Illusts)
                {
                    Uri validImage = new Uri($"http://localhost:80/PixivRSS/Image/{new Uri("https://i.pximg.net/").MakeRelativeUri(item.ImageUrls.Medium)}");
                    SyndicationItem syndicationItem = new SyndicationItem
                        (
                            item.Title,
                            $"<img src=\"{validImage}\" alt=\"{item.Title}\"/><br/>{item.Caption}",
                            validImage,
                            item.Id.ToString(),
                            DateTimeOffset.Now
                        );
                    items.Add(syndicationItem);
                }
                feed.Items = items;
                Console.WriteLine($"提供了{items.Count}条推荐");
                return new Rss20FeedFormatter(feed);
            }
        }

        static PixivCS.PixivBaseAPI api = new PixivCS.PixivBaseAPI();

        public class Host
        {
            static async Task Main(string[] args)
            {
                Console.Write("请输入您的Pixiv用户名：");
                var username = Console.ReadLine();
                Console.Write("请输入您的Pixiv密码：");
                string password = "";
                do
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        password += key.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                        {
                            password = password.Substring(0, (password.Length - 1));
                            Console.Write("\b \b");
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            Console.Write('\n');
                            break;
                        }
                    }
                } while (true);
                Console.WriteLine("正在登录...");
                api.ExperimentalConnection = true;
                PixivCS.Objects.AuthResult authRes;
                try
                {
                    authRes = await api.AuthAsync(username, password);
                }
                catch
                {
                    Console.WriteLine("登录失败，程序即将退出");
                    Console.ReadLine();
                    return;
                }
                Console.WriteLine($"登录成功；用于提供服务的Pixiv用户名为：{authRes.Response.User.Account}");
                Uri baseAddress = new Uri("http://localhost:80/PixivRSS");
                WebServiceHost svcHost = new WebServiceHost(typeof(PixivRecommendationService), baseAddress);
                try
                {
                    svcHost.Open();
                    Console.WriteLine("服务正在运行");
                }
                catch (CommunicationException e)
                {
                    Console.WriteLine("引发了异常：{0}", e.Message);
                    svcHost.Abort();
                }
                Console.WriteLine("输入“quit”终止程序");
                while (Console.ReadLine() != "quit") ;
                Console.WriteLine("程序正在清理...");
                svcHost.Close();
            }
        }
    }
}