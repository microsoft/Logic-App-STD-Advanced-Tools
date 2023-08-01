using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using static LogicAppAdvancedTool.Program;

namespace LogicAppAdvancedTool
{
    public class ExpectedException : Exception
    { 
        public ExpectedException(string message) : base(message) { }
    }

    public class UserInputException : ExpectedException
    {
        public UserInputException(string Message) : base(Message) { }
    }

    public class UserCanceledException : ExpectedException
    { 
        public UserCanceledException(string message): base(message) { }
    }
}
