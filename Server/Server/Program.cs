using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;
using ServerCore;

namespace Server
{
	class Program
	{
		static Listener _listener = new Listener();

		static void FlushRoom()
		{
			JobTimer.Instance.Push(FlushRoom, 250);
		}

		static void Main(string[] args)
		{
			ConfigManager.LoadConfig(); // 설정파일 읽어오기
			DataManager.LoadData(); // 설정파일에 맞춰 데이터 불러오기

			var a = DataManager.StatDict;

			RoomManager.Instance.Add(1); // 서버 시작할때 일단 게임룸 하나 추가, 맵 번호는 1번이라 가정

			// DNS (Domain Name System)
			string host = Dns.GetHostName();
			IPHostEntry ipHost = Dns.GetHostEntry(host);
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

			_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
			Console.WriteLine("Listening...");

			//FlushRoom();
			//JobTimer.Instance.Push(FlushRoom);

			// 이렇게 메인 루프 어딘가에서 콘텐츠들을 전부 업댓 시켜주는 코드가 있어야 한다.
			// 이걸 어디서 놓고 돌릴지 매우 햇갈림
			while (true)
			{
				//JobTimer.Instance.Flush();
				GameRoom room = RoomManager.Instance.Find(1);
				room.Push(room.Update);
				Thread.Sleep(100);
			}
		}
	}
}
