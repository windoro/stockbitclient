using PuppeteerSharp;

namespace StockbitClient
{
    public class Stockbit : IStockbit
    {
        private string accessToken;
        private string refreshToken;

        public Stockbit()
        {
            accessToken = string.Empty;
            refreshToken = string.Empty;
        }

        public async Task Login(string username, string password)
        {
            await new BrowserFetcher().DownloadAsync();

            var launchOptions = new LaunchOptions
            {
                Headless = false, // should be false, because we need to see the captcha
            };

            using var browser = await Puppeteer.LaunchAsync(launchOptions);
            using var page = await browser.NewPageAsync();

            // Disable `navigator.webdriver`
            await page.EvaluateFunctionOnNewDocumentAsync(@"() => {
                Object.defineProperty(navigator, 'webdriver', {
                    get: () => false
                });
            }");

            // Listen for network responses
            page.Response += async (sender, e) =>
            {
                var url = e.Response.Url;
                await GetTokenAsync(e, url);
            };

            await Navigate(page, "https://stockbit.com/login");
            await PutCredentialAndLogin(username, password, page);
            await HandleCaptcha(page);

            Thread.Sleep(10000);

            await browser.CloseAsync();
        }

        private async Task GetTokenAsync(ResponseCreatedEventArgs e, string url)
        {
            if (url.Contains("/api/login/email"))
            {
                var status = e.Response.Status;
                Console.WriteLine($"Login API called with status: {status}");
                if (status == System.Net.HttpStatusCode.OK)
                {
                    var json = await e.Response.JsonAsync();
                    var root = json.RootElement;

                    if (root.TryGetProperty("data", out var data))
                    {
                        if (data.TryGetProperty("access", out var accessObj))
                        {
                            this.accessToken = accessObj.GetProperty("token").GetString();
                        }

                        if (data.TryGetProperty("refresh", out var refreshObj))
                        {
                            this.refreshToken = refreshObj.GetProperty("token").GetString();
                        }
                    }
                }
            }
        }

        private static async Task Navigate(IPage page, string url)
        {
            await page.GoToAsync(url, WaitUntilNavigation.Networkidle2);
        }

        private static async Task PutCredentialAndLogin(string username, string password, IPage page)
        {
            await page.TypeAsync("#username", username);
            await page.TypeAsync("#password", password);
            await page.ClickAsync("#email-login-button");
        }

        private static async Task HandleCaptcha(IPage page)
        {
            Thread.Sleep(20000);
            try
            {
                await page.WaitForFunctionAsync(
                    @"() => !document.body.innerText.includes('I\'m not a robot')",
                    new WaitForFunctionOptions { Timeout = 60000 } // 60 seconds timeout
                );

                var button = await page.QuerySelectorAsync("#email-login-button");
                if (button != null)
                {
                    await button.ClickAsync();
                }
            }
            catch (PuppeteerException ex)
            {
                throw new Exception("Captcha verification failed", ex);
            }
        }

        public string GetAccessToken()
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Access token is not set. Please login first.");
            }

            return accessToken;
        }

        public string GetRefreshToken()
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new InvalidOperationException("Access token is not set. Please login first.");
            }

            return refreshToken;
        }
    }
}
