namespace MassTransit.MSMQ.Tests
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Transactions;
	using NUnit.Framework;
	using NUnit.Framework.SyntaxHelpers;


	// the purpose of this test is to identify how to hand a transaction scope off to another thread
	// and have it complete the transaction. What I've found so far, is the original thread is waiting
	// for the dependent transaction to complete before the dispose returns, making it not likely to scale
	// beyond a single receiver at a time. However, we could continue to use the dispatcher/batch 
	// symantics to make tranasactions in receive a reality.




	[TestFixture]
	public class When_reading_from_a_transactional_queue
	{
		private readonly ManualResetEvent _txCompleted = new ManualResetEvent(false);

		[Test]
		public void A_transaction_should_be_dependent_upon_the_worker_thread_committing()
		{
			Thread thx = new Thread(ThreadProc);

			Debug.WriteLine(string.Format("{0} Opening transaction", DateTime.Now));

			using ( TransactionScope ts = new TransactionScope())
			{
				DependentTransaction dts = Transaction.Current.DependentClone(DependentCloneOption.BlockCommitUntilComplete);

				Debug.WriteLine(string.Format("{0} Starting thread", DateTime.Now));

				thx.Start(dts);

				Debug.WriteLine(string.Format("{0} Completing outer transaction", DateTime.Now));

				ts.Complete();

				Debug.WriteLine(string.Format("{0} Exiting transaction scope", DateTime.Now));
			}

			Debug.WriteLine(string.Format("{0} Verifying transaction not yet complete", DateTime.Now));


			Assert.That(_txCompleted.WaitOne(0, false), Is.True, "It seems that the original thread blocks until the dependent transaction is completed.");
		}

		public void ThreadProc(object tsObject)
		{
			DependentTransaction dts = (DependentTransaction)tsObject;

			Debug.WriteLine(string.Format("{0} Opening dependent transaction", DateTime.Now));

			using (TransactionScope ts = new TransactionScope(dts))
			{
				Debug.WriteLine(string.Format("{0} Going to sleep", DateTime.Now));

				Thread.Sleep(10000);

				Debug.WriteLine(string.Format("{0} Completing dependent transaction", DateTime.Now));

				ts.Complete();

				Debug.WriteLine(string.Format("{0} Dependent transaction completed, setting event", DateTime.Now));

				_txCompleted.Set();
			}

			Debug.WriteLine(string.Format("{0} Completing outer transaction", DateTime.Now));

			dts.Complete();

			Debug.WriteLine(string.Format("{0} Thread Exiting", DateTime.Now));

		}
	}
}
//
//        static void Main(string[] args)
//        {
//            DependentTransaction dtx;
//            Thread newThread = new Thread (new ParameterizedThreadStart(Program.ThreadProc));
//
//            using (TransactionScope s = new TransactionScope())
//            {
//                dtx = Transaction.Current.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
//                newThread.Start(dtx);
//                Console.WriteLine("About to complete the main thread");
//                s.Complete();
//            }
//
//            Console.WriteLine("Transaction Completed");
//        }
//    }
//}
//
//When you run it you get the following output: