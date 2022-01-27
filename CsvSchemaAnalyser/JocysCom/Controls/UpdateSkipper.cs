using System;
using System.Threading;


namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Used for reporting progress on User Interface. UI update will be skipped if busy. Status update works 1000 times faster this way.
	/// </summary>
	public class UpdateSkipper<T>
	{
		public T LastValue { get; set; }
		public T LastUpdatedValue { get; set; }

		public System.Windows.Threading.DispatcherObject Control;

		public Action<T> Action { get; set; }

		private int _lockFlag;
		public long Skipped;
		public long Changed;
		public long Updated;

		private object _lock = new object();

		public void Update(T value)
		{
			lock (_lock)
			{
				LastValue = value;
				Interlocked.Increment(ref Updated);
				if (Interlocked.CompareExchange(ref _lockFlag, 1, 0) != 0)
				{
					// Return if BeginInvoke is still running.
					Interlocked.Increment(ref Skipped);
					return;
				}
			}
			Interlocked.Increment(ref Changed);
			// BeginInvoke will silently skip if more than 25 threads opened.
			Control.Dispatcher.BeginInvoke(new Action<T>(_Update), value);
		}

		private void _Update(T value)
		{
			LastUpdatedValue = value;
			Action(value);
			Interlocked.Decrement(ref _lockFlag);
		}

		public void UpdateFinalize()
		{
			// Loop while locked i.e. wait for BeginInvoke to finish.
			lock (_lock)
				while (_lockFlag != 0) { }
			if (!Equals(LastValue, LastUpdatedValue))
				Control.Dispatcher.Invoke(() => Action(LastValue));
		}

	}

}
