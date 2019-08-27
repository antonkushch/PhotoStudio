using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.Exceptions
{
    public class SyncNeededException : Exception
    {
        public SyncNeededException() { }

        public SyncNeededException(string msg) : base(msg) { }
    }
}