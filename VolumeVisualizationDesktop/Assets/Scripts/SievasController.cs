/* Sievas Controller for interfacing with SIEVAS. Randall Reese 12/07/2018.
    This class is heavily based on the class SessionViewController.cs in the SIEVAS_UnityDev project. 
 */

using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Apache.NMS;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

public class SievasController : MonoBehaviour
{
    //SievasController is based on SessionViewController in SIEVAS_UnityDev

    public SIEVASSession[] sessionList;
    public UnityEngine.Object row;
    public Canvas canvas;
	
    public IMessageProducer controlMsgProducer;
    public IMessageProducer dataMsgProducer;
    public IConnection connection;

    public bool runonce = true;//Originally set to false
    public IMessageEvent myEvent;
    public bool autoSelect = true;//Originally set to false. Allows for automatic selection of the session as a VolViz session.
    public string SessionName = "VolViz Session";
    private string volName = "VisMale"; //defaults to VisMale

    private byte[] nextBrickData;
    private bool sievasInitialized = false;
    private int nextBrick;
    private Dictionary<int, byte[]> byteDataDict = new Dictionary<int, byte[]>();


    private bool autoCheck = false;

    public byte[] NextBrickData
    {
        get
        {
            return nextBrickData;
        }

        set
        {
            nextBrickData = value;
        }
    }

    public bool SievasInitialized
    {
        get
        {
            return sievasInitialized;
        }

        set
        {
            sievasInitialized = value;
        }
    }

    public int NextBrick
    {
        get
        {
            return nextBrick;
        }

        set
        {
            nextBrick = value;
        }
    }

    public Dictionary<int, byte[]> ByteDataDict
    {
        get
        {
            return byteDataDict;
        }

        set
        {
            byteDataDict = value;
        }
    }

    public string VolName
    {
        get
        {
            return volName;
        }

        set
        {
            volName = value;
        }
    }

    // Use this for initialization
    void Start () {

    }

    /// <summary>
    /// Finds the auto login session by lowercase match of name
    /// </summary>
    /// <returns>The session found by name. Otherwise null is returned.</returns>
    private SIEVASSession FindSession()
    {
        if (sessionList == null)
            return null;
        SIEVASSession found = null;

        for(int s=0; s < sessionList.Length; s++)
        {
            if (sessionList[s].name.ToLower() == SessionName.ToLower())
            {
                found = sessionList[s];

                print(found.name); 

                break;
            }
        }
        return found;
    }
	
	// Update is called once per frame
	void Update ()
    {


        if (runonce && autoCheck)
        {
            
            return;
        }

        if (autoSelect && !autoCheck)
        {
            SIEVASSession session = FindSession();

            
            if (session != null)
            {
                initSession(session);
                autoCheck = true;
            }
        }

        if (!runonce)
        {
            

            for (int i = 0; i < sessionList.Length; i++)
            {
                GameObject newRow = (GameObject)Instantiate(row, transform.GetChild(0).GetChild(0).GetChild(0).transform, false);
                newRow.GetComponentsInChildren<Text>()[0].text = sessionList[i].id.ToString();
                newRow.GetComponentsInChildren<Text>()[1].text = sessionList[i].name;
                newRow.GetComponent<btnSession>().index = i;

                // hook into the click on the new row
                EventTrigger trigger = newRow.GetComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                entry.callback.AddListener((data) => { handleClick((PointerEventData)data); });
                trigger.triggers.Add(entry);
                runonce = true;
            }
        }

    }

    //Handles the click of the "Login" button on the SIEVAS login screen. 
    public void handleClick(PointerEventData data)
    {
        PointerEventData evt = data;
        if ((evt.button == PointerEventData.InputButton.Left) && (evt.clickCount == 2) && sessionList.Length > 0)
        {
            SIEVASSession session = sessionList[EventSystem.current.currentSelectedGameObject.GetComponent<btnSession>().index];
            initSession(session);  
        }
    }


    private void initSession(SIEVASSession session)
    {
        print("INIT");
        NMSConnectionFactory cf = new NMSConnectionFactory("activemq:" + session.activemqUrl);
        connection = cf.CreateConnection();
        ISession amqSession = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
        ITopic controlTopic = amqSession.GetTopic(session.controlStreamName);
        IMessageConsumer controlConsumer = amqSession.CreateConsumer(controlTopic);
        controlConsumer.Listener += onControlMessage;
        //Tell the consumer what functions to listen to. This tells the listener that one of the places it needs to listen is to this.onControlMessage()
      

        ITopic dataTopic = amqSession.GetTopic(session.dataStreamName);
        IMessageConsumer dataConsumer = amqSession.CreateConsumer(dataTopic);
        dataConsumer.Listener += this.onDataMessage;
       

        controlMsgProducer = amqSession.CreateProducer(controlTopic);
        dataMsgProducer = amqSession.CreateProducer(dataTopic); 


        connection.Start();
        canvas.gameObject.SetActive(false);

        print(dataTopic);

        ITextMessage initializeMessage = controlMsgProducer.CreateTextMessage("Initializing the HZReader");
        initializeMessage.Properties.SetBool("InitializeHZR", true);
        initializeMessage.Properties.SetString("VolumeName", VolName);

        controlMsgProducer.Send(initializeMessage); 
        SievasInitialized = true; 

    }


    private void onControlMessage(IMessage msg)
	{
		print ("We are receiving a CONTROL message.");
		
	}

    private void onDataMessage(IMessage msg)
    {
        int bytesRead = 0;
        int zLev = msg.Properties.GetInt("zLevReply");
        byte[] byteBuffer = new byte[1 << 3 * zLev];

        if(msg is IBytesMessage)
        {
            IBytesMessage bMsg = (IBytesMessage) msg;

            bytesRead = bMsg.ReadBytes(byteBuffer);

            print("We are receiving a DATA message of " + bytesRead + " bytes.");

            NextBrickData = byteBuffer; //This sets the byte buffer of the next brick up. 
            NextBrick = msg.Properties.GetInt("BrickNumReply");

            print("This is the nextBrick number: " + NextBrick); 
            
        }

       

        ByteDataDict.Add(NextBrick, nextBrickData); 

    }


    private void amqMessage (IMessage msg)
    {
        myEvent.Invoke(msg);
        CancelInvoke();
    }
}
