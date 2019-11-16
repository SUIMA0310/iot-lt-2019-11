using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReHome.WebServer.Services
{
    public class SyncService
    {
        private Action<DateTime> _action;

        public void Event(DateTime time)
        {
            _action?.Invoke(time);
        }

        public void OnEvent(Action<DateTime> action)
        {
            _action = action;
        }
    }
}
