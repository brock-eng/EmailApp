using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using FluentEmail.Smtp;
using FluentEmail.Core;
using Dapper;

namespace EmailUtility
{
  public class EmailMessage
  {
    public string address { get; set; }
    public string from { get; set; }
    public string subject { get; set; }
    public string message { get; set; }
  }

  public class EmailUtil
  {
    private struct SmtpSettings
    {
      public string Host;
      public int Port;
      public bool EnableSsl;
      public string Username;
      public string Password;
      public int MaxAttempts;
    }

    private SmtpSettings Config;
    private SmtpSender Sender;

    public EmailUtil()
    {
      ConfigureSmtp();
    }

    /// <summary>
    /// Configures the SmtpSender used by FluentEmail
    /// </summary>
    private void ConfigureSmtp()
    {
      Config = JsonConvert.DeserializeObject<SmtpSettings>(File.ReadAllText("smtpconfig.json"));

      Sender = new SmtpSender(() => new SmtpClient(Config.Host)
      {
        UseDefaultCredentials = false,
        EnableSsl = Config.EnableSsl,
        Port = Config.Port,
        Credentials = new NetworkCredential(Config.Username, Config.Password)
      });

      Email.DefaultSender = Sender;
    }

    /// <summary>
    /// Sends an email asyncronously.  Logs the message and send status.
    /// </summary>
    /// <param name="msg">Email object</param>
    /// <returns>Send success/failure</returns>
    public async Task<bool> SendEmailAsync(EmailMessage msg)
    {
      var email = Email
        .From(Config.Username)
        .To(msg.address, msg.from)
        .Subject(msg.subject)
        .Body(msg.message);

      // Tuple<sendSuccess, errorMessage>
      Tuple<bool, string> response = await Task.Run(() => SendEmail(email));

      SQLiteInsertNewEmail(msg, response);

      return response.Item1;
    }

    /// <summary>
    /// Attempts to send an email up to Config.MaxAttempts # of times.
    /// </summary>
    /// <returns>Send Success/Failure, Error Message (on fail)</returns>
    private Tuple<bool, string> SendEmail(IFluentEmail email)
    {
      bool sendSuccess = false;
      string message = "";

      int attempt = 1;
      while (!sendSuccess && attempt <= Config.MaxAttempts)
      {
        try
        {
          var response = email.Send();
          sendSuccess = response.Successful;
        }
        catch (Exception e)
        {
          message = e.Message;
        }
        finally
        {
          attempt++;
        }
      }
      return new Tuple<bool, string>(sendSuccess, message);
    }

    /// <summary>
    /// Inserts EmailMessage and send status into SQLite database.
    /// </summary>
    private void SQLiteInsertNewEmail(EmailMessage msg, Tuple<bool, string> status)
    {
      using (IDbConnection conn = new SQLiteConnection("Data Source=.\\MailDb.db;Version=3"))
      {
        // Illegal SQL Query Characters
        Regex rg = new Regex("[\"';,\t\r\n]");

        string insertStatement = "INSERT INTO MAIL (address, sender, subject, message, status, status_message, timestamp) " +
                     String.Format("VALUES ('{0}','{1}','{2}','{3}','{4}','{5}', CURRENT_TIMESTAMP)",
                     rg.Replace(msg.address, " "), rg.Replace(msg.from, " "), rg.Replace(msg.subject, " "), rg.Replace(msg.message, " "), 
                     status.Item1.ToString(), rg.Replace(status.Item2, " "));
        conn.Execute(insertStatement);
      }
    }
  }
}
