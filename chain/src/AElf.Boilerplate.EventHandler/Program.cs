using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Volo.Abp;

namespace AElf.Boilerplate.EventHandler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var application = AbpApplicationFactory.Create<EventHandlerAElfModule>(options =>
            {
                options.UseAutofac();
            });

            application.Initialize();

            Console.WriteLine("Start subscribing messages.");
            Console.ReadLine();

            application.Shutdown();
        }
    }
}