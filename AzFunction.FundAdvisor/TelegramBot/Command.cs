using System;
using System.Threading.Tasks;

namespace AzFunction.FundAdvisor.TelegramBot
{
    internal abstract class Command
    {
        protected readonly IContext Context;

        protected Command(IContext context)
        {
            this.Context = context;
        }

        public virtual Task Execute()
        {
            throw new NotSupportedException();
        }
    }
}
