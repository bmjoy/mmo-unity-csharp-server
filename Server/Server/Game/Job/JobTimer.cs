using System;
using System.Collections.Generic;
using System.Text;
using ServerCore;

namespace Server.Game
{
	// Job의 단위를 무엇으로 할까? Action? 직접 만든 클래스?
	struct JobTimerElem : IComparable<JobTimerElem>
	{
		public int execTick; // 실행 시간
		public IJob job;

		public int CompareTo(JobTimerElem other)
		{
			return other.execTick - execTick;
		}
	}

	public class JobTimer
	{
		PriorityQueue<JobTimerElem> _pq = new PriorityQueue<JobTimerElem>();
		object _lock = new object();

		// public static JobTimer Instance { get; } = new JobTimer();

		public void Push(IJob job, int tickAfter = 0)
		{
			JobTimerElem jobElement;
			jobElement.execTick = System.Environment.TickCount + tickAfter;
			jobElement.job = job;

			lock (_lock)
			{
				_pq.Push(jobElement);
			}
		}

		public void Flush()
		{
			while (true)
			{
				// 현재 시간을 가져오고
				int now = System.Environment.TickCount;

				JobTimerElem jobElement;

				// 할 수 있는 작업들이 있으면 실행하겠다.
				lock (_lock)
				{
					if (_pq.Count == 0)
						break;

					jobElement = _pq.Peek();
					if (jobElement.execTick > now)
						break;

					_pq.Pop();
				}

				jobElement.job.Execute();
			}
		}
	}
}
