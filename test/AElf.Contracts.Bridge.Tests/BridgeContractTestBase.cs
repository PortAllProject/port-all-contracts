using System.Collections.Generic;
using System.Linq;
using AElf.Boilerplate.TestBase;
using AElf.Contracts.MerkleTreeGeneratorContract;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.Contracts.Parliament;
using AElf.Contracts.Regiment;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using MTRecorder;

namespace AElf.Contracts.Bridge.Tests
{
    public class BridgeContractTestBase : DAppContractTestBase<BridgeContractTestModule>
    {
        protected Address DefaultSenderAddress { get; set; }
        protected ECKeyPair DefaultKeypair => SampleAccount.Accounts.First().KeyPair;
        internal List<Account> Transmitters => SampleAccount.Accounts.Skip(1).Take(5).ToList();
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub { get; set; }
        internal BridgeContractContainer.BridgeContractStub BridgeContractStub { get; set; }
        internal OracleContractContainer.OracleContractStub OracleContractStub { get; set; }
        internal RegimentContractContainer.RegimentContractStub RegimentContractStub { get; set; }

        internal Address MerkleTreeRecorderContractAddress =>
            GetAddress(MerkleTreeRecorderSmartContractAddressNameProvider.StringName);

        internal Address MerkleTreeGeneratorContractAddress =>
            GetAddress(MerkleTreeGeneratorSmartContractAddressNameProvider.StringName);

        internal Address BridgeContractAddress =>
            GetAddress(BridgeSmartContractAddressNameProvider.StringName);

        internal Address OracleContractAddress =>
            GetAddress(OracleSmartContractAddressNameProvider.StringName);

        internal Address RegimentContractAddress =>
            GetAddress(RegimentSmartContractAddressNameProvider.StringName);

        public BridgeContractTestBase()
        {
            DefaultSenderAddress = SampleAccount.Accounts.First().Address;
            TokenContractStub = GetTokenContractStub(DefaultKeypair);
            ParliamentContractStub = GetParliamentContractStub(DefaultKeypair);
            BridgeContractStub = GetBridgeContractStub(DefaultKeypair);
            OracleContractStub = GetOracleContractStub(DefaultKeypair);
            RegimentContractStub = GetRegimentContractStub(DefaultKeypair);
            var merkleTreeGeneratorContractStub = GetMerkleTreeGeneratorContractStub(DefaultKeypair);
            var merkleTreeRecorderContractStub = GetMerkleTreeRecorderContractStub(DefaultKeypair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, senderKeyPair);
        }

        internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractStub(
            ECKeyPair senderKeyPair)
        {
            return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
                senderKeyPair);
        }

        internal BridgeContractContainer.BridgeContractStub GetBridgeContractStub(
            ECKeyPair senderKeyPair)
        {
            return GetTester<BridgeContractContainer.BridgeContractStub>(
                BridgeContractAddress,
                senderKeyPair);
        }

        internal MerkleTreeRecorderContractContainer.MerkleTreeRecorderContractStub GetMerkleTreeRecorderContractStub(
            ECKeyPair senderKeyPair)
        {
            return GetTester<MerkleTreeRecorderContractContainer.MerkleTreeRecorderContractStub>(
                MerkleTreeRecorderContractAddress,
                senderKeyPair);
        }

        internal MerkleTreeGeneratorContractContainer.MerkleTreeGeneratorContractStub
            GetMerkleTreeGeneratorContractStub(
                ECKeyPair senderKeyPair)
        {
            return GetTester<MerkleTreeGeneratorContractContainer.MerkleTreeGeneratorContractStub>(
                MerkleTreeGeneratorContractAddress,
                senderKeyPair);
        }

        internal OracleContractContainer.OracleContractStub
            GetOracleContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<OracleContractContainer.OracleContractStub>(
                OracleContractAddress,
                senderKeyPair);
        }

        internal RegimentContractContainer.RegimentContractStub
            GetRegimentContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<RegimentContractContainer.RegimentContractStub>(
                RegimentContractAddress,
                senderKeyPair);
        }
    }
}