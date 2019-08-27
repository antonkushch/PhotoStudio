using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoStudioDiploma.Exceptions
{
    public class NoFoldersGrantedToClientException : Exception
    {
        public NoFoldersGrantedToClientException() { }

        public NoFoldersGrantedToClientException(string msg) : base(msg) { }
    }
}