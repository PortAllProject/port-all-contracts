using System;

namespace AElf.TokenSwap
{
    public class MessageHelper
    {
        public enum Message
        {
            Success,
            WrongIdCard,
            ParameterMissed,
            ParameterTypeNotMatch,
            Unsupport,
            AuthorizeFailed,
            FailToUploadFile
        }

        public static string GetCode(Message message)
        {
            switch (message)
            {
                case Message.Success:
                    return "0000";

                case Message.WrongIdCard:
                    return "0002";

                case Message.ParameterMissed:
                    return "0003";

                case Message.ParameterTypeNotMatch:
                    return "0004";

                case Message.Unsupport:
                    return "0005";

                case Message.AuthorizeFailed:
                    return "0006";
                
                case Message.FailToUploadFile:
                    return "0007";

                default:
                    return "0001";
            }
        }

        public static string GetMessage(Message message)
        {
            switch (message)
            {
                case Message.Success:
                    return "成功";

                case Message.WrongIdCard:
                    return "身份证号不是合法的身份证号码";

                case Message.ParameterMissed:
                    return "参数缺失";

                case Message.ParameterTypeNotMatch:
                    return "参数类型不匹配";

                case Message.Unsupport:
                    return "不支持该请求方式";

                case Message.AuthorizeFailed:
                    return "验签失败";
                
                case Message.FailToUploadFile:
                    return "上传文件失败";

                default:
                    return "系统异常（其他所有未列出的异常）";
            }
        }
    }
}