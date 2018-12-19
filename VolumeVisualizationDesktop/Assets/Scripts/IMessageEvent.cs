using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using Apache.NMS;

[System.Serializable]
public class IMessageEvent : UnityEvent<IMessage> { }
