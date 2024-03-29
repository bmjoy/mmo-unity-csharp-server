﻿using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

// 서버와 통신하는 부분

public class ServerSession : PacketSession
{
	// 서버 프로젝트에 있는것과 똑같다
	public void Send(IMessage packet)
	{
		string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
		MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);

		ushort size = (ushort)packet.CalculateSize(); // packet 사이즈 계산

		byte[] sendBuffer = new byte[size + 4];
		Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));

		Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort)); // sendBuffer 위치는 2

		Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size); // sendBuffer 위치는 4

		Send(new ArraySegment<byte>(sendBuffer));
	}

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