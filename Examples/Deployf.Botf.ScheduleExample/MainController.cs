﻿using SQLite;

namespace Deployf.Botf.ScheduleExample;

class MainController : BotControllerBase
{
    readonly TableQuery<User> _users;
    readonly SQLiteConnection _db;
    readonly ILogger<MainController> _logger;
    readonly BotfOptions _options;

    public MainController(TableQuery<User> users, SQLiteConnection db, ILogger<MainController> logger, BotfOptions options)
    {
        _users = users;
        _db = db;
        _logger = logger;
        _options = options;
    }


    [Action("/start", "start the bot")]
    public void Start()
    {
        PushL("Hello!");
        PushL("This bot allow you and users to book the time slot");
        PushL();
        PushL($"Link to book your free slots: https://t.me/{_options.Username}?start={FromId.Base64()}");
    }

    [Action("/timezone", "set language and timezone")]
    public void Timezone()
    {
        var user = _users.FirstOrDefault(c => c.Id == FromId);

        Push($"Current time zone: {user.Timezone}");

        Button("Russua", Q(SetTimezone, "ru"));
        Button("Ukraine", Q(SetTimezone, "ua"));
        Button("USA", Q(SetTimezone, "usa"));
    }

    [Action]
    public void SetTimezone(string zone)
    {
        var user = _users.First(c => c.Id == FromId);
        user.Timezone = zone;
        _db.Update(user);

        Push("Timezone changed");
    }


    // if user sent unknown action, say it to them
    [On(Handle.Unknown)]
    public void Unknown()
    {
        Push("Unknown command. Or use /start command");
    }

    // handle all messages before botf has processed it
    // and yes, action methods can be void
    [On(Handle.BeforeAll)]
    public void PreHandle()
    {
        // if user has never contacted with the bot we add them to our db at first time
        if(!_users.Any(c => c.Id == FromId))
        {
            var user = new User
            {
                Id = FromId,
                FullName = Context!.GetUserFullName(),
                Username = Context!.GetUsername()!,
                Roles = UserRole.scheduler
            };
            _db.Insert(user);
            _logger.LogInformation("Added user {tgId} at first time", user.Id);
        }
    }

    // handle all errors while message are processing
    [On(Handle.Exception)]
    public void OnException(Exception e)
    {
        _logger.LogError(e, "Unhandled exception");
        Push("Something went wrong");
    }

    // we'll handle auth error if user without roles try use action marked with [Authorize("policy")]
    [On(Handle.Unauthorized)]
    public void Forbidden()
    {
        Push("Forbidden!");
    }
}