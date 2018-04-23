using System;
using BDArmory.Core.Interface;

namespace BDArmory.Core.Services
{
    public abstract class NotificableService <T>: INotificableService<T> where T: EventArgs
    {
        public event EventHandler<T> OnActionExecuted;

        public void PublishEvent(T t)
        {
            OnActionExecuted?.Invoke(this, t);
        }
    }
}