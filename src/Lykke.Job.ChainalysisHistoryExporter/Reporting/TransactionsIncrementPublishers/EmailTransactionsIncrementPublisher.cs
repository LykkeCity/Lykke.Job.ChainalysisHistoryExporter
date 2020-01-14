using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Lykke.Service.EmailSender;
using Lykke.Service.EmailSender.AutorestClient.Models;

namespace Lykke.Job.ChainalysisHistoryExporter.Reporting.TransactionsIncrementPublishers
{
    public class EmailTransactionsIncrementPublisher : ITransactionsIncrementPublisher
    {
        private readonly IEmailSender _emailSender;
        private readonly TransactionsReportWriter _writer;
        private readonly EmailSettings _settings;
        private readonly ILog _log;

        public EmailTransactionsIncrementPublisher(IEmailSender emailSender,
            ILogFactory logFactory,
            TransactionsReportWriter writer,
            EmailSettings settings)
        {
            _emailSender = emailSender;
            _writer = writer;
            _settings = settings;
            _log = logFactory.CreateLog(this);
        }

        public async Task Publish(HashSet<Transaction> increment, DateTime incrementFrom, DateTime incrementTo)
        {
            var fileName = $"transactions-{incrementTo:s}.csv";
            var bccDestinations = string.Join(", ", _settings.Bcc);

            _log.Info($"Sending transactions increment '{fileName}' via email to address '{_settings.To}' and BCC '{bccDestinations}'...");

            using (var stream = new MemoryStream())
            {
                await _writer.WriteAsync(increment, stream, leaveOpen: true);

                stream.Position = 0;

                var base64Report = Convert.ToBase64String(stream.ToArray());
                var message = new EmailMessage
                {
                    Subject = $"Lykke transactions batch for the period {incrementFrom:s} - {incrementTo:s}",
                    TextBody = $"Hi, please find enclosed the Lykke transactions batch for the period {incrementFrom:s} - {incrementTo:s} UTC, best regards",
                    Attachments = new[]
                    {
                        new EmailAttachment(fileName, "text/csv", base64Report) 
                    }
                    
                };
                var to = new EmailAddressee {EmailAddress = _settings.To};
                var bcc = _settings.Bcc.Select(x => new EmailAddressee {EmailAddress = x});

                await _emailSender.SendAsync(message, to, bcc);
            }

            _log.Info($"Transactions increment with {increment.Count} transactions sent via email");
        }
    }
}
