using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using ReHome.gRPC;

namespace ReHome.WebServer.Services
{
    public class ReturnHomeServiceImpl : ReturnHomeService.ReturnHomeServiceBase
    {
        private SyncService _syncService;

        public ReturnHomeServiceImpl(SyncService syncService)
        {
            _syncService = syncService;
        }

        public override async Task WaitForCall(
            CallRequest request, 
            IServerStreamWriter<CallResponse> responseStream, 
            ServerCallContext context)
        {
            _syncService.OnEvent(async (time) => {
                if (!context.CancellationToken.IsCancellationRequested)
                {
                    var responceBody = new CallResponse()
                    {
                        EntrainingPoint = "海田市",
                        EntrainingTime = time.ToLongTimeString(),
                        DisembarkingTime = time.AddMinutes(46).ToLongTimeString()
                    };
                    await responseStream.WriteAsync(responceBody);
                }
            } );

            await Task.Yield();
            context.CancellationToken.WaitHandle.WaitOne();
            _syncService.OnEvent(null);
        }
    }
}
