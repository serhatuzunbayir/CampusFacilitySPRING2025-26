using CampusBooking.Desktop.Config;
using CampusBooking.Shared;
using Microsoft.Extensions.Options;

namespace CampusBooking.Desktop.Services;

public class NotificationPoller
{
    private readonly ApiClient _api;
    private readonly UserSession _session;
    private readonly System.Windows.Forms.Timer _timer;
    private int _lastUnread;

    // Subscribers (e.g. MainForm) attach here to raise toast popups when new notifications arrive.
    public event NotificationHandler? OnNotification;

    public NotificationPoller(ApiClient api, UserSession session, IOptions<ApiOptions> opts)
    {
        _api = api;
        _session = session;
        _timer = new System.Windows.Forms.Timer
        {
            Interval = Math.Max(5, opts.Value.PollingIntervalSeconds) * 1000
        };
        _timer.Tick += async (_, _) => await PollAsync();
    }

    public void Start()
    {
        if (!_session.IsLoggedIn) return;
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private async Task PollAsync()
    {
        try
        {
            var unread = await _api.GetUnreadNotificationsAsync();
            if (unread.Count > _lastUnread)
            {
                foreach (var n in unread.Take(unread.Count - _lastUnread))
                {
                    Enum.TryParse<NotificationKind>(n.Kind, out var kind);
                    OnNotification?.Invoke(kind, n.Message);
                }
            }
            _lastUnread = unread.Count;
        }
        catch
        {
            // network blips shouldn't kill the timer
        }
    }
}
