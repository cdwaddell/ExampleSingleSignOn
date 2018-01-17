// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Modifications Copyright (c) C Daniel Waddell. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using ExampleSingleSignOn.Models;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ExampleSingleSignOn.Services
{
    // This class is used by the application to send email for account confirmation and password reset.
    // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
    public class EmailSender : IEmailSender
    {
        private readonly ISendGridClient _client;
        private readonly EmailSenderOptions _options;

        public EmailSender(ISendGridClient client, IOptions<EmailSenderOptions> optionsAccessor)
        {
            _client = client;
            _options = optionsAccessor.Value;
        }
        
        public Task SendEmailAsync(string email, string subject, string message)
        {
            // Plug in your email service here to send an email.
            var myMessage = new SendGridMessage();
            myMessage.AddTo(email);
            myMessage.From = new EmailAddress(_options.FromAddress, _options.FromName);
            myMessage.Subject = subject;
            myMessage.PlainTextContent = message;
            myMessage.HtmlContent = message;
            
            return _client.SendEmailAsync(myMessage);
        }
    }
}
