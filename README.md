# PixivRssGen

一个运行在本机的Pixiv RSS源。登录您的账号后即会在`http://localhost/PixivRSS/`下提供RSS。
- `http://localhost/PixivRSS/Recommendation`：针对您的推荐；
- `http://localhost/PixivRSS/Following`：您关注的画师的新作品；

运行之前请确保正确配置了您的保留Url。在管理员权限的命令提示符或PowerShell中运行：`netsh http add urlacl url=http://+:80/PixivRSS user=[您的用户名]`。

注意：本程序暂时不保存您的登录凭证，因此每次使用均需要输入您的用户名和密码，并会收到一份来自Pixiv的提示邮件。