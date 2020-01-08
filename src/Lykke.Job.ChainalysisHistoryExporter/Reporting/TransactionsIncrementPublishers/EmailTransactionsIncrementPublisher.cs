using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Lykke.Job.ChainalysisHistoryExporter.Reporting.TransactionsIncrementPublishers
{
    public class EmailTransactionsIncrementPublisher : ITransactionsIncrementPublisher
    {
        private readonly TransactionsReportWriter _writer;
        private readonly EmailSettings _settings;
        private readonly ILog _log;

        public EmailTransactionsIncrementPublisher(ILogFactory logFactory,
            TransactionsReportWriter writer,
            EmailSettings settings)
        {
            _writer = writer;
            _settings = settings;
            _log = logFactory.CreateLog(this);
        }

        public async Task Publish(HashSet<Transaction> increment, DateTime incrementFrom, DateTime incrementTo)
        {
            var fileName = $"transactions-{incrementTo:s}.csv";
            var destinations = string.Join(", ", _settings.To);
            var bccDestinations = string.Join(", ", _settings.Bcc);

            _log.Info($"Sending transactions increment '{fileName}' via email to addresses '{destinations}' and BCC '{bccDestinations}'...");

            using (var stream = new MemoryStream())
            {
                await _writer.WriteAsync(increment, stream, leaveOpen: true);

                stream.Position = 0;

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromDisplayName, _settings.FromEmailAddress));

                foreach (var to in _settings.To)
                {
                    message.To.Add(new MailboxAddress(to));
                }

                foreach (var bcc in _settings.Bcc)
                {
                    message.Bcc.Add(new MailboxAddress(bcc));
                }
                
                message.Subject =  $"Lykke transactions report for the period {incrementFrom:s} - {incrementTo:s}";

                var text = new TextPart("plain")
                {
                    Text = @"Hi, please find enclosed the transactions batch from last week, best regards"
                };

                var report = new MimePart("text", "csv")
                {
                    Content = new MimeContent(stream),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = fileName
                };

                message.Body =  new Multipart("mixed")
                {
                    text,
                    report
                };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync
                    (
                        _settings.SmtpHost,
                        _settings.Port,
                        _settings.EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto
                    );
                    await client.AuthenticateAsync(_settings.UserName, _settings.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }

            _log.Info($"Transactions increment with {increment.Count} transactions sent via email");
        }
    }
}
