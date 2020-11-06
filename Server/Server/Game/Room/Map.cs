using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// 여기 들어오기전에 GameRoom 클래스에서 lock 걸고 들어오기 때문에 여기서는 안걸어도 된다.

// 맵 읽어오기 위한 클래스, 서버에서도 맵의 충돌체 정보를 알기 위해
// 플레이어를 맵 좌표 기준으로 빨리 찾기위함
// 이 게임은 맵이 하나고 맵id가 1번이라는것으로 가정
namespace Server.Game
{
	// 컴포넌트 식으로 게임룸마다 이 맵을 하나씩 들고있을거임
	public struct Pos
	{
		public Pos(int y, int x) { Y = y; X = x; }
		public int Y;
		public int X;
	}

	public struct PQNode : IComparable<PQNode>
	{
		public int F;
		public int G;
		public int Y;
		public int X;

		public int CompareTo(PQNode other)
		{
			if (F == other.F)
				return 0;
			return F < other.F ? 1 : -1;
		}
	}

	// 차피 2차원 게임이라 vector2면된다
	public struct Vector2Int
    {
		public int x;
		public int y;

		public Vector2Int(int x, int y) { this.x = x; this.y = y; }

		public static Vector2Int up { get { return new Vector2Int(0, 1); } }
		public static Vector2Int down { get { return new Vector2Int(0, -1); } }
		public static Vector2Int left { get { return new Vector2Int(-1, 0); } }
		public static Vector2Int right { get { return new Vector2Int(1, 0); } }

		public static Vector2Int operator+(Vector2Int a, Vector2Int b)
        {
			return new Vector2Int(a.x + b.x, a.y + b.y);
        }

		public static Vector2Int operator -(Vector2Int a, Vector2Int b)
		{
			return new Vector2Int(a.x - b.x, a.y - b.y);
		}

