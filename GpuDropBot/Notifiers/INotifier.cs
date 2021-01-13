using System.Collections.Generic;
using System.Threading.Tasks;

using GpuDropBot.Model;

namespace GpuDropBot.Notifiers
{
    public interface INotifier
    {
        Task Notify(IEnumerable<ProductDetails> products);
    }
}
