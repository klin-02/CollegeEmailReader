internal class ClickLinks
{
    private readonly HttpClient _httpClient;

    public ClickLinks(HttpClient httpClient) 
    { 
        _httpClient = httpClient;
    }

    public async Task ClickAsync(List<string> links)
    {
        Random random = new Random();

        foreach (var link in links)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(link);

            response.EnsureSuccessStatusCode();
            Console.WriteLine(response.StatusCode);

            Thread.Sleep(random.Next(1000, 5000));
        }
    }
}
