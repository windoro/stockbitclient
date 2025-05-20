namespace StockbitClient
{
    public interface IStockbit
    {
        Task Login(string username, string password);

        string GetAccessToken();
        string GetRefreshToken();
    }
}
