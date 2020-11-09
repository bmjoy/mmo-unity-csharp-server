using System;
using System.Collections.Generic;
using System.Text;

// JobQueue를 완전히 대체함
// 이제 action(lambda 캡쳐) 대신 IJob 인터페이스를 사용하는 Job클래스를 쓸것이다.
// 서버에 lock을 일일이 거는게 아니라, Task나 Job방식으로 Serialized 해줘서, 쓰레드 하나가 전담으로 경합없이 작업을 하게 한다.
namespace Server.Game
{
    public class JobSerializer
    {
		JobTimer _timer = new JobTimer(); // 미래(나중에) 실행되어야 하는 작업 (예약)
		Queue<IJob> _jobQueue = new Queue<IJob>(); // 즉시 실행되어야 하는 작업
		object _lock = new object();
		bool _flush = false;

		/// PushAfter Helper Function
		// 외부에서는 Action으로 던져준다. Job을 직접 만들지는 않음
		public void PushAfter(int tickAfter, Action action) { PushAfter(tickAfter, new Job(action)); }
		public void PushAfter<T1>(int tickAfter, Action<T1> action, T1 t1) { PushAfter(tickAfter, new Job<T1>(action, t1)); }
		public void PushAfter<T1, T2>(int tickAfter, Action<T1, T2> action, T1 t1, T2 t2) { PushAfter(tickAfter, new Job<T1, T2>(action, t1, t2)); }
		public void PushAfter<T1, T2, T3>(int tickAfter, Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { PushAfter(tickAfter, new Job<T1, T2, T3>(action, t1, t2, t3)); }

		public void PushAfter(int tickAfter, IJob job) // 작업을 예약한다
		{
			_timer.Push(job, tickAfter);
		}

		/// Push Helper Function
		public void Push(Action action) { Push(new Job(action)); } 
		public void Push<T1>(Action<T1> action, T1 t1) { Push(new Job<T1>(action, t1)); } 
		public void Push<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2) { Push(new Job<T1, T2>(action, t1, t2)); } 
		public void Push<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { Push(new Job<T1, T2, T3>(action, t1, t2, t3)); }

		// Push는 작업 밀어넣는 일만 하도록
		public void Push(IJob job)
		{
			lock (_lock)
			{
				_jobQueue.Enqueue(job);
			}
		}

		// 누가 Flush를 해줄까
		public void Flush() // 일감처리
		{
			_timer.Flush();

			while (true)
			{
				IJob job = Pop();
				if (job == null)
					return;

				job.Execute();
			}
		}

		IJob Pop()
		{
			lock (_lock)
			{
				if (_jobQueue.Count == 0) // 더이상 처리할 일감이 없다.
				{
					_flush = false;
					return null;
				}
				return _jobQueue.Dequeue();
			}
		}

	}
}
