using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;
 
namespace ReportGenerator
{
    static public class FunctionOutputHelpers
    {
        // event MultipliedEvent(
        // address indexed sender,
        // int oldProduct,
        // int value,
        // int newProduct
        // );
 
        [FunctionOutput]
        public class MultipliedEventArgs
        {
            [Parameter("address", "sender", 1, true)]
            public string sender { get; set; }
 
            [Parameter("int", "oldProduct", 2, false)]
            public int oldProduct { get; set; }
 
            [Parameter("int", "value", 3, false)]
            public int value { get; set; }
 
            [Parameter("int", "newProduct", 4, false)]
            public int newProduct { get; set; }
 
        }
 
//event NewMessageEvent(
        // address indexed sender,
        // uint256 indexed ind,
        // string msg
        //);
 
        [FunctionOutput]
        public class NewMessageEventArgs
        {
            [Parameter("address", "sender", 1, true)]
            public string sender { get; set; }
 
            [Parameter("uint256", "ind", 2, true)]
            public int ind { get; set; }
 
            [Parameter("string", "msg", 3, false)]
            public string msg { get; set; }
        }
    }
}