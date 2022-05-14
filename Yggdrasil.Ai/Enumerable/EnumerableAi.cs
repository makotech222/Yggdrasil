﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Yggdrasil.Ai.Enumerable
{
	/// <summary>
	/// An IEnumerable based, state-machine/behavior-tree hybrid AI.
	/// </summary>
	public class EnumerableAi
	{
		private readonly static Random RandomSeed = new Random(Environment.TickCount);
		private readonly Random _rnd = new Random(RandomSeed.Next());

		private int _switchRandomN, _switchRandomM;

		private IEnumerator _currentRoutine;
		private readonly List<IEnumerator> _subRoutines = new List<IEnumerator>();

		/// <summary>
		/// Makes the AI execute once.
		/// </summary>
		protected void Heartbeat()
		{
			// Prioritize sub-routines, that were returned via yield return.
			if (_subRoutines.Count != 0)
			{
				// Keep running the last sub-routine on the stack until
				// it's over and add potential new sub-routines to the
				// stack.
				var subRoutine = _subRoutines.Last();
				if (subRoutine.MoveNext())
				{
					if (subRoutine?.Current is IEnumerable newSubRoutine1)
						_subRoutines.Add(newSubRoutine1.GetEnumerator());
					return;
				}

				// Once the sub-routine is done, remove it. If there's
				// more sub-routines on the stack, continue with those
				// on the next tick, otherwise continue to the main
				// routine.
				_subRoutines.RemoveAt(_subRoutines.Count - 1);

				if (_subRoutines.Count != 0)
					return;
			}

			var prevRoutine = _currentRoutine;
			if (_currentRoutine == null || !_currentRoutine.MoveNext())
			{
				// If the routine changes on the last iteration, we end up
				// here, with a new routine. If we didn't check for that,
				// the new routine would be replaced by the Root call
				// right away.
				if (_currentRoutine == prevRoutine)
				{
					this.Root();
					_currentRoutine?.MoveNext();
				}
			}

			// If a sub-routine was returned, add it to the stack to get
			// executed on the next tick.
			if (_currentRoutine?.Current is IEnumerable newSubRoutine2)
				_subRoutines.Add(newSubRoutine2.GetEnumerator());
		}

		/// <summary>
		/// Starts given routine.
		/// </summary>
		/// <param name="routine"></param>
		protected virtual void StartRoutine(IEnumerable routine)
		{
			_currentRoutine = routine.GetEnumerator();
		}

		/// <summary>
		/// Stops the current routine and makes the AI start over.
		/// </summary>
		protected virtual void ClearRoutine()
		{
			_currentRoutine = null;
		}

		/// <summary>
		/// Routine is created and executed once.
		/// </summary>
		/// <remarks>
		/// This method can be used when a usually long-running routine
		/// is supposed to be started, but not waited for. For example,
		/// you might execute a movement routine, but not wait for the
		/// character to actually arrive at the destination.
		/// </remarks>
		/// <param name="routine"></param>
		protected void ExecuteOnce(IEnumerable routine)
		{
			routine.GetEnumerator().MoveNext();
		}

		/// <summary>
		/// Called to start routines when none are active.
		/// </summary>
		protected virtual void Root()
		{
		}

		// Utility
		//-------------------------------------------------------------------

		/// <summary>
		/// Imitates a switch-case that selects based on probability,
		/// instead of static values.
		/// </summary>
		/// <remarks>
		/// SwitchRandom generates a random number between 0 and 99,
		/// which is then referenced by the Case method.
		/// 
		/// SwitchRandom only keeps track of one random number at a time.
		/// Switches can be nested, but randomly calling SwitchRandom in
		/// between will give unexpected results.
		/// </remarks>
		/// <example>
		/// SwitchRandom();
		/// if (Case(60))
		/// {
		///		SwitchRandom();
		///		if (Case(20))
		///		{
		///		    Do(Wander(250, 500));
		///		}
		///		else if (Case(80))
		///		{
		///		    Do(Wait(4000, 6000));
		///		}
		/// }
		/// else if (Case(40))
		/// {
		///     Do(Wander(250, 500, false));
		/// }
		/// </example>
		/// <param name="total"></param>
		protected void SwitchRandom(int total = 100)
		{
			_switchRandomN = _rnd.Next(total);
			_switchRandomM = 0;
		}

		/// <summary>
		/// Returns true if the given value matches the probability
		/// generated by SwitchRandom, indicating that this case should
		/// be used.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		protected bool Case(int value)
		{
			_switchRandomM += value;
			return (_switchRandomN < _switchRandomM);
		}

		/// <summary>
		/// Returns a random number between 0 and max-1.
		/// </summary>
		/// <param name="max"></param>
		/// <returns></returns>
		protected int Random(int max)
		{
			return _rnd.Next(max);
		}

		/// <summary>
		/// Returns a random number between min and max-1.
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		protected int Random(int min, int max)
		{
			return _rnd.Next(min, max);
		}

		/// <summary>
		/// Return a random element from the given list.
		/// </summary>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="values"></param>
		/// <returns></returns>
		protected TValue RandomValue<TValue>(params TValue[] values)
		{
			return values[_rnd.Next(values.Length)];
		}

		// Actions
		//-------------------------------------------------------------------

		/// <summary>
		/// Does nothing, an idle action.
		/// </summary>
		/// <returns></returns>
		protected IEnumerable Nothing()
		{
			yield break;
		}

		/// <summary>
		/// Waits between min and max milliseconds.
		/// </summary>
		/// <param name="min">Minimum wait in milliseconds.</param>
		/// <param name="max">Maximum wait in milliseconds.</param>
		/// <returns></returns>
		protected virtual IEnumerable Wait(int min, int max = 0)
		{
			var waitTimeMs = max <= min ? min : _rnd.Next(min, max + 1);
			return this.Wait(TimeSpan.FromMilliseconds(waitTimeMs));
		}

		/// <summary>
		/// Waits for the given amount of time.
		/// </summary>
		/// <param name="timeSpan">Time to wait.</param>
		/// <returns></returns>
		protected virtual IEnumerable Wait(TimeSpan timeSpan)
		{
			var endTime = DateTime.Now.Add(timeSpan);

			do { yield return true; }
			while (DateTime.Now < endTime);
		}

		/// <summary>
		/// Executes routine until it's done or the given amount of time
		/// has passed.
		/// </summary>
		/// <param name="timeout">Timeout in milliseconds.</param>
		/// <param name="routine">Routine to execute.</param>
		/// <returns></returns>
		protected IEnumerable Timeout(int timeout, IEnumerable routine)
		{
			var endTime = DateTime.Now.AddMilliseconds(timeout);

			foreach (var _ in routine)
			{
				if (DateTime.Now >= endTime)
					yield break;

				yield return true;
			}
		}

		/// <summary>
		/// Executes routine until it's done or the given amount of time
		/// has passed.
		/// </summary>
		/// <param name="timeout">Time after which the routine returns.</param>
		/// <param name="routine">Routine to execute.</param>
		/// <returns></returns>
		protected IEnumerable Timeout(TimeSpan timeout, IEnumerable routine)
		{
			var endTime = DateTime.Now.Add(timeout);

			foreach (var _ in routine)
			{
				if (DateTime.Now >= endTime)
					yield break;

				yield return true;
			}
		}
	}
}
