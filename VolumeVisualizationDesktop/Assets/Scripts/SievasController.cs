using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Apache.NMS;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Reflection;
using System;
using System.Linq;

public class SievasController : MonoBehaviour {

    public SIEVASSession[] sessionList;
    public UnityEngine.Object row;
    public Canvas canvas;
	//public Planet_Data_Loader loader;
    public DVRMessageViewer dvrMessageViewer;
    public TimeSlider timeSlider;
    public IMessageProducer controlMsgProducer;
    public IMessageProducer dataMsgProducer;
    public IConnection connection;
    bool runonce = false;
    public IMessageEvent myEvent;
    public bool autoSelect = true;//Originally set to false. Allows for automatic selection of the session as a VolViz session.
    public string SessionName = "VolViz Session";
    

    private bool autoCheck = false;

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
	void Update () {

        //print("autoSelect" + autoSelect);
        //print("autoCheck" + autoCheck);


        if (runonce && autoCheck)
        {
            //print("We are entering here SessViewCont line 63"); 
            return;
        }

        if (autoSelect && !autoCheck)
        {
            SIEVASSession session = FindSession();

            //print("We are entering here SessViewCont line 71");

            //print(session.name); 

            if (session != null)
            {
                initSession(session);
                autoCheck = true;
            }
        }

        if (!runonce)
        {
            //print("We are entering here SessViewCont line 82");

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

       // print("We are entering here SessViewCont line 101");

    }

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
        controlConsumer.Listener += onControlMessage;//Tell the consumer what functions to listen to. This tells the listener that one of the places it needs to listen is to this.onControlMessage()
        controlConsumer.Listener += timeSlider.onControlMessage;//Also listen to timeSlider.onControlMessage(). 

        ITopic dataTopic = amqSession.GetTopic(session.dataStreamName);
        IMessageConsumer dataConsumer = amqSession.CreateConsumer(dataTopic);
        dataConsumer.Listener += dvrMessageViewer.onDataMessage;
        dataConsumer.Listener += timeSlider.onDataMessage;
        //dataConsumer.Listener += WaterDataLoader.Instance.onDataMessage;
        dataConsumer.Listener += this.onDataMessage;
        //dataConsumer.Listener += Planet_Data_Loader.Instance.onDataMessage;

        controlMsgProducer = amqSession.CreateProducer(controlTopic);
        dataMsgProducer = amqSession.CreateProducer(dataTopic); 


        connection.Start();
        canvas.gameObject.SetActive(false);

        print(dataTopic);

        ITextMessage initializeMessage = controlMsgProducer.CreateTextMessage("Initializing the HZReader");
        initializeMessage.Properties.SetBool("InitializeHZR", true);
        initializeMessage.Properties.SetString("VolumeName", "visMale");

        controlMsgProducer.Send(initializeMessage); 


        //get the current status of backend
        DVRCommandMessage dvrControlMsg = new DVRCommandMessage("GetStatus", 13L);
        ITextMessage msg = controlMsgProducer.CreateTextMessage(JsonConvert.SerializeObject(dvrControlMsg));
        msg.Properties.SetString("ObjectName", dvrControlMsg.GetType().Name);
        controlMsgProducer.Send(msg);

        //send a little test message to request that the data channel send something back. 

        ITextMessage dataRequest = controlMsgProducer.CreateTextMessage("This is a test message to request that data be sent.");
        dataRequest.Properties.SetBool("BrickRequest", true);
        dataRequest.Properties.SetInt("BrickNum", 0);
        dataRequest.Properties.SetInt("zLevel", 4);
        dataRequest.Properties.SetString("VolumeName", "visMale");
        dataMsgProducer.Send(dataRequest); 

    }


    private void onControlMessage(IMessage msg)
	{
		print ("We are receiving a CONTROL message.");
		//print (msg);
	}

    private void onDataMessage(IMessage msg)//Maybe we will have to eventually pass out a byte/int buffer from here.
    {
        int bytesRead = 0;
        int zLev = msg.Properties.GetInt("zLevReply");
        byte[] byteBuffer = new byte[1 << 3 * zLev];

        if(msg is IBytesMessage)
        {
            IBytesMessage bMsg = (IBytesMessage) msg;

            bytesRead = bMsg.ReadBytes(byteBuffer);

            print("We are receiving a DATA message of " + bytesRead + " bytes.");

            /*System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append("[");

            foreach (var b in byteBuffer)
            {
                sb.Append(b + ", ");

            }

            sb.Append("END]");
            print(sb.ToString()); */
        }

       

        




        //print (msg);
    }


    private void amqMessage (IMessage msg)
    {
        myEvent.Invoke(msg);
        CancelInvoke();
    }
}
