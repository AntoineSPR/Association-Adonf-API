using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using AssociationAdonfAPI.Models;
using AssociationAdonfAPI.Utilities;

namespace AssociationAdonfAPI.Services
{
    public class SendMailService
    {
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<UserApp> _userManager;
        private static readonly Dictionary<string, DateTime> _lastResetRequest = new();


        public SendMailService(IWebHostEnvironment env, UserManager<UserApp> userManager)
        {
            _env = env;
            _userManager = userManager;
        }

        public async Task<bool> SendPasswordResetEmail(string emailAddress)
        {
            try
            {
                if (_lastResetRequest.TryGetValue(emailAddress, out var lastRequest))
                {
                    if (DateTime.UtcNow - lastRequest < TimeSpan.FromMinutes(5)) return true;
                }
                _lastResetRequest[emailAddress] = DateTime.UtcNow;

                var smtpClient = new SmtpClient(Env.SMTP_HOST)
                {
                    Port = Int32.Parse(Env.SMTP_PORT),
                    Credentials = new NetworkCredential(
                        Env.SMTP_EMAILFROM,
                        Env.SMTP_PASSWORD
                    ),
                    EnableSsl = true
                };

                var user = await _userManager.FindByEmailAsync(emailAddress);
                if (user == null)
                {
                    // Pour des raisons de sécurité, on ne révèle pas si l'email existe
                    return true;
                }

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = Uri.EscapeDataString(resetToken);
                var resetLink = $"{Env.API_FRONT_URL}/reset-password?token={encodedToken}&email={Uri.EscapeDataString(emailAddress)}";
                var name = user.Name;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("ne-pas-repondre@hexaplanning.fr"),
                    Subject = "Hexaplanning : Réinitialisez votre mot de passe",
                    Body = $"""
                    Bonjour {name},

                    Vous avez demandé un lien pour réinitialiser votre mot de passe Hexaplanning. Si la demande ne venait pas de vous, veuillez ignorer ce message.
                    
                    Ce lien est valable 24 heures : 
                    
                    {resetLink}

                    Au plaisir de vous revoir sur Hexaplanning !
                    """,
                    IsBodyHtml = false,
                };

                mailMessage.To.Add(user.Email);

                await smtpClient.SendMailAsync(mailMessage);

                return true;
            }
            catch
            {
                throw;
            }
        }

        ///// <summary>
        ///// Cette m thode sert   envoyer un email de confirmation de mail.
        ///// C'est une version asynchrone
        ///// </summary>
        ///// <param name="mail"></param>
        ///// <param name="link"></param>
        ///// <returns></returns>
        //public async Task SendConfirmationEmail(Mail mail, string link)
        //{
        //    mail.MailFrom = "ne-pas-repondre@hexaplanning.fr";
        //    await SendEmail(mail, link, "ValidationEmailTemplate.cshtml");
        //}
    }
}

    //    public async Task SendResetEmail(Mail mail, string link)
    //    {
    //        mail.MailFrom = EnvironmentVariables.DO_NO_REPLY_MAIL;
    //        await SendEmail(mail, link, "PasswordResetTemplate.cshtml");
    //    }

    //    /// <summary>
    //    /// M thode g n rale d'envoi de mail 
    //    /// </summary>
    //    /// <param name="mail"></param>
    //    /// <param name="link"></param>
    //    /// <param name="templateName"></param>
    //    /// <returns></returns>
    //    private async Task SendEmail(Mail mail, string link, string templateName)
    //    {
    //        var smtpClient = new SmtpClient(EnvironmentVariables.SMTP_HostAddress)
    //        {
    //            Port = EnvironmentVariables.SMTP_Port,
    //            Credentials = new NetworkCredential(
    //                EnvironmentVariables.SMTP_EmailFrom,
    //                EnvironmentVariables.SMTP_Password
    //            ),
    //            EnableSsl = true
    //        };
    //        string templatePath = templateName;
    //        var model = new ValidationMailTemplateViewModel(link, link);
    //        string htmlContent = await _razorLightEngine.CompileRenderAsync(templatePath, model);
    //        var mailBody = htmlContent;

    //        var mailMessage = new MailMessage
    //        {
    //            From = new MailAddress(mail.MailFrom),
    //            Subject = mail.MailSubject,
    //            Body = mailBody,
    //            IsBodyHtml = true,
    //        };

    //        mailMessage.To.Add(mail.MailTo);

    //        await smtpClient.SendMailAsync(mailMessage);
    //    }
    //}
//}

