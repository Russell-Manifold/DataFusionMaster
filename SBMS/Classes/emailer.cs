using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Threading;

namespace SBMS.Classes
{
    public class emailer
    {
        public string SendEmail(MimeMessage msg, int maxRetries = 3)
        {
            using (var smtp = new SmtpClient())
            {
                int attempt = 0;
                while (attempt < maxRetries)
                {
                    try
                    {
                        smtp.Connect("mail.syncflo.co.za", 465, SecureSocketOptions.Auto);
                        smtp.Authenticate("enquiries@syncflo.co.za", "PDbgkjKUZA6?");
                        smtp.Send(msg);
                        smtp.Disconnect(true);

                        return "OK";
                    }
                    catch (SmtpCommandException ex)
                    {
                        // Soft fails like greylisting (451) often resolve on retry
                        if (ex.StatusCode == SmtpStatusCode.MailboxUnavailable ||
                            ex.StatusCode == SmtpStatusCode.InsufficientStorage ||
                            ex.StatusCode == SmtpStatusCode.TransactionFailed)
                        {
                            attempt++;
                            Thread.Sleep(2000); // wait 2 seconds before retrying
                            continue;
                        }

                        return $"SMTP command error: {ex.StatusCode} - {ex.Message}";
                    }
                    catch (SmtpProtocolException ex)
                    {
                        return $"SMTP protocol error: {ex.Message}";
                    }
                    catch (Exception ex)
                    {
                        return $"General error: {ex.Message}";
                    }
                }
                return $"Email failed after {maxRetries} attempts.";
            }
        }
    }
}