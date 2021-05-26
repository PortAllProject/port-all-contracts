using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Report;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using ReportGenerator;

namespace ReportGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // TestSignWithAelfKey();
            // TestSignWithEthereumPrivateKey();
            // await TestSetContractInteraction();
            // await TestGetContractInteraction();
            // await TestTransmit();
            
            // test single answer and its length is less than 32
            //TestSingleAnswerWithin32Byte();
            
            // test multiple answer and their length are less than 32
            // var report = TestMultipleObservationsWithin32Byte();
            // var privateKey1 = "7f6965ae260469425ae839f5abc85b504883022140d5f6fc9664a96d480c068d";
            // var privateKey2 = "996e00ecd273f49a96b1af85ee24b6724d8ba3d9957c5bdc5fc16fd1067d542a";
            // SignReport(privateKey1, report);
            // SignReport(privateKey2, report);
            
            // var ob1 = ByteStringHelper
            //     .FromHexString("0x7177656f6c657763776a00000000000000000000000000000000000000000000").Take(10).ToArray();
            // Console.WriteLine(Encoding.ASCII.GetString(ob1));
            // var ob2 = ByteStringHelper
            //     .FromHexString("0x6a756e61656c696f766561000000000000000000000000000000000000000000").Take(11).ToArray();
            // Console.WriteLine(Encoding.ASCII.GetString(ob2));
            // var ob3 = ByteStringHelper
            //     .FromHexString("0x31323332313433322e3132333132330000000000000000000000000000000000").Take(15).ToArray();
            // Console.WriteLine(Encoding.ASCII.GetString(ob3));
            
            //test multiple answer with long answer
            // var report = TestMultipleObservationsWithOut32Byte();
            // var privateKey1 = "7f6965ae260469425ae839f5abc85b504883022140d5f6fc9664a96d480c068d";
            // var privateKey2 = "996e00ecd273f49a96b1af85ee24b6724d8ba3d9957c5bdc5fc16fd1067d542a";
            // SignReport(privateKey1, report);
            // SignReport(privateKey2, report);
            
            var ob1 = ByteStringHelper
                .FromHexString("0x7177656f6c657763776a00000000000000000000000000000000000000000000").Take(10).ToArray();
            Console.WriteLine(Encoding.ASCII.GetString(ob1));
            var ob2 = ByteStringHelper
                .FromHexString("0x63343265646566633735383731653463653231343666636461363764303364646130356363323666646639336231376235356634326331656164666463333232").Take(64).ToArray();
            Console.WriteLine(Encoding.ASCII.GetString(ob2));
            var ob3 = ByteStringHelper
                .FromHexString("0x633432656465666337353837316534636532313436666364613637643033646461303563633236666466393362313762353566343263316561646664633332326334326564656663373538373165346365323134366663646136376430336464").Take(108).ToArray();
            Console.WriteLine(Encoding.ASCII.GetString(ob3));
            var ob4 = ByteStringHelper
                .FromHexString("0x6130356363323666646639336231376235356634326331656164666463333232633432656465666337353837316534636532313436666364613637643033646461303563633236666466393362313762353566343263316561646664633332326164736461646164640000000000000000000000000000000000000000000000").Take(117).ToArray();
            Console.WriteLine(Encoding.ASCII.GetString(ob4));
        }

        static string TransferEthereumHexToBytesArray(string str)
        {
            var bytesArray = new List<byte>();
            for (var i = 0; i < str.Length; i += 2)
            {
                var subChar = str.Substring(i, 2);
                var b = (byte)(int.Parse(subChar.Substring(0,1)) * 16 + int.Parse(subChar.Substring(1,1)));
                bytesArray.Add(b);
            }
            return Encoding.ASCII.GetString(bytesArray.ToArray());
        }

        static async Task TestTransmit()
        {
            var file = "./contractBuild/Test.json";
            var abi = AbiCodeService.ReadJson(file, "abi");
            var contractAddress = "0xce457474cacfba0e618c15925a59525b9e7e0cb4";
            var service = new AbiCodeService();
            var report = "0x000000000000f6f3ed664fd0e7be332f035ec351acf1000000000000000a0007000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f0a056173646173000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000e00000000000000000000000000000000000000000000000000000000000000000".HexToByteArray();
            // var rs = new []
            // {
            //     "0x366740b1d0afaed7dcabe6008068675a8e65a8cdaa4ed1b2f042ddcec9c242d7",
            //     "0x446dfa1ada5c498c5c689ae0a7c28d8e7f9632465f17574a7841f2c630538e80"
            // };
            var rs = new[]
            {
                "0x366740b1d0afaed7dcabe6008068675a8e65a8cdaa4ed1b2f042ddcec9c242d7".HexToByteArray(),
                "0x446dfa1ada5c498c5c689ae0a7c28d8e7f9632465f17574a7841f2c630538e80".HexToByteArray()
            };
               
            // var ss = new []
            // {
            //     "0x13afc3c576972824fe5d252c7276639710ff1ee45330948bf335c086241ea8a6",
            //     "0x30974bcb26f23d06f9af47798fd4bc234d03cc3a1467f90a06943c5b2dda1109"
            // };
            var ss = new []
            {
                "0x13afc3c576972824fe5d252c7276639710ff1ee45330948bf335c086241ea8a6".HexToByteArray(),
                "0x30974bcb26f23d06f9af47798fd4bc234d03cc3a1467f90a06943c5b2dda1109".HexToByteArray()
            };
            var rawVs = "0x13afc3c576972824fe5d252c7276639710ff1ee45330948bf335c086241ea8a6".HexToByteArray();
            await service.TransmitValue(contractAddress, abi, report,rs,ss,rawVs);
        }

        static async Task TestSetContractInteraction()
        {
            var abi = @"[{""inputs"": [],""name"": ""value"",""outputs"": [{""internalType"": ""uint256"",""name"": """",""type"": ""uint256""}],""stateMutability"": ""view"",""type"": ""function""},{""inputs"": [{""internalType"": ""uint256"",""name"": ""_value"",""type"": ""uint256""}],""name"": ""setValue"",""outputs"": [],""stateMutability"": ""nonpayable"",""type"": ""function""},{""inputs"": [],""name"": ""getValue"",""outputs"": [{""internalType"": ""uint256"",""name"": """",""type"": ""uint256""}],""stateMutability"": ""view"",""type"": ""function""}]";
            var contractAddress1 = "0xf64996f528c37ebc09c0f6f51f3c80e33780aee6";
            var contractAddress2 = "0xd03a0e125de8991cc8d59fddde0df478d467d2cc";
            var newValue = 1213;
            var service = new AbiCodeService();
            await service.SetValue(contractAddress1, abi, newValue);
            await service.SetValue(contractAddress2, abi, newValue);
        }
        
        static async Task TestGetContractInteraction()
        {
            var abi = @"[{""inputs"": [],""name"": ""value"",""outputs"": [{""internalType"": ""uint256"",""name"": """",""type"": ""uint256""}],""stateMutability"": ""view"",""type"": ""function""},{""inputs"": [{""internalType"": ""uint256"",""name"": ""_value"",""type"": ""uint256""}],""name"": ""setValue"",""outputs"": [],""stateMutability"": ""nonpayable"",""type"": ""function""},{""inputs"": [],""name"": ""getValue"",""outputs"": [{""internalType"": ""uint256"",""name"": """",""type"": ""uint256""}],""stateMutability"": ""view"",""type"": ""function""}]";
            var contractAddress1 = "0xf64996f528c37ebc09c0f6f51f3c80e33780aee6";
            var contractAddress2 = "0xd03a0e125de8991cc8d59fddde0df478d467d2cc";
            var service = new AbiCodeService();
            var value1 = await service.GetValue(contractAddress1, abi);
            var value2 = await service.GetValue(contractAddress2, abi);
            Console.WriteLine(value1);
            Console.WriteLine(value2);
        }

        static void TestSignWithAelfKey()
        {
            var signService = new SignService();
            var publicKey = "0x04436c5ea4d5bd45d5369e80096af55d81e93053233423a71536b360708c880402d935e4d9d888bff43be1b2b1d92e168a59c63e940c86e4d4108e456c7cbc9bf0";
            var address = signService.GenerateAddressOnEthereum(publicKey);
            Console.WriteLine(address);
            Console.WriteLine("0x824b3998700F7dcB7100D484c62a7b472B6894B6");
        }
        static void TestSignWithEthereumPrivateKey()
        {
            var privateKey = "7f6965ae260469425ae839f5abc85b504883022140d5f6fc9664a96d480c068d";
            var signService = new SignService();
            var privateKeyBytes = ByteStringHelper.FromHexString(privateKey).ToByteArray();
            var report =
                "0x000000000000f6f3ed664fd0e7be332f035ec351acf1000000000000000a02400001020000000000000000000000000000000000000000000000000000000000000102000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000e000010200000000000000000000000000000000000000000000000000000000000a0b0f0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000014000000000000000000000000000000000000000000000000000000000000000026334326564656663373538373165346365323134366663646136376430336464613035636332366664663933623137623535663432633165616466646333323200000000000000000000000000000000000000000000000000000000000000037177656f6c657763776a000000000000000000000000000000000000000000006a756e61656c696f76656100000000000000000000000000000000000000000031323332313433322e3132333132330000000000000000000000000000000000";
            var hashMsg = SignService.GetKeccak256(report);
            Console.WriteLine(hashMsg);
            var signature = signService.Sign(report, privateKeyBytes);
            Console.WriteLine("r is : " + signature.R);
            Console.WriteLine("s is : " + signature.S);
            Console.WriteLine("v is : " + signature.V);
        }
        
        static void SignReport(string privateKey, string report)
        {
            var signService = new SignService();
            var privateKeyBytes = ByteStringHelper.FromHexString(privateKey).ToByteArray();
            var hashMsg = SignService.GetKeccak256(report);
            Console.WriteLine(hashMsg);
            var signature = signService.Sign(report, privateKeyBytes);
            Console.WriteLine("r is : " + signature.R);
            Console.WriteLine("s is : " + signature.S);
            Console.WriteLine("v is : " + signature.V);
        }

        static void TestSingleAnswerWithin32Byte()
        {
            var rs = new ReportContract();
            var digestStr = "0xf6f3ed664fd0e7be332f035ec351acf1";
            var digestBytes = ByteStringHelper.FromHexString(digestStr).ToByteArray();
            //Console.WriteLine("digest length :" + digestBytes.Length);
            var digest = ByteString.CopyFrom(digestBytes);
            var data = "asdas";
            var report = new Report
            {
                RoundId = 10,
                AggregatedData = ByteString.CopyFrom(data.GetBytes()),
                Observations = new Observations(),
                Observers =
                {
                    new ObserverList(),
                    new ObserverList()
                }
            };
            var bytesInEthereum = rs.GenerateEthereumReport(digest, report);
            Console.WriteLine("0x" + bytesInEthereum);
        }
        
        static string TestMultipleObservationsWithin32Byte()
        {
            var rs = new ReportContract();
            var digestStr = "0x22d6f8928689ea183a3eb24df3919a94";
            var digestBytes = ByteStringHelper.FromHexString(digestStr).ToByteArray();
            //Console.WriteLine("digest length :" + digestBytes.Length);
            var digest = ByteString.CopyFrom(digestBytes);
            var data = "asdas";
            var merkleTreeRoot = HashHelper.ComputeFrom(data).Value;
            //Console.WriteLine($"merkle tree root is {merkleTreeRoot}  and its length is {merkleTreeRoot.Length}");
            var report = new Report
            {
                RoundId = 11, AggregatedData = merkleTreeRoot, Observations = new Observations
                {
                    Value =
                    {
                        new Observation
                        {
                            Key = "0",
                            Data = "qweolewcwj"
                        },
                        new Observation
                        {
                            Key = "1",
                            Data = "junaeliovea"
                        },
                        new Observation
                        {
                            Key = "2",
                            Data = "12321432.123123"
                        }
                    }
                },
                Observers =
                {
                    new ObserverList(),
                    new ObserverList()
                }
            };
            var bytesInEthereum = rs.GenerateEthereumReport(digest, report);
            var hexReport = "0x" + bytesInEthereum;
            Console.WriteLine(hexReport);
            return hexReport;
        }
        
        static string TestMultipleObservationsWithOut32Byte()
        {
            var rs = new ReportContract();
            var digestStr = "0x22d6f8928689ea183a3eb24df3919a94";
            var digestBytes = ByteStringHelper.FromHexString(digestStr).ToByteArray();
            //Console.WriteLine("digest length :" + digestBytes.Length);
            var digest = ByteString.CopyFrom(digestBytes);
            var data = "asdas";
            var merkleTreeRoot = HashHelper.ComputeFrom(data);
            var longAnswer = merkleTreeRoot.ToHex() + merkleTreeRoot.ToHex();
            Console.WriteLine("long answer : " + longAnswer);
            var longestAnswer = longAnswer + "adsdadadd";
            Console.WriteLine("longest answer : " + longestAnswer);
            //Console.WriteLine($"merkle tree root is {merkleTreeRoot}  and its length is {merkleTreeRoot.Length}");
            var report = new Report
            {
                RoundId = 12, AggregatedData = merkleTreeRoot.Value, Observations = new Observations
                {
                    Value =
                    {
                        new Observation
                        {
                            Key = "0",
                            Data = "qweolewcwj"
                        },
                        new Observation
                        {
                            Key = "1",
                            Data = merkleTreeRoot.ToHex()
                        },
                        new Observation
                        {
                            Key = "2",
                            Data = longAnswer
                        },
                        new Observation
                        {
                            Key = "3",
                            Data = longestAnswer
                        }
                    }
                },
                Observers =
                {
                    new ObserverList(),
                    new ObserverList()
                }
            };
            var bytesInEthereum = rs.GenerateEthereumReport(digest, report);
            var hexReport = "0x" + bytesInEthereum;
            Console.WriteLine(hexReport);
            return hexReport;
        }
    }
}