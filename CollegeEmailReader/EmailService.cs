using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using MailKit.Net.Imap;
using MailKit;
using MailKit.Search;
using MimeKit;

internal class EmailService
{
    //Values from config
    private readonly string _username;
    private readonly string _password;
    private readonly List<string> _emails;

    //Services
    private readonly ClickLinks _clickLinks;


    public EmailService(IConfiguration configuration, ClickLinks clickLinks)
    {
        _username = configuration["Username"] ?? throw new Exception("\'Username\' is empty");
        _password = configuration["Password"] ?? throw new Exception("\'Password\' is empty");

        _emails = configuration.GetSection("Emails").Get<List<string>>() ?? throw new Exception("No emails listed!");
        _clickLinks = clickLinks;
    }

    public async Task DoDemonstratedInterestAsync()
    {
        using (ImapClient client = new ImapClient())
        {
            try
            {
                await client.ConnectAsync("imap.gmail.com", 993, true);
                await client.AuthenticateAsync(_username, _password);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to login!");
                return;
            }

            IMailFolder inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            List<UniqueId> uids = await GetUids(inbox);
            Console.WriteLine(uids.Count);

            //Mark them as read
            await ReadCollegeEmailsAsync(uids, inbox);

            //Get the message contents and links
            List<MimeMessage> messages = await GetMessagesAsync(uids, inbox);
            List<string> links = ExtractFirstLinks(messages);

            await _clickLinks.ClickAsync(links);
        }
    }

    private async Task<List<UniqueId>> GetUids(IMailFolder inbox)
    {
        List<UniqueId> uids = new List<UniqueId>();

        foreach (var email in _emails)
        {
            IList<UniqueId> emailUids = await inbox.SearchAsync(SearchQuery.FromContains(email).And(SearchQuery.NotSeen));
            uids.AddRange(emailUids);
        }
        return uids;
    }

    private async Task ReadCollegeEmailsAsync(List<UniqueId> uids, IMailFolder inbox) =>
        await inbox.AddFlagsAsync(uids, MessageFlags.Seen, true);

    private async Task<List<MimeMessage>> GetMessagesAsync(List<UniqueId> uids, IMailFolder inbox)
    {
        List<MimeMessage> messages = new List<MimeMessage>();

        foreach (var uid in uids)
        {
            MimeMessage message = await inbox.GetMessageAsync(uid);
            messages.Add(message);
        }
        return messages;
    }

    private List<string> ExtractFirstLinks(List<MimeMessage> messages)
    {
        //Get the technolutions links (which mark engagement)
        string pattern = @"https?://[^\s""'<>]*mx\.technolutions\.[a-z]{2,}(?![^\s""'<>]*unsubscribe)[^\s""'<>]*";
        Regex regex = new Regex(pattern);

        List<string> collegeLinks = new List<string>();

        foreach (var m in messages)
        {
            Console.WriteLine(m.HtmlBody);
            string link = regex.Match(m.HtmlBody).ToString();

            collegeLinks.Add(link);
        }
        return collegeLinks;
    }
}
