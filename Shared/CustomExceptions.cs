using System;

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
