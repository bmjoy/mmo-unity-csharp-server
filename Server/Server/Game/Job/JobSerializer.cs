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
		Queue<IJob> _jobQueue = new Queue<IJob>();
		object _lock = new object();
		bool _flush = false;

		/// Helper Function
		// 외부에서는 Action으로 던져준다. Job을 직접 만들지는 않음
		public void Push(Action action) { Push(new Job(action)); } 
		public void Push<T1>(Action<T1> action, T1 t1) { Push(new Job<T1>(action, t1)); } 
		public void Push<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2) { Push(new Job<T1, T2>(action, t1, t2)); } 
		public void Push<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { Push(new Job<T1, T2, T3>(action, t1, t2, t3)); }

		public void Push(IJob job)
		{
			bool flush = false; // 들어온애가 실행까지 맡을지 아닐지를 판단하는 부분

			lock (_lock)
			{
				_jobQueue.Enqueue(job);
				if (_flush == false) // 내가 가장 처음으로 들어왔다면?
					flush = _flush = true; // 실행까지 할거야
			}

			if (flush)
				Flush();
		}

		void Flush() // 일감처리
		{
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
