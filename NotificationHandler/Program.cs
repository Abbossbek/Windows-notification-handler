using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;

using NotificationHandler;

using Windows.ApplicationModel.Background;
using Windows.Foundation.Metadata;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

internal class Program
{
    static List<UserNotification> notifications = new List<UserNotification>();
    static Config config;
    private static void Main(string[] args)
    {
        try
        {
            config = JsonSerializer.Deserialize<Config>(File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}config.json"));
            Listen().Wait();
        }
        catch
        {
            Console.WriteLine("Couldn't read config file!");
        }
    }

    private static async Task Listen()
    {
        if (ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
        {
            // Get the listener
            UserNotificationListener listener = UserNotificationListener.Current;
            // And request access to the user's notifications (must be called from UI thread)
            UserNotificationListenerAccessStatus accessStatus = await listener.RequestAccessAsync();

            switch (accessStatus)
            {
                // This means the user has granted access.
                case UserNotificationListenerAccessStatus.Allowed:
                    while (true)
                    {
                        IEnumerable<UserNotification> notifs = await listener.GetNotificationsAsync(NotificationKinds.Toast);
                        var newNots = notifs.Where(x => !notifications.Any(y => y.Id == x.Id)).ToList();
                        if (newNots.Any())
                        {
                            foreach (var item in newNots)
                            {
                                await Task.Delay(2000);
                                if (item.AppInfo.DisplayInfo.DisplayName.ToLower().Contains("microsoft teams"))
                                    NotificationReseived(item);
                            }
                            notifications = notifs.ToList();
                        }
                        await Task.Delay(500);
                    }
                case UserNotificationListenerAccessStatus.Denied:
                    break;
                case UserNotificationListenerAccessStatus.Unspecified:
                    break;
            }
        }
        else
        {
            Console.WriteLine("Older version of Windows, no Listener");
        }
    }
    private static void NotificationReseived(UserNotification notif)
    {
        NotificationBinding toastBinding = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
        if (toastBinding != null)
        {
            IReadOnlyList<AdaptiveNotificationText> textElements = toastBinding.GetTextElements();
            string titleText = textElements.FirstOrDefault()?.Text;
            string bodyText = string.Join("\n", textElements.Skip(1).Select(t => t.Text));
            Console.WriteLine(notif.AppInfo.DisplayInfo.DisplayName);
            Console.WriteLine(titleText);
            Console.WriteLine(bodyText);
            Console.WriteLine();

            SendToApi(titleText, bodyText);
        }
    }

    private static async void SendToApi(string? titleText, string bodyText)
    {
        try
        {
            HttpClientHandler handler = new HttpClientHandler();
            IWebProxy proxy = WebRequest.GetSystemWebProxy();
            proxy.Credentials = CredentialCache.DefaultCredentials;
            handler.Proxy = proxy;

            using (HttpClient client = new(handler))
            using (HttpRequestMessage request = new(HttpMethod.Post, config.Url))
            {

                NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
                outgoingQueryString.Add("token", config.Token);
                outgoingQueryString.Add("user", config.User);
                outgoingQueryString.Add("device", config.Device);
                outgoingQueryString.Add("title", titleText);
                outgoingQueryString.Add("message", bodyText);
                string postdata = outgoingQueryString.ToString();
                request.Content = new StringContent(postdata, Encoding.UTF8, "application/x-www-form-urlencoded");
                var result = await client.SendAsync(request);
                Console.WriteLine(await result.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}