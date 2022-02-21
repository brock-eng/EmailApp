# EmailApp

C# automated email application built on a sample REST backend.  Logs all email attempts to a SQLite DB.

### Build & Configuration
- git clone https://github.com/brock-eng/EmailApp  
- Edit <strong>smtpconfig.json</strong> with your email settings 
- Build/run application

### Usage

Email calls are made via JSON Posts to the <strong>/api/email/send</strong> endpoint.
```
// When testing against the local machine
POST https://localhost:5001/api/email/send
```

EmailMessage JSON format:
```json
{
    "address" : "[Address to send to]",
    "subject" : "Sample Message",
    "from"    : "[Your name / Your Email]",
    "message" : "Hello!\nWe've been trying to reach you about your cars extended warranty.\n\nThanks,\nRobot",
}
```



Sample Postman POST call:
<img src="https://github.com/brock-eng/EmailApp/blob/main/img/postman_preview.png">
  


### External Libraries Used
<a href="https://github.com/lukencode/FluentEmail">FluentEmail</a><br>
<a href="https://www.sqlite.org/index.html">SQLite</a><br>
<a href="https://github.com/DapperLib/Dapper">Dapper</a>

