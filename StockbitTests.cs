using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StockbitClient
{
    [TestClass]
    public class StockbitTests
    {
        [TestMethod]
        public async Task TestLogin()
        {
            string username = "username";
            string password = "password";

            var client = new Stockbit();

            await client.Login(username, password);

            var accessToken = client.GetAccessToken();
            var refreshToken = client.GetRefreshToken();

            Assert.IsFalse(string.IsNullOrEmpty(accessToken), "Access token should not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(refreshToken), "Refresh token should not be empty");
        }
    }
}
