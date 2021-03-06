﻿namespace ConcurrentFlows.FindingCompletion.Challenge2 {
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    public class LoopDataflow1 {

        public LoopDataflow1() {
            InputMessageBlock = new TransformBlock<Message, Message>(async msg => await InputMessage(msg));
            HandleMessageBlock = new TransformManyBlock<Message, Message>(async msg => await HandleMessage(msg));
            OutputMessageBlock = new ActionBlock<Message>(msg => OutputMessage(msg));

            var linkOptions = new DataflowLinkOptions() { PropagateCompletion = false };
            InputMessageBlock.LinkTo(HandleMessageBlock, linkOptions);
            HandleMessageBlock.LinkTo(OutputMessageBlock, linkOptions, msg => msg.WasProcessed == true);
            HandleMessageBlock.LinkTo(HandleMessageBlock, linkOptions, msg => msg.WasProcessed == false);

            InputMessageBlock.Completion.ContinueWith(async tsk => {
                await HandleMessageIsComplete();
                HandleMessageBlock.Complete();
            });

            HandleMessageBlock.Completion.ContinueWith(tsk => {
                OutputMessageBlock.Complete();
            });
            //DebuggingLoop();
        }

        private async Task<bool> HandleMessageIsComplete() {
            while (!(handlingMessages == 0 &&
                     HandleMessageBlock.InputCount == 0 &&
                     HandleMessageBlock.OutputCount == 0)) {
                await Task.Delay(100);
            }
            return true;
        }

        private int handlingMessages;        

        //public async void DebuggingLoop() {
        //    while (true) {
        //        await Task.Delay(1000);
        //    }
        //}

        public Task Completion {
            get { return OutputMessageBlock.Completion; }
        }

        public IList<Message> Output {
            get;
            private set;
        }

        public void Post(IEnumerable<Message> data) {
            foreach (var item in data) {
                InputMessageBlock.Post(item);
            }
        }

        public void Complete() {
            InputMessageBlock.Complete();
        }

        private TransformBlock<Message, Message> InputMessageBlock {
            get;
        }

        private async Task<Message> InputMessage(Message message) {
            await Task.Delay(10);
            return message;
        }

        private TransformManyBlock<Message, Message> HandleMessageBlock {
            get;
        }

        private async Task<IEnumerable<Message>> HandleMessage(Message message) {
            Interlocked.Increment(ref handlingMessages);
            await Task.Delay(10);
            var result = Enumerable.Range(0, message.GenerateNewMessages + 1)
                                   .Select(x => new Message(x, 0, x % 2 == 0));
            Interlocked.Decrement(ref handlingMessages);
            return result;
        }        

        private ActionBlock<Message> OutputMessageBlock {
            get;
        }

        private void OutputMessage(Message message) {
            if (Output == null) Output = new List<Message>();
            Output.Add(message);
        }
    }
}
