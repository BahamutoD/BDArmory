using System;

namespace BDArmory.Core.Interface
{
    public interface INotificableService <T> where T: EventArgs
    {
        event EventHandler<T> OnActionExecuted;

        void PublishEvent(T t);
    }
}