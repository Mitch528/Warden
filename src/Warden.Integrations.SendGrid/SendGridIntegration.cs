﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using SendGrid;

namespace Warden.Integrations.SendGrid
{
    /// <summary>
    /// Integration with the SendGrid - email delivery & transactional service.
    /// </summary>
    public class SendGridIntegration : IIntegration
    {
        private readonly SendGridIntegrationConfiguration _configuration;

        public SendGridIntegration(SendGridIntegrationConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Sends email message.
        /// </summary>
        /// <returns></returns>
        public async Task SendEmailAsync()
        {
            await SendEmailAsync(Enumerable.Empty<string>().ToArray());
        }

        /// <summary>
        /// Sends email message.
        /// </summary>
        /// <param name="receivers">Receiver(s) email address(es). If default receivers have been set, it will merge these 2 lists.</param>
        /// <returns></returns>
        public async Task SendEmailAsync(params string[] receivers)
        {
            await SendEmailAsync(null, null, receivers);
        }

        /// <summary>
        /// Sends email message.
        /// </summary>
        /// <param name="message">Body of the email message. If default message has been set, it will override its value.</param>
        /// <param name="receivers">Receiver(s) email address(es). If default receivers have been set, it will merge these 2 lists.</param>
        /// <returns></returns>
        public async Task SendEmailAsync(string message, params string[] receivers)
        {
            await SendEmailAsync(null, message, receivers);
        }

        /// <summary>
        /// Sends email message.
        /// </summary>
        /// <param name="subject">Subject of the email message. If default subject has been set, it will override its value.</param>
        /// <param name="message">Body of the email message. If default message has been set, it will override its value.</param>
        /// <param name="receivers">Receiver(s) email address(es). If default receivers have been set, it will merge these 2 lists.</param>
        /// <returns></returns>
        public async Task SendEmailAsync(string subject = null,
            string message = null, params string[] receivers)
        {
            var emailMessage = CreateMessage(subject, receivers);
            var body = _configuration.DefaultMessage + emailMessage;
            if (_configuration.UseHtmlBody)
                emailMessage.Html = body;
            else
                emailMessage.Text = body;

            await SendMessageAsync(emailMessage);
        }

        /// <summary>
        /// Sends templated email message using transactional template.
        /// </summary>
        /// <returns></returns>
        public async Task SendTemplatedEmailAsync()
        {
            await SendTemplatedEmailAsync(Enumerable.Empty<string>().ToArray());
        }

        /// <summary>
        /// Sends templated email message using transactional template.
        /// <param name="receivers">Receiver(s) email address(es). If default receivers have been set, it will merge these 2 lists.</param>
        /// </summary>
        /// <returns></returns>
        public async Task SendTemplatedEmailAsync(params string[] receivers)
        {
            await SendTemplatedEmailAsync(null, null, null, receivers);
        }

        /// <summary>
        /// Sends templated email message using transactional template.
        /// </summary>
        /// <param name="templateId">Id of the transactional template. If default template id has been set, it will override its value.</param>
        /// <returns></returns>
        public async Task SendTemplatedEmailAsync(string templateId)
        {
            await SendTemplatedEmailAsync(null, templateId, null, Enumerable.Empty<string>().ToArray());
        }

        /// <summary>
        /// Sends templated email message using transactional template.
        /// </summary>
        /// <param name="parameters">Parameters of the transactional template. If default parameters have been set, it will merge these 2 lists.</param>
        /// <returns></returns>
        public async Task SendTemplatedEmailAsync(IEnumerable<EmailTemplateParameter> parameters)
        {
            await SendTemplatedEmailAsync(null, null, parameters);
        }

        /// <summary>
        /// Sends templated email message using transactional template.
        /// </summary>
        /// <param name="templateId">Id of the transactional template. If default template id has been set, it will override its value.</param>
        /// <param name="parameters">Parameters of the transactional template. If default parameters have been set, it will merge these 2 lists.</param>
        /// <returns></returns>
        public async Task SendTemplatedEmailAsync(string templateId, IEnumerable<EmailTemplateParameter> parameters)
        {
            await SendTemplatedEmailAsync(null, templateId, parameters, Enumerable.Empty<string>().ToArray());
        }

        /// <summary>
        /// Sends templated email message using transactional template.
        /// </summary>
        /// <param name="templateId">Id of the transactional template. If default template id has been set, it will override its value.</param>
        /// <param name="receivers">Receiver(s) email address(es). If default receivers have been set, it will merge these 2 lists.</param>
        /// <returns></returns>
        public async Task SendTemplatedEmailAsync(string templateId, params string[] receivers)
        {
            await SendTemplatedEmailAsync(null, templateId, null, receivers);
        }

        /// <summary>
        /// Sends templated email message using transactional template.
        /// </summary>
        /// <param name="templateId">Id of the transactional template. If default template id has been set, it will override its value.</param>
        /// <param name="parameters">Parameters of the transactional template. If default parameters have been set, it will merge these 2 lists.</param>
        /// <param name="receivers">Receiver(s) email address(es). If default receivers have been set, it will merge these 2 lists.</param>
        /// <returns></returns>
        public async Task SendTemplatedEmailAsync(string templateId, IEnumerable<EmailTemplateParameter> parameters,
            params string[] receivers)
        {
            await SendTemplatedEmailAsync(null,  templateId, parameters, receivers);
        }

        /// <summary>
        /// Sends templated email message using transactional template.
        /// </summary>
        /// <param name="subject">Subject of the email message. If default subject has been set, it will override its value.</param>
        /// <param name="templateId">Id of the transactional template. If default template id has been set, it will override its value.</param>
        /// <param name="parameters">Parameters of the transactional template. If default parameters have been set, it will merge these 2 lists.</param>
        /// <param name="receivers">Receiver(s) email address(es). If default receivers have been set, it will merge these 2 lists.</param>
        /// <returns></returns>
        public async Task SendTemplatedEmailAsync(string subject = null,
            string templateId = null, IEnumerable<EmailTemplateParameter> parameters = null,
            params string[] receivers)
        {
            var emailMessage = CreateMessage(subject, receivers);
            var emailTemplateId = templateId.Or(_configuration.DefaultTemplateId);
            //Space and some empty html tag are required if the template is being used
            emailMessage.Text = " ";
            emailMessage.Html = "<span></span>";
            emailMessage.EnableTemplateEngine(emailTemplateId);
            var customParameters = parameters ?? Enumerable.Empty<EmailTemplateParameter>();
            var templateParameters = (_configuration.DefaultTemplateParameters.Any()
                ? _configuration.DefaultTemplateParameters.Union(customParameters)
                : customParameters).ToList();

            foreach (var parameter in templateParameters)
            {
                emailMessage.AddSubstitution(parameter.ReplacementTag, parameter.Values.ToList());
            }

            await SendMessageAsync(emailMessage);
        }

        private SendGridMessage CreateMessage(string subject = null, params string[] receivers)
        {
            var customReceivers = receivers ?? Enumerable.Empty<string>();
            var emailReceivers = (_configuration.DefaultReceivers.Any()
                ? _configuration.DefaultReceivers.Union(customReceivers)
                : customReceivers).ToList();
            if (!emailReceivers.Any())
                throw new ArgumentException("Email message receivers have not been defined.", nameof(emailReceivers));

            emailReceivers.ValidateEmails(nameof(receivers));
            var message = new SendGridMessage
            {
                From = new MailAddress(_configuration.Sender),
                Subject = subject.Or(_configuration.DefaultSubject)
            };
            message.AddTo(emailReceivers);

            return message;
        }

        private async Task SendMessageAsync(SendGridMessage message)
        {
            var transportWeb = string.IsNullOrWhiteSpace(_configuration.ApiKey)
                ? new Web(new NetworkCredential(_configuration.Username, _configuration.Password))
                : new Web(_configuration.ApiKey);

            await transportWeb.DeliverAsync(message);
        }

        /// <summary>
        /// Factory method for creating a new instance of SendGridIntegration.
        /// </summary>
        /// <param name="username">Username of the SendGrid account.</param>
        /// <param name="password">Password of the SendGrid account.</param>
        /// <param name="sender">Email address of the message sender.</param>
        /// <param name="configurator">Lambda expression for configuring the SendGrid integration.</param>
        /// <returns>Instance of SendGridIntegration.</returns>
        public static SendGridIntegration Create(string username, string password, string sender, 
            Action<SendGridIntegrationConfiguration.Builder> configurator)
        {
            var config = new SendGridIntegrationConfiguration.Builder(username, password, sender);
            configurator?.Invoke(config);

            return Create(config.Build());
        }

        /// <summary>
        /// Factory method for creating a new instance of SendGridIntegration.
        /// </summary>
        /// <param name="apiKey">API key of the SendGrid account.</param>
        /// <param name="sender">Email address of the message sender.</param>
        /// <param name="configurator">Lambda expression for configuring the SendGrid integration.</param>
        /// <returns>Instance of SendGridIntegration.</returns>
        public static SendGridIntegration Create(string apiKey, string sender,
            Action<SendGridIntegrationConfiguration.Builder> configurator = null)
        {
            var config = new SendGridIntegrationConfiguration.Builder(apiKey, sender);
            configurator?.Invoke(config);

            return Create(config.Build());
        }

        /// <summary>
        /// Factory method for creating a new instance of SendGridIntegration.
        /// </summary>
        /// <param name="configuration">Configuration of SendGrid integration.</param>
        /// <returns>Instance of SendGridIntegration.</returns>
        public static SendGridIntegration Create(SendGridIntegrationConfiguration configuration)
            => new SendGridIntegration(configuration);
    }
}