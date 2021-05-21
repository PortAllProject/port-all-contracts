using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.Ethereum
{
    public class HelloWorldService : ITransientDependency
    {
        private string _url;
        private string _privateKey;
        private string _contractAddress;
        private Web3 _web3;
        public HelloWorldService()
        {
            _contractAddress = string.Empty;
            _url = "https://kovan.infura.io/v3/undefined"; //api key
            _privateKey = "0xb5b1870957d373ef0eeffecc6e4812c0fd08f554b37b233526acc331bf1544f7";
            var account = new Account(_privateKey, 42);
            _web3 = new Web3(account, _url);
        }

        public void SayHello()
        {
            Console.WriteLine("Hello World!");
        }

        public async void HandleEvent()
        {
            var events = await GetEvent();
            foreach (var eventLog in events)
            {
                
            }
        }

        private async Task<List<EventLog<RoundRequestedEventDto>>> GetEvent()
        {
            var eventHandler = _web3.Eth.GetEvent<RoundRequestedEventDto>(_contractAddress);
            var filterForContractReceiverAddress = eventHandler.CreateFilterInput();
            var requestEventAddress  = await eventHandler.CreateFilterAsync(filterForContractReceiverAddress);
            return await eventHandler.GetFilterChanges(requestEventAddress);
        }
    }
}
