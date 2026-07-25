using BookingManagement.NotificationWorker.Messaging;
using BookingManagement.NotificationWorker.Notifications;
using SendGrid;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection(NotificationOptions.SectionName));

var notificationProvider = builder.Configuration[$"{NotificationOptions.SectionName}:Provider"];
if (string.Equals(notificationProvider, "SendGrid", StringComparison.OrdinalIgnoreCase))
{
    var sendGridApiKey = builder.Configuration[$"{NotificationOptions.SectionName}:SendGridApiKey"]
        ?? throw new InvalidOperationException("Missing 'Notification:SendGridApiKey' configuration value.");

    builder.Services.AddSingleton<ISendGridClient>(_ => new SendGridClient(sendGridApiKey));
    builder.Services.AddSingleton<INotificationSender, SendGridNotificationSender>();
}
else
{
    builder.Services.AddSingleton<INotificationSender, ConsoleNotificationSender>();
}

builder.Services.AddHostedService<KafkaConsumerWorker>();

var host = builder.Build();
host.Run();
