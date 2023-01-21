using CanonRPWService.DSSDCommands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CanonRPWService.Spoolers
{
    public abstract class Spooler
    {
        protected BlockingCollection<RawDssdCommand> messageQueue = new BlockingCollection<RawDssdCommand>();
        public void PutCommandForProcessing(RawDssdCommand command)
        {
            messageQueue.Add(command);
        }
        public void ClearQueue()
        {
            while (messageQueue.Count > 0)
            {
                var obj = messageQueue.Take();
                obj.Dispose();
            }
        }
        abstract protected void ProcessIncomingMessages(CancellationToken cancellation);
    }
}
