using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FishBench
{
    class Tester
    {
        private int amount;
        private string pathA, pathB;
        private decimal sumA, sumB;
        private int completedA, completedB;

        private List<decimal> listA, listB;

        public event EventHandler TestFinished, JobFinished;
        private bool jobInProgress;
        private Thread job;

        public int Completed { get { return completedA + completedB; } }

        public double PercentCompleted
        {
            get
            {
                return (double)(completedA + completedB) / (double)(Amount) * (double)100;
            }
        }

        public void AbortJob()
        {
            if (jobInProgress && !abortJob)
                abortJob = true;
        }

        public int Amount
        {
            get { return amount * 2; }
            set
            {
                if (!jobInProgress && value > 0)
                    amount = value;
            }
        }

        public long AverageA
        {
            get { return (long)(completedA == 0 ? 0 : sumA / (decimal)completedA); }
        }

        public long AverageB
        {
            get { return (long)(completedB == 0 ? 0 : sumB / (decimal)completedB); }
        }

        public long AverageDiff
        {
            get
            {
                return AverageA - AverageB;
            }
        }

        public long StdevA
        {
            get
            {
                long avg = AverageA;
                return completedA == 0 ?
                    0 : (long)Math.Sqrt( (long)listA.Select(d => (d - avg) * (d - avg)).Sum() / (long)completedA );
            }
        }

        public long StdevB
        {
            get
            {
                long avg = AverageB;
                return completedB == 0 ?
                    0 : (long)Math.Sqrt((long)listB.Select(d => (d - avg) * (d - avg)).Sum() / (long)completedB);
            }
        }

        public long StdevDiff
        {
            get
            {
                long avg = AverageDiff, sum = 0;
                int len = Math.Min(listA.Count, listB.Count);
                for (int i = 0; i < len; i++)
                {
                    long cur = (long)listA[i] - (long)listB[i] - avg;
                    cur *= cur;
                    sum += cur;
                }
                return len == 0 ? 0 : (long)Math.Sqrt(sum / (len * 2));
            }
        }

        public Tester(string pathA, string pathB)
        {
            this.pathA = pathA;
            this.pathB = pathB;
            amount = 5;
            completedA = completedB = 0;
            jobInProgress = false;
        }
        public Tester() : this("", "") { }
        public void SetPathes(string pathA, string pathB)
        {
            this.pathA = pathA;
            this.pathB = pathB;
        }
        public void WaitJobEnd()
        {
            if (job != null && jobInProgress)
                job.Join();
        }

        private bool abortJob = false;
        private void doJob()
        {
            jobInProgress = true;
            listA = new List<decimal>(amount);
            listB = new List<decimal>(amount);

            int amountD = amount;
            decimal obs;
            string[] sep = {": "};
            sumA = sumB = completedA = completedB = 0;
            ProcessStartInfo infoA = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = pathA,
                Arguments = "bench",
                //FileName = "cmd.exe",
                //Arguments = "/c start /B /REALTIME /AFFINITY 0x1 \"" + pathA + "\" bench 1>nul",
                RedirectStandardError = true
            }, infoB = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = pathA,
                Arguments = "bench",
                //FileName = "cmd.exe",
                //Arguments = "/c start /B /REALTIME /AFFINITY 0x1 \"" + pathB + "\" bench 1>nul",
                RedirectStandardError = true
            };

            for (int i = 0; i < amountD; i++)
            {
                Process pa = new Process();
                pa.StartInfo = infoA;
                pa.Start();
                string line = "";
                while (!(line = pa.StandardError.ReadLine()).StartsWith("Nodes/second")) ;
                obs = decimal.Parse(line
                    .Split(sep, 2, StringSplitOptions.RemoveEmptyEntries)[1]);
                sumA += obs;
                listA.Add(obs);
                completedA++;
                if (TestFinished != null)
                    TestFinished(this, EventArgs.Empty);
                if (abortJob)
                {
                    jobInProgress = false;
                    abortJob = false;
                    return;
                }

                Process pb = new Process();
                pb.StartInfo = infoB;
                pb.Start();
                line = "";
                while (!(line = pb.StandardError.ReadLine()).StartsWith("Nodes/second")) ;
                obs = decimal.Parse(line
                    .Split(sep, 2, StringSplitOptions.RemoveEmptyEntries)[1]);
                sumB += obs;
                listB.Add(obs);
                completedB++;
                if (TestFinished != null)
                    TestFinished(this, EventArgs.Empty);
                if (abortJob)
                {
                    jobInProgress = false;
                    abortJob = false;
                    return;
                }
            }
            if (JobFinished != null)
                JobFinished(this, EventArgs.Empty);

            jobInProgress = false;
            abortJob = false;
        }

        public void DoJob()
        {
            if (jobInProgress) return;
            job = new Thread(doJob);
            job.Start();
        }
    }
}
