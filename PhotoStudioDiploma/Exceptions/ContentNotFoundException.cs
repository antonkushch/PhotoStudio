using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.Exceptions
{
    public class ContentNotFoundException : Exception
    {
        public ContentNotFoundException() { }

        public ContentNotFoundException(string msg) : base(msg) { }
    }
}