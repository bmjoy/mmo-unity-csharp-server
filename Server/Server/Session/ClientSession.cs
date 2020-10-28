using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using System.Net;
using Google.Protobuf.Protocol;
using static Google.Protobuf.Protocol.Person.Types;
using Google.Protobuf;

namespace Server
{
	class ClientSession : PacketSession
	{
		public int SessionId { get; set; }		

		public override void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnConnected : {endPoint}");

			// Google ProtoBuf 테스트
			S_Chat chat = new S_Chat()
			{
				Context = "ㅎㅇㅎㅇㅎㅇㅎㅇ!"
			};

			#region Protobuf를 이용해서 sendBuffer생성하기, 자동화 필요
			ushort size = (ushort)chat.CalculateSize(); // person 사이즈 계산

			// Session.cs 를 보면 전송할 데이터 + 아래와 같은 추가정보를 넣는다.
			// [size(2)] : 얼만큼 파싱을 할지
			// [packetId(2)] : 변환할 클래스
			// 각각 2바이트씩 필요하므로 위에서 구한 사이즈에 4바이트를 더해줘야 한다
			byte[] sendBuffer = new byte[size + 4];
			// BitConverter.GetBytes의 경우 구현이 내부에서 또 byte배열을 할당하기 때문에 문제가 있다.
			// -> 비트연산을 이용해서 직접 넣으면 되긴함
			Array.Copy(BitConverter.GetBytes(size + 4), 0, sendBuffer, 0, sizeof(ushort));

			ushort protocolId = (ushort)MsgId.SChat; // 서버에서 보내는 채팅타입 메시지
			Array.Copy(BitConverter.GetBytes(protocolId), 0, sendBuffer, 2, sizeof(ushort)); // sendBuffer 위치는 2
																							 // 이제 위에 만들었던 person을 버퍼에 넣는다.
			Array.Copy(chat.ToByteArray(), 0, sendBuffer, 4, size); // sendBuffer 위치는 4
			#endregion

            Send(new ArraySegment<byte>(sendBuffer)); // 전송

			//Program.Room.Push(() => Program.Room.Enter(this));
		}

		public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			// PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
			SessionManager.Instance.Remove(this);


			Console.WriteLine($"OnDisconnected : {endPoint}");
		}

		public override void OnSend(int numOfBytes)
		{
			//Console.WriteLine($"Transferred bytes: {numOfBytes}");
		}
	}
}
