using System;
using System.Net.Http;
using System.Threading.Tasks;
using ReHome.gRPC;
using Grpc.Net.Client;
using System.Threading;
using ras_pi_cs.Gpio;

namespace ReHome.Home
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            // The port number(5001) must match the port of the gRPC server.
            using var cancelSource = new CancellationTokenSource();

            var pinConfig_G = new PinConfigration(14, true);
            var pinConfig_Y = new PinConfigration(15, true);
            var pinConfig_R = new PinConfigration(18, true);
            GpioController gpioCtrl_G = null;
            GpioController gpioCtrl_Y = null;
            GpioController gpioCtrl_R = null;

            try
            {
                gpioCtrl_G = new GpioController(pinConfig_G) { Value = false };
                gpioCtrl_Y = new GpioController(pinConfig_Y) { Value = false };
                gpioCtrl_R = new GpioController(pinConfig_R) { Value = false };
            }
            catch
            {
                Console.Error.WriteLine("GPIO 初期化に失敗");
            }

            DateTime? receiveTime = null;

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            //using var channel = GrpcChannel.ForAddress("http://iot.penguins-lab.net:80");
            using var channel = GrpcChannel.ForAddress("http://localhost:5000");


            var client = new ReturnHomeService.ReturnHomeServiceClient(channel);
            using var streamResult = client.WaitForCall(new CallRequest() { DisembarkingPoint = "西高屋" });

            using var receiveTask = Task.Run(async () =>
            {
                await Task.Yield();
                while (await streamResult.ResponseStream.MoveNext(cancelSource.Token))
                {
                    var result = streamResult.ResponseStream.Current;
                    Console.WriteLine(result.EntrainingTime);
                    receiveTime = DateTime.Parse(result.DisembarkingTime);
                }

                Console.WriteLine("受信タスク完了");
            });
            using var pinControlTask = Task.Run(async () => 
            {
                await Task.Yield();
                while (!cancelSource.IsCancellationRequested)
                {
                    await Task.Delay(50);
                    if (!receiveTime.HasValue)
                    {
                        continue;
                    }

                    try
                    {
                        if (gpioCtrl_G.Value == false)
                        {
                            gpioCtrl_G.Value = true;
                        }

                        if (receiveTime.Value > DateTime.Now.AddSeconds(-30))
                        {
                            if (gpioCtrl_Y.Value == false)
                            {
                                gpioCtrl_Y.Value = true;
                            }
                        }

                        if (receiveTime.Value > DateTime.Now.AddSeconds(-5))
                        {
                            if (gpioCtrl_R.Value == false)
                            {
                                gpioCtrl_R.Value = true;
                            }
                        }
                    }catch
                    { }
                }
            });

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            cancelSource.Cancel();

            try
            {
                await receiveTask;
                await pinControlTask;
                (gpioCtrl_G as IDisposable)?.Dispose();
                (gpioCtrl_Y as IDisposable)?.Dispose();
                (gpioCtrl_R as IDisposable)?.Dispose();
                
            }
            catch (Grpc.Core.RpcException)
            {

            }
        }
    }
}
