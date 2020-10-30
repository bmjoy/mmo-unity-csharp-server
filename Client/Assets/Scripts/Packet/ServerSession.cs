using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

// 서버와 통신하는 부분

public class ServerSession : PacketSession
{
	public override void OnConnected(EndPoint endPoint)
	{
		Debug.Log($"OnConnected : {endPoint}");

		// session message id
		PacketManager.Instance.CustomHandler = (s, m, i) =>
		{
			// 일단 패킷큐에 떤지자.. 메인쓰레드 아닌대서 통신하면 터짐
			PacketQueue.Instance.Push(i, m); 
		};
	}

	public override void OnDisconnected(EndPoint endPoint)
	{
		Debug.Log($"OnDisconnected : {endPoint}");
	}

	public override void OnRecvPacket(ArraySegment<byte> buffer)
	{
		// 서버에서 날아온 메시지가 여기로
		PacketManager.Instance.OnRecvPacket(this, buffer);
	}

	public override void OnSend(int numOfBytes)
	{
		//Console.WriteLine($"Transferred bytes: {numOfBytes}");
	}
}