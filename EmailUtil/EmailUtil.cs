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
    public string Address { get; set; }
    public string From { get; set; }
    public string Subject { get; set; }
    public string Message { get; set; }
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
      public int AttemptDelay;  // seconds
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
        .To(msg.Address, msg.From)
        .Subject(msg.Subject)
        .Body(msg.Message);

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
          // System.Threading.Thread.Sleep(Config.AttemptDelay * 1000); // interval between attempts
        }
        finally
        {
          attempt++;
        }
      }
      return new Tuple<bool, string>(sendSuccess, message);
    }

    /// <summary>
    /// Inserts EmailMessage along with send status into SQLite database.
    /// </summary>
    private void SQLiteInsertNewEmail(EmailMessage msg, Tuple<bool, string> status)
    {
      string connString = "Data Source=.\\MailDb.db;Version=3";
      using (var conn = new SQLiteConnection(connString))
      {
        SQLiteCommand cmd = conn.CreateCommand();

        cmd.CommandText = "INSERT INTO MAIL (address, sender, subject, message, status, status_message, timestamp) " +
                          "VALUES (@address, @from, @subject, @message, @success, @status_message, CURRENT_TIMESTAMP)";
        cmd.Parameters.Add(new SQLiteParameter("@address", msg.Address));
        cmd.Parameters.Add(new SQLiteParameter("@from", msg.From));
        cmd.Parameters.Add(new SQLiteParameter("@subject", msg.Subject));
        cmd.Parameters.Add(new SQLiteParameter("@message", msg.Message));
        cmd.Parameters.Add(new SQLiteParameter("@success", status.Item1.ToString()));
        cmd.Parameters.Add(new SQLiteParameter("@status_message", status.Item2));

#if DEBUG
        string query = cmd.CommandText;
        foreach (SQLiteParameter p in cmd.Parameters)
        {
          query = query.Replace(p.ParameterName, p.Value.ToString());
        }
        Console.WriteLine(query);
#endif

        conn.Open();
       
        if (cmd.ExecuteNonQuery() != 1)
        {
          // error handling
        }
      }
    }
  }
}
