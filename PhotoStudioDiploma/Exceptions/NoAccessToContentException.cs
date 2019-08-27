using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.Exceptions
{
    public class NoAccessToContentException : Exception
    {
        public NoAccessToContentException() { }

        public NoAccessToContentException(string msg) : base(msg) { }
    }
}