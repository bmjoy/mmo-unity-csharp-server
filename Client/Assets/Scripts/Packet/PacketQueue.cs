using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 유니티는 메인쓰레드를 통해 게임에 접근하지 않으면 터지기 때문에 큐방식으로 접근해야 안터진다.
public class PacketMessage
{
	public ushort Id { get; set; } // 프로토콜 아이디를 따로 받는다.
	public IMessage Message { get; set; } // IMessage만으로는 프로토콜 아이디를 알 수 없다.
}

public class PacketQueue
{
	public static PacketQueue Instance { get; } = new PacketQueue();

	Queue<PacketMessage> _packetQueue = new Queue<PacketMessage>();
	object _lock = new object();

	public void Push(ushort id, IMessage packet)
	{
		lock (_lock)
		{
			_packetQueue.Enqueue(new PacketMessage() { Id = id, Message = packet });
		}
	}

	public PacketMessage Pop()
	{
		lock (_lock)
		{
			if (_packetQueue.Count == 0)
				return null;

			return _packetQueue.Dequeue();
		}
	}

	public List<PacketMessage> PopAll()
	{
		List<PacketMessage> list = new List<PacketMessage>();

		lock (_lock)
		{
			while (_packetQueue.Count > 0)
				list.Add(_packetQueue.Dequeue());
		}

		return list;
	}
}