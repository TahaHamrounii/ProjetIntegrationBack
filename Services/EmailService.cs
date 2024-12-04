namespace Message.Services
{
    using System.Net.Mail;
    using System.Net;
    using Message.Models;
    using Microsoft.AspNetCore.Mvc;

    public class EmailService
    {
        private static readonly MailAddress fromAddress = new MailAddress("DrunkS680@gmail.com", "From ISET");
        private static readonly string fromPassword = "xzei eenl ukak vjsl"; 

        public static void sendEmail([FromBody] User user, string subj, string cont)
        {
            var toAddress = new MailAddress(user.Email, "To " + user.Username);
            var subject = subj;
            var body = cont;
            System.Diagnostics.Debug.WriteLine(user.Email);
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587, 
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            try
            {
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }
            }
            catch (SmtpException ex)
            {
                System.Diagnostics.Debug.WriteLine($"SMTP Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
            }
        }
    }

}
