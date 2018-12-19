using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using UnityEngine.Networking;
using System.Collections.Generic;
using Newtonsoft.Json;


delegate void WWWAction(UnityWebRequest request);

public class btnLoginScript : MonoBehaviour
{

    public Canvas canvas;
    public Canvas cvsSessions;
    public InputField ifUsername;
    public InputField ifPassword;
    public InputField ifServerURL;
    public string sessionId = "";

    private string baseURL;
    private static string LOGIN_URL = "login";
    private static string SESSION_LIST_URL = "api/sessions/";
    private static string URL_PATH_SEPERATOR = "/";
    private string error = null;

    private const int REDIRECT_HTTP_STATUS = 302;
    private const string COOKIE_HEADER_NAME = "Cookie";
    private const string SET_COOKIE_HEADER_NAME = "Set-Cookie";
    private const string LOCATION_HEADER = "Location";

    private const string SPRING_SESSION_COOKIE_NAME = "JSESSIONID";
    private const char COOKIE_SEPARATOR = ';';
    private const char COOKIE_EQUALS = '=';

    private const string USERNAME_FIELD = "username";
    private const string PASSWORD_FIELD = "password";

    private const string ERROR_PARAM = "?error";

    private int clickCount = 0; 


    private static bool TrustCertificate(object sender, X509Certificate x509Certificate, X509Chain x509Chain, SslPolicyErrors sslPolicyErrors)
    {
        // all Certificates are accepted
        return true;
    }


    // Use this for initialization
    void Start()
    {
        ServicePointManager.ServerCertificateValidationCallback = TrustCertificate;
        onClick(); 

    }


    public void onClick()
    {
        baseURL = generateURL(ifServerURL.text);
        string url = baseURL + LOGIN_URL;
        

        WWWForm form = new WWWForm();
        form.AddField(USERNAME_FIELD, ifUsername.text);
        form.AddField(PASSWORD_FIELD, ifPassword.text);
        clickCount++;
        UnityWebRequest request = UnityWebRequest.Post(url, form);
        //needed to handle redirect and get cookie
        request.redirectLimit = 0;
        StartCoroutine(WaitForRequest(request, onComplete));
        print("clicked! This is the " + clickCount+ "th click");
        





    }


    private IEnumerator WaitForRequest(UnityWebRequest request, WWWAction onComplete)
    {
        yield return request.Send();
        print("Waiting for request");
        // check for errors
        //this will return error on redirect limit right now
        if (!request.isNetworkError)
        {
            if (request.url.EndsWith(ERROR_PARAM))
            {
                print("Login Error1");
            }
            else
                onComplete(request);
        }
       
        else 
        {
            error = request.error;
            print(error + " resultCode=" + request.responseCode);
            //check for the redirect
            if (request.responseCode == REDIRECT_HTTP_STATUS)
            {
                Dictionary<string, string> headers = request.GetResponseHeaders();
                string location = headers[LOCATION_HEADER];
                if (!string.IsNullOrEmpty(location))
                {
                    if (location.EndsWith(ERROR_PARAM))
                    {
                        print("Login Error2");
                    }
                    else
                        onComplete(request);
                }
                else
                    onComplete(request);

            }
            else
            {
                print("Unknown. Not a REDIRECT_HTTP_STATUS error. This is a network error. ");
            }
        }

    }

    private void onComplete(UnityWebRequest request)
    {

        print("Login success");

        baseURL = generateURL(ifServerURL.text);
        string url = baseURL + SESSION_LIST_URL;


        ServicePointManager.ServerCertificateValidationCallback = TrustCertificate;
        sessionId = getSessionId(request, sessionId);
        UnityWebRequest request2 = UnityWebRequest.Get(url);
        setSessionInfo(request2, sessionId);
        request2.redirectLimit = 0;

        //now run the listing
        StartCoroutine(WaitForRequest2(request2, onCompleteListing));


    }


    private IEnumerator WaitForRequest2(UnityWebRequest request, WWWAction onComplete)
    {
        yield return request.Send();

        // check for errors
        if (!request.isNetworkError)
        {
            onCompleteListing(request);
        }
        else
        {
            error = request.error;
            print(error + "This is the resultCode=" + request.responseCode);
        }

    }


    private string generateURL(string baseURL)
    {
        string url = baseURL;
        if (!url.EndsWith(URL_PATH_SEPERATOR))
            url += URL_PATH_SEPERATOR;

        return url;
    }

    private string getSessionId(UnityWebRequest request, string currentSessionId)
    {
        string sessionId = currentSessionId;

        Dictionary<string, string> headers = request.GetResponseHeaders();
        if (headers != null)
        {
            foreach(string s in headers.Keys)
            {
                print(s); 
            }

         

            string cookies = headers[SET_COOKIE_HEADER_NAME];
            if (!string.IsNullOrEmpty(cookies))
            {
                string[] parts = cookies.Split(COOKIE_SEPARATOR);
                if (parts != null)
                {
                    foreach (string part in parts)
                    {
                        string[] parts2 = part.Split(COOKIE_EQUALS);
                        if ((parts2.Length == 2) && (parts2[0] == SPRING_SESSION_COOKIE_NAME))
                            sessionId = parts2[1];
                    }
                }
            }
        }
        return sessionId;
    }

    private void setSessionInfo(UnityWebRequest request, string sessionId)
    {
        request.SetRequestHeader(COOKIE_HEADER_NAME, SPRING_SESSION_COOKIE_NAME + COOKIE_EQUALS + sessionId);
    }


    public void onCompleteListing(UnityWebRequest request)
    {
        print("LISTING success");
        print(request.downloadHandler.text);

        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.MissingMemberHandling = MissingMemberHandling.Ignore;
        settings.CheckAdditionalContent = false;
        JsonList<SIEVASSession> list = JsonConvert.DeserializeObject<JsonList<SIEVASSession>>(request.downloadHandler.text, settings);

        print(list);
        canvas.gameObject.SetActive(false);
        GameObject tblSessions = cvsSessions.transform.Find("tblSessions").gameObject;
        SievasController controller = tblSessions.GetComponent<SievasController>();
        controller.sessionList = list.data;
        cvsSessions.gameObject.SetActive(true);
        canvas.gameObject.SetActive(false);
        StopAllCoroutines();
    }



    // Update is called once per frame
    void Update () {
	
	}
    void OnApplicationQuit()
    {

        StopAllCoroutines();
    }
}
