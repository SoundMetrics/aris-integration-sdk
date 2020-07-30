using System;
using System.Threading.Tasks.Dataflow;

namespace SoundMetrics.Aris.Network
{
    internal sealed class BufferedActionQueue<Message> : IDisposable
    {
        public BufferedActionQueue(Action<Message> action)
        {
            bufferBlock = new BufferBlock<Message>();
            actionBlock = new ActionBlock<Message>(action);
            actionSub = bufferBlock.LinkTo(actionBlock);
        }

        public bool Post(Message message)
        {
            return bufferBlock.Post(message);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    bufferBlock.Complete();
                    actionBlock.Completion.Wait();
                    actionSub.Dispose();
                }

                // no unmanaged resources
                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private readonly BufferBlock<Message> bufferBlock;
        private readonly ActionBlock<Message> actionBlock;
        private readonly IDisposable actionSub;
        private bool disposed;
    }
}
