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
using Server.DB;
using Server.Game;
using ServerCore;

namespace Server
{
	class Program
	{
		static Listener _listener = new Listener();
		static List<System.Timers.Timer> _timers = new List<System.Timers.Timer>();
		
		// Main 함수에서 While 안에 Update 돌릴때와 다른 점은
		// 이제 update 작업을 main 함수를 작업하는 쓰레드만 하는게 아니라
		// 다른 쓰레드들도 update 작업에 동원 될 수 있다.
		static void TickRoom(GameRoom room, int tick = 1000)
		{
			var timer = new System.Timers.Timer();
			timer.Interval = tick; // 몇(ms)마다 반복할지
			timer.Elapsed += (s, e) => { room.Update(); }; // 반복할 작업
			timer.AutoReset = true; // 작업 후 재실행 여부
			timer.Enabled = true; // 실행

			_timers.Add(timer); // 나중에 끄고싶을수도 있으니
			// timer.Stop();
		}

		static void Main(string[] args)
		{
			ConfigManager.LoadConfig(); // 설정파일 읽어오기
			DataManager.LoadData(); // 설정파일에 맞춰 데이터 불러오기

			// DB Test
			using (AppDbContext db = new AppDbContext())
            {
				db.Accounts.Add(new AccountDb() { AccountName = "TestAccount" });
				db.SaveChanges();
            }

			GameRoom room = RoomManager.Instance.Add(1); // 서버 시작할때 일단 게임룸 하나 추가, 맵 번호는 1번이라 가정
			TickRoom(room, 50); // 생성된 room의 update가 50ms마다 한번씩 실행되도록 한다.

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
				//GameRoom room = RoomManager.Instance.Find(1);
				//room.Push(room.Update);
				Thread.Sleep(100);
			}
		}
	}
}
