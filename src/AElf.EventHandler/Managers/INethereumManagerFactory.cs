using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public interface INethereumManagerFactory
    {
        INethereumManager CreateManager(IEthereumContractNameProvider ethereumContractNameProvider);
    }

    public class NethereumManagerFactory : INethereumManagerFactory, ITransientDependency
    {
        private readonly ConfigOptions _configOptions;
        private readonly EthereumConfigOptions _ethereumOptions;
        private readonly ILogger<NethereumManagerFactory> _logger;

        public NethereumManagerFactory(ILogger<NethereumManagerFactory> logger,
            IOptionsSnapshot<EthereumConfigOptions> ethereumOptions,
            IOptionsSnapshot<ConfigOptions> configOptions)
        {
            _logger = logger;
            _configOptions = configOptions.Value;
            _ethereumOptions = ethereumOptions.Value;
        }

        public INethereumManager CreateManager(IEthereumContractNameProvider ethereumContractNameProvider)
        {
            return new NethereumManager(GetContractAddress(ethereumContractNameProvider.AddressConfigName),
                GetContractAbi(ethereumContractNameProvider.AbiFileName),
                _ethereumOptions.Address,
                _ethereumOptions.PrivateKey,
                _ethereumOptions.Url);
        }

        private string GetContractAddress(string addressConfigName)
        {
            var contractAddress = (string) _configOptions.GetType()
                .GetProperty(addressConfigName)?
                .GetValue(_configOptions);
            if (contractAddress == null)
            {
                contractAddress = (string) _ethereumOptions.GetType()
                    .GetProperty(addressConfigName)?
                    .GetValue(_ethereumOptions);
                if (contractAddress == null)
                {
                    _logger.LogError(
                        $"Failed to get value of {addressConfigName} from Options.");
                }
            }

            return contractAddress;
        }

        private string GetContractAbi(string abiFileName)
        {
            var abiFilePath = GetContractAbiFilePath(abiFileName);
            if (!File.Exists(abiFilePath))
            {
                _logger.LogError($"Cannot found file {abiFilePath}");
            }

            return JsonHelper.ReadJson(abiFilePath, "abi");
        }

        private string GetContractAbiFilePath(string abiFileName)
        {
            return $"./ContractBuild/{abiFileName}.json";
        }
    }
}