		public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } } // 이걸 루트씌우믄 벡터크기고
		public int sqrMagnitude { get { return (x * x + y * y); } } // 피타고라스 두번한게 벡터크기 제곱이고
		public int cellDistFromZero { get { return Math.Abs(x) + Math.Abs(y); } } // cell 좌표를 입력받아서 좌로 몇칸 우로 몇칸가야 따라잡는지 뱉어줌
	}

	// 실시간으로 맵을 로드 및 삭제
	// 맵에 딸린 충돌 정보 로드
	// 클라는 유저가 한맵에만 있으니 매니저가 하나만 있으면 되는데
	// 서버의 경우 모든 맵을 관리해야 하니깐.. 매니저가 여러맵을 다 들고있어야함
	public class Map
	{
		// 맵의 min,max
		public int MinX { get; set; }
		public int MaxX { get; set; }
		public int MinY { get; set; }
		public int MaxY { get; set; }

		public int SizeX { get { return MaxX - MinX + 1; } }
		public int SizeY { get { return MaxY - MinY + 1; } }

		bool[,] _collision; // 충돌할 벽이 있나 없나 여부, 차후에는 벽이 아니라 다른 몬스터 같은것도 포함
		GameObject[,] _objects; // 플레이어 좌표들, 충돌처리를 위해 맵이 들고있는다.

		// cellPos 위치와 _collision 배열의 대응하는 좌표값이 다르다.
		// cellPos의 경우 좌측 최상단 좌표가 0,0이 아니라 MinX,MaxY임.
		public bool CanGo(Vector2Int cellPos, bool checkObjects = true)
		{
			// _collision 을 이용하여 MapManager에게 내가 가려는 위치가 이동 가능한 위치인지 질의
			if (cellPos.x < MinX || cellPos.x > MaxX)
				return false;
			if (cellPos.y < MinY || cellPos.y > MaxY)
				return false;

			// 맵 좌표가 Min 또는 Max 값부터 시작하기 때문에 
			// X의 경우 : _collision 배열에서의 대응하는 값을 찾으려면 MinX값을 빼면 된다.
			// Y의 경우 : 위로 갈수록 y값이 커지므로 MaxY값에서 빼자
			// _collision에 맵 정보를 입력할 때 위에서부터 넣었는데
			// 우리가 받은 cellPos.y는 아래서부터 얼마나 떨어져있는지 값이니깐
			// 가장 위인 MaxY 기준으로 얼마나 떨어진 좌표인지로 변환해야 한다.
			int x = cellPos.x - MinX;
			int y = MaxY - cellPos.y;

			// _collision[y, x]은 갈수있다 0, 갈수없다 1인데 질의가 CanGo라서 반전시킨다
			// checkObjects가 false이면 플레이어가 y,x 좌표에 있는지 체크하지 않는다.
			// => 플레이어는 무시하고 맵에 있는 충돌체만 체크한다는거
			return !_collision[y, x] && (!checkObjects || _objects[y, x] == null); 
		}

		public GameObject Find(Vector2Int cellPos)
		{
			if (cellPos.x < MinX || cellPos.x > MaxX)
				return null;
			if (cellPos.y < MinY || cellPos.y > MaxY)
				return null;

			// 좌표찾기
			int x = cellPos.x - MinX;
			int y = MaxY - cellPos.y;
			return _objects[y, x];
		}

		public bool ApplyLeave(GameObject gameObject)
		{
			// 플레이어가 정상적인 좌표에 있는지 체크
			PositionInfo posInfo = gameObject.PosInfo;
			if (posInfo.PosX < MinX || posInfo.PosX > MaxX)
				return false;
			if (posInfo.PosY < MinY || posInfo.PosY > MaxY)
				return false;

			// gameObject가 있던 좌표를 null로 밀어버림
			{
				// 플레이어의 이동 전 좌표를 해당하는 _players 배열의 위치로 찾아가게 변환
				int x = posInfo.PosX - MinX;
				int y = MaxY - posInfo.PosY;
				// 내 좌표가 처음부터 잘못되어 있다면? 
				// -> 구한 x,y가 유효범위내에 있다고 확신할수가 없다. -> 위에서 체크
				if (_objects[y, x] == gameObject) // 정말 그 좌표에 있는게 player인지 한번 더 체크, 사실 아니면 진짜 문제있는거임
					_objects[y, x] = null;
			}

			return true;
		}

		// 나중에는 플레이어 대신 더 상위객체가 들어갈거임(몹같은것도 포함하는)
		// 실질적인 이동 처리
		public bool ApplyMove(GameObject gameObject, Vector2Int dest)
        {

			if (ApplyLeave(gameObject) == false) // 플레이어가 정상위치에 있는지 체크 후 -> 그 자리를 비워줌 (이동 전 처리)
				return false;

			if (CanGo(dest, checkObjects: true) == false)
				return false;

			// dest(목적지) 좌표값(유니티)에 대응하는  _players배열의 위치를 찾는 부분
			int x = dest.x - MinX;
			int y = MaxY - dest.y;
			_objects[y, x] = gameObject;

			// 실제 좌표 이동
			PositionInfo posInfo = gameObject.PosInfo;
			posInfo.PosX = dest.x;
			posInfo.PosY = dest.y;
			return true;
        }

		public void LoadMap(int mapId, string pathPrefix = "../../../../../Common/MapData")
		{
			string mapName = "Map_" + mapId.ToString("000"); // 아이디가 1이면 001로 변환

			// Collision 관련 파일 로드
			string text = File.ReadAllText($"{pathPrefix}/{mapName}.txt");
			// 파싱
			StringReader reader = new StringReader(text);

			// 맵 정보 파일 구조
			// 1~4줄 xMin,xMax,yMin,yMax // 나머지줄 맵 가장 윗줄부터
			MinX = int.Parse(reader.ReadLine());
			MaxX = int.Parse(reader.ReadLine());
			MinY = int.Parse(reader.ReadLine());
			MaxY = int.Parse(reader.ReadLine());

			// MinX와 MaxX 좌표도 모두 맵에 포함되기 때문에 카운트값에 +1 해줘야 한다. Y도
			int xCount = MaxX - MinX + 1;
			int yCount = MaxY - MinY + 1;
			_collision = new bool[yCount, xCount];
			_objects = new GameObject[yCount, xCount]; // y,x 순서는 취향임

			// _collision에 충돌정보 입력
			for (int y = 0; y < yCount; y++)
			{
				string line = reader.ReadLine();
				for (int x = 0; x < xCount; x++)
				{
					_collision[y, x] = (line[x] == '1' ? true : false);
				}
			}
		}

		#region A* PathFinding
		// 이걸 좀 더 개선할거면 탐색범위를 내 기준 몇칸이내로만 하거나
		// bool[,] closed = new bool[SizeY, SizeX]; // CloseList
		// 위와 같은 방문여부 체크 배열을 매번 new 하는걸 고치든지

		// U D L R
		int[] _deltaY = new int[] { 1, -1, 0, 0 };
		int[] _deltaX = new int[] { 0, 0, -1, 1 };
		int[] _cost = new int[] { 10, 10, 10, 10 };

		// 셀 기반 위치 입력 -> 내부적으로는 _collision 배열기반 위치 -> 셀 기반 위치값을 반환
		public List<Vector2Int> FindPath(Vector2Int startCellPos, Vector2Int destCellPos, bool checkObjects = true)
		{
			// checkObjects = ture -> 길찾기시 다른 충돌판정 오브젝트들을 모두 고려(몹,다른플레이어까지), false는 벽정도만 고려함

			// 일단 내가 가는 경로에 아무것도 없다 생각하고 계산하고
			// 가는길에 뭔가 있으면 다시 계산을 하는 경우가 많다

			// checkObjects 최종 목적지를 충돌처리 할지 여부
			// 만약 목적지에 플레이어가 있어서 못간다고 처리하면 안되니깐, 무조건 간다고 처리하게

			List<Pos> path = new List<Pos>();

			// 점수 매기기
			// F = G + H
			// F = 최종 점수 (작을 수록 좋음, 경로에 따라 달라짐)
			// G = 시작점에서 해당 좌표까지 이동하는데 드는 비용 (작을 수록 좋음, 경로에 따라 달라짐)
			// H = 목적지에서 얼마나 가까운지 (작을 수록 좋음, 고정)

			// (y, x) 이미 방문했는지 여부 (방문 = closed 상태)
			bool[,] closed = new bool[SizeY, SizeX]; // CloseList

			// (y, x) 가는 길을 한 번이라도 발견했는지
			// 발견X => MaxValue
			// 발견O => F = G + H
			int[,] open = new int[SizeY, SizeX]; // OpenList
			for (int y = 0; y < SizeY; y++)
				for (int x = 0; x < SizeX; x++)
					open[y, x] = Int32.MaxValue;

			Pos[,] parent = new Pos[SizeY, SizeX];

			// 오픈리스트에 있는 정보들 중에서, 가장 좋은 후보를 빠르게 뽑아오기 위한 도구
			PriorityQueue<PQNode> pq = new PriorityQueue<PQNode>();

			// CellPos -> ArrayPos
			Pos pos = Cell2Pos(startCellPos);
			Pos dest = Cell2Pos(destCellPos);

			// 시작점 발견 (예약 진행)		
			open[pos.Y, pos.X] = 10 * (Math.Abs(dest.Y - pos.Y) + Math.Abs(dest.X - pos.X));
			pq.Push(new PQNode() { F = 10 * (Math.Abs(dest.Y - pos.Y) + Math.Abs(dest.X - pos.X)), G = 0, Y = pos.Y, X = pos.X });
			parent[pos.Y, pos.X] = new Pos(pos.Y, pos.X);

			while (pq.Count > 0)
			{
				// 제일 좋은 후보를 찾는다
				PQNode node = pq.Pop();
				// 동일한 좌표를 여러 경로로 찾아서, 더 빠른 경로로 인해서 이미 방문(closed)된 경우 스킵
				if (closed[node.Y, node.X])
					continue;

				// 방문한다
				closed[node.Y, node.X] = true;
				// 목적지 도착했으면 바로 종료
				if (node.Y == dest.Y && node.X == dest.X)
					break;

				// 상하좌우 등 이동할 수 있는 좌표인지 확인해서 예약(open)한다
				for (int i = 0; i < _deltaY.Length; i++)
				{
					Pos next = new Pos(node.Y + _deltaY[i], node.X + _deltaX[i]);

					// 유효 범위를 벗어났으면 스킵
					// 벽으로 막혀서 갈 수 없으면 스킵
					// 목적지를 충돌처리 시킬거면 스킵 
					if (next.Y != dest.Y || next.X != dest.X)
					{
						if (CanGo(Pos2Cell(next), checkObjects) == false) // CellPos
							continue;
					}

					// 이미 방문한 곳이면 스킵
					if (closed[next.Y, next.X])
						continue;

					// 비용 계산 (휴리스틱)
					// AStar는 최단경로 찾는애가 아님
					int g = 0;// node.G + _cost[i]; // 내가 시작한 위치에서 가는 비용
					int h = 10 * ((dest.Y - next.Y) * (dest.Y - next.Y) + (dest.X - next.X) * (dest.X - next.X)); // 목적지까지 거리의 절대값만 사용(멘하탄 거리)
																												  // 다른 경로에서 더 빠른 길 이미 찾았으면 스킵
					if (open[next.Y, next.X] < g + h)
						continue;

					// 예약 진행
					open[dest.Y, dest.X] = g + h;
					pq.Push(new PQNode() { F = g + h, G = g, Y = next.Y, X = next.X });
					parent[next.Y, next.X] = new Pos(node.Y, node.X);
				}
			}

			return CalcCellPathFromParent(parent, dest);
		}

		List<Vector2Int> CalcCellPathFromParent(Pos[,] parent, Pos dest)
		{
			List<Vector2Int> cells = new List<Vector2Int>();

			int y = dest.Y;
			int x = dest.X;
			while (parent[y, x].Y != y || parent[y, x].X != x)
			{
				cells.Add(Pos2Cell(new Pos(y, x)));
				Pos pos = parent[y, x];
				y = pos.Y;
				x = pos.X;
			}
			cells.Add(Pos2Cell(new Pos(y, x)));
			cells.Reverse();

			return cells;
		}

		// Cell 위치에 대응하는 배열위치 반환
		Pos Cell2Pos(Vector2Int cell)
		{
			// CellPos -> ArrayPos
			return new Pos(MaxY - cell.y, cell.x - MinX);
		}

		// 위와 반대
		Vector2Int Pos2Cell(Pos pos)
		{
			// ArrayPos -> CellPos
			return new Vector2Int(pos.X + MinX, MaxY - pos.Y);
		}

		#endregion
	}

}
