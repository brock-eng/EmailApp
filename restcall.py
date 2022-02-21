import os

# pip install requests
import requests as r

def main():
    # if testing against localhost
    os.environ['NO_PROXY'] = '127.0.0.1'

    try:
        running = r.get("https://127.0.0.1:5001/api/email", verify=False)
        print("GET: ", running.content)
    except Exception as e:
        print(e)

    try:
        jsonmsg = {
            "address" : "[Address to send to]",
            "subject" : "Sample Message",
            "from"    : "[Your name / Your Email]",
            "message" : "Hello!\nWe've been trying to reach you about your cars extended warranty.\n\nThanks,\nRobot",
        }
        samplePost = r.post("https://127.0.0.1:5001/api/email/send", json = jsonmsg, verify=False)
        print("POST: ", samplePost.content)
    except Exception as e:
        print(e)

if __name__ == "__main__": main()